namespace MiniErp.Api.Models;

public class PedidoCompraItem
{
    public int Id { get; set; }
    public int PedidoCompraId { get; set; }
    public PedidoCompra? PedidoCompra { get; set; }
    public int ProdutoCodigo { get; set; }
    public Produto? Produto { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}
