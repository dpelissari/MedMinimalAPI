using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace MedMinimalApi.Endpoints;

public static class FabricanteEndpoints
{
    public static void MapFabricanteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/fabricantes"); 

        group.MapGet("/", async (MedicamentoDb db) =>
            await db.Fabricantes.ToListAsync()
        );

        group.MapGet("/{id:guid}", async (Guid id, MedicamentoDb db) => {
            var fabricanteFromDb = await db.Fabricantes.Include(f => f.Medicamentos).FirstOrDefaultAsync(f => f.Id == id);
            if (fabricanteFromDb is null)
                return Results.NotFound($"Fabricante com o id {id} não encontrado");
            return Results.Ok(fabricanteFromDb);
        });

        group.MapPost("/", async (Fabricante fabricante, MedicamentoDb db, IValidator<Fabricante> validator) =>
        {
            var validationResult = await validator.ValidateAsync(fabricante);
            if (!validationResult.IsValid) 
                return Results.ValidationProblem(validationResult.ToDictionary());

            var cnpjExistente = await db.Fabricantes.AnyAsync(f => f.Cnpj == fabricante.Cnpj);
            if (cnpjExistente)
                return Results.BadRequest("Ja existe um fabricante cadastrado com o CNPJ informado");

            fabricante.Id = Guid.NewGuid();
            db.Fabricantes.Add(fabricante);
            await db.SaveChangesAsync();
            return Results.Created($"/{fabricante.Id}", fabricante);
        }).RequireAuthorization("AdminOuUsuario");

        group.MapPut("/{id:guid}", async (Guid id, Fabricante fabricante, MedicamentoDb db, IValidator<Fabricante> validator) =>
        {
            var validationResult = await validator.ValidateAsync(fabricante);
            if (!validationResult.IsValid) 
                return Results.ValidationProblem(validationResult.ToDictionary());

            var fabricanteRegistro = await db.Fabricantes.FirstOrDefaultAsync(f => f.Id == id);
            if (fabricanteRegistro is null)
                return Results.NotFound($"Fabricante com o id {id} não encontrado");

            var cnpjEmUso = await db.Fabricantes.AnyAsync(f => f.Cnpj == fabricante.Cnpj && f.Id != id);
            if (cnpjEmUso)
                return Results.BadRequest("CNPJ informado pertence a outro fabricante");

            db.Entry(fabricanteRegistro!).CurrentValues.SetValues(fabricante);
            fabricanteRegistro!.Id = id; 

            await db.SaveChangesAsync();
            return Results.Ok(fabricanteRegistro);
        }).RequireAuthorization("AdminOuUsuario");

        group.MapDelete("/{id:guid}", async (Guid id, MedicamentoDb db) =>
        {
            var fabricanteFromDb = await db.Fabricantes.FirstOrDefaultAsync(f => f.Id == id);
            if (fabricanteFromDb is null)
                return Results.NotFound("Fabricante não encontrado.");

            var possuiMedicamentos = await db.Medicamentos.AnyAsync(m => m.FabricanteId == id);
            if (possuiMedicamentos)
                return Results.BadRequest("Não é possível excluir este fabricante porque ele possui medicamentos vinculados a ele.");    

            db.Fabricantes.Remove(fabricanteFromDb);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("AdminOuUsuario");
    }
}
