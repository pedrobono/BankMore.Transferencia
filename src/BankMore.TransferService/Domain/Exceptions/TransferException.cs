namespace BankMore.TransferService.Domain.Exceptions;

public class TransferException : Exception
{
    public string FailureType { get; }

    public TransferException(string message, string failureType) : base(message)
    {
        FailureType = failureType;
    }

    public TransferException(string message, string failureType, Exception innerException)
        : base(message, innerException)
    {
        FailureType = failureType;
    }
}
