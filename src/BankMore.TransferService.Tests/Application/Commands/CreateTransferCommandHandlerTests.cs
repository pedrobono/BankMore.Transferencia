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
    private readonly Mock<ITransferRepository> _repositoryMock;
    private readonly Mock<IAccountServiceClient> _accountServiceMock;
    private readonly Mock<ILogger<CreateTransferCommandHandler>> _loggerMock;
    private readonly CreateTransferCommandHandler _handler;

    public CreateTransferCommandHandlerTests()
    {
        _repositoryMock = new Mock<ITransferRepository>();
        _accountServiceMock = new Mock<IAccountServiceClient>();
        _loggerMock = new Mock<ILogger<CreateTransferCommandHandler>>();
        _handler = new CreateTransferCommandHandler(_repositoryMock.Object, _accountServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_TransferenciaBemSucedida_DeveRetornarUnit()
    {
        // Arrange
        var command = new CreateTransferCommand("request-123", "85381-6", 100.50m, Guid.NewGuid(), "Bearer token");

        _repositoryMock.Setup(x => x.GetByOriginAndRequestIdAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync((Transfer?)null);
        _accountServiceMock.Setup(x => x.CreateMovementAsync(It.IsAny<CreateMovementRequest>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.CreateAsync(It.IsAny<Transfer>())).ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _accountServiceMock.Verify(x => x.CreateMovementAsync(It.IsAny<CreateMovementRequest>(), It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_RequestIdDuplicado_DeveRetornarSemReprocessar()
    {
        // Arrange
        var command = new CreateTransferCommand("request-123", "85381-6", 100.50m, Guid.NewGuid(), "Bearer token");
        var existingTransfer = new Transfer("request-123", command.OriginAccountId, Guid.NewGuid(), 100.50m);

        _repositoryMock.Setup(x => x.GetByOriginAndRequestIdAsync(command.OriginAccountId, command.RequestId))
            .ReturnsAsync(existingTransfer);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _accountServiceMock.Verify(x => x.CreateMovementAsync(It.IsAny<CreateMovementRequest>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FalhaNoDebito_DeveLancarTransferException()
    {
        // Arrange
        var command = new CreateTransferCommand("request-123", "85381-6", 100.50m, Guid.NewGuid(), "Bearer token");

        _repositoryMock.Setup(x => x.GetByOriginAndRequestIdAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync((Transfer?)null);
        _accountServiceMock.Setup(x => x.CreateMovementAsync(It.Is<CreateMovementRequest>(r => r.Type == "D"), It.IsAny<string>()))
            .ThrowsAsync(new TransferException("Saldo insuficiente", "INSUFFICIENT_BALANCE"));

        // Act & Assert
        await Assert.ThrowsAsync<TransferException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
