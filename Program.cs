using PiraRemumeApi.Data;
using PiraRemumeApi.Endpoints;
using PiraRemumeApi.Servicos;
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
        policy.SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                    return false;

                return uri.IsLoopback;
            })
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddHttpClient("Sincronismo", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("PiraRemumeClient/1.0 (consulta informativa REMUME Piracicaba)");
});
builder.Services.AddScoped<SincronismoFonteService>();
builder.Services.AddScoped<CatalogoAtualizador>();
builder.Services.AddHostedService<SincronismoHostedService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MedicamentoDb>();
    await db.Database.MigrateAsync();
    await RemumeSeeder.SeedAsync(db, app.Environment);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("Angular");
app.UseAuthentication();
app.UseAuthorization();

app.MapMedicamentoEndpoints();
app.MapFarmaciaEndpoints();
app.MapSincronismoEndpoints();
app.MapUsuarioEndpoints();

app.Run();
