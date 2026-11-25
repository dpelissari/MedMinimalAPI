namespace PiraRemumeApi.Dtos;

public record FarmaciaFonte(string Nome, string PublicadoEm, string Url);

public record FarmaciaResumo(
    Guid Id,
    string Slug,
    string Nome,
    string Tipo,
    string TipoDescricao,
    bool ZonaRural,
    string Endereco,
    string Bairro,
    string? Telefone,
    bool Whatsapp,
    string Horario,
    IReadOnlyList<string> UnidadesReferencia,
    string? Observacao);
