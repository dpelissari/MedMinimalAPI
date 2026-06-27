using System.Text.Json.Serialization;

public class Medicamento
{
    public Guid Id { get; set; }
    public required string Nome { get; set; }
    public required FormaFarmaceutica FormaFarmaceutica { get; set; }
    public bool Disponivel => QtdeDisponivel > 0;
    public required int QtdeDisponivel { get; set; }
    [JsonIgnore] public Fabricante? Fabricante { get; set; }
    public required Guid FabricanteId { get; set; }
}