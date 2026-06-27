using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MedicamentoDb>(opt => opt.UseNpgsql(connectionString));

// Registra os serviços nativos de OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // Gera o JSON da API
    app.MapScalarApiReference(); // Cria a interface moderna em /scalar-api
}

app.MapGet("/medicamentos", async (MedicamentoDb db) => 
    await db.Medicamentos.Include(m => m.Fabricante).ToListAsync()
);

app.MapGet("/medicamentos/{id}", async (int id, MedicamentoDb db) =>
    await db.Medicamentos.FindAsync(id)
        is Medicamento medicamento ? Results.Ok(medicamento) : Results.NotFound()
);

app.MapPost("/medicamentos", async (Medicamento medicamento, MedicamentoDb db) =>
{
    db.Medicamentos.Add(medicamento);
    await db.SaveChangesAsync();
    return Results.Created($"/Medicamentos/{medicamento.Id}", medicamento);
});


app.MapGet("/fabricantes", async (MedicamentoDb db) =>
    await db.Fabricantes.ToListAsync()
);

app.MapGet("/fabricantes/{id}", async (int id, MedicamentoDb db) =>
    await db.Fabricantes.FindAsync(id)
    is Fabricante fabricamente ? Results.Ok(fabricamente) : Results.NotFound()
);

app.MapPost("/fabricantes", async (Fabricante fabricante, MedicamentoDb db) =>
{
    db.Fabricantes.Add(fabricante);
    await db.SaveChangesAsync();
    return Results.Created($"/Fabricantes/{fabricante.Id}", fabricante);
});

app.Run();
