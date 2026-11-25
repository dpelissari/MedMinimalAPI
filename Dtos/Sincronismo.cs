namespace PiraRemumeApi.Dtos;

public record SincronismoResumo(
    Guid Id,
    string UrlFonte,
    string UrlFarmacias,
    int IntervaloDias,
    bool Ativo,
    DateTimeOffset? UltimaVerificacaoEm,
    DateTimeOffset? ProximaVerificacaoEm,
    string? UltimoStatus,
    string? UltimaMensagem,
    IReadOnlyList<string> LinksDetectados);

public record AtualizarSincronismoRequest(string UrlFonte, string UrlFarmacias, int IntervaloDias, bool Ativo);
