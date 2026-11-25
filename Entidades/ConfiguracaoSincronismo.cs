public class ConfiguracaoSincronismo
{
    public Guid Id { get; set; }
    public required string UrlFonte { get; set; }
    public required string UrlFarmacias { get; set; }
    public int IntervaloDias { get; set; }
    public bool Ativo { get; set; }
    public DateTimeOffset? UltimaVerificacaoEm { get; set; }
    public string? UltimoStatus { get; set; }
    public string? UltimaMensagem { get; set; }
    public string? UltimaAssinatura { get; set; }
    public string[] LinksDetectados { get; set; } = [];
}
