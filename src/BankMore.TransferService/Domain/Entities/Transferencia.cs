namespace BankMore.TransferService.Domain.Entities;

public class Transferencia
{
    public Guid IdTransferencia { get; private set; }
    public Guid IdContaCorrenteOrigem { get; private set; }
    public Guid IdContaCorrenteDestino { get; private set; }
    public string DataMovimento { get; private set; }
    public decimal Valor { get; private set; }

    private Transferencia() { }

    public Transferencia(
        Guid idContaCorrenteOrigem,
        Guid idContaCorrenteDestino,
        decimal valor)
    {
        IdTransferencia = Guid.NewGuid();
        IdContaCorrenteOrigem = idContaCorrenteOrigem;
        IdContaCorrenteDestino = idContaCorrenteDestino;
        Valor = valor;
        DataMovimento = DateTime.Now.ToString("dd/MM/yyyy");
    }
}
