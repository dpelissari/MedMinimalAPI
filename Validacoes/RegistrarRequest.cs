using FluentValidation;
using MedMinimalApi.Dtos;

public class RegistrarRequestValidator : AbstractValidator<RegistrarRequest>
{
    public RegistrarRequestValidator()
    {
        RuleFor(x => x.NomeUsuario)
            .NotEmpty().WithMessage("O nome do usuário é obrigatório.");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("Senha é obrigatório.");

        RuleFor(x => x.Perfil)
            .NotEmpty().WithMessage("Perfil é obrigatório.");
    }
}
