using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace MedMinimalApi.Endpoints;

public static class MedicamentoEndpoints
{
    public static void MapMedicamentoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/medicamentos");

        group.MapGet("/", async (MedicamentoDb db) =>
            await db.Medicamentos.ToListAsync()
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
