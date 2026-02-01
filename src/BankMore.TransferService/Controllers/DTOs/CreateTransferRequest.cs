namespace BankMore.TransferService.Controllers.DTOs;

public record CreateTransferRequest(
    string RequestId,
    string DestinationAccountNumber,
    decimal Value
);
