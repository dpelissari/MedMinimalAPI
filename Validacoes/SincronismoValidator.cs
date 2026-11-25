using FluentValidation;
using PiraRemumeApi.Dtos;
using PiraRemumeApi.Servicos;

namespace PiraRemumeApi.Validacoes;

public class AtualizarSincronismoRequestValidator : AbstractValidator<AtualizarSincronismoRequest>
{
    public AtualizarSincronismoRequestValidator()
    {
        RuleFor(x => x.UrlFonte)
            .NotEmpty().WithMessage("Informe a página ou o PDF da REMUME.")
            .Must(SincronismoFonteService.UrlPermitida)
            .WithMessage("Use um endereço https da Prefeitura de Piracicaba para a REMUME.");

        RuleFor(x => x.UrlFarmacias)
            .NotEmpty().WithMessage("Informe a página das farmácias municipais.")
            .Must(SincronismoFonteService.UrlPermitida)
            .WithMessage("Use um endereço https da Prefeitura de Piracicaba para as farmácias.");

        RuleFor(x => x.IntervaloDias)
            .Must(dias => dias is 1 or 7 or 30)
            .WithMessage("O intervalo precisa ser de 1, 7 ou 30 dias.");
    }
}
