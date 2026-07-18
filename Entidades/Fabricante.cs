using System.Text.Json.Serialization;

public class Fabricante
{
    public Guid Id { get; set; }
    public required string NomeFantasia { get; set; }
    public required string Cnpj { get; set; }

    [JsonIgnore]
    public ICollection<Medicamento> Medicamentos { get; set; } = [];
}