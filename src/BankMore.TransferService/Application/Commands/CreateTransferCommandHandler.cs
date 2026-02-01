using BankMore.TransferService.Application.DTOs;
using BankMore.TransferService.Application.Interfaces;
using BankMore.TransferService.Domain.Entities;
using BankMore.TransferService.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankMore.TransferService.Application.Commands;

public class CreateTransferCommandHandler : IRequestHandler<CreateTransferCommand, Unit>
{
    private readonly ITransferRepository _repository;
    private readonly IAccountServiceClient _accountService;
    private readonly ILogger<CreateTransferCommandHandler> _logger;

    public CreateTransferCommandHandler(
        ITransferRepository repository,
        IAccountServiceClient accountService,
        ILogger<CreateTransferCommandHandler> logger)
    {
        _repository = repository;
        _accountService = accountService;
        _logger = logger;
    }

    public async Task<Unit> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando transferência. RequestId: {RequestId}, Origin: {Origin}, Destination: {Destination}, Value: {Value}",
            request.RequestId, request.OriginAccountId, request.DestinationAccountNumber, request.Value);

        // Verificar idempotência
        var existingTransfer = await _repository.GetByOriginAndRequestIdAsync(
            request.OriginAccountId, request.RequestId);

        if (existingTransfer != null)
        {
            _logger.LogInformation("Transferência já processada. RequestId: {RequestId}, Status: {Status}",
                request.RequestId, existingTransfer.Status);

            return existingTransfer.Status switch
            {
                Domain.ValueObjects.TransferStatus.Success => Unit.Value,
                Domain.ValueObjects.TransferStatus.Failed => throw new TransferException(
                    existingTransfer.ErrorMessage ?? "Transferência falhou anteriormente",
                    existingTransfer.ErrorType ?? "TRANSFER_FAILED"),
                Domain.ValueObjects.TransferStatus.Compensated => throw new TransferException(
                    existingTransfer.ErrorMessage ?? "Transferência foi compensada",
                    existingTransfer.ErrorType ?? "TRANSFER_COMPENSATED"),
                Domain.ValueObjects.TransferStatus.CompensationFailed => throw new CompensationFailedException(
                    existingTransfer.ErrorMessage ?? "Falha crítica na compensação"),
                _ => Unit.Value
            };
        }

        // Criar entidade de transferência
        var transfer = new Transfer(
            request.RequestId,
            request.OriginAccountId,
            Guid.Empty, // Será atualizado após resolver o número da conta
            request.Value);

        try
        {
            // Etapa 1: Débito na origem
            _logger.LogInformation("Etapa 1: Debitando conta de origem. RequestId: {RequestId}", request.RequestId);
            await DebitOriginAsync(request);

            // Etapa 2: Crédito no destino
            _logger.LogInformation("Etapa 2: Creditando conta de destino. RequestId: {RequestId}", request.RequestId);
            await CreditDestinationAsync(request);

            // Sucesso
            _logger.LogInformation("Transferência concluída com sucesso. RequestId: {RequestId}", request.RequestId);
            await _repository.CreateAsync(transfer);

            return Unit.Value;
        }
        catch (TransferException ex) when (ex is not CompensationFailedException)
        {
            _logger.LogWarning("Falha na transferência. RequestId: {RequestId}, Error: {Error}",
                request.RequestId, ex.Message);

            // Se falhou no débito, apenas marca como falha
            if (!await WasDebitSuccessful(request))
            {
                transfer.MarkAsFailed(ex.Message, ex.FailureType);
                await _repository.CreateAsync(transfer);
                throw;
            }

            // Se falhou no crédito, tentar compensação
            _logger.LogWarning("Iniciando compensação. RequestId: {RequestId}", request.RequestId);
            await CompensateAsync(request, ex);

            transfer.MarkAsCompensated(ex.Message, ex.FailureType);
            await _repository.CreateAsync(transfer);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado na transferência. RequestId: {RequestId}", request.RequestId);
            transfer.MarkAsFailed(ex.Message, "INTERNAL_ERROR");
            await _repository.CreateAsync(transfer);
            throw new TransferException("Erro interno ao processar transferência", "INTERNAL_ERROR", ex);
        }
    }

    private async Task DebitOriginAsync(CreateTransferCommand request)
    {
        var debitRequest = new CreateMovementRequest(
            request.RequestId,
            null, // Sem accountNumber = usa o do token
            request.Value,
            "D");

        await _accountService.CreateMovementAsync(debitRequest, request.AuthorizationToken);
    }

    private async Task CreditDestinationAsync(CreateTransferCommand request)
    {
        var creditRequest = new CreateMovementRequest(
            request.RequestId,
            request.DestinationAccountNumber,
            request.Value,
            "C");

        await _accountService.CreateMovementAsync(creditRequest, request.AuthorizationToken);
    }

    private async Task CompensateAsync(CreateTransferCommand request, TransferException originalException)
    {
        const int maxRetries = 3;
        var delays = new[] { 1000, 2000, 4000 }; // Backoff exponencial

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Tentativa de compensação {Attempt}/{MaxRetries}. RequestId: {RequestId}",
                    attempt, maxRetries, request.RequestId);

                var compensationRequest = new CreateMovementRequest(
                    $"{request.RequestId}-COMP",
                    null, // Creditar na origem
                    request.Value,
                    "C");

                await _accountService.CreateMovementAsync(compensationRequest, request.AuthorizationToken);

                _logger.LogInformation("Compensação bem-sucedida. RequestId: {RequestId}", request.RequestId);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha na tentativa {Attempt} de compensação. RequestId: {RequestId}",
                    attempt, request.RequestId);

                if (attempt < maxRetries)
                {
                    await Task.Delay(delays[attempt - 1]);
                }
                else
                {
                    _logger.LogCritical("COMPENSAÇÃO FALHOU após {MaxRetries} tentativas! RequestId: {RequestId}. " +
                        "INTERVENÇÃO MANUAL NECESSÁRIA!", maxRetries, request.RequestId);

                    throw new CompensationFailedException(
                        $"Falha crítica na compensação após {maxRetries} tentativas. Contate o suporte.", ex);
                }
            }
        }
    }

    private async Task<bool> WasDebitSuccessful(CreateTransferCommand request)
    {
        // Simplificação: assumimos que se chegou aqui, o débito foi feito
        // Em produção, poderia consultar o Account Service
        return await Task.FromResult(true);
    }
}
