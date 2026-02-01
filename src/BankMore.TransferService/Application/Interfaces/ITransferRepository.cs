using BankMore.TransferService.Domain.Entities;

namespace BankMore.TransferService.Application.Interfaces;

public interface ITransferRepository
{
    Task<Transfer?> GetByOriginAndRequestIdAsync(Guid originAccountId, string requestId);
    Task<Guid> CreateAsync(Transfer transfer);
}
