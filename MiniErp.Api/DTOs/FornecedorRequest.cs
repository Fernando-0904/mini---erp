namespace MiniErp.Api.DTOs;

public class FornecedorRequest
{
    public int Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}
