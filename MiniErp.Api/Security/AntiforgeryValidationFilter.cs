using Microsoft.AspNetCore.Antiforgery;

namespace MiniErp.Api.Security;

public class AntiforgeryValidationFilter : IEndpointFilter
{
    private readonly IAntiforgery antiforgery;

    public AntiforgeryValidationFilter(IAntiforgery antiforgery)
    {
        this.antiforgery = antiforgery;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.BadRequest("Token de segurança ausente ou inválido.");
        }

        return await next(context);
    }
}
