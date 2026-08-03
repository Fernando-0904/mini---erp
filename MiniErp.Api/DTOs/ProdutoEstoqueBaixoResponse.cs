namespace MiniErp.Api.DTOs;

public class ProdutoEstoqueBaixoResponse
{
    public int Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int QuantidadeEstoque { get; set; }
    public int EstoqueMinimo { get; set; }
    public string Categoria { get; set; } = string.Empty;
}