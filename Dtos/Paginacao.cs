namespace MedMinimalApi.Dtos;

public record Paginacao(int Pagina = 1, int Tamanho = 20);

public record ResultadoPaginado<T>(IReadOnlyList<T> Itens, int Pagina, int Tamanho, int Total)
{
    public int TotalPaginas => (int)Math.Ceiling(Total / (double)Tamanho); // Math.Ceiling: Arredonda pra cima
}
