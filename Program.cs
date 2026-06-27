using MedMinimalApi.Endpoints; 
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MedicamentoDb>(opt => opt.UseNpgsql(connectionString));

builder.Services.AddOpenApi();

builder.Services.AddValidatorsFromAssemblyContaining<MedicamentoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<FabricanteValidator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapFabricanteEndpoints();
app.MapMedicamentoEndpoints();

app.Run();