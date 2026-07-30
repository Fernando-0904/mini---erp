namespace MiniErp.Api.Services;

public sealed class EmailSimulado
{
    public string Para { get; set; } = string.Empty;
    public string Assunto { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public DateTime ExpiraEmUtc { get; set; }
    public DateTime CriadoEmUtc { get; set; }
}
