public class Medicamento
{
    public Guid Id { get; set; }
    public required string Nome { get; set; }
    public required string GrupoFarmacologico { get; set; }
    public required string ClasseTerapeutica { get; set; }

    public ICollection<Apresentacao> Apresentacoes { get; set; } = [];
}
