public class Apresentacao
{
    public Guid Id { get; set; }
    public required string Descricao { get; set; }
    public AbrangenciaDisponibilidade Abrangencia { get; set; }
    public string? Observacao { get; set; }

    public Guid MedicamentoId { get; set; }
    public Medicamento Medicamento { get; set; } = null!;

    public ICollection<FarmaciaMunicipal> Unidades { get; set; } = [];
}
