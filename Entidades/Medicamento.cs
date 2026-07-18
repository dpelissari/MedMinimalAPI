public class Medicamento
{
    public Guid Id { get; set; }
    public required string Nome { get; set; }
    public required FormaFarmaceutica FormaFarmaceutica { get; set; }
    public required ClasseTerapeutica ClasseTerapeutica { get; set; }
    public bool FornecidoPeloSUS { get; set; }
    public Fabricante? Fabricante { get; set; }
    public required Guid FabricanteId { get; set; }
}