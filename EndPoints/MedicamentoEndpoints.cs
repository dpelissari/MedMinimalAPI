using PiraRemumeApi.Dtos;
using PiraRemumeApi.Extensions;
using Microsoft.EntityFrameworkCore;

namespace PiraRemumeApi.Endpoints;

public static class MedicamentoEndpoints
{
    public static readonly RemumeFonte FonteRemume = new(
        "REMUME Piracicaba 2023",
        "2024-08-06",
        "https://piracicaba.sp.gov.br/wp-content/uploads/2024/08/REMUME-2023-Grupo-Farmacologico-07-24.pdf");

    public static void MapMedicamentoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/medicamentos");

        group.MapGet("/grupos", async (MedicamentoDb db) =>
        {
            var grupos = await db.Medicamentos
                .AsNoTracking()
                .Select(m => m.GrupoFarmacologico)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            return Results.Ok(grupos);
        });

        group.MapGet("/", async ([AsParameters] Paginacao p, string? termo, string? tipo, MedicamentoDb db) =>
        {
            IQueryable<Medicamento> query = db.Medicamentos.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(termo))
            {
                var termoLimpo = termo.Trim();
                var tipoFiltro = tipo?.Trim() ?? "nome";
                var porGrupo = tipoFiltro.Equals("grupo", StringComparison.OrdinalIgnoreCase);

                if (porGrupo)
                {
                    query = query.Where(m => m.GrupoFarmacologico == termoLimpo);
                }
                else
                {
                    query = query.Where(m =>
                        EF.Functions.ILike(m.Nome, $"%{termoLimpo}%")
                        || m.Apresentacoes.Any(a => EF.Functions.ILike(a.Descricao, $"%{termoLimpo}%")));
                }
            }

            var idsPaginados = await query
                .OrderBy(m => m.Nome)
                .Select(m => m.Id)
                .PaginarAsync(p);

            var medicamentos = await db.Medicamentos
                .AsNoTracking()
                .Where(m => idsPaginados.Itens.Contains(m.Id))
                .Include(m => m.Apresentacoes)
                    .ThenInclude(a => a.Unidades)
                .ToListAsync();

            var porId = medicamentos.ToDictionary(m => m.Id);
            var itens = idsPaginados.Itens
                .Where(porId.ContainsKey)
                .Select(id => Mapear(porId[id]))
                .ToList();

            return Results.Ok(new
            {
                Itens = itens,
                idsPaginados.Pagina,
                idsPaginados.Tamanho,
                idsPaginados.Total,
                idsPaginados.TotalPaginas,
                Fonte = FonteRemume
            });
        });

        group.MapGet("/{id:guid}", async (Guid id, MedicamentoDb db) =>
        {
            var medicamento = await db.Medicamentos
                .AsNoTracking()
                .Include(m => m.Apresentacoes)
                    .ThenInclude(a => a.Unidades)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medicamento is null)
                return Results.NotFound("Nenhum medicamento encontrado na REMUME com o ID informado.");

            return Results.Ok(new { Medicamento = Mapear(medicamento), Fonte = FonteRemume });
        });
    }

    private static MedicamentoRemume Mapear(Medicamento m)
    {
        var apresentacoes = m.Apresentacoes
            .OrderBy(a => a.Descricao)
            .Select(a => new ApresentacaoResumo(
                a.Id,
                a.Descricao,
                a.Abrangencia.ToString(),
                a.Abrangencia.GetDescription(),
                a.Observacao,
                a.Unidades.OrderBy(u => u.Nome).Select(u => new UnidadeResumo(u.Id, u.Nome, u.Slug)).ToList()))
            .ToList();

        return new MedicamentoRemume(
            m.Id,
            m.Nome,
            m.GrupoFarmacologico,
            m.ClasseTerapeutica,
            apresentacoes,
            apresentacoes.Count > 0 && apresentacoes.All(a => a.Abrangencia == nameof(AbrangenciaDisponibilidade.TodasAsFarmaciasMunicipais)),
            apresentacoes.Any(a => a.Abrangencia == nameof(AbrangenciaDisponibilidade.Restrito)));
    }
}
