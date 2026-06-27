using FluentValidation;

public class FabricanteValidator : AbstractValidator<Fabricante>
{
    public FabricanteValidator()
    {
        RuleFor(x => x.NomeFantasia)
            .NotEmpty().WithMessage("O nome fantasia é obrigatório.");

        RuleFor(x => x.Cnpj)
            .NotEmpty().WithMessage("CNPJ é obrigatório.");
    }
}
