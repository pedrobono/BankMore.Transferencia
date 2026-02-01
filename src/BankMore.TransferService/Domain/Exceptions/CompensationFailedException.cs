namespace BankMore.TransferService.Domain.Exceptions;

public class CompensationFailedException : TransferException
{
    public CompensationFailedException(string message)
        : base(message, "COMPENSATION_ERROR")
    {
    }

    public CompensationFailedException(string message, Exception innerException)
        : base(message, "COMPENSATION_ERROR", innerException)
    {
    }
}
