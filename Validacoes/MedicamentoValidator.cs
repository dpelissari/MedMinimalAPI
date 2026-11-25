using FluentValidation;

public class MedicamentoValidator : AbstractValidator<Medicamento>
{
    public MedicamentoValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome do medicamento é obrigatório.");
    }
}
