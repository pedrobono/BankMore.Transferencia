namespace BankMore.TransferService.Domain.ValueObjects;

public enum TransferStatus
{
    Success,
    Failed,
    Compensated,
    CompensationFailed
}
