namespace projetoerp;

public class Produto
{
    public int Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal PrecoUnitario { get; set; }
    public int QuantidadeEstoque { get; set; }

    public decimal CalcularValorTotal()
    {
        return PrecoUnitario * QuantidadeEstoque;
    }
}
