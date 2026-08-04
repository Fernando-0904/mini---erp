using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

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
            ProblemDetails problemDetails = new()
            {
                Title = "Token de segurança inválido.",
                Detail = "Token de segurança ausente ou inválido.",
                Status = StatusCodes.Status400BadRequest,
                Type = "https://httpstatuses.com/400",
                Instance = context.HttpContext.Request.Path
            };
            problemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;

            return Results.Problem(
                detail: problemDetails.Detail,
                statusCode: problemDetails.Status,
                title: problemDetails.Title,
                type: problemDetails.Type,
                instance: problemDetails.Instance,
                extensions: problemDetails.Extensions);
        }

        return await next(context);
    }
}
