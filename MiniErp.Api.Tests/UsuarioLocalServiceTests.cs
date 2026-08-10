using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MiniErp.Api.Data;
using MiniErp.Api.DTOs;
using MiniErp.Api.Models;
using MiniErp.Api.Services;
using Xunit;

namespace MiniErp.Api.Tests;

public class UsuarioLocalServiceTests
{
    [Fact]
    public async Task Cadastrar_ComDadosValidos_GeraTokenEPermiteAutenticarAposConfirmacao()
    {
        using BancoDeTeste banco = new();
        EmailSimuladoService emailService = CriarEmailService();
        UsuarioLocalService service = CriarService(banco.Contexto, emailService);

        (UsuarioResponse? cadastrado, string erro) = await service.CadastrarAsync(
            "Fernando Antunes",
            "fernando@teste.com",
            "senha123");
        ResultadoAutenticacao bloqueado = service.Autenticar("fernando@teste.com", "senha123");
        bool confirmado = service.ConfirmarEmail(ExtrairToken(emailService.Listar().Single().Link));
        ResultadoAutenticacao autenticado = service.Autenticar("fernando@teste.com", "senha123");

        Assert.NotNull(cadastrado);
        Assert.Equal(string.Empty, erro);
        Assert.True(bloqueado.EmailNaoConfirmado);
        Assert.True(confirmado);
        Assert.NotNull(autenticado.Usuario);
        Assert.Equal("Fernando Antunes", autenticado.Usuario.Nome);
        Assert.Equal("Operador", autenticado.Usuario.Perfil);
    }

    [Fact]
    public async Task Cadastrar_ComEmailDuplicado_RetornaErro()
    {
        using BancoDeTeste banco = new();
        UsuarioLocalService service = CriarService(banco.Contexto);
        await service.CadastrarAsync("Primeiro Usuário", "usuario@teste.com", "senha123");

        (UsuarioResponse? usuario, string erro) = await service.CadastrarAsync(
            "Segundo Usuário",
            "USUARIO@teste.com",
            "outrasenha123");

        Assert.Null(usuario);
        Assert.Equal("Já existe uma conta com este e-mail.", erro);
    }

    [Fact]
    public async Task Cadastrar_ComSenhaCurta_RetornaErro()
    {
        using BancoDeTeste banco = new();
        UsuarioLocalService service = CriarService(banco.Contexto);

        (UsuarioResponse? usuario, string erro) = await service.CadastrarAsync(
            "Fernando Antunes",
            "fernando@teste.com",
            "1234567");

        Assert.Null(usuario);
        Assert.Equal("A senha deve possuir pelo menos 8 caracteres.", erro);
    }

    [Fact]
    public async Task Autenticar_ComSenhaIncorreta_RetornaNull()
    {
        using BancoDeTeste banco = new();
        UsuarioLocalService service = CriarService(banco.Contexto);
        await service.CadastrarAsync("Fernando Antunes", "fernando@teste.com", "senha123");

        ResultadoAutenticacao autenticacao = service.Autenticar("fernando@teste.com", "senha-errada");

        Assert.Null(autenticacao.Usuario);
        Assert.False(autenticacao.EmailNaoConfirmado);
    }

    [Fact]
    public async Task Cadastrar_EmNovoContexto_ContaContinuaDisponivelAposConfirmacao()
    {
        using BancoDeTeste banco = new();
        EmailSimuladoService emailService = CriarEmailService();
        UsuarioLocalService cadastroService = CriarService(banco.Contexto, emailService);
        await cadastroService.CadastrarAsync("Fernando Antunes", "fernando@teste.com", "senha123");
        cadastroService.ConfirmarEmail(ExtrairToken(emailService.Listar().Single().Link));

        using AppDbContext novoContexto = banco.CriarContexto();
        UsuarioLocalService loginService = CriarService(novoContexto);
        ResultadoAutenticacao autenticacao = loginService.Autenticar("fernando@teste.com", "senha123");

        Assert.NotNull(autenticacao.Usuario);
        Assert.Equal("Fernando Antunes", autenticacao.Usuario.Nome);
    }

    [Fact]
    public async Task Cadastrar_ComDadosValidos_ArmazenaHashESaltEmVezDaSenha()
    {
        using BancoDeTeste banco = new();
        UsuarioLocalService service = CriarService(banco.Contexto);
        await service.CadastrarAsync("Fernando Antunes", "fernando@teste.com", "senha123");

        Usuario usuario = banco.Contexto.Usuarios.Single(item => item.Email == "fernando@teste.com");

        Assert.Equal(16, usuario.SenhaSalt.Length);
        Assert.Equal(32, usuario.SenhaHash.Length);
        Assert.False(usuario.SenhaHash.SequenceEqual(Encoding.UTF8.GetBytes("senha123")));
        Assert.False(usuario.EmailConfirmado);
        Assert.True(usuario.CriadoEmUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void Autenticar_AdministradorDaMigration_RetornaPerfilAdmin()
    {
        using BancoDeTeste banco = new();
        UsuarioLocalService service = CriarService(banco.Contexto);

        ResultadoAutenticacao autenticacao = service.Autenticar("admin@mini-erp.com", "123456");

        Assert.NotNull(autenticacao.Usuario);
        Assert.Equal("Administrador", autenticacao.Usuario.Nome);
        Assert.Equal("Admin", autenticacao.Usuario.Perfil);
    }

    [Fact]
    public async Task Salvar_EmailDuplicadoComOutraCapitalizacao_BancoRejeita()
    {
        using BancoDeTeste banco = new();
        UsuarioLocalService service = CriarService(banco.Contexto);
        await service.CadastrarAsync("Primeiro Usuário", "usuario@teste.com", "senha123");
        banco.Contexto.Usuarios.Add(new Usuario
        {
            Nome = "Segundo Usuário",
            Email = "USUARIO@TESTE.COM",
            Perfil = "Usuário",
            SenhaHash = new byte[32],
            SenhaSalt = new byte[16],
            EmailConfirmado = false,
            CriadoEmUtc = DateTime.UtcNow
        });

        Assert.Throws<DbUpdateException>(() => banco.Contexto.SaveChanges());
    }

    private static UsuarioLocalService CriarService(AppDbContext contexto, EmailSimuladoService? emailService = null)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Frontend:BaseUrl"] = "http://localhost:5500"
            })
            .Build();

        return new UsuarioLocalService(
            contexto,
            emailService ?? CriarEmailService(),
            configuration,
            new LoginAttemptGuardService(configuration));
    }

    private static EmailSimuladoService CriarEmailService()
    {
        return new EmailSimuladoService(NullLogger<EmailSimuladoService>.Instance);
    }

    private static string ExtrairToken(string link)
    {
        Uri uri = new(link);
        string query = uri.Query.TrimStart('?');
        return query
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Split('=', 2))
            .Where(partes => partes.Length == 2 && partes[0] == "token")
            .Select(partes => Uri.UnescapeDataString(partes[1]))
            .Single();
    }

    private sealed class BancoDeTeste : IDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        private readonly DbContextOptions<AppDbContext> options;

        public BancoDeTeste()
        {
            connection.Open();
            options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            Contexto = CriarContexto();
            Contexto.Database.EnsureCreated();
        }

        public AppDbContext Contexto { get; }

        public AppDbContext CriarContexto()
        {
            return new AppDbContext(options);
        }

        public void Dispose()
        {
            Contexto.Dispose();
            connection.Dispose();
        }
    }
}
