namespace MiniErp.Api.DTOs;

public class PedidoCompraRequest
{
    public int FornecedorId { get; set; }
    public List<PedidoCompraItemRequest> Itens { get; set; } = [];
}

public class PedidoCompraItemRequest
{
    public int ProdutoCodigo { get; set; }
    public int Quantidade { get; set; }
}
