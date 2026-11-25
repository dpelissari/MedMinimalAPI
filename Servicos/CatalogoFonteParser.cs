using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PiraRemumeApi.Servicos;

public static class CatalogoFonteParser
{
    private static readonly HashSet<string> Cabecalhos = new(StringComparer.OrdinalIgnoreCase)
    {
        "medicamento", "forma", "farmacêutica", "farmaceutica", "apresentação", "apresentacao",
        "disponibilidade", "classe", "terapêutica", "terapeutica", "e"
    };

    public static List<CatalogoMedicamentoLido> ParseRemumeAlfabetica(byte[] pdf)
    {
        var linhas = LerLinhas(pdf, corteNome: 200, corteMeio: 370);
        var itens = new List<CatalogoMedicamentoLido>();
        CatalogoMedicamentoLido? atual = null;

        foreach (var linha in linhas)
        {
            if (EhLixo(linha.Nome, linha.Meio, linha.Direita))
                continue;

            if (!string.IsNullOrWhiteSpace(linha.Nome))
            {
                var nome = LimparEspacos(linha.Nome);
                if (atual is not null && NomeIncompleto(atual.Nome))
                {
                    atual.Nome = LimparEspacos($"{atual.Nome} {nome}");
                }
                else
                {
                    atual = new CatalogoMedicamentoLido(nome, []);
                    itens.Add(atual);
                }
            }

            if (atual is null)
                continue;

            if (!string.IsNullOrWhiteSpace(linha.Meio))
            {
                foreach (var descricao in PartirApresentacoes(linha.Meio))
                    atual.Apresentacoes.Add(new CatalogoApresentacaoLida(descricao));
            }

            if (!string.IsNullOrWhiteSpace(linha.Direita) && atual.Apresentacoes.Count > 0)
                AplicarDisponibilidade(atual.Apresentacoes[^1], linha.Direita);
        }

        return itens
            .Where(i => i.Apresentacoes.Count > 0 && i.Nome.Length > 2)
            .GroupBy(i => i.Nome, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var item = g.First();
                foreach (var extra in g.Skip(1))
                    item.Apresentacoes.AddRange(extra.Apresentacoes);
                return item;
            })
            .ToList();
    }

    public static Dictionary<string, CatalogoGrupoLido> ParseRemumeGrupos(byte[] pdf)
    {
        var linhas = LerLinhas(pdf, corteNome: 130, corteMeio: 310, corteDireita: 445);
        var mapa = new Dictionary<string, CatalogoGrupoLido>(StringComparer.OrdinalIgnoreCase);
        var grupo = "Sem classificação";
        string? ultimoNome = null;

        foreach (var linha in linhas)
        {
            var textoLinha = LimparEspacos(string.Join(" ", new[] { linha.Nome, linha.Meio, linha.Direita, linha.Extra }
                .Where(p => !string.IsNullOrWhiteSpace(p))));

            if (Regex.IsMatch(textoLinha, @"^\d+\.\s+\S"))
            {
                grupo = Regex.Replace(textoLinha, @"^\d+\.\s*", "").Trim();
                ultimoNome = null;
                continue;
            }

            if (EhLixo(linha.Nome, linha.Meio, linha.Direita))
                continue;

            if (!string.IsNullOrWhiteSpace(linha.Nome))
            {
                var nome = LimparEspacos(linha.Nome);
                if (ultimoNome is not null && NomeIncompleto(ultimoNome))
                {
                    var fundido = LimparEspacos($"{ultimoNome} {nome}");
                    if (mapa.Remove(ultimoNome, out var anterior))
                        mapa[fundido] = anterior with { Grupo = grupo };
                    nome = fundido;
                }

                if (nome.Length < 3)
                    continue;

                var classe = string.IsNullOrWhiteSpace(linha.Extra) ? "Não informada" : LimparEspacos(linha.Extra);
                mapa[nome] = new CatalogoGrupoLido(grupo, classe);
                ultimoNome = nome;
                continue;
            }

            if (ultimoNome is not null
                && mapa.TryGetValue(ultimoNome, out var atual)
                && !string.IsNullOrWhiteSpace(linha.Extra))
            {
                var classe = atual.Classe == "Não informada"
                    ? LimparEspacos(linha.Extra)
                    : LimparEspacos($"{atual.Classe} {linha.Extra}");
                mapa[ultimoNome] = atual with { Classe = classe };
            }
        }

        return mapa;
    }

    public static CatalogoGrupoLido? BuscarGrupo(
        IReadOnlyDictionary<string, CatalogoGrupoLido> grupos,
        string nome)
    {
        if (grupos.TryGetValue(nome, out var direto))
            return direto;

        var chave = NormalizarChave(nome);
        foreach (var par in grupos)
        {
            if (NormalizarChave(par.Key) == chave)
                return par.Value;
        }

        return null;
    }

    public static List<CatalogoFarmaciaLida> ParseFarmaciasHtml(string html)
    {
        var titulos = Regex.Matches(
            html,
            @"<strong>\s*(Farmácia\s+[^<]+)</strong>",
            RegexOptions.IgnoreCase);

        var itens = new List<CatalogoFarmaciaLida>();
        for (var i = 0; i < titulos.Count; i++)
        {
            var titulo = WebUtility.HtmlDecode(titulos[i].Groups[1].Value).Trim();
            if (titulo.StartsWith("Farmácias Municipais", StringComparison.OrdinalIgnoreCase))
                continue;

            var inicio = titulos[i].Index + titulos[i].Length;
            var fim = i + 1 < titulos.Count ? titulos[i + 1].Index : html.Length;
            var bloco = NormalizarHtml(html[inicio..fim]);
            var lida = LerFarmacia(titulo, bloco);
            if (lida is not null)
                itens.Add(lida);
        }

        return itens;
    }

    private static CatalogoFarmaciaLida? LerFarmacia(string titulo, string bloco)
    {
        var zonaRural = titulo.Contains("Zona Rural", StringComparison.OrdinalIgnoreCase);
        var nomeBruto = Regex.Replace(titulo, @"\s*[–-]\s*Zona Rural", "", RegexOptions.IgnoreCase).Trim();
        nomeBruto = Regex.Replace(nomeBruto, @"^Farmácia\s+", "", RegexOptions.IgnoreCase).Trim();
        if (nomeBruto.Length is < 3 or > 80)
            return null;

        var tipo = nomeBruto.Contains("alto custo", StringComparison.OrdinalIgnoreCase) ? "AltoCusto"
            : nomeBruto.Contains("cedic", StringComparison.OrdinalIgnoreCase) ? "Cedic"
            : "AtencaoBasica";

        var nome = tipo switch
        {
            "AltoCusto" => "Farmácia de Alto Custo",
            "Cedic" => "Farmácia do CEDIC",
            _ => "Farmácia " + nomeBruto
        };

        var horario = Campo(bloco, @"Dias e horário de funcionamento\s*:\s*(.+?)(?=\s+Endereço|\s+Contato|\s+Objeto|$)") ?? "";
        horario = Regex.Replace(horario, @"\s+das das\s+", " das ", RegexOptions.IgnoreCase);
        horario = LimparEspacos(horario).Trim().TrimEnd('.');

        var enderecoBruto = Campo(bloco, @"Endereço\s*:\s*(.+?)(?=\s+Contato|\s+Unidades|\s+Objeto|$)") ?? "";
        var (endereco, bairro) = SepararEndereco(enderecoBruto);
        if (string.IsNullOrWhiteSpace(endereco))
            return null;

        var contato = Campo(bloco, @"Contato\s*:\s*(.+?)(?=\s+Unidades|\s+Objeto|$)") ?? "";
        var whatsapp = contato.Contains("whatsapp", StringComparison.OrdinalIgnoreCase);
        var telefoneMatch = Regex.Match(contato, @"\d{4}-?\d{4}");
        var telefone = telefoneMatch.Success ? telefoneMatch.Value : null;

        var refsBruto = Campo(bloco, @"Unidades? de Referência para Atendimento\s*:\s*(.+)$") ?? "";
        var referencias = refsBruto
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(LimparEspacos)
            .Where(x => x.Length > 1)
            .ToList();

        return new CatalogoFarmaciaLida(
            Slugificar(nome),
            LimparEspacos(nome),
            tipo,
            zonaRural,
            endereco,
            bairro,
            telefone,
            whatsapp,
            horario,
            referencias);
    }

    private static List<LinhaPdf> LerLinhas(
        byte[] pdf,
        double corteNome,
        double corteMeio,
        double? corteDireita = null)
    {
        using var doc = PdfDocument.Open(pdf);
        var linhas = new List<LinhaPdf>();
        foreach (var page in doc.GetPages())
        {
            foreach (var grupo in page.GetWords()
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom * 2) / 2)
                .OrderByDescending(g => g.Key))
            {
                var palavras = grupo.OrderBy(w => w.BoundingBox.Left).ToList();
                var nome = Juntar(palavras.Where(w => w.BoundingBox.Left < corteNome));
                var meio = Juntar(palavras.Where(w =>
                    w.BoundingBox.Left >= corteNome
                    && w.BoundingBox.Left < (corteDireita ?? corteMeio)));
                var direita = Juntar(palavras.Where(w =>
                    corteDireita is null
                        ? w.BoundingBox.Left >= corteMeio
                        : w.BoundingBox.Left >= corteMeio && w.BoundingBox.Left < corteDireita.Value));
                var extra = corteDireita is null
                    ? ""
                    : Juntar(palavras.Where(w => w.BoundingBox.Left >= corteDireita.Value));

                if (string.IsNullOrWhiteSpace(nome + meio + direita + extra))
                    continue;
                linhas.Add(new LinhaPdf(nome, meio, direita, extra));
            }
        }

        return FundirDisponibilidade(linhas);
    }

    private static List<LinhaPdf> FundirDisponibilidade(List<LinhaPdf> linhas)
    {
        var fused = new List<LinhaPdf>();
        foreach (var linha in linhas)
        {
            if (fused.Count > 0
                && string.IsNullOrWhiteSpace(linha.Nome)
                && string.IsNullOrWhiteSpace(linha.Meio)
                && (!string.IsNullOrWhiteSpace(linha.Direita) || !string.IsNullOrWhiteSpace(linha.Extra)))
            {
                var prev = fused[^1];
                fused[^1] = prev with
                {
                    Direita = JuntarTexto(prev.Direita, linha.Direita),
                    Extra = JuntarTexto(prev.Extra, linha.Extra)
                };
                continue;
            }

            fused.Add(linha);
        }

        return fused;
    }

    private static void AplicarDisponibilidade(CatalogoApresentacaoLida apres, string texto)
    {
        var t = LimparEspacos(texto);
        if (t.StartsWith("Todas", StringComparison.OrdinalIgnoreCase))
            return;

        if (!t.Contains("Restrito", StringComparison.OrdinalIgnoreCase))
        {
            apres.Descricao = JuntarTexto(apres.Descricao, t);
            return;
        }

        apres.Abrangencia = "Restrito";
        var obs = Regex.Match(t, @"\(([^)]+)\)");
        if (obs.Success)
            apres.Observacao = LimparEspacos(obs.Groups[1].Value);

        if (t.Contains("CEDIC", StringComparison.OrdinalIgnoreCase))
        {
            apres.Unidades = ["CEDIC"];
            return;
        }

        var lista = Regex.Match(t, @"farmácias?:\s*(.+)$", RegexOptions.IgnoreCase);
        var trecho = lista.Success ? lista.Groups[1].Value : t;
        trecho = Regex.Replace(trecho, @"\([^)]*\)", " ");
        trecho = Regex.Replace(trecho, @"\s+e\s+", ", ", RegexOptions.IgnoreCase);
        apres.Unidades = trecho
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 1 && !x.StartsWith("Restrito", StringComparison.OrdinalIgnoreCase))
            .Select(LimparEspacos)
            .ToList();
    }

    private static IEnumerable<string> PartirApresentacoes(string meio)
    {
        var texto = LimparEspacos(meio);
        var partes = Regex.Split(texto, @"(?<=cápsula|comprimido|drágea|gotas|xarope|creme|pomada|loção|injetável)\s+(?=\d)");
        return partes.Select(LimparEspacos).Where(p => p.Length > 1);
    }

    private static bool NomeIncompleto(string nome) =>
        nome.EndsWith('+') || nome.EndsWith(',')
        || nome.EndsWith(" de", StringComparison.OrdinalIgnoreCase)
        || nome.EndsWith(" e", StringComparison.OrdinalIgnoreCase)
        || nome.EndsWith(" +", StringComparison.OrdinalIgnoreCase);

    private static bool EhLixo(string nome, string meio, string direita)
    {
        var t = $"{nome} {meio} {direita}";
        if (t.Contains("RELAÇÃO MUNICIPAL", StringComparison.OrdinalIgnoreCase)) return true;
        if (t.Contains("COMPONENTE BÁSICO", StringComparison.OrdinalIgnoreCase)) return true;
        if (t.Contains("POR ORDEM", StringComparison.OrdinalIgnoreCase)) return true;
        if (t.Contains("POR GRUPO", StringComparison.OrdinalIgnoreCase)) return true;
        var tokens = t.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length > 0 && tokens.All(p => Cabecalhos.Contains(p.TrimEnd('/')));
    }

    private static string Juntar(IEnumerable<Word> palavras) =>
        string.Join(" ", palavras.Select(p => p.Text)).Trim();

    private static string JuntarTexto(string a, string b) =>
        string.Join(" ", new[] { a, b }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

    private static string LimparEspacos(string texto) =>
        Regex.Replace(texto.Trim(), @"\s+", " ");

    private static string NormalizarHtml(string html)
    {
        html = Regex.Replace(html, "<script[\\s\\S]*?</script>", " ", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, "<style[\\s\\S]*?</style>", " ", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, "<[^>]+>", " ");
        html = WebUtility.HtmlDecode(html);
        return LimparEspacos(html);
    }

    private static string? Campo(string bloco, string padrao)
    {
        var m = Regex.Match(bloco, padrao, RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static (string Endereco, string Bairro) SepararEndereco(string bruto)
    {
        bruto = LimparEspacos(WebUtility.HtmlDecode(bruto)).Trim().TrimEnd('.');
        var partes = Regex.Split(bruto, @"\s+[–-]\s+");
        if (partes.Length >= 2)
            return (partes[0].Trim(), partes[^1].Replace("Piracicaba", "", StringComparison.OrdinalIgnoreCase).Trim().Trim(','));
        return (bruto, "");
    }

    public static string Slugificar(string nome)
    {
        var t = Regex.Replace(nome, @"Farmácia\s+(do\s+)?", "", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, @"\([^)]*\)", " ");
        t = Regex.Replace(t, @"([a-z])([A-ZÁÉÍÓÚÂÊÔÃÕ])", "$1 $2");
        t = t.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(t.Length);
        foreach (var c in t.ToLowerInvariant())
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(c is >= 'a' and <= 'z' or >= '0' and <= '9' ? c : '-');
        }

        return Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
    }

    public static string NormalizarChave(string texto)
    {
        var t = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(t.Length);
        foreach (var c in t.ToLowerInvariant())
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c)) sb.Append(c);
        }

        return sb.ToString();
    }

    private sealed record LinhaPdf(string Nome, string Meio, string Direita, string Extra);
}

public sealed class CatalogoMedicamentoLido
{
    public CatalogoMedicamentoLido(string nome, List<CatalogoApresentacaoLida> apresentacoes)
    {
        Nome = nome;
        Apresentacoes = apresentacoes;
    }

    public string Nome { get; set; }
    public List<CatalogoApresentacaoLida> Apresentacoes { get; }
}

public sealed class CatalogoApresentacaoLida
{
    public CatalogoApresentacaoLida(string descricao) => Descricao = descricao;

    public string Descricao { get; set; }
    public string? Abrangencia { get; set; }
    public string? Observacao { get; set; }
    public List<string>? Unidades { get; set; }
}

public sealed record CatalogoGrupoLido(string Grupo, string Classe);

public sealed record CatalogoFarmaciaLida(
    string Slug,
    string Nome,
    string Tipo,
    bool ZonaRural,
    string Endereco,
    string Bairro,
    string? Telefone,
    bool Whatsapp,
    string Horario,
    IReadOnlyList<string> UnidadesReferencia);
