using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace PiraRemumeApi.Data;

public static class RemumeSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task SeedAsync(MedicamentoDb db, IWebHostEnvironment env, bool forcar = false)
    {
        var pasta = Path.Combine(env.ContentRootPath, "Data");
        var caminhoItens = Path.Combine(pasta, "remume-piracicaba.json");
        var caminhoGrupos = Path.Combine(pasta, "remume-grupos.json");
        var caminhoFarmacias = Path.Combine(pasta, "farmacias-piracicaba.json");

        if (!File.Exists(caminhoItens))
            throw new FileNotFoundException("Arquivo da REMUME não encontrado.", caminhoItens);
        if (!File.Exists(caminhoGrupos))
            throw new FileNotFoundException("Arquivo de grupos da REMUME não encontrado.", caminhoGrupos);
        if (!File.Exists(caminhoFarmacias))
            throw new FileNotFoundException("Arquivo de farmácias municipais não encontrado.", caminhoFarmacias);

        var arquivoFarmacias = JsonSerializer.Deserialize<FarmaciasArquivo>(
            await File.ReadAllTextAsync(caminhoFarmacias), JsonOptions)
            ?? throw new InvalidOperationException("Não foi possível ler as farmácias municipais.");

        var farmaciasDesatualizadas = await db.FarmaciasMunicipais.CountAsync() != arquivoFarmacias.Itens.Count
            || await db.FarmaciasMunicipais.AnyAsync(f => string.IsNullOrWhiteSpace(f.Endereco));

        var precisaReseed = forcar
            || !await db.Medicamentos.AnyAsync()
            || await db.Medicamentos.AnyAsync(m => string.IsNullOrWhiteSpace(m.GrupoFarmacologico))
            || farmaciasDesatualizadas;

        if (!precisaReseed)
            return;

        db.Medicamentos.RemoveRange(db.Medicamentos);
        db.FarmaciasMunicipais.RemoveRange(db.FarmaciasMunicipais);
        await db.SaveChangesAsync();

        var farmacias = SeedFarmacias(db, arquivoFarmacias);

        var arquivo = JsonSerializer.Deserialize<RemumeArquivo>(
            await File.ReadAllTextAsync(caminhoItens), JsonOptions)
            ?? throw new InvalidOperationException("Não foi possível ler a REMUME.");

        var grupos = JsonSerializer.Deserialize<Dictionary<string, RemumeGrupo>>(
            await File.ReadAllTextAsync(caminhoGrupos), JsonOptions)
            ?? throw new InvalidOperationException("Não foi possível ler os grupos da REMUME.");

        var gruposPorNome = new Dictionary<string, RemumeGrupo>(grupos, StringComparer.OrdinalIgnoreCase);

        foreach (var item in arquivo.Itens)
        {
            gruposPorNome.TryGetValue(item.Nome.Trim(), out var classificacao);

            var medicamento = new Medicamento
            {
                Id = Guid.NewGuid(),
                Nome = item.Nome.Trim(),
                GrupoFarmacologico = string.IsNullOrWhiteSpace(classificacao?.Grupo) ? "Sem classificação" : classificacao.Grupo.Trim(),
                ClasseTerapeutica = string.IsNullOrWhiteSpace(classificacao?.Classe) ? "Não informada" : classificacao.Classe.Trim()
            };

            foreach (var linha in item.Apresentacoes)
            {
                var restrito = string.Equals(linha.Abrangencia, "Restrito", StringComparison.OrdinalIgnoreCase);
                var apresentacao = new Apresentacao
                {
                    Id = Guid.NewGuid(),
                    Descricao = linha.Descricao.Trim(),
                    Abrangencia = restrito
                        ? AbrangenciaDisponibilidade.Restrito
                        : AbrangenciaDisponibilidade.TodasAsFarmaciasMunicipais,
                    Observacao = string.IsNullOrWhiteSpace(linha.Observacao) ? null : linha.Observacao.Trim()
                };

                if (restrito)
                {
                    foreach (var nomeUnidade in linha.Unidades ?? [])
                    {
                        var nome = nomeUnidade.Trim();
                        if (nome.Length == 0) continue;

                        if (!farmacias.TryGetValue(nome, out var farmacia))
                            throw new InvalidOperationException($"Farmácia municipal não encontrada para '{nome}'.");

                        apresentacao.Unidades.Add(farmacia);
                    }
                }

                medicamento.Apresentacoes.Add(apresentacao);
            }

            db.Medicamentos.Add(medicamento);
        }

        await db.SaveChangesAsync();
    }

    private static Dictionary<string, FarmaciaMunicipal> SeedFarmacias(
        MedicamentoDb db,
        FarmaciasArquivo arquivo)
    {
        var indice = new Dictionary<string, FarmaciaMunicipal>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in arquivo.Itens)
        {
            if (!Enum.TryParse<TipoFarmacia>(item.Tipo, ignoreCase: true, out var tipo))
                throw new InvalidOperationException($"Tipo de farmácia inválido: '{item.Tipo}'.");

            var farmacia = new FarmaciaMunicipal
            {
                Id = Guid.NewGuid(),
                Slug = item.Slug.Trim(),
                Nome = item.Nome.Trim(),
                Tipo = tipo,
                ZonaRural = item.ZonaRural,
                Endereco = item.Endereco.Trim(),
                Bairro = item.Bairro.Trim(),
                Telefone = string.IsNullOrWhiteSpace(item.Telefone) ? null : item.Telefone.Trim(),
                Whatsapp = item.Whatsapp,
                Horario = item.Horario.Trim(),
                UnidadesReferencia = item.UnidadesReferencia?.Select(u => u.Trim()).Where(u => u.Length > 0).ToArray() ?? [],
                Aliases = item.Aliases?.Select(a => a.Trim()).Where(a => a.Length > 0).ToArray() ?? [],
                Observacao = string.IsNullOrWhiteSpace(item.Observacao) ? null : item.Observacao.Trim()
            };

            db.FarmaciasMunicipais.Add(farmacia);
            Indexar(indice, farmacia.Slug, farmacia);
            Indexar(indice, farmacia.Nome, farmacia);
            foreach (var alias in farmacia.Aliases)
                Indexar(indice, alias, farmacia);
        }

        return indice;
    }

    private static void Indexar(Dictionary<string, FarmaciaMunicipal> indice, string chave, FarmaciaMunicipal farmacia)
    {
        indice[chave] = farmacia;
    }

    private sealed record RemumeArquivo(string Nome, string PublicadoEm, string Url, List<RemumeItem> Itens);
    private sealed record RemumeItem(string Nome, List<RemumeApresentacao> Apresentacoes);
    private sealed record RemumeApresentacao(string Descricao, string? Abrangencia, string? Observacao, List<string>? Unidades);
    private sealed record RemumeGrupo(string Grupo, string Classe);
    private sealed record FarmaciasArquivo(string Nome, string PublicadoEm, string Url, List<FarmaciaArquivo> Itens);
    private sealed record FarmaciaArquivo(
        string Slug,
        string Nome,
        string Tipo,
        bool ZonaRural,
        string Endereco,
        string Bairro,
        string? Telefone,
        bool Whatsapp,
        string Horario,
        List<string>? UnidadesReferencia,
        List<string>? Aliases,
        string? Observacao);
}
