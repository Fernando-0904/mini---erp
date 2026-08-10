using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniErp.Api.Data;
using MiniErp.Api.DTOs;
using MiniErp.Api.Models;
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
    public async Task Login_AposMuitasFalhas_BloqueiaTemporariamente()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        string token = await ObterTokenAntiforgery(client);

        for (int tentativa = 0; tentativa < 5; tentativa++)
        {
            HttpResponseMessage falha = await PostComToken(client, "/auth/login", new
            {
                email = "admin@mini-erp.com",
                senha = "senha-incorreta"
            }, token);

            Assert.Equal(HttpStatusCode.Unauthorized, falha.StatusCode);
        }

        HttpResponseMessage bloqueio = await PostComToken(client, "/auth/login", new
        {
            email = "admin@mini-erp.com",
            senha = "123456"
        }, token);

        Assert.Equal((HttpStatusCode)429, bloqueio.StatusCode);
        Assert.True(bloqueio.Headers.TryGetValues("Retry-After", out IEnumerable<string>? retryAfter));
        Assert.True(int.TryParse(retryAfter?.SingleOrDefault(), out int segundos) && segundos > 0);
        Assert.Contains(
            "Detectamos muitas tentativas de acesso",
            await bloqueio.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_ComSucesso_AntesDoLimite_NaoBloqueiaTentativaSeguinte()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        string token = await ObterTokenAntiforgery(client);
        string email = "limite-login@teste.com";
        string senha = "senha123";

        HttpResponseMessage cadastro = await PostComToken(client, "/auth/cadastro", new
        {
            nome = "Usuário Limite",
            email,
            senha
        }, token);
        cadastro.EnsureSuccessStatusCode();

        string tokenConfirmacao = ExtrairToken((await ObterEmailsSimulados(client)).Single().Link);
        HttpResponseMessage confirmacao = await PostComToken(client, "/auth/confirmar-email", new
        {
            token = tokenConfirmacao
        }, token);
        confirmacao.EnsureSuccessStatusCode();

        for (int tentativa = 0; tentativa < 3; tentativa++)
        {
            HttpResponseMessage falha = await PostComToken(client, "/auth/login", new
            {
                email,
                senha = "senha-incorreta"
            }, token);

            Assert.Equal(HttpStatusCode.Unauthorized, falha.StatusCode);
        }

        HttpResponseMessage sucesso = await PostComToken(client, "/auth/login", new
        {
            email,
            senha
        }, token);

        token = await ObterTokenAntiforgery(client);

        HttpResponseMessage novaFalha = await PostComToken(client, "/auth/login", new
        {
            email,
            senha = "senha-incorreta"
        }, token);

        Assert.Equal(HttpStatusCode.OK, sucesso.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, novaFalha.StatusCode);
        Assert.NotEqual((HttpStatusCode)429, novaFalha.StatusCode);
    }

    [Fact]
    public async Task Cadastro_ComDadosValidos_PersisteUsuarioEGeraConfirmacaoSemIniciarSessao()
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
        DevEmail[] emails = await ObterEmailsSimulados(client);

        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        bool persistidoSemConfirmacao = await contexto.Usuarios
            .AsNoTracking()
            .AnyAsync(usuario => usuario.Email == "integracao@teste.com" && !usuario.EmailConfirmado);

        Assert.Equal(HttpStatusCode.Created, cadastro.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.True(persistidoSemConfirmacao);
        Assert.Contains(emails, email =>
            email.Para == "integracao@teste.com" &&
            email.Assunto == "Confirme seu e-mail no Mini ERP" &&
            email.Link.Contains("confirmar-email.html?token=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Login_ComEmailNaoConfirmado_RetornaBadRequest()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        string token = await ObterTokenAntiforgery(client);
        await PostComToken(client, "/auth/cadastro", new
        {
            nome = "Usuário Pendente",
            email = "pendente@teste.com",
            senha = "senha123"
        }, token);

        HttpResponseMessage login = await PostComToken(client, "/auth/login", new
        {
            email = "pendente@teste.com",
            senha = "senha123"
        }, token);

        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
        Assert.Contains("Confirme seu e-mail antes de entrar.", await login.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConfirmarEmail_ComTokenValido_LiberaLogin()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        string csrf = await ObterTokenAntiforgery(client);
        await PostComToken(client, "/auth/cadastro", new
        {
            nome = "Usuário Confirmado",
            email = "confirmado@teste.com",
            senha = "senha123"
        }, csrf);
        string token = ExtrairToken((await ObterEmailsSimulados(client)).Single().Link);

        HttpResponseMessage confirmacao = await PostComToken(client, "/auth/confirmar-email", new
        {
            token
        }, csrf);
        HttpResponseMessage login = await PostComToken(client, "/auth/login", new
        {
            email = "confirmado@teste.com",
            senha = "senha123"
        }, csrf);

        Assert.Equal(HttpStatusCode.OK, confirmacao.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task ReenviarConfirmacao_InvalidaTokenAnterior()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        string csrf = await ObterTokenAntiforgery(client);
        await PostComToken(client, "/auth/cadastro", new
        {
            nome = "Usuário Reenvio",
            email = "reenvio@teste.com",
            senha = "senha123"
        }, csrf);
        string tokenAntigo = ExtrairToken((await ObterEmailsSimulados(client)).Single().Link);

        await PostComToken(client, "/auth/reenviar-confirmacao", new { email = "reenvio@teste.com" }, csrf);
        string tokenNovo = ExtrairToken((await ObterEmailsSimulados(client)).First().Link);

        HttpResponseMessage confirmacaoAntiga = await PostComToken(client, "/auth/confirmar-email", new
        {
            token = tokenAntigo
        }, csrf);
        HttpResponseMessage confirmacaoNova = await PostComToken(client, "/auth/confirmar-email", new
        {
            token = tokenNovo
        }, csrf);

        Assert.NotEqual(tokenAntigo, tokenNovo);
        Assert.Equal(HttpStatusCode.BadRequest, confirmacaoAntiga.StatusCode);
        Assert.Equal(HttpStatusCode.OK, confirmacaoNova.StatusCode);
    }

    [Fact]
    public async Task RecuperacaoSenha_ComTokenValido_AlteraSenhaEUsoUnico()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        string csrf = await ObterTokenAntiforgery(client);
        HttpResponseMessage cadastro = await PostComToken(client, "/auth/cadastro", new
        {
            nome = "Usuário Recuperação",
            email = "recuperacao@teste.com",
            senha = "senha123"
        }, csrf);
        cadastro.EnsureSuccessStatusCode();
        string tokenConfirmacao = ExtrairToken((await ObterEmailsSimulados(client)).Single().Link);
        HttpResponseMessage confirmacao = await PostComToken(client, "/auth/confirmar-email", new { token = tokenConfirmacao }, csrf);
        confirmacao.EnsureSuccessStatusCode();

        HttpResponseMessage solicitacao = await PostComToken(client, "/auth/esqueci-senha", new { email = "recuperacao@teste.com" }, csrf);
        solicitacao.EnsureSuccessStatusCode();
        string tokenRedefinicao = ExtrairToken((await ObterEmailsSimulados(client))
            .First(email => email.Assunto == "Redefina sua senha no Mini ERP")
            .Link);

        HttpResponseMessage redefinicao = await PostComToken(client, "/auth/redefinir-senha", new
        {
            token = tokenRedefinicao,
            novaSenha = "senha-nova123"
        }, csrf);
        HttpResponseMessage reutilizacao = await PostComToken(client, "/auth/redefinir-senha", new
        {
            token = tokenRedefinicao,
            novaSenha = "outra-senha123"
        }, csrf);
        HttpResponseMessage loginAntigo = await PostComToken(client, "/auth/login", new
        {
            email = "recuperacao@teste.com",
            senha = "senha123"
        }, csrf);
        HttpResponseMessage loginNovo = await PostComToken(client, "/auth/login", new
        {
            email = "recuperacao@teste.com",
            senha = "senha-nova123"
        }, csrf);

        Assert.Equal(HttpStatusCode.OK, redefinicao.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, reutilizacao.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, loginAntigo.StatusCode);
        Assert.Equal(HttpStatusCode.OK, loginNovo.StatusCode);
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
        string nomeCategoria = $"Categoria protegida {Guid.NewGuid():N}";

        HttpResponseMessage response = await PostComToken(client, "/categorias", new
        {
            id = 0,
            nome = nomeCategoria
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

    [Fact]
    public async Task DevEmails_EmProducao_NaoFicaPublico()
    {
        using MiniErpApiFactory factory = new("Production");
        using HttpClient client = factory.CriarCliente();

        HttpResponseMessage response = await client.GetAsync("/dev/emails");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PerfilConsulta_PodeConsultarMasNaoPodeCadastrar()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        await AutenticarUsuarioComPerfil(client, factory, "consulta@teste.com", "Consulta");
        string token = await ObterTokenAntiforgery(client);

        HttpResponseMessage consulta = await client.GetAsync("/produtos");
        HttpResponseMessage cadastro = await PostComToken(client, "/categorias", new
        {
            id = 0,
            nome = "Categoria negada"
        }, token);

        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, cadastro.StatusCode);
    }

    [Fact]
    public async Task PerfilOperador_PodeCadastrarMasNaoPodeRemover()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        await AutenticarUsuarioComPerfil(client, factory, "operador@teste.com", "Operador");
        string token = await ObterTokenAntiforgery(client);

        HttpResponseMessage cadastro = await PostComToken(client, "/categorias", new
        {
            id = 0,
            nome = "Categoria operador"
        }, token);
        HttpResponseMessage remocao = await DeleteComToken(client, "/categorias/1", token);

        Assert.Equal(HttpStatusCode.Created, cadastro.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, remocao.StatusCode);
    }

    [Fact]
    public async Task PerfilAdministrador_PodeRemover()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        await AutenticarAdministrador(client);
        string token = await ObterTokenAntiforgery(client);

        HttpResponseMessage cadastro = await PostComToken(client, "/categorias", new
        {
            id = 0,
            nome = "Categoria removivel"
        }, token);
        Categoria? categoria = await cadastro.Content.ReadFromJsonAsync<Categoria>();
        Assert.NotNull(categoria);

        HttpResponseMessage remocao = await DeleteComToken(client, $"/categorias/{categoria.Id}", token);

        Assert.Equal(HttpStatusCode.Created, cadastro.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, remocao.StatusCode);
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
            nome = $"Usuário {perfil}",
            email,
            senha = "senha123"
        }, csrf);
        cadastro.EnsureSuccessStatusCode();

        string tokenConfirmacao = ExtrairToken((await ObterEmailsSimulados(client)).Single().Link);
        HttpResponseMessage confirmacao = await PostComToken(client, "/auth/confirmar-email", new
        {
            token = tokenConfirmacao
        }, csrf);
        confirmacao.EnsureSuccessStatusCode();

        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Usuario usuario = contexto.Usuarios.Single(item => item.Email == email);
        usuario.Perfil = perfil;
        contexto.SaveChanges();

        HttpResponseMessage login = await PostComToken(client, "/auth/login", new
        {
            email,
            senha = "senha123"
        }, csrf);
        login.EnsureSuccessStatusCode();
    }

    private static async Task<string> ObterTokenAntiforgery(HttpClient client)
    {
        CsrfResponse? response = await client.GetFromJsonAsync<CsrfResponse>("/auth/csrf");
        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        return response.Token;
    }

    private static async Task<DevEmail[]> ObterEmailsSimulados(HttpClient client)
    {
        DevEmail[]? emails = await client.GetFromJsonAsync<DevEmail[]>("/dev/emails");
        Assert.NotNull(emails);
        return emails;
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
        public string Para { get; set; } = string.Empty;
        public string Assunto { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
    }
}
