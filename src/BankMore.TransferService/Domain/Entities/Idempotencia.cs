namespace BankMore.TransferService.Domain.Entities;

public class Idempotencia
{
    public Guid ChaveIdempotencia { get; private set; }
    public string Requisicao { get; private set; }
    public string? Resultado { get; private set; }

    private Idempotencia() { }

    public Idempotencia(Guid chaveIdempotencia, string requisicao)
    {
        ChaveIdempotencia = chaveIdempotencia;
        Requisicao = requisicao;
    }

    public void DefinirResultado(string resultado)
    {
        Resultado = resultado;
    }
}
