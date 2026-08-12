namespace MiniErp.Api.Models;

public class PedidoCompra
{
    public int Id { get; set; }
    public int FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }
    public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RejeitadoEmUtc { get; set; }
    public DateTime? RecebidoEmUtc { get; set; }
    public string? MotivoRejeicao { get; set; }
    public PedidoCompraStatus Status { get; set; } = PedidoCompraStatus.Aberto;
    public List<PedidoCompraItem> Itens { get; set; } = [];
}
