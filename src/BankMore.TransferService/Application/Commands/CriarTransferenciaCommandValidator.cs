using FluentValidation;

namespace BankMore.TransferService.Application.Commands;

public class CriarTransferenciaCommandValidator : AbstractValidator<CriarTransferenciaCommand>
{
    public CriarTransferenciaCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithMessage("RequestId é obrigatório");

        RuleFor(x => x.NumeroContaDestino)
            .NotEmpty()
            .WithMessage("Número da conta de destino é obrigatório");

        RuleFor(x => x.Valor)
            .GreaterThan(0)
            .WithMessage("O valor deve ser maior que zero");

        RuleFor(x => x.IdContaOrigem)
            .NotEmpty()
            .WithMessage("Conta de origem é obrigatória");

        RuleFor(x => x.TokenAutorizacao)
            .NotEmpty()
            .WithMessage("Token de autorização é obrigatório");
    }
}
