namespace MiniErp.Api.DTOs;

public class ProdutoSemEstoqueResponse
{
    public int Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int QuantidadeEstoque { get; set; }
    public string Categoria { get; set; } = string.Empty;
}