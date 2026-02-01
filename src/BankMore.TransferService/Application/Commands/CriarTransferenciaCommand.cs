using MediatR;

namespace BankMore.TransferService.Application.Commands;

public record CriarTransferenciaCommand(
    string RequestId,
    string NumeroContaDestino,
    decimal Valor,
    Guid IdContaOrigem,
    string TokenAutorizacao
) : IRequest<Unit>;
