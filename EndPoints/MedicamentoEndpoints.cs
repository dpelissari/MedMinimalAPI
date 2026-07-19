using FluentValidation;
using MedMinimalApi.Dtos;
using MedMinimalApi.Extensions;
using Microsoft.EntityFrameworkCore;

namespace MedMinimalApi.Endpoints;

public static class MedicamentoEndpoints
{
    public static void MapMedicamentoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/medicamentos");

        group.MapGet("/", async ([AsParameters] Paginacao p, MedicamentoDb db) =>
            await db.Medicamentos
                .Include(f => f.Fabricante)
                .AsNoTracking() // EF: leitura, sem rastrear
                .OrderBy(m => m.Nome)
                .PaginarAsync(p)
        );

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

            var fabricante = await db.Fabricantes.FirstOrDefaultAsync(f => f.Id == medicamento.FabricanteId);
            if (fabricante is null)
                return Results.BadRequest("Fabricante não encontrado.");

            medicamento.Id = Guid.NewGuid();
            db.Medicamentos.Add(medicamento);
            await db.SaveChangesAsync();

            return Results.Created($"/{medicamento.Id}", medicamento);
        }).RequireAuthorization("AdminOuUsuario");

        group.MapPost("/lote", async (List<Medicamento> medicamentos, MedicamentoDb db, IValidator<Medicamento> validator) =>
        {
            var criados = new List<Medicamento>();
            var erros = new List<object>();

            foreach (var medicamento in medicamentos)
            {
                var validationResult = await validator.ValidateAsync(medicamento);
                if (!validationResult.IsValid)
                {
                    erros.Add(new { medicamento.Nome, Erros = validationResult.ToDictionary() });
                    continue;
                }

                var fabricante = await db.Fabricantes.FirstOrDefaultAsync(f => f.Id == medicamento.FabricanteId);
                if (fabricante is null)
                {
                    erros.Add(new { medicamento.Nome, Erro = "Fabricante não encontrado." });
                    continue;
                }

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
