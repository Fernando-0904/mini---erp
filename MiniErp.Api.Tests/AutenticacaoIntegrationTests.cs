using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniErp.Api.Data;
using MiniErp.Api.DTOs;
using Xunit;

namespace MiniErp.Api.Tests;

public class AutenticacaoIntegrationTests
{
    [Fact]
    public async Task Produtos_SemSessao_RetornaUnauthorized()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();

        HttpResponseMessage response = await client.GetAsync("/produtos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_SemTokenAntiforgery_RetornaBadRequest()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();

        HttpResponseMessage response = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "admin@mini-erp.com",
            senha = "123456"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_CriaCookieERestauraSessao()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        string token = await ObterTokenAntiforgery(client);

        HttpResponseMessage login = await PostComToken(client, "/auth/login", new
        {
            email = "admin@mini-erp.com",
            senha = "123456"
        }, token);
        HttpResponseMessage me = await client.GetAsync("/auth/me");
        UsuarioResponse? usuario = await me.Content.ReadFromJsonAsync<UsuarioResponse>();

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.NotNull(usuario);
        Assert.Equal("Administrador", usuario.Nome);
        Assert.Contains(login.Headers.GetValues("Set-Cookie"), cookie =>
            cookie.Contains("MiniErp.Auth=", StringComparison.Ordinal) &&
            cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase) &&
            cookie.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase) &&
            cookie.Contains("secure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_EmProducao_CriaCookieCompativelComFrontendCrossSite()
    {
        using MiniErpApiFactory factory = new("Production");
        using HttpClient client = factory.CriarCliente();
        string token = await ObterTokenAntiforgery(client);

        HttpResponseMessage login = await PostComToken(client, "/auth/login", new
        {
            email = "admin@mini-erp.com",
            senha = "123456"
        }, token);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(login.Headers.GetValues("Set-Cookie"), cookie =>
            cookie.Contains("MiniErp.Auth=", StringComparison.Ordinal) &&
            cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase) &&
            cookie.Contains("samesite=none", StringComparison.OrdinalIgnoreCase) &&
            cookie.Contains("secure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_ComCredenciaisInvalidas_RetornaUnauthorized()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        string token = await ObterTokenAntiforgery(client);

        HttpResponseMessage response = await PostComToken(client, "/auth/login", new
        {
            email = "admin@mini-erp.com",
            senha = "senha-incorreta"
        }, token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookies) &&
                     cookies.Any(cookie => cookie.Contains("MiniErp.Auth=")));
    }

    [Fact]
    public async Task Cadastro_ComDadosValidos_PersisteUsuarioEIniciaSessao()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        string token = await ObterTokenAntiforgery(client);

        HttpResponseMessage cadastro = await PostComToken(client, "/auth/cadastro", new
        {
            nome = "Usuário Integração",
            email = "integracao@teste.com",
            senha = "senha123"
        }, token);
        HttpResponseMessage me = await client.GetAsync("/auth/me");

        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        bool persistido = await contexto.Usuarios
            .AsNoTracking()
            .AnyAsync(usuario => usuario.Email == "integracao@teste.com");

        Assert.Equal(HttpStatusCode.Created, cadastro.StatusCode);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.True(persistido);
    }

    [Fact]
    public async Task EndpointProtegido_ComSessao_RetornaOk()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        await AutenticarAdministrador(client);

        HttpResponseMessage response = await client.GetAsync("/produtos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OperacaoDeEscrita_SemCsrf_RetornaBadRequest()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        await AutenticarAdministrador(client);

        HttpResponseMessage response = await client.PostAsJsonAsync("/categorias", new
        {
            id = 0,
            nome = "Sem token"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OperacaoDeEscrita_ComCsrf_RetornaCreated()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        await AutenticarAdministrador(client);
        string token = await ObterTokenAntiforgery(client);

        HttpResponseMessage response = await PostComToken(client, "/categorias", new
        {
            id = 0,
            nome = "Categoria protegida"
        }, token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ComSessaoValida_EncerraAcesso()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        await AutenticarAdministrador(client);
        string token = await ObterTokenAntiforgery(client);

        HttpResponseMessage logout = await PostComToken(client, "/auth/logout", null, token);
        HttpResponseMessage me = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Cors_OrigemPermitida_AceitaCredenciais()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        using HttpRequestMessage request = new(HttpMethod.Options, "/auth/login");
        request.Headers.Add("Origin", "http://localhost:5500");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:5500", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
    }

    [Fact]
    public async Task Cors_OrigemNaoPermitida_NaoRetornaCabecalhosDeAcesso()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        using HttpRequestMessage request = new(HttpMethod.Options, "/auth/login");
        request.Headers.Add("Origin", "https://origem-nao-permitida.example");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    private static async Task AutenticarAdministrador(HttpClient client)
    {
        string token = await ObterTokenAntiforgery(client);
        HttpResponseMessage response = await PostComToken(client, "/auth/login", new
        {
            email = "admin@mini-erp.com",
            senha = "123456"
        }, token);

        response.EnsureSuccessStatusCode();
    }

    private static async Task<string> ObterTokenAntiforgery(HttpClient client)
    {
        CsrfResponse? response = await client.GetFromJsonAsync<CsrfResponse>("/auth/csrf");
        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        return response.Token;
    }

    private static async Task<HttpResponseMessage> PostComToken(
        HttpClient client,
        string url,
        object? body,
        string token)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, url);
        request.Headers.Add("X-CSRF-TOKEN", token);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }

    private sealed class CsrfResponse
    {
        public string Token { get; set; } = string.Empty;
    }
}
