namespace MiniErp.Api.Models;

public class Produto
{
    public int Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal PrecoUnitario { get; set; }
    public int QuantidadeEstoque { get; set; }
    public int EstoqueMinimo { get; set; }
    public Categoria? Categoria { get; set; }
    public int CategoriaId { get; set; }
    public Fornecedor? Fornecedor { get; set; }
    public int? FornecedorId { get; set; }

    public decimal CalcularValorTotal()
    {
        return PrecoUnitario * QuantidadeEstoque;
    }
}
