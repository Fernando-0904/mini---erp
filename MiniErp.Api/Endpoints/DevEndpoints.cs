using MiniErp.Api.Services;

namespace MiniErp.Api.Endpoints;

internal static class DevEndpoints
{
    internal static void MapDevEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        app.MapGet("/dev/emails", (EmailSimuladoService emailService) =>
        {
            return Results.Ok(emailService.Listar());
        })
            .AllowAnonymous();
    }
}
