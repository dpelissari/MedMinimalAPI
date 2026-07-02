using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Perfil
{
    Admin = 10,
    User = 20
}