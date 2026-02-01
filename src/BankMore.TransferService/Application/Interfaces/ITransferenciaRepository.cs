using BankMore.TransferService.Domain.Entities;

namespace BankMore.TransferService.Application.Interfaces;

public interface ITransferenciaRepository
{
    Task<Transferencia?> GetByOriginAndRequestIdAsync(Guid originAccountId, string requestId);
    Task<Guid> CreateAsync(Transferencia transfer);
    Task SaveIdempotenciaAsync(Guid originAccountId, string requestId, string requisicao, string resultado);
}
