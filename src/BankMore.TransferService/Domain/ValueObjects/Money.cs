namespace BankMore.TransferService.Domain.ValueObjects;

public record Money
{
    public decimal Value { get; }

    public Money(decimal value)
    {
        if (value <= 0)
            throw new ArgumentException("O valor deve ser maior que zero", nameof(value));

        Value = value;
    }

    public static implicit operator decimal(Money money) => money.Value;
    public static implicit operator Money(decimal value) => new(value);
}
