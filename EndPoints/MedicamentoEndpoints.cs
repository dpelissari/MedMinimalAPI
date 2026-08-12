using FluentValidation;
using MedMinimalApi.Dtos;
using MedMinimalApi.Extensions;
using Microsoft.EntityFrameworkCore;

namespace MedMinimalApi.Endpoints;

public static class MedicamentoEndpoints
{
    private static readonly (ClasseTerapeutica Valor, string Nome, string Descricao)[] ClassesCache =
        Enum.GetValues<ClasseTerapeutica>()
            .Select(e => (e, e.ToString(), e.GetDescription()))
            .ToArray();

    public static void MapMedicamentoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/medicamentos");

        group.MapGet("/", async ([AsParameters] Paginacao p, string? termo, string? tipo, MedicamentoDb db) =>
        {
            IQueryable<Medicamento> query = db.Medicamentos.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(termo))
            {
                var termoLimpo = termo.Trim();
                var tipoFiltro = tipo?.Trim() ?? "nome";

                if (tipoFiltro.Equals("classeTerapeutica", StringComparison.OrdinalIgnoreCase))
                {
                    var enumsCorrespondentes = ClassesCache
                        .Where(c =>
                            c.Nome.Contains(termoLimpo, StringComparison.OrdinalIgnoreCase) ||
                            c.Descricao.Contains(termoLimpo, StringComparison.OrdinalIgnoreCase))
                        .Select(c => c.Valor)
                        .ToList();

                    query = enumsCorrespondentes.Count == 0
                        ? query.Where(_ => false)
                        : query.Where(x => enumsCorrespondentes.Contains(x.ClasseTerapeutica));
                }
                else
                {
                    query = query.Where(x => EF.Functions.ILike(x.Nome, $"%{termoLimpo}%"));
                }
            }

            var resultadoPaginado = await query
                .OrderBy(m => m.Nome)
                .Select(m => new
                {
                    m.Id,
                    m.Nome,
                    m.ClasseTerapeutica,
                    m.FormaFarmaceutica,
                    m.FornecidoPeloSUS,
                    m.FabricanteId,
                    Fabricante = m.Fabricante == null
                        ? null
                        : new
                        {
                            m.Fabricante.Id,
                            m.Fabricante.NomeFantasia,
                            m.Fabricante.Cnpj
                        }
                })
                .PaginarAsync(p);

            var itens = resultadoPaginado.Itens.Select(m => new
            {
                m.Id,
                m.Nome,
                m.ClasseTerapeutica,
                ClasseTerapeuticaDescricao = m.ClasseTerapeutica.GetDescription(),
                m.FormaFarmaceutica,
                FormaFarmaceuticaDescricao = m.FormaFarmaceutica.GetDescription(),
                m.FornecidoPeloSUS,
                m.FabricanteId,
                m.Fabricante
            });

            return Results.Ok(new { Itens = itens, resultadoPaginado.Pagina, resultadoPaginado.Tamanho, resultadoPaginado.Total });
        });


        group.MapGet("/{id:guid}", async (Guid id, MedicamentoDb db) => {
            var medicamento = await db.Medicamentos.FirstOrDefaultAsync(m => m.Id == id);
            if (medicamento is null)
                return Results.NotFound("Nenhum medicamento encontrado com o ID informado.");
            return Results.Ok(medicamento);
        });

        group.MapPost("/", async (Medicamento medicamento, MedicamentoDb db, IValidator<Medicamento> validator) =>
        {
            var validationResult = await validator.ValidateAsync(medicamento);
            if (!validationResult.IsValid) 
                return Results.ValidationProblem(validationResult.ToDictionary());

            var fabricanteExiste = await db.Fabricantes.AnyAsync(f => f.Id == medicamento.FabricanteId);
            if (!fabricanteExiste)
                return Results.BadRequest("Fabricante não encontrado.");

            // Evita conflito de tracking: o body pode trazer Fabricante aninhado,
            // mas a FK FabricanteId já basta para o insert.
            medicamento.Fabricante = null;
            medicamento.Id = Guid.NewGuid();
            db.Medicamentos.Add(medicamento);
            await db.SaveChangesAsync();

            return Results.Created($"/{medicamento.Id}", medicamento);
        }).RequireAuthorization("AdminOuUsuario");

        group.MapPost("/lote", async (List<Medicamento> medicamentos, MedicamentoDb db, IValidator<Medicamento> validator) =>
        {
            var criados = new List<Medicamento>();
            var erros = new List<object>();

            // Valida existência dos fabricantes uma vez (evita N queries e tracking duplicado)
            var fabricanteIds = medicamentos.Select(m => m.FabricanteId).Distinct().ToList();
            var fabricantesExistentes = await db.Fabricantes
                .AsNoTracking()
                .Where(f => fabricanteIds.Contains(f.Id))
                .Select(f => f.Id)
                .ToHashSetAsync();

            foreach (var medicamento in medicamentos)
            {
                var validationResult = await validator.ValidateAsync(medicamento);
                if (!validationResult.IsValid)
                {
                    erros.Add(new { medicamento.Nome, Erros = validationResult.ToDictionary() });
                    continue;
                }

                if (!fabricantesExistentes.Contains(medicamento.FabricanteId))
                {
                    erros.Add(new { medicamento.Nome, Erro = "Fabricante não encontrado." });
                    continue;
                }

                // Mesmo fabricante aparece em vários itens do lote com instâncias diferentes.
                // Sem limpar a navegação, o EF tenta trackear 2+ Fabricante com o mesmo Id.
                medicamento.Fabricante = null;
                medicamento.Id = Guid.NewGuid();
                db.Medicamentos.Add(medicamento);
                criados.Add(medicamento);
            }

            if (criados.Count > 0)
                await db.SaveChangesAsync();

            return Results.Ok(new
            {
                TotalRecebidos = medicamentos.Count,
                TotalCriados = criados.Count,
                TotalErros = erros.Count,
                Criados = criados,
                Erros = erros
            });
        }).RequireAuthorization("AdminOuUsuario");

        group.MapPut("/{id:guid}", async (Guid id, Medicamento medicamento, MedicamentoDb db, IValidator<Medicamento> validator) =>
        {
            var validationResult = await validator.ValidateAsync(medicamento);
            if (!validationResult.IsValid) 
                return Results.ValidationProblem(validationResult.ToDictionary());

            var medicamentoFromDb = await db.Medicamentos.FindAsync(id);
            if (medicamentoFromDb is null) 
                return Results.NotFound($"Nenhum medicamento encontrado com o id {id}");

            var fabricanteExiste = await db.Fabricantes.AnyAsync(f => f.Id == medicamento.FabricanteId);
            if (!fabricanteExiste)
                return Results.BadRequest($"Fabricante com o id {medicamento.FabricanteId} não encontrado");

            db.Entry(medicamentoFromDb!).CurrentValues.SetValues(medicamento);
            medicamentoFromDb!.Id = id;

            await db.SaveChangesAsync();
            return Results.Ok(medicamentoFromDb);
        }).RequireAuthorization("AdminOuUsuario");
    }
}
