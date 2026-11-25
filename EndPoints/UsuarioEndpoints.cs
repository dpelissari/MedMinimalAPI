using System.Security.Claims;
using FluentValidation;
using PiraRemumeApi.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace PiraRemumeApi.Endpoints;

public static class UsuarioEndpoints
{
    public static void MapUsuarioEndpoints(this WebApplication app)
    {
        var passwordHasher = new PasswordHasher<Usuario>();

        app.MapPost("/registrar", async (RegistrarRequest dto, MedicamentoDb db, IValidator<RegistrarRequest> validator) =>
        {
            var validationResult = await validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            var usuarioExiste = await db.Usuarios.AnyAsync(u => u.Nome == dto.NomeUsuario);
            if (usuarioExiste)
                return Results.BadRequest("Este nome de usuário já está em uso.");

            var novoUsuario = new Usuario
            {
                Nome = dto.NomeUsuario,
                Perfil = dto.Perfil
            };

            novoUsuario.SenhaHash = passwordHasher.HashPassword(novoUsuario, dto.Senha);

            db.Usuarios.Add(novoUsuario);
            await db.SaveChangesAsync();

            return Results.Created($"/usuarios/{novoUsuario.Id}", new
            {
                novoUsuario.Id,
                novoUsuario.Nome,
                novoUsuario.Perfil
            });
        }).RequireAuthorization("ApenasAdmin");

        app.MapPost("/login", async (LoginRequest dto, MedicamentoDb db, IConfiguration config, IValidator<LoginRequest> validator) =>
        {
            var validationResult = await validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Nome == dto.NomeUsuario);
            if (usuario == null)
                return Results.Unauthorized();

            var resultadoSenha = passwordHasher.VerifyHashedPassword(usuario, usuario.SenhaHash, dto.Senha);
            if (resultadoSenha == PasswordVerificationResult.Failed)
                return Results.Unauthorized();

            var expiraEm = DateTime.UtcNow.AddHours(6);
            var key = System.Text.Encoding.ASCII.GetBytes(config["Jwt:Key"]!);
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, usuario.Nome),
                    new Claim(ClaimTypes.Role, usuario.Perfil.ToString())
                ]),
                Expires = expiraEm,
                Issuer = config["Jwt:Issuer"],
                Audience = config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Results.Ok(new LoginResposta(
                tokenHandler.WriteToken(token),
                usuario.Nome,
                usuario.Perfil.ToString(),
                expiraEm));
        });
    }
}
