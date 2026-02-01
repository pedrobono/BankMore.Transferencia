using FluentValidation;

namespace BankMore.TransferService.Application.Commands;

public class CreateTransferCommandValidator : AbstractValidator<CreateTransferCommand>
{
    public CreateTransferCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithMessage("RequestId é obrigatório");

        RuleFor(x => x.DestinationAccountNumber)
            .NotEmpty()
            .WithMessage("Número da conta de destino é obrigatório");

        RuleFor(x => x.Value)
            .GreaterThan(0)
            .WithMessage("O valor deve ser maior que zero");

        RuleFor(x => x.OriginAccountId)
            .NotEmpty()
            .WithMessage("Conta de origem é obrigatória");

        RuleFor(x => x.AuthorizationToken)
            .NotEmpty()
            .WithMessage("Token de autorização é obrigatório");
    }
}
