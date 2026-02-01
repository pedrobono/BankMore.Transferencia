namespace BankMore.TransferService.Controllers.DTOs;

public record CriarTransferenciaRequest(
    string RequestId,
    string NumeroContaDestino,
    decimal Valor
);
