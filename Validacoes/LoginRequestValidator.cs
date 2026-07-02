using FluentValidation;
using MedMinimalApi.Dtos;

public class LoginRequesValidator : AbstractValidator<LoginRequest>
{
    public LoginRequesValidator()
    {
        RuleFor(x => x.NomeUsuario)
            .NotEmpty().WithMessage("O nome do usuário é obrigatório.");
        
        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("Senha é obrigatório.");
    }
}
