using System.Security.Claims;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using  MedMinimalApi.Dtos;

namespace MedMinimalApi.Endpoints;

public static class UsuarioEndpoints
{
    public static void MapUsuarioEndpoints(this WebApplication app)
    {
        var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Usuario>();
        
        app.MapPost("/registrar", async (RegistrarRequest dto, MedicamentoDb db,  IValidator<RegistrarRequest> validator) =>
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

            // Transforma a senha pura em um Hash seguro
            novoUsuario.SenhaHash = passwordHasher.HashPassword(novoUsuario, dto.Senha);

            db.Usuarios.Add(novoUsuario);
            await db.SaveChangesAsync();

            return Results.Created($"/usuarios/{novoUsuario.Id}", new {
                 novoUsuario.Id,
                 novoUsuario.Nome,
                 novoUsuario.Perfil }
            );
        });

        app.MapPost("/login", async (LoginRequest dto, MedicamentoDb db, IConfiguration config, IValidator<LoginRequest> validator) =>
        {
            var validationResult = await validator.ValidateAsync(dto);
            if (!validationResult.IsValid) 
                return Results.ValidationProblem(validationResult.ToDictionary());

            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Nome == dto.NomeUsuario);
            if (usuario == null) 
                return Results.Unauthorized();

            // Verifica se a senha digitada bate com o Hash salvo
            var resultadoSenha = passwordHasher.VerifyHashedPassword(usuario, usuario.SenhaHash, dto.Senha);
            if (resultadoSenha == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed) 
                return Results.Unauthorized();

            // Gera o token JWT
            var key = System.Text.Encoding.ASCII.GetBytes(config["Jwt:Key"]!);
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, usuario.Nome),
                    new Claim(ClaimTypes.Role, usuario.Perfil.ToString()) // Injeta a role do banco no Token
                }),
                Expires = DateTime.UtcNow.AddHours(6),
                Issuer = config["Jwt:Issuer"],
                Audience = config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature) // Assina o token com a chave secreta usando criptografia para evitar fraudes
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Results.Ok(new { Token = tokenHandler.WriteToken(token) });
        });
    }
}
