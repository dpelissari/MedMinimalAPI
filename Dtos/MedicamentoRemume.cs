namespace PiraRemumeApi.Dtos;

public record RemumeFonte(string Nome, string PublicadoEm, string Url);

public record UnidadeResumo(Guid Id, string Nome, string Slug);

public record ApresentacaoResumo(
    Guid Id,
    string Descricao,
    string Abrangencia,
    string AbrangenciaDescricao,
    string? Observacao,
    IReadOnlyList<UnidadeResumo> Unidades);

public record MedicamentoRemume(
    Guid Id,
    string Nome,
    string GrupoFarmacologico,
    string ClasseTerapeutica,
    IReadOnlyList<ApresentacaoResumo> Apresentacoes,
    bool TodasAsFarmacias,
    bool AlgumaRestrita);
