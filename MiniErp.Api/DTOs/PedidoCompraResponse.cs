namespace MiniErp.Api.DTOs;

public class PedidoCompraResponse
{
    public int Id { get; set; }
    public int FornecedorId { get; set; }
    public string FornecedorNome { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CriadoEmUtc { get; set; }
    public DateTime? RecebidoEmUtc { get; set; }
    public decimal ValorTotal { get; set; }
    public List<PedidoCompraItemResponse> Itens { get; set; } = [];
}

public class PedidoCompraItemResponse
{
    public int ProdutoCodigo { get; set; }
    public string ProdutoNome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal ValorTotal { get; set; }
}
