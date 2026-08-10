using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MiniErp.Api.DTOs;
using MiniErp.Api.Security;
using MiniErp.Api.Services;

namespace MiniErp.Api.Endpoints;

internal static class AuthEndpoints
{
    internal static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapGet("/auth/csrf", (HttpContext context, IAntiforgery antiforgery) =>
        {
            AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(new { token = tokens.RequestToken });
        })
            .AllowAnonymous();

        app.MapPost("/auth/cadastro", async (CadastroUsuarioRequest request, UsuarioLocalService usuarioService, HttpContext context) =>
        {
            (UsuarioResponse? usuario, string erro) = await usuarioService.CadastrarAsync(
                request.Nome,
                request.Email,
                request.Senha);

            if (usuario is null)
            {
                return erro == "Já existe uma conta com este e-mail."
                    ? ApiHttpHelpers.CriarProblem(context, StatusCodes.Status409Conflict, "Conflito de dados.", erro)
                    : ApiHttpHelpers.CriarProblem(context, StatusCodes.Status400BadRequest, "Dados inválidos.", erro);
            }

            context.Response.Headers.CacheControl = "no-store";
            return Results.Json(new
            {
                usuario.Email,
                mensagem = "Conta criada. Confirme seu e-mail para entrar."
            }, statusCode: StatusCodes.Status201Created);
        })
            .AllowAnonymous()
            .RequireAntiforgery();

        app.MapPost("/auth/login", async (LoginRequest request, UsuarioLocalService usuarioService, AuditoriaService auditoriaService, HttpContext context) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
            {
                await auditoriaService.RegistrarSemImpactarFluxoAsync(
                    context,
                    "LoginInvalido",
                    "Auth",
                    "n/a",
                    "Tentativa de login sem e-mail ou senha.",
                    new
                    {
                        EmailInformado = request.Email,
                        Ip = context.Connection.RemoteIpAddress?.ToString()
                    });

                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Dados inválidos.",
                    "E-mail e senha são obrigatórios.");
            }

            string? ipAddress = context.Connection.RemoteIpAddress?.ToString();
            ResultadoAutenticacao autenticacao = usuarioService.Autenticar(request.Email, request.Senha, ipAddress);

            if (autenticacao.TentativaBloqueada)
            {
                context.Response.Headers.RetryAfter = autenticacao.RetryAfterSeconds.ToString();
                await auditoriaService.RegistrarSemImpactarFluxoAsync(
                    context,
                    "LoginBloqueado",
                    "Auth",
                    request.Email.Trim(),
                    "Tentativa de login bloqueada por excesso de falhas.",
                    new
                    {
                        EmailInformado = request.Email,
                        Ip = ipAddress,
                        autenticacao.RetryAfterSeconds
                    });

                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status429TooManyRequests,
                    "Acesso temporariamente bloqueado.",
                    "Detectamos muitas tentativas de acesso. Aguarde alguns minutos e tente novamente.");
            }

            if (autenticacao.EmailNaoConfirmado)
            {
                await auditoriaService.RegistrarSemImpactarFluxoAsync(
                    context,
                    "LoginPendenteConfirmacao",
                    "Auth",
                    request.Email.Trim(),
                    "Tentativa de login com e-mail ainda não confirmado.",
                    new
                    {
                        EmailInformado = request.Email,
                        Ip = ipAddress
                    });

                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Confirmação pendente.",
                    "Confirme seu e-mail antes de entrar.");
            }

            if (autenticacao.Usuario is null)
            {
                await auditoriaService.RegistrarSemImpactarFluxoAsync(
                    context,
                    "LoginFalhou",
                    "Auth",
                    request.Email.Trim(),
                    "Falha de login por credenciais inválidas.",
                    new
                    {
                        EmailInformado = request.Email,
                        Ip = ipAddress
                    });

                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "Não autenticado.",
                    "E-mail ou senha inválidos.");
            }

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                ApiHttpHelpers.CriarPrincipal(autenticacao.Usuario),
                ApiHttpHelpers.CriarPropriedadesAutenticacao());

            await auditoriaService.RegistrarSemImpactarFluxoAsync(
                context,
                "LoginSucesso",
                "Auth",
                autenticacao.Usuario.Id.ToString(),
                "Login realizado com sucesso.",
                new
                {
                    EmailInformado = request.Email,
                    autenticacao.Usuario.Email,
                    Ip = ipAddress
                });

            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(autenticacao.Usuario);
        })
            .AllowAnonymous()
            .RequireAntiforgery();

        app.MapPost("/auth/confirmar-email", (ConfirmarEmailRequest request, UsuarioLocalService usuarioService, HttpContext context) =>
        {
            bool confirmado = usuarioService.ConfirmarEmail(request.Token);
            context.Response.Headers.CacheControl = "no-store";
            return confirmado
                ? Results.Ok(new { mensagem = "E-mail confirmado com sucesso. Você já pode entrar." })
                : ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Token inválido.",
                    "Token de confirmação inválido ou expirado.");
        })
            .AllowAnonymous()
            .RequireAntiforgery();

        app.MapPost("/auth/reenviar-confirmacao", async (ReenviarConfirmacaoEmailRequest request, UsuarioLocalService usuarioService, HttpContext context) =>
        {
            await usuarioService.ReenviarConfirmacaoEmailAsync(request.Email);
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(new { mensagem = "Se a conta existir e ainda não estiver confirmada, enviaremos uma nova confirmação." });
        })
            .AllowAnonymous()
            .RequireAntiforgery();

        app.MapPost("/auth/esqueci-senha", async (EsqueciSenhaRequest request, UsuarioLocalService usuarioService, HttpContext context) =>
        {
            await usuarioService.SolicitarRedefinicaoSenhaAsync(request.Email);
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(new { mensagem = "Se o e-mail estiver cadastrado, enviaremos instruções de recuperação." });
        })
            .AllowAnonymous()
            .RequireAntiforgery();

        app.MapPost("/auth/redefinir-senha", (RedefinirSenhaRequest request, UsuarioLocalService usuarioService, HttpContext context) =>
        {
            bool redefinida = usuarioService.RedefinirSenha(request.Token, request.NovaSenha);
            context.Response.Headers.CacheControl = "no-store";
            return redefinida
                ? Results.Ok(new { mensagem = "Senha redefinida com sucesso. Entre com sua nova senha." })
                : ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Token inválido.",
                    "Token inválido ou expirado, ou senha fora dos critérios.");
        })
            .AllowAnonymous()
            .RequireAntiforgery();

        app.MapGet("/auth/me", (ClaimsPrincipal principal, HttpContext context) =>
        {
            UsuarioResponse? usuario = ApiHttpHelpers.MapearUsuarioClaims(principal);
            context.Response.Headers.CacheControl = "no-store";
            return usuario is null
                ? ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "Sessão expirada ou não autenticada.",
                    "Faça login para continuar.")
                : Results.Ok(usuario);
        });

        app.MapPost("/auth/logout", async (AuditoriaService auditoriaService, HttpContext context) =>
        {
            string? emailUsuario = context.User.FindFirst(ClaimTypes.Email)?.Value;
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            await auditoriaService.RegistrarSemImpactarFluxoAsync(
                context,
                "Logout",
                "Auth",
                emailUsuario ?? "n/a",
                "Logout realizado.",
                new
                {
                    EmailUsuario = emailUsuario,
                    Ip = context.Connection.RemoteIpAddress?.ToString()
                });

            context.Response.Headers.CacheControl = "no-store";
            return Results.NoContent();
        })
            .RequireAntiforgery();
    }
}
