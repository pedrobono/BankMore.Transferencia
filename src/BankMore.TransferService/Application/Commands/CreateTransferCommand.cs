using MediatR;

namespace BankMore.TransferService.Application.Commands;

public record CreateTransferCommand(
    string RequestId,
    string DestinationAccountNumber,
    decimal Value,
    Guid OriginAccountId,
    string AuthorizationToken
) : IRequest<Unit>;
