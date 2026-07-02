namespace MedMinimalApi.Dtos;

public record RegistrarRequest(string NomeUsuario, string Senha, Perfil Perfil);
