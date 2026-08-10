using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MiniErp.Api.Data;
using MiniErp.Api.Models;

namespace MiniErp.Api.Services;

public class AuditoriaService
{
    private readonly AppDbContext contexto;
    private readonly ILogger<AuditoriaService> logger;

    public AuditoriaService(AppDbContext contexto, ILogger<AuditoriaService> logger)
    {
        this.contexto = contexto;
        this.logger = logger;
    }

    public async Task RegistrarAsync(
        HttpContext httpContext,
        string acao,
        string entidade,
        string entidadeId,
        string descricao,
        object? dados = null)
    {
        (int? usuarioId, string usuarioEmail) = ExtrairUsuario(httpContext.User);

        AuditoriaEvento evento = new()
        {
            Acao = acao,
            Entidade = entidade,
            EntidadeId = entidadeId,
            Descricao = descricao,
            UsuarioId = usuarioId,
            UsuarioEmail = usuarioEmail,
            Dados = dados is null ? string.Empty : JsonSerializer.Serialize(dados),
            DataUtc = DateTime.UtcNow
        };

        contexto.AuditoriaEventos.Add(evento);
        await contexto.SaveChangesAsync();
    }

    public async Task RegistrarSemImpactarFluxoAsync(
        HttpContext httpContext,
        string acao,
        string entidade,
        string entidadeId,
        string descricao,
        object? dados = null)
    {
        try
        {
            await RegistrarAsync(httpContext, acao, entidade, entidadeId, descricao, dados);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Falha ao registrar auditoria ({Acao}/{Entidade}/{EntidadeId}).",
                acao,
                entidade,
                entidadeId);
        }
    }

    private static (int? UsuarioId, string UsuarioEmail) ExtrairUsuario(ClaimsPrincipal usuario)
    {
        string? idClaim = usuario.FindFirstValue(ClaimTypes.NameIdentifier);
        int? usuarioId = int.TryParse(idClaim, out int id) ? id : null;

        string usuarioEmail = usuario.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        return (usuarioId, usuarioEmail);
    }
}
