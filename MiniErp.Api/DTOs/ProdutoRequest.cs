namespace MiniErp.Api.DTOs;

public class ProdutoRequest
{
    public int Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal PrecoUnitario { get; set; }
    public int QuantidadeEstoque { get; set; }
    public int EstoqueMinimo { get; set; }
    public int CategoriaId { get; set; }
    public int? FornecedorId { get; set; }
}