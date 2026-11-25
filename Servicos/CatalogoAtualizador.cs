using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PiraRemumeApi.Servicos;

public class CatalogoAtualizador
{
    private static readonly JsonSerializerOptions JsonLeitura = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions JsonEscrita = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly MedicamentoDb _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<CatalogoAtualizador> _logger;

    public CatalogoAtualizador(MedicamentoDb db, IWebHostEnvironment env, ILogger<CatalogoAtualizador> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    public async Task<string> AplicarAsync(
        IReadOnlyList<CatalogoMedicamentoLido>? medicamentos,
        IReadOnlyDictionary<string, CatalogoGrupoLido>? grupos,
        IReadOnlyList<CatalogoFarmaciaLida>? farmacias,
        string urlRemume,
        string urlFarmacias,
        DateOnly publicadoEm,
        CancellationToken ct)
    {
        var pasta = Path.Combine(_env.ContentRootPath, "Data");
        Directory.CreateDirectory(pasta);

        var partes = new List<string>();

        if (farmacias is { Count: >= 20 })
        {
            await GravarFarmaciasAsync(pasta, farmacias, urlFarmacias, publicadoEm, ct);
            partes.Add($"{farmacias.Count} farmácias");
        }
        else if (farmacias is not null)
        {
            _logger.LogWarning("Leitura das farmácias incompleta ({Count} itens). Mantive o JSON atual.", farmacias.Count);
        }

        if (medicamentos is { Count: >= 80 } && grupos is not null)
        {
            await GravarRemumeAsync(pasta, medicamentos, grupos, urlRemume, publicadoEm, ct);
            partes.Add($"{medicamentos.Count} medicamentos");
        }
        else if (medicamentos is not null)
        {
            _logger.LogWarning("Leitura da REMUME incompleta ({Count} itens). Mantive o JSON atual.", medicamentos.Count);
        }

        if (partes.Count == 0)
            throw new InvalidOperationException("A fonte mudou, mas não deu para ler o catálogo com segurança. A lista daqui ficou como estava.");

        await PiraRemumeApi.Data.RemumeSeeder.SeedAsync(_db, _env, forcar: true);
        return $"Atualizei o catálogo com {string.Join(" e ", partes)}.";
    }

    private async Task GravarFarmaciasAsync(
        string pasta,
        IReadOnlyList<CatalogoFarmaciaLida> lidas,
        string url,
        DateOnly publicadoEm,
        CancellationToken ct)
    {
        var caminho = Path.Combine(pasta, "farmacias-piracicaba.json");
        var anteriores = await LerAnterioresFarmaciasAsync(caminho, ct);
        var itens = lidas.Select(lida =>
        {
            var anterior = EncontrarAnterior(lida, anteriores);
            return new
            {
                slug = anterior?.Slug ?? lida.Slug,
                nome = lida.Nome,
                tipo = lida.Tipo,
                zonaRural = lida.ZonaRural,
                endereco = lida.Endereco,
                bairro = string.IsNullOrWhiteSpace(lida.Bairro) ? anterior?.Bairro ?? "" : lida.Bairro,
                telefone = lida.Telefone,
                whatsapp = lida.Whatsapp ? true : (bool?)null,
                horario = lida.Horario,
                unidadesReferencia = lida.UnidadesReferencia,
                aliases = anterior?.Aliases is { Count: > 0 } ? anterior.Aliases : null,
                observacao = anterior?.Observacao
            };
        }).ToList();

        var arquivo = new
        {
            nome = "Farmácias Municipais de Piracicaba",
            publicadoEm = publicadoEm.ToString("yyyy-MM-dd"),
            url,
            itens
        };

        await File.WriteAllTextAsync(caminho, JsonSerializer.Serialize(arquivo, JsonEscrita) + Environment.NewLine, Encoding.UTF8, ct);
    }

    private async Task GravarRemumeAsync(
        string pasta,
        IReadOnlyList<CatalogoMedicamentoLido> medicamentos,
        IReadOnlyDictionary<string, CatalogoGrupoLido> grupos,
        string url,
        DateOnly publicadoEm,
        CancellationToken ct)
    {
        var gruposArquivo = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var itens = new List<object>();

        foreach (var med in medicamentos)
        {
            var classificacao = CatalogoFonteParser.BuscarGrupo(grupos, med.Nome)
                ?? new CatalogoGrupoLido("Sem classificação", "Não informada");
            gruposArquivo[med.Nome] = new { grupo = classificacao.Grupo, classe = classificacao.Classe };

            itens.Add(new
            {
                nome = med.Nome,
                apresentacoes = med.Apresentacoes.Select(a => new
                {
                    descricao = a.Descricao,
                    abrangencia = a.Abrangencia,
                    observacao = a.Observacao,
                    unidades = a.Unidades is { Count: > 0 } ? a.Unidades : null
                }).ToList()
            });
        }

        var remume = new
        {
            nome = "REMUME Piracicaba",
            publicadoEm = publicadoEm.ToString("yyyy-MM-dd"),
            url,
            itens
        };

        await File.WriteAllTextAsync(
            Path.Combine(pasta, "remume-piracicaba.json"),
            JsonSerializer.Serialize(remume, JsonEscrita) + Environment.NewLine,
            Encoding.UTF8,
            ct);
        await File.WriteAllTextAsync(
            Path.Combine(pasta, "remume-grupos.json"),
            JsonSerializer.Serialize(gruposArquivo, JsonEscrita) + Environment.NewLine,
            Encoding.UTF8,
            ct);
    }

    private static async Task<List<FarmaciaAnterior>> LerAnterioresFarmaciasAsync(string caminho, CancellationToken ct)
    {
        if (!File.Exists(caminho))
            return [];

        try
        {
            var arquivo = JsonSerializer.Deserialize<FarmaciasArquivo>(
                await File.ReadAllTextAsync(caminho, ct), JsonLeitura);
            return arquivo?.Itens ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static FarmaciaAnterior? EncontrarAnterior(CatalogoFarmaciaLida lida, List<FarmaciaAnterior> anteriores)
    {
        var exato = anteriores.FirstOrDefault(a => a.Slug.Equals(lida.Slug, StringComparison.OrdinalIgnoreCase));
        if (exato is not null)
            return exato;

        var porNome = anteriores.FirstOrDefault(a =>
            CatalogoFonteParser.NormalizarChave(a.Nome) == CatalogoFonteParser.NormalizarChave(lida.Nome));
        if (porNome is not null)
            return porNome;

        var porAlias = anteriores.FirstOrDefault(a =>
            (a.Aliases ?? []).Any(alias => CatalogoFonteParser.Slugificar(alias) == lida.Slug
                || CatalogoFonteParser.NormalizarChave(alias) == CatalogoFonteParser.NormalizarChave(lida.Nome)));
        if (porAlias is not null)
            return porAlias;

        return anteriores
            .Where(a => lida.Slug.EndsWith(a.Slug, StringComparison.OrdinalIgnoreCase) && lida.Slug != a.Slug)
            .OrderByDescending(a => a.Slug.Length)
            .FirstOrDefault();
    }

    private sealed record FarmaciasArquivo(List<FarmaciaAnterior> Itens);

    private sealed class FarmaciaAnterior
    {
        public string Slug { get; set; } = "";
        public string Nome { get; set; } = "";
        public string Bairro { get; set; } = "";
        public string? Observacao { get; set; }
        public List<string>? Aliases { get; set; }
    }
}
