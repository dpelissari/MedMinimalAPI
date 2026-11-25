using FluentValidation;
using PiraRemumeApi.Dtos;
using PiraRemumeApi.Servicos;

namespace PiraRemumeApi.Endpoints;

public static class SincronismoEndpoints
{
    public static void MapSincronismoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/sincronismo").RequireAuthorization("ApenasAdmin");

        group.MapGet("/", async (SincronismoFonteService sincronismo, CancellationToken ct) =>
        {
            var config = await sincronismo.ObterAsync(ct);
            return Results.Ok(SincronismoFonteService.Mapear(config));
        });

        group.MapPut("/", async (
            AtualizarSincronismoRequest pedido,
            SincronismoFonteService sincronismo,
            IValidator<AtualizarSincronismoRequest> validator,
            CancellationToken ct) =>
        {
            var validacao = await validator.ValidateAsync(pedido, ct);
            if (!validacao.IsValid)
                return Results.ValidationProblem(validacao.ToDictionary());

            var config = await sincronismo.AtualizarAsync(pedido, ct);
            return Results.Ok(SincronismoFonteService.Mapear(config));
        });

        group.MapPost("/verificar", async (SincronismoFonteService sincronismo, CancellationToken ct) =>
        {
            var config = await sincronismo.VerificarAsync(forcarCatalogo: true, ct);
            return Results.Ok(SincronismoFonteService.Mapear(config));
        });
    }
}
