using BankMore.TransferService.Domain.ValueObjects;

namespace BankMore.TransferService.Domain.Entities;

public class Transfer
{
    public Guid Id { get; private set; }
    public string RequestId { get; private set; }
    public Guid OriginAccountId { get; private set; }
    public Guid DestinationAccountId { get; private set; }
    public decimal Value { get; private set; }
    public TransferStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorType { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Transfer() { }

    public Transfer(
        string requestId,
        Guid originAccountId,
        Guid destinationAccountId,
        decimal value)
    {
        Id = Guid.NewGuid();
        RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
        OriginAccountId = originAccountId;
        DestinationAccountId = destinationAccountId;
        Value = value;
        Status = TransferStatus.Success;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage, string errorType)
    {
        Status = TransferStatus.Failed;
        ErrorMessage = errorMessage;
        ErrorType = errorType;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsCompensated(string errorMessage, string errorType)
    {
        Status = TransferStatus.Compensated;
        ErrorMessage = errorMessage;
        ErrorType = errorType;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsCompensationFailed(string errorMessage)
    {
        Status = TransferStatus.CompensationFailed;
        ErrorMessage = errorMessage;
        ErrorType = "COMPENSATION_ERROR";
        UpdatedAt = DateTime.UtcNow;
    }
}
