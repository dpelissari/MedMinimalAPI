public class Medicamento
{
    public Guid Id { get; set; }
    public string Nome { get; set; }
    public FormaFarmaceutica FormaFarmaceutica { get; set; }
    public bool Disponivel { get; set; }
    public int QtdeDisponivel { get; set; }
    public Fabricante Fabricante { get; set; }
    public Guid FabricanteId { get; set; }
}