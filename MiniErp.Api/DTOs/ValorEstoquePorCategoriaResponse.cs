namespace MiniErp.Api.DTOs;

public class ValorEstoquePorCategoriaResponse
{
    public string Categoria { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
}