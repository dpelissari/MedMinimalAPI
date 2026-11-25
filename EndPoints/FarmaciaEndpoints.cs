using PiraRemumeApi.Dtos;
using PiraRemumeApi.Extensions;
using Microsoft.EntityFrameworkCore;

namespace PiraRemumeApi.Endpoints;

public static class FarmaciaEndpoints
{
    public static readonly FarmaciaFonte FonteFarmacias = new(
        "Farmácias municipais de Piracicaba",
        "2026-07-30",
        "https://piracicaba.sp.gov.br/servicos/farmacias-municipais/");

    public static void MapFarmaciaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/farmacias");

        group.MapGet("/", async (string? termo, string? tipo, MedicamentoDb db) =>
        {
            IQueryable<FarmaciaMunicipal> query = db.FarmaciasMunicipais.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(tipo)
                && Enum.TryParse<TipoFarmacia>(tipo.Trim(), ignoreCase: true, out var tipoFiltro))
            {
                query = query.Where(f => f.Tipo == tipoFiltro);
            }

            if (!string.IsNullOrWhiteSpace(termo))
            {
                var termoLimpo = termo.Trim();
                query = query.Where(f =>
                    EF.Functions.ILike(f.Nome, $"%{termoLimpo}%")
                    || EF.Functions.ILike(f.Bairro, $"%{termoLimpo}%")
                    || EF.Functions.ILike(f.Endereco, $"%{termoLimpo}%"));
            }

            var farmacias = await query
                .OrderBy(f => f.Tipo)
                .ThenBy(f => f.Nome)
                .ToListAsync();

            return Results.Ok(new
            {
                Itens = farmacias.Select(Mapear).ToList(),
                Fonte = FonteFarmacias
            });
        });

        group.MapGet("/{slug}", async (string slug, MedicamentoDb db) =>
        {
            var farmacia = await db.FarmaciasMunicipais
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Slug == slug.Trim());

            if (farmacia is null)
                return Results.NotFound("Nenhuma farmácia municipal encontrada com o endereço informado.");

            return Results.Ok(new { Farmacia = Mapear(farmacia), Fonte = FonteFarmacias });
        });
    }

    private static FarmaciaResumo Mapear(FarmaciaMunicipal f) =>
        new(
            f.Id,
            f.Slug,
            f.Nome,
            f.Tipo.ToString(),
            f.Tipo.GetDescription(),
            f.ZonaRural,
            f.Endereco,
            f.Bairro,
            f.Telefone,
            f.Whatsapp,
            f.Horario,
            f.UnidadesReferencia,
            f.Observacao);
}
