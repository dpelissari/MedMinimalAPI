using FluentValidation;

public class MedicamentoValidator : AbstractValidator<Medicamento>
{
    public MedicamentoValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome do medicamento é obrigatório.");

        RuleFor(x => x.FormaFarmaceutica)
            .IsInEnum().WithMessage("Forma farmacêutica inválida.");

        RuleFor(x => x.QtdeDisponivel)
            .GreaterThanOrEqualTo(0).WithMessage("A quantidade não pode ser negativa.");

        RuleFor(x => x.FabricanteId)
            .NotEmpty().WithMessage("O ID do fabricante é obrigatório.");
    }
}
