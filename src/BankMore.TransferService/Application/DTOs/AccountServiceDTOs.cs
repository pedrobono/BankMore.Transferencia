namespace BankMore.TransferService.Application.DTOs;

public record CreateMovementRequest(
    string RequestId,
    string? AccountNumber,
    decimal Value,
    string Type
);

public record ErrorResponse(
    string Message,
    string FailureType
);
