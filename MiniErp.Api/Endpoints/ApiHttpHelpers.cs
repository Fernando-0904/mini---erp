using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MiniErp.Api.DTOs;
using MiniErp.Api.Models;

namespace MiniErp.Api.Endpoints;

internal static class ApiHttpHelpers
{
    internal static IResult CriarProblem(HttpContext context, int statusCode, string title, string detail)
    {
        return Results.Problem(
            detail: detail,
            statusCode: statusCode,
            title: title,
            type: $"https://httpstatuses.com/{statusCode}",
            instance: context.Request.Path,
            extensions: new Dictionary<string, object?>
            {
                ["correlationId"] = context.TraceIdentifier
            });
    }

    internal static async Task EscreverProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        ProblemDetails problemDetails = new()
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = context.Request.Path
        };
        problemDetails.Extensions["correlationId"] = context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    internal static Produto MapearProdutoRequest(ProdutoRequest request)
    {
        return new Produto
        {
            Codigo = request.Codigo,
            Nome = request.Nome,
            PrecoUnitario = request.PrecoUnitario,
            QuantidadeEstoque = request.QuantidadeEstoque,
            EstoqueMinimo = request.EstoqueMinimo,
            CategoriaId = request.CategoriaId,
            FornecedorId = request.FornecedorId
        };
    }

    internal static Categoria MapearCategoriaRequest(CategoriaRequest request)
    {
        return new Categoria
        {
            Id = request.Id,
            Nome = request.Nome
        };
    }

    internal static Fornecedor MapearFornecedorRequest(FornecedorRequest request)
    {
        return new Fornecedor
        {
            Codigo = request.Codigo,
            Nome = request.Nome,
            Documento = request.Documento,
            Email = request.Email,
            Telefone = request.Telefone,
            Ativo = request.Ativo
        };
    }

    internal static ClaimsPrincipal CriarPrincipal(UsuarioResponse usuario)
    {
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Perfil)
        ];

        ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    internal static AuthenticationProperties CriarPropriedadesAutenticacao()
    {
        return new AuthenticationProperties
        {
            AllowRefresh = true,
            IsPersistent = false
        };
    }

    internal static UsuarioResponse? MapearUsuarioClaims(ClaimsPrincipal principal)
    {
        string? id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        string? nome = principal.FindFirstValue(ClaimTypes.Name);
        string? email = principal.FindFirstValue(ClaimTypes.Email);
        string? perfil = principal.FindFirstValue(ClaimTypes.Role);

        if (!int.TryParse(id, out int usuarioId) ||
            string.IsNullOrWhiteSpace(nome) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(perfil))
        {
            return null;
        }

        return new UsuarioResponse
        {
            Id = usuarioId,
            Nome = nome,
            Email = email,
            Perfil = perfil
        };
    }

    internal static string GerarCsv(IEnumerable<string> cabecalho, IEnumerable<IEnumerable<string>> linhas)
    {
        StringBuilder csv = new();
        csv.AppendLine(string.Join(";", cabecalho.Select(EscapeCsvCampo)));

        foreach (IEnumerable<string> linha in linhas)
        {
            csv.AppendLine(string.Join(";", linha.Select(EscapeCsvCampo)));
        }

        return csv.ToString();
    }

    internal static string EscapeCsvCampo(string? valor)
    {
        string texto = valor ?? string.Empty;
        bool precisaEscape = texto.Contains(';') || texto.Contains('"') || texto.Contains('\n') || texto.Contains('\r');

        if (!precisaEscape)
        {
            return texto;
        }

        return $"\"{texto.Replace("\"", "\"\"")}\"";
    }

    internal static IResult CriarArquivoCsv(string nomeBase, string conteudo)
    {
        byte[] bytesConteudo = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(conteudo))
            .ToArray();

        string nomeArquivo = $"{nomeBase}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return Results.File(bytesConteudo, "text/csv; charset=utf-8", nomeArquivo);
    }
}
