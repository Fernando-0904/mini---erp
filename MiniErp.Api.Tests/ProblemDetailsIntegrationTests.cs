using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniErp.Api.Data;
using MiniErp.Api.Models;
using Xunit;

namespace MiniErp.Api.Tests;

public class ProblemDetailsIntegrationTests
{
    [Fact]
    public async Task Erro401_DeveRetornarProblemDetailsEHeaderCorrelationId()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();

        HttpResponseMessage response = await client.GetAsync("/produtos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemDetailsAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Erro403_DeveRetornarProblemDetailsEHeaderCorrelationId()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();

        await AutenticarUsuarioComPerfil(client, factory, "consulta-problem@teste.com", "Consulta");
        string csrf = await ObterTokenAntiforgery(client);

        HttpResponseMessage response = await DeleteComToken(client, "/categorias/1", csrf);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertProblemDetailsAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Erro404_DeveRetornarProblemDetailsEHeaderCorrelationId()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        await AutenticarAdministrador(client);

        HttpResponseMessage response = await client.GetAsync("/rota-inexistente-problem-details");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Erro400_Antiforgery_DeveRetornarProblemDetailsEHeaderCorrelationId()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();

        await AutenticarAdministrador(client);

        HttpResponseMessage response = await client.PostAsJsonAsync("/categorias", new
        {
            id = 0,
            nome = "Categoria sem csrf"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        JsonDocument problem = await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
        string detail = problem.RootElement.GetProperty("detail").GetString() ?? string.Empty;
        Assert.Contains("Token de segurança ausente ou inválido.", detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Erro409_DeveRetornarProblemDetailsEHeaderCorrelationId()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();

        await AutenticarAdministrador(client);
        string csrf = await ObterTokenAntiforgery(client);
        string nomeCategoria = $"Categoria conflito {Guid.NewGuid():N}";

        HttpResponseMessage primeiraCriacao = await PostComToken(client, "/categorias", new
        {
            id = 0,
            nome = nomeCategoria
        }, csrf);
        Assert.Equal(HttpStatusCode.Created, primeiraCriacao.StatusCode);

        HttpResponseMessage segundaCriacao = await PostComToken(client, "/categorias", new
        {
            id = 0,
            nome = nomeCategoria
        }, csrf);

        Assert.Equal(HttpStatusCode.Conflict, segundaCriacao.StatusCode);
        await AssertProblemDetailsAsync(segundaCriacao, HttpStatusCode.Conflict);
    }

    private static async Task<JsonDocument> AssertProblemDetailsAsync(HttpResponseMessage response, HttpStatusCode statusCode)
    {
        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out IEnumerable<string>? headerValues));
        string headerCorrelationId = headerValues!.Single();
        Assert.False(string.IsNullOrWhiteSpace(headerCorrelationId));

        string payload = await response.Content.ReadAsStringAsync();
        JsonDocument document = JsonDocument.Parse(payload);

        Assert.True(document.RootElement.TryGetProperty("status", out JsonElement status));
        Assert.Equal((int)statusCode, status.GetInt32());

        Assert.True(document.RootElement.TryGetProperty("title", out JsonElement title));
        Assert.False(string.IsNullOrWhiteSpace(title.GetString()));

        Assert.True(document.RootElement.TryGetProperty("detail", out JsonElement detail));
        Assert.False(string.IsNullOrWhiteSpace(detail.GetString()));

        Assert.True(document.RootElement.TryGetProperty("type", out JsonElement type));
        Assert.Contains(((int)statusCode).ToString(), type.GetString() ?? string.Empty, StringComparison.Ordinal);

        Assert.True(document.RootElement.TryGetProperty("instance", out JsonElement instance));
        Assert.False(string.IsNullOrWhiteSpace(instance.GetString()));

        Assert.True(document.RootElement.TryGetProperty("correlationId", out JsonElement bodyCorrelationId));
        string correlationIdBody = bodyCorrelationId.GetString() ?? string.Empty;
        Assert.False(string.IsNullOrWhiteSpace(correlationIdBody));
        Assert.Equal(headerCorrelationId, correlationIdBody);

        return document;
    }

    private static async Task<string> ObterTokenAntiforgery(HttpClient client)
    {
        CsrfResponse? response = await client.GetFromJsonAsync<CsrfResponse>("/auth/csrf");
        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        return response.Token;
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

    private static async Task AutenticarUsuarioComPerfil(
        HttpClient client,
        MiniErpApiFactory factory,
        string email,
        string perfil)
    {
        string csrf = await ObterTokenAntiforgery(client);

        HttpResponseMessage cadastro = await PostComToken(client, "/auth/cadastro", new
        {
            nome = "Usuário de perfil",
            email,
            senha = "senha123"
        }, csrf);
        cadastro.EnsureSuccessStatusCode();

        DevEmail[]? emails = await client.GetFromJsonAsync<DevEmail[]>("/dev/emails");
        Assert.NotNull(emails);
        string tokenConfirmacao = ExtrairToken(emails.Single().Link);

        HttpResponseMessage confirmacao = await PostComToken(client, "/auth/confirmar-email", new
        {
            token = tokenConfirmacao
        }, csrf);
        confirmacao.EnsureSuccessStatusCode();

        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Usuario usuario = await contexto.Usuarios.SingleAsync(item => item.Email == email);
        usuario.Perfil = perfil;
        await contexto.SaveChangesAsync();

        HttpResponseMessage login = await PostComToken(client, "/auth/login", new
        {
            email,
            senha = "senha123"
        }, csrf);
        login.EnsureSuccessStatusCode();
    }

    private static string ExtrairToken(string link)
    {
        Uri uri = new(link);

        return uri.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Split('=', 2))
            .Where(partes => partes.Length == 2 && partes[0] == "token")
            .Select(partes => Uri.UnescapeDataString(partes[1]))
            .Single();
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

    private static async Task<HttpResponseMessage> DeleteComToken(HttpClient client, string url, string token)
    {
        using HttpRequestMessage request = new(HttpMethod.Delete, url);
        request.Headers.Add("X-CSRF-TOKEN", token);
        return await client.SendAsync(request);
    }

    private sealed class CsrfResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    private sealed class DevEmail
    {
        public string Link { get; set; } = string.Empty;
    }
}