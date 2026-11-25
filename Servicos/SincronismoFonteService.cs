using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PiraRemumeApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace PiraRemumeApi.Servicos;

public class SincronismoFonteService
{
    public const string UrlPadraoRemume = "https://piracicaba.sp.gov.br/servicos/relacao-municipal-de-medicamentos-essenciais-remume/";

    public const string UrlPadraoFarmacias = "https://piracicaba.sp.gov.br/servicos/farmacias-municipais/";

    private static readonly HashSet<string> HostsPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "piracicaba.sp.gov.br",
        "www.piracicaba.sp.gov.br",
        "saude.piracicaba.sp.gov.br"
    };

    private static readonly Regex HrefRegex = new(
        """href\s*=\s*["']([^"']+)["']""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ScriptRegex = new(
        "<script[\\s\\S]*?</script>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StyleRegex = new(
        "<style[\\s\\S]*?</style>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TagRegex = new("<[^>]+>", RegexOptions.Compiled);

    private readonly MedicamentoDb _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CatalogoAtualizador _catalogo;
    private readonly ILogger<SincronismoFonteService> _logger;

    public SincronismoFonteService(
        MedicamentoDb db,
        IHttpClientFactory httpClientFactory,
        CatalogoAtualizador catalogo,
        ILogger<SincronismoFonteService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _catalogo = catalogo;
        _logger = logger;
    }

    public static bool UrlPermitida(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && HostsPermitidos.Contains(uri.Host);

    public async Task<ConfiguracaoSincronismo> ObterAsync(CancellationToken ct = default)
    {
        var config = await _db.ConfiguracoesSincronismo.AsNoTracking().FirstOrDefaultAsync(ct);
        if (config is not null)
            return config;

        config = CriarPadrao();
        _db.ConfiguracoesSincronismo.Add(config);
        await _db.SaveChangesAsync(ct);
        return config;
    }

    public async Task<ConfiguracaoSincronismo> AtualizarAsync(
        AtualizarSincronismoRequest pedido,
        CancellationToken ct = default)
    {
        var config = await GarantirAsync(ct);
        var urlRemume = pedido.UrlFonte.Trim();
        var urlFarmacias = pedido.UrlFarmacias.Trim();
        var mudou = !string.Equals(config.UrlFonte, urlRemume, StringComparison.Ordinal)
            || !string.Equals(config.UrlFarmacias, urlFarmacias, StringComparison.Ordinal);

        if (mudou)
        {
            config.UltimaAssinatura = null;
            config.LinksDetectados = [];
            config.UltimoStatus = null;
            config.UltimaMensagem = "Os endereços vigiados mudaram. Rode uma verificação para ler as novas fontes.";
            config.UltimaVerificacaoEm = null;
        }

        config.UrlFonte = urlRemume;
        config.UrlFarmacias = urlFarmacias;
        config.IntervaloDias = pedido.IntervaloDias;
        config.Ativo = pedido.Ativo;
        await _db.SaveChangesAsync(ct);
        return config;
    }

    public async Task VerificarSeDueAsync(CancellationToken ct = default)
    {
        var config = await GarantirAsync(ct);
        if (!config.Ativo)
            return;

        if (config.UltimaVerificacaoEm is { } ultima
            && ultima.AddDays(config.IntervaloDias) > DateTimeOffset.UtcNow)
            return;

        await VerificarAsync(forcarCatalogo: false, ct);
    }

    public async Task<ConfiguracaoSincronismo> VerificarAsync(bool forcarCatalogo = false, CancellationToken ct = default)
    {
        var config = await GarantirAsync(ct);

        try
        {
            var remume = await LerRemumeAsync(config.UrlFonte, ct);
            var farmacias = await LerFarmaciasAsync(config.UrlFarmacias, ct);
            var assinatura = $"{remume.Assinatura}\n{farmacias.Assinatura}";
            var links = remume.Links.Concat(farmacias.Links).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var anterior = config.UltimaAssinatura;
            var fonteIgual = !string.IsNullOrWhiteSpace(anterior)
                && string.Equals(anterior, assinatura, StringComparison.Ordinal);

            config.LinksDetectados = links;
            config.UltimaVerificacaoEm = DateTimeOffset.UtcNow;

            if (string.IsNullOrWhiteSpace(anterior) && !forcarCatalogo)
            {
                config.UltimaAssinatura = assinatura;
                config.UltimoStatus = "PrimeiraLeitura";
                config.UltimaMensagem =
                    $"Passamos a vigiar {remume.Links.Count} PDF(s) da REMUME e a página das farmácias. Na próxima mudança, o catálogo é atualizado sozinho.";
            }
            else if (fonteIgual && !forcarCatalogo)
            {
                config.UltimaAssinatura = assinatura;
                config.UltimoStatus = "SemAlteracao";
                config.UltimaMensagem = "A REMUME e a página das farmácias continuam iguais.";
            }
            else
            {
                var mensagem = await AtualizarCatalogoAsync(remume, farmacias, config, ct);
                config.UltimaAssinatura = assinatura;
                config.UltimoStatus = "CatalogoAtualizado";
                config.UltimaMensagem = mensagem;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Falha ao verificar as fontes da Prefeitura.");
            config.UltimaVerificacaoEm = DateTimeOffset.UtcNow;
            config.UltimoStatus = "Erro";
            config.UltimaMensagem = ex.Message;
        }

        await _db.SaveChangesAsync(ct);
        return config;
    }

    public static SincronismoResumo Mapear(ConfiguracaoSincronismo config)
    {
        DateTimeOffset? proxima = null;
        if (config.Ativo)
        {
            proxima = config.UltimaVerificacaoEm is { } ultima
                ? ultima.AddDays(config.IntervaloDias)
                : DateTimeOffset.UtcNow;
        }

        return new SincronismoResumo(
            config.Id,
            config.UrlFonte,
            config.UrlFarmacias,
            config.IntervaloDias,
            config.Ativo,
            config.UltimaVerificacaoEm,
            proxima,
            config.UltimoStatus,
            config.UltimaMensagem,
            config.LinksDetectados);
    }

    public static ConfiguracaoSincronismo CriarPadrao() => new()
    {
        Id = Guid.NewGuid(),
        UrlFonte = UrlPadraoRemume,
        UrlFarmacias = UrlPadraoFarmacias,
        IntervaloDias = 30,
        Ativo = true
    };

    private async Task<ConfiguracaoSincronismo> GarantirAsync(CancellationToken ct)
    {
        var config = await _db.ConfiguracoesSincronismo.FirstOrDefaultAsync(ct);
        if (config is not null)
        {
            if (string.IsNullOrWhiteSpace(config.UrlFarmacias))
            {
                config.UrlFarmacias = UrlPadraoFarmacias;
                await _db.SaveChangesAsync(ct);
            }

            return config;
        }

        config = CriarPadrao();
        _db.ConfiguracoesSincronismo.Add(config);
        await _db.SaveChangesAsync(ct);
        return config;
    }

    private async Task<string> AtualizarCatalogoAsync(
        LeituraRemume remume,
        LeituraFarmacias farmacias,
        ConfiguracaoSincronismo config,
        CancellationToken ct)
    {
        var meds = remume.Alfa is { Length: > 0 }
            ? CatalogoFonteParser.ParseRemumeAlfabetica(remume.Alfa)
            : null;
        var grupos = remume.Grupo is { Length: > 0 }
            ? CatalogoFonteParser.ParseRemumeGrupos(remume.Grupo)
            : [];
        var unidades = string.IsNullOrWhiteSpace(farmacias.Html)
            ? null
            : CatalogoFonteParser.ParseFarmaciasHtml(farmacias.Html);

        return await _catalogo.AplicarAsync(
            meds,
            grupos,
            unidades,
            remume.Links.FirstOrDefault() ?? config.UrlFonte,
            config.UrlFarmacias,
            DateOnly.FromDateTime(DateTime.UtcNow),
            ct);
    }

    private async Task<LeituraRemume> LerRemumeAsync(string url, CancellationToken ct)
    {
        var (destino, tipo, conteudo) = await BaixarAsync(url, "REMUME", ct);
        if (EhPdf(destino, tipo))
        {
            var assinatura = Convert.ToHexString(SHA256.HashData(conteudo.Bytes));
            var ehGrupo = destino.AbsolutePath.Contains("grupo", StringComparison.OrdinalIgnoreCase);
            return new LeituraRemume(
                assinatura,
                [destino.AbsoluteUri],
                ehGrupo ? null : conteudo.Bytes,
                ehGrupo ? conteudo.Bytes : null);
        }

        var html = conteudo.Texto ?? Encoding.UTF8.GetString(conteudo.Bytes);
        var links = ExtrairLinksPdf(html, destino);
        if (links.Count == 0)
            throw new InvalidOperationException("Não foi encontrado o PDF na página da REMUME. Confira o endereço.");

        byte[]? alfa = null;
        byte[]? grupo = null;
        var hashes = new List<string>();
        foreach (var link in links)
        {
            var pdf = await BaixarAsync(link, "REMUME", ct);
            hashes.Add(Convert.ToHexString(SHA256.HashData(pdf.Conteudo.Bytes)));
            if (link.Contains("grupo", StringComparison.OrdinalIgnoreCase))
                grupo = pdf.Conteudo.Bytes;
            else
                alfa ??= pdf.Conteudo.Bytes;
        }

        return new LeituraRemume(string.Join('\n', hashes), links, alfa, grupo);
    }

    private async Task<LeituraFarmacias> LerFarmaciasAsync(string url, CancellationToken ct)
    {
        var (destino, tipo, conteudo) = await BaixarAsync(url, "farmácias", ct);
        var html = EhPdf(destino, tipo) ? null : conteudo.Texto ?? Encoding.UTF8.GetString(conteudo.Bytes);
        var baseAssinatura = html is null
            ? Convert.ToHexString(SHA256.HashData(conteudo.Bytes))
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                ExtrairMarcadoresFarmacia(html) is { Count: > 0 } pecas
                    ? string.Join('\n', pecas)
                    : NormalizarTexto(html))));

        return new LeituraFarmacias(baseAssinatura, [destino.AbsoluteUri], html);
    }

    private async Task<(Uri Destino, string Tipo, ConteudoBaixado Conteudo)> BaixarAsync(
        string url,
        string rotulo,
        CancellationToken ct)
    {
        if (!UrlPermitida(url))
            throw new InvalidOperationException($"O endereço de {rotulo} não é uma fonte permitida da Prefeitura.");

        var cliente = _httpClientFactory.CreateClient("Sincronismo");
        using var resposta = await cliente.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resposta.IsSuccessStatusCode)
            throw new InvalidOperationException($"A fonte de {rotulo} respondeu {(int)resposta.StatusCode}.");

        var destino = resposta.RequestMessage?.RequestUri ?? new Uri(url);
        var tipo = resposta.Content.Headers.ContentType?.MediaType ?? "";
        var bytes = await LerComLimiteAsync(resposta, ct);
        var texto = EhPdf(destino, tipo) ? null : Encoding.UTF8.GetString(bytes);
        return (destino, tipo, new ConteudoBaixado(bytes, texto));
    }

    private static bool EhPdf(Uri destino, string tipo) =>
        tipo.Contains("pdf", StringComparison.OrdinalIgnoreCase)
        || destino.AbsolutePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    private static async Task<byte[]> LerComLimiteAsync(HttpResponseMessage resposta, CancellationToken ct)
    {
        const int limite = 5 * 1024 * 1024;
        await using var stream = await resposta.Content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var bloco = new byte[81920];
        int lidos;
        while ((lidos = await stream.ReadAsync(bloco, ct)) > 0)
        {
            if (buffer.Length + lidos > limite)
                throw new InvalidOperationException("O arquivo da fonte é grande demais para conferir daqui.");
            buffer.Write(bloco, 0, lidos);
        }

        return buffer.ToArray();
    }

    private static IReadOnlyList<string> ExtrairLinksPdf(string html, Uri baseUri)
    {
        var encontrados = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in HrefRegex.Matches(html))
        {
            var href = match.Groups[1].Value.Trim();
            if (href.Length == 0) continue;
            if (!href.Contains(".pdf", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!Uri.TryCreate(baseUri, href, out var absoluto))
                continue;
            if (absoluto.Scheme != Uri.UriSchemeHttps)
                continue;
            if (!HostsPermitidos.Contains(absoluto.Host))
                continue;

            encontrados.Add(absoluto.GetLeftPart(UriPartial.Path));
        }

        return encontrados.ToList();
    }

    private static IReadOnlyList<string> ExtrairMarcadoresFarmacia(string html)
    {
        var texto = NormalizarTexto(html);
        var encontrados = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Regex.Matches(texto, @"Farmácia\s+[^\.]{2,80}", RegexOptions.IgnoreCase))
            encontrados.Add(Regex.Replace(match.Value, @"\s+", " ").Trim());
        foreach (Match match in Regex.Matches(texto, @"Endereço:\s+[^\.]{5,120}", RegexOptions.IgnoreCase))
            encontrados.Add(Regex.Replace(match.Value, @"\s+", " ").Trim());
        return encontrados.ToList();
    }

    private static string NormalizarTexto(string html)
    {
        var texto = ScriptRegex.Replace(html, " ");
        texto = StyleRegex.Replace(texto, " ");
        texto = TagRegex.Replace(texto, " ");
        texto = WebUtility.HtmlDecode(texto);
        return Regex.Replace(texto, @"\s+", " ").Trim();
    }

    private sealed record LeituraRemume(string Assinatura, IReadOnlyList<string> Links, byte[]? Alfa, byte[]? Grupo);
    private sealed record LeituraFarmacias(string Assinatura, IReadOnlyList<string> Links, string? Html);
    private sealed record ConteudoBaixado(byte[] Bytes, string? Texto);
}
