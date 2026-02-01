namespace BankMore.TransferService.Domain.Exceptions;

public class TransferenciaException : Exception
{
    public string FailureType { get; }

    public TransferenciaException(string message, string failureType) : base(message)
    {
        FailureType = failureType;
    }

    public TransferenciaException(string message, string failureType, Exception innerException)
        : base(message, innerException)
    {
        FailureType = failureType;
    }
}
