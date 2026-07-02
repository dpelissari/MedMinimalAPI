public class Fabricante
{
    public Guid Id { get; set; }
    public required string NomeFantasia { get; set; }
    public required string Cnpj { get; set; }
    public ICollection<Medicamento> Medicamentos { get; set; } = [];
}