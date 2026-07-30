namespace MiniErp.Api.Security;

public static class AntiforgeryEndpointExtensions
{
    public static RouteHandlerBuilder RequireAntiforgery(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter<AntiforgeryValidationFilter>();
    }
}
