using MiniErp.Api.DTOs;
using MiniErp.Api.Services;
using Xunit;

namespace MiniErp.Api.Tests;

public class UsuarioLocalServiceTests
{
    [Fact]
    public void Cadastrar_ComDadosValidos_PermiteAutenticarNovaConta()
    {
        UsuarioLocalService service = new();

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
        UsuarioLocalService service = new();
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
        UsuarioLocalService service = new();

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
        UsuarioLocalService service = new();
        service.Cadastrar("Fernando Antunes", "fernando@teste.com", "senha123");

        UsuarioResponse? usuario = service.Autenticar("fernando@teste.com", "senha-errada");

        Assert.Null(usuario);
    }
}
