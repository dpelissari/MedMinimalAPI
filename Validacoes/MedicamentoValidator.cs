using FluentValidation;

public class MedicamentoValidator : AbstractValidator<Medicamento>
{
    public MedicamentoValidator()
    {
        RuleFor(x => x.FornecidoPeloSUS)
            .NotEmpty().WithMessage("o indicador de fornecimento do medicamento pelo SUS é obrigatório.");

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome do medicamento é obrigatório.");

        RuleFor(x => x.FormaFarmaceutica)
            .IsInEnum().WithMessage("Forma farmacêutica inválida.");

        RuleFor(x => x.ClasseTerapeutica)
            .IsInEnum().WithMessage("Classe terapêutica inválida.");

        RuleFor(x => x.FabricanteId)
            .NotEmpty().WithMessage("O ID do fabricante é obrigatório.");
    }
}