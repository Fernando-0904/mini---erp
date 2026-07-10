namespace MiniErp.Api.Models;

public enum TipoMovimentacaoEstoque
{
    Entrada = 1,
    Saida = 2,
}

public class MovimentacaoEstoque
{
    public int Id { get; set; }
    public int ProdutoCodigo { get; set; }
    public Produto? Produto { get; set; }
    public TipoMovimentacaoEstoque Tipo { get; set; }
    public int Quantidade { get; set; }
    public int SaldoAnterior { get; set; }
    public int SaldoNovo { get; set; }
    public DateTime DataMovimentacaoUtc { get; set; } = DateTime.UtcNow;
}
