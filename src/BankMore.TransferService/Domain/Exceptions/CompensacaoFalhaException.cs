namespace BankMore.TransferService.Domain.Exceptions;

public class CompensacaoFalhaException : TransferenciaException
{
    public CompensacaoFalhaException(string message)
        : base(message, "COMPENSATION_ERROR")
    {
    }

    public CompensacaoFalhaException(string message, Exception innerException)
        : base(message, "COMPENSATION_ERROR", innerException)
    {
    }
}
