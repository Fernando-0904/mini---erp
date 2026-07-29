using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Data;
using MiniErp.Api.DTOs;
using MiniErp.Api.Models;
using MiniErp.Api.Services;
using Xunit;

namespace MiniErp.Api.Tests;

public class UsuarioLocalServiceTests
{
    [Fact]
    public void Cadastrar_ComDadosValidos_PermiteAutenticarNovaConta()
    {
        using BancoDeTeste banco = new();
        UsuarioLocalService service = new(banco.Contexto);

        (UsuarioResponse? cadastrado, string erro) = service.Cadastrar(
            "Fernando Antunes",
            "fernando@teste.com",
            "senha123");
        UsuarioResponse? autenticado = service.Autenticar("fernando@teste.com", "senha123");

        Assert.NotNull(cadastrado);
        Assert.Equal(string.Empty, erro);
        Assert.NotNull(autenticado);
        Assert.Equal("Fernando Antunes", autenticado.Nome);
        Assert.Equal("Usuário", autenticado.Perfil);
    }

    [Fact]
    public void Cadastrar_ComEmailDuplicado_RetornaErro()
    {
        using BancoDeTeste banco = new();
        UsuarioLocalService service = new(banco.Contexto);
        service.Cadastrar("Primeiro Usuário", "usuario@teste.com", "senha123");

        (UsuarioResponse? usuario, string erro) = service.Cadastrar(
            "Segundo Usuário",
            "USUARIO@teste.com",
            "outrasenha123");

        Assert.Null(usuario);
        Assert.Equal("Já existe uma conta com este e-mail.", erro);
    }

    [Fact]
    public void Cadastrar_ComSenhaCurta_RetornaErro()
    {
        using BancoDeTeste banco = new();
        UsuarioLocalService service = new(banco.Contexto);

        (UsuarioResponse? usuario, string erro) = service.Cadastrar(
            "Fernando Antunes",
            "fernando@teste.com",
            "1234567");

        Assert.Null(usuario);
        Assert.Equal("A senha deve possuir pelo menos 8 caracteres.", erro);
    }

    [Fact]
    public void Autenticar_ComSenhaIncorreta_RetornaNull()
    {
        using BancoDeTeste banco = new();
        UsuarioLocalService service = new(banco.Contexto);
        service.Cadastrar("Fernando Antunes", "fernando@teste.com", "senha123");

        UsuarioResponse? usuario = service.Autenticar("fernando@teste.com", "senha-errada");

        Assert.Null(usuario);
    }

    [Fact]
    public void Cadastrar_EmNovoContexto_ContaContinuaDisponivel()
    {
        using BancoDeTeste banco = new();
        UsuarioLocalService cadastroService = new(banco.Contexto);
        cadastroService.Cadastrar("Fernando Antunes", "fernando@teste.com", "senha123");

        using AppDbContext novoContexto = banco.CriarContexto();
        UsuarioLocalService loginService = new(novoContexto);
        UsuarioResponse? usuario = loginService.Autenticar("fernando@teste.com", "senha123");

        Assert.NotNull(usuario);
        Assert.Equal("Fernando Antunes", usuario.Nome);
    }

    [Fact]
    public void Cadastrar_ComDadosValidos_ArmazenaHashESaltEmVezDaSenha()
    {
        using BancoDeTeste banco = new();
        UsuarioLocalService service = new(banco.Contexto);
        service.Cadastrar("Fernando Antunes", "fernando@teste.com", "senha123");

        Usuario usuario = banco.Contexto.Usuarios.Single(item => item.Email == "fernando@teste.com");

        Assert.Equal(16, usuario.SenhaSalt.Length);
        Assert.Equal(32, usuario.SenhaHash.Length);
        Assert.False(usuario.SenhaHash.SequenceEqual(Encoding.UTF8.GetBytes("senha123")));
        Assert.True(usuario.CriadoEmUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void Autenticar_AdministradorDaMigration_RetornaPerfilAdmin()
    {
        using BancoDeTeste banco = new();
        UsuarioLocalService service = new(banco.Contexto);

        UsuarioResponse? usuario = service.Autenticar("admin@mini-erp.com", "123456");

        Assert.NotNull(usuario);
        Assert.Equal("Administrador", usuario.Nome);
        Assert.Equal("Admin", usuario.Perfil);
    }

    [Fact]
    public void Salvar_EmailDuplicadoComOutraCapitalizacao_BancoRejeita()
    {
        using BancoDeTeste banco = new();
        UsuarioLocalService service = new(banco.Contexto);
        service.Cadastrar("Primeiro Usuário", "usuario@teste.com", "senha123");
        banco.Contexto.Usuarios.Add(new Usuario
        {
            Nome = "Segundo Usuário",
            Email = "USUARIO@TESTE.COM",
            Perfil = "Usuário",
            SenhaHash = new byte[32],
            SenhaSalt = new byte[16],
            CriadoEmUtc = DateTime.UtcNow
        });

        Assert.Throws<DbUpdateException>(() => banco.Contexto.SaveChanges());
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
