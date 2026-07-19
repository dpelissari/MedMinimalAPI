using MedMinimalApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MedMinimalApi.Extensions;

public static class PaginacaoExtensions
{
    public static async Task<ResultadoPaginado<T>> PaginarAsync<T>(
        this IQueryable<T> query,
        Paginacao p)
    {
        var pagina = p.Pagina < 1 ? 1 : p.Pagina;
        var tamanho = p.Tamanho is < 1 or > 50 ? 20 : p.Tamanho;

        var total = await query.CountAsync();
        var itens = await query
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync();

        return new ResultadoPaginado<T>(itens, pagina, tamanho, total);
    }
}
