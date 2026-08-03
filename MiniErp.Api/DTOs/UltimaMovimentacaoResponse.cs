namespace MiniErp.Api.DTOs;

public class UltimaMovimentacaoResponse
{
    public string Produto { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public int SaldoAnterior { get; set; }
    public int SaldoNovo { get; set; }
    public DateTime DataMovimentacaoUtc { get; set; }
}