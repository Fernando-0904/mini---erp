namespace MiniErp.Api.Models;

public class TokenUsuario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public TokenUsuarioTipo Tipo { get; set; }
    public byte[] TokenHash { get; set; } = [];
    public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiraEmUtc { get; set; }
    public DateTime? UsadoEmUtc { get; set; }
}
