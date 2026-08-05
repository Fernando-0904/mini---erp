namespace MiniErp.Api.Models;

public class AuditoriaEvento
{
    public int Id { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string Entidade { get; set; } = string.Empty;
    public string EntidadeId { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int? UsuarioId { get; set; }
    public string UsuarioEmail { get; set; } = string.Empty;
    public string Dados { get; set; } = string.Empty;
    public DateTime DataUtc { get; set; } = DateTime.UtcNow;
}