namespace BankMore.TransferService.Application.DTOs;

public record CriarMovimentoRequest(
    string RequestId,
    string? NumeroConta,
    decimal Valor,
    string Tipo
);

public record ErroResponse(
    string Message,
    string FailureType
);
