public class Fabricante
{
    public Guid Id { get; set; }
    public string NomeFantasia { get; set; }
    public string Cnpj { get; set; }

    public ICollection<Medicamento> Medicamentos { get; set; }
}