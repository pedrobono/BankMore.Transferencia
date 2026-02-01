using BankMore.TransferService.Application.DTOs;

namespace BankMore.TransferService.Application.Interfaces;

public interface IAccountServiceClient
{
    Task CreateMovementAsync(CreateMovementRequest request, string authorizationToken);
}
