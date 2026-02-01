using BankMore.TransferService.Application.Commands;
using BankMore.TransferService.Application.DTOs;
using BankMore.TransferService.Application.Interfaces;
using BankMore.TransferService.Domain.Entities;
using BankMore.TransferService.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BankMore.TransferService.Tests.Application.Commands;

public class CreateTransferCommandHandlerTests
{
    private readonly Mock<ITransferenciaRepository> _repositoryMock;
    private readonly Mock<IContaCorrenteServiceClient> _accountServiceMock;
    private readonly Mock<ILogger<CriarTransferenciaCommandHandler>> _loggerMock;
    private readonly CriarTransferenciaCommandHandler _handler;

    public CreateTransferCommandHandlerTests()
    {
        _repositoryMock = new Mock<ITransferenciaRepository>();
        _accountServiceMock = new Mock<IContaCorrenteServiceClient>();
        _loggerMock = new Mock<ILogger<CriarTransferenciaCommandHandler>>();
        _handler = new CriarTransferenciaCommandHandler(_repositoryMock.Object, _accountServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_TransferenciaBemSucedida_DeveRetornarUnit()
    {
        // Arrange
        var command = new CriarTransferenciaCommand("request-123", "85381-6", 100.50m, Guid.NewGuid(), "Bearer token");

        _repositoryMock.Setup(x => x.GetByOriginAndRequestIdAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync((Transferencia?)null);
        _accountServiceMock.Setup(x => x.CreateMovementAsync(It.IsAny<CriarMovimentoRequest>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.CreateAsync(It.IsAny<Transferencia>())).ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _accountServiceMock.Verify(x => x.CreateMovementAsync(It.IsAny<CriarMovimentoRequest>(), It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_RequestIdDuplicado_DeveRetornarSemReprocessar()
    {
        // Arrange
        var command = new CriarTransferenciaCommand("request-123", "85381-6", 100.50m, Guid.NewGuid(), "Bearer token");
        var existingTransfer = new Transferencia(command.IdContaOrigem, Guid.NewGuid(), 100.50m);

        _repositoryMock.Setup(x => x.GetByOriginAndRequestIdAsync(command.IdContaOrigem, command.RequestId))
            .ReturnsAsync(existingTransfer);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _accountServiceMock.Verify(x => x.CreateMovementAsync(It.IsAny<CriarMovimentoRequest>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FalhaNoDebito_DeveLancarTransferException()
    {
        // Arrange
        var command = new CriarTransferenciaCommand("request-123", "85381-6", 100.50m, Guid.NewGuid(), "Bearer token");

        _repositoryMock.Setup(x => x.GetByOriginAndRequestIdAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync((Transferencia?)null);
        _accountServiceMock.Setup(x => x.CreateMovementAsync(It.Is<CriarMovimentoRequest>(r => r.Tipo == "D"), It.IsAny<string>()))
            .ThrowsAsync(new TransferenciaException("Saldo insuficiente", "INSUFFICIENT_BALANCE"));

        // Act & Assert
        await Assert.ThrowsAsync<TransferenciaException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
