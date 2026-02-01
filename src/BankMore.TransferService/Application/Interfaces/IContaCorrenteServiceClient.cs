using BankMore.TransferService.Application.DTOs;

namespace BankMore.TransferService.Application.Interfaces;

public interface IContaCorrenteServiceClient
{
    Task CreateMovementAsync(CriarMovimentoRequest request, string authorizationToken);
}
