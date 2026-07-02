using MedMinimalApi.Endpoints; 
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MedicamentoDb>(opt => opt.UseNpgsql(connectionString));

builder.Services.AddOpenApi();

builder.Services.AddValidatorsFromAssemblyContaining<MedicamentoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<FabricanteValidator>();

// Configuracao do JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = System.Text.Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApenasAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Usuario", policy => policy.RequireRole("User"));
    options.AddPolicy("AdminOuUsuario", policy => policy.RequireRole("Admin", "User"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapFabricanteEndpoints();
app.MapMedicamentoEndpoints();

app.UseAuthentication();
app.UseAuthorization();

// 3. MAPEAR OS ENDPOINTS DO BANCO E DE LOGIN
app.MapUsuarioEndpoints();

app.Run();