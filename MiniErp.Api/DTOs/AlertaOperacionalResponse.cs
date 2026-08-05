namespace MiniErp.Api.DTOs;

public class AlertaOperacionalResponse
{
    public string Prioridade { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Produto { get; set; } = string.Empty;
    public string Detalhe { get; set; } = string.Empty;
    public AlertaOperacionalAcaoResponse Acao { get; set; } = new();
}

public class AlertaOperacionalAcaoResponse
{
    public string Label { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
}