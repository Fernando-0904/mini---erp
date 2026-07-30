namespace MiniErp.Api.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public byte[] SenhaHash { get; set; } = [];
    public byte[] SenhaSalt { get; set; } = [];
    public bool EmailConfirmado { get; set; }
    public DateTime? EmailConfirmadoEmUtc { get; set; }
    public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;
}
