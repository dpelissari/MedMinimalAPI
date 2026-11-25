public class FarmaciaMunicipal
{
    public Guid Id { get; set; }
    public required string Slug { get; set; }
    public required string Nome { get; set; }
    public TipoFarmacia Tipo { get; set; }
    public bool ZonaRural { get; set; }
    public required string Endereco { get; set; }
    public required string Bairro { get; set; }
    public string? Telefone { get; set; }
    public bool Whatsapp { get; set; }
    public required string Horario { get; set; }
    public string[] UnidadesReferencia { get; set; } = [];
    public string[] Aliases { get; set; } = [];
    public string? Observacao { get; set; }

    public ICollection<Apresentacao> Apresentacoes { get; set; } = [];
}
