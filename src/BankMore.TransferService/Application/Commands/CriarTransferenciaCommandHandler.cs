using BankMore.TransferService.Application.DTOs;
using BankMore.TransferService.Application.Interfaces;
using BankMore.TransferService.Domain.Entities;
using BankMore.TransferService.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankMore.TransferService.Application.Commands;

public class CriarTransferenciaCommandHandler : IRequestHandler<CriarTransferenciaCommand, Unit>
{
    private readonly ITransferenciaRepository _repository;
    private readonly IContaCorrenteServiceClient _accountService;
    private readonly ILogger<CriarTransferenciaCommandHandler> _logger;

    public CriarTransferenciaCommandHandler(
        ITransferenciaRepository repository,
        IContaCorrenteServiceClient accountService,
        ILogger<CriarTransferenciaCommandHandler> logger)
    {
        _repository = repository;
        _accountService = accountService;
        _logger = logger;
    }

    public async Task<Unit> Handle(CriarTransferenciaCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("\n========== INICIANDO TRANSFERÊNCIA ==========\n" +
            "RequestId: {RequestId}\n" +
            "Origem: {Origin}\n" +
            "Destino: {Destination}\n" +
            "Valor: R$ {Value:N2}",
            request.RequestId, request.IdContaOrigem, request.NumeroContaDestino, request.Valor);

        var existingTransfer = await _repository.GetByOriginAndRequestIdAsync(
            request.IdContaOrigem, request.RequestId);

        if (existingTransfer != null)
        {
            _logger.LogInformation("✅ Transferência já processada (idempotência). RequestId: {RequestId}",
                request.RequestId);
            return Unit.Value;
        }

        var transfer = new Transferencia(
            request.IdContaOrigem,
            Guid.Empty,
            request.Valor);

        try
        {
            _logger.LogInformation("💸 [ETAPA 1/2] Debitando R$ {Value:N2} da conta origem...", request.Valor);
            await DebitOriginAsync(request);
            _logger.LogInformation("✅ Débito realizado com sucesso");

            _logger.LogInformation("💰 [ETAPA 2/2] Creditando R$ {Value:N2} na conta destino {Destination}...", 
                request.Valor, request.NumeroContaDestino);
            await CreditDestinationAsync(request);
            _logger.LogInformation("✅ Crédito realizado com sucesso");

            await _repository.CreateAsync(transfer);
            _logger.LogInformation("✅ ========== TRANSFERÊNCIA CONCLUÍDA COM SUCESSO ==========\n");

            return Unit.Value;
        }
        catch (TransferenciaException ex) when (ex is not CompensacaoFalhaException)
        {
            _logger.LogWarning("⚠️ Falha na transferência: {Error} (Tipo: {FailureType})",
                ex.Message, ex.FailureType);

            if (!await WasDebitSuccessful(request))
            {
                _logger.LogWarning("❌ Falha no débito - transferência cancelada\n");
                throw;
            }

            _logger.LogWarning("🔄 Iniciando processo de COMPENSAÇÃO...");
            await CompensateAsync(request, ex);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ERRO INESPERADO na transferência\n" +
                "RequestId: {RequestId}\n" +
                "Mensagem: {Message}\n" +
                "StackTrace será exibido abaixo:",
                request.RequestId, ex.Message);
            throw new TransferenciaException("Erro interno ao processar transferência", "INTERNAL_ERROR", ex);
        }
    }

    private async Task DebitOriginAsync(CriarTransferenciaCommand request)
    {
        var debitRequest = new CriarMovimentoRequest(
            request.RequestId,
            null,
            request.Valor,
            "D");

        await _accountService.CreateMovementAsync(debitRequest, request.TokenAutorizacao);
    }

    private async Task CreditDestinationAsync(CriarTransferenciaCommand request)
    {
        var creditRequest = new CriarMovimentoRequest(
            request.RequestId,
            request.NumeroContaDestino,
            request.Valor,
            "C");

        await _accountService.CreateMovementAsync(creditRequest, request.TokenAutorizacao);
    }

    private async Task CompensateAsync(CriarTransferenciaCommand request, TransferenciaException originalException)
    {
        const int maxRetries = 3;
        var delays = new[] { 1000, 2000, 4000 };

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("🔄 Tentativa {Attempt}/{MaxRetries} de compensação - Estornando R$ {Value:N2}...",
                    attempt, maxRetries, request.Valor);

                var compensationRequest = new CriarMovimentoRequest(
                    $"{request.RequestId}-COMP",
                    null,
                    request.Valor,
                    "C");

                await _accountService.CreateMovementAsync(compensationRequest, request.TokenAutorizacao);

                _logger.LogInformation("✅ Compensação realizada com sucesso!\n");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Falha na tentativa {Attempt}: {Error}",
                    attempt, ex.Message);

                if (attempt < maxRetries)
                {
                    _logger.LogInformation("⏳ Aguardando {Delay}ms antes da próxima tentativa...", delays[attempt - 1]);
                    await Task.Delay(delays[attempt - 1]);
                }
                else
                {
                    _logger.LogCritical(ex,
                        "\n" +
                        "❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌\n" +
                        "⚠️  ALERTA CRÍTICO - COMPENSAÇÃO FALHOU!\n" +
                        "RequestId: {RequestId}\n" +
                        "Valor: R$ {Value:N2}\n" +
                        "Tentativas: {MaxRetries}\n" +
                        "👨💻 INTERVENÇÃO MANUAL NECESSÁRIA!\n" +
                        "StackTrace será exibido abaixo:\n" +
                        "❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌❌",
                        request.RequestId, request.Valor, maxRetries);

                    throw new CompensacaoFalhaException(
                        $"Falha crítica na compensação após {maxRetries} tentativas. Contate o suporte.", ex);
                }
            }
        }
    }

    private async Task<bool> WasDebitSuccessful(CriarTransferenciaCommand request)
    {
        return await Task.FromResult(true);
    }
}
