using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Data;
using MiniErp.Api.Models;
using MiniErp.Api.Services;
using Xunit;

namespace MiniErp.Api.Tests;

public class FornecedorServiceTests
{
    [Fact]
    public void CadastrarFornecedor_ComDadosValidos_RetornaTrue()
    {
        using BancoDeTeste banco = new();
        FornecedorService service = new(banco.Contexto);

        bool cadastrado = service.CadastrarFornecedor(CriarFornecedor());

        Assert.True(cadastrado);
        Assert.Single(banco.Contexto.Fornecedores);
    }

    [Fact]
    public void CadastrarFornecedor_ComCodigoDuplicado_RetornaFalse()
    {
        using BancoDeTeste banco = new();
        FornecedorService service = new(banco.Contexto);
        Fornecedor primeiroFornecedor = CriarFornecedor(codigo: 100);
        Fornecedor fornecedorDuplicado = CriarFornecedor(codigo: 100, documento: "22222222000122");

        bool primeiroCadastro = service.CadastrarFornecedor(primeiroFornecedor);
        bool segundoCadastro = service.CadastrarFornecedor(fornecedorDuplicado);

        Assert.True(primeiroCadastro);
        Assert.False(segundoCadastro);
        Assert.Single(banco.Contexto.Fornecedores);
    }

    [Fact]
    public void CadastrarFornecedor_ComDocumentoDuplicado_RetornaFalse()
    {
        using BancoDeTeste banco = new();
        FornecedorService service = new(banco.Contexto);
        Fornecedor primeiroFornecedor = CriarFornecedor(codigo: 100);
        Fornecedor fornecedorDuplicado = CriarFornecedor(codigo: 200);

        service.CadastrarFornecedor(primeiroFornecedor);
        bool segundoCadastro = service.CadastrarFornecedor(fornecedorDuplicado);

        Assert.False(segundoCadastro);
        Assert.Single(banco.Contexto.Fornecedores);
    }

    [Fact]
    public void ValidarFornecedor_ComEmailInvalido_RetornaErro()
    {
        using BancoDeTeste banco = new();
        FornecedorService service = new(banco.Contexto);
        Fornecedor fornecedor = CriarFornecedor();
        fornecedor.Email = "email-invalido";

        List<string> erros = service.ValidarFornecedor(fornecedor);

        Assert.Contains("Informe um e-mail válido.", erros);
    }

    [Fact]
    public void CadastrarFornecedor_Inativo_PersisteStatusInativo()
    {
        using BancoDeTeste banco = new();
        FornecedorService service = new(banco.Contexto);
        Fornecedor fornecedor = CriarFornecedor();
        fornecedor.Ativo = false;

        bool cadastrado = service.CadastrarFornecedor(fornecedor);

        Assert.True(cadastrado);
        Assert.False(banco.Contexto.Fornecedores.Single().Ativo);
    }

    [Fact]
    public void Produto_SemFornecedor_PodeSerPersistido()
    {
        using BancoDeTeste banco = new();
        Produto produto = CriarProduto(codigo: 101);

        banco.Contexto.Produtos.Add(produto);
        banco.Contexto.SaveChanges();

        Produto produtoPersistido = banco.Contexto.Produtos.Single();
        Assert.Null(produtoPersistido.FornecedorId);
    }

    [Fact]
    public void PossuiProdutosVinculados_ComProdutoAssociado_RetornaTrue()
    {
        using BancoDeTeste banco = new();
        FornecedorService service = new(banco.Contexto);
        Fornecedor fornecedor = CriarFornecedor();
        service.CadastrarFornecedor(fornecedor);
        banco.Contexto.Produtos.Add(CriarProduto(codigo: 101, fornecedorId: fornecedor.Id));
        banco.Contexto.SaveChanges();

        bool possuiProdutosVinculados = service.PossuiProdutosVinculados(fornecedor.Id);

        Assert.True(possuiProdutosVinculados);
    }

    [Fact]
    public void ValidarFornecedorDoProduto_ComFornecedorInexistente_RetornaErro()
    {
        using BancoDeTeste banco = new();
        FornecedorService service = new(banco.Contexto);

        List<string> erros = service.ValidarFornecedorDoProduto(999);

        Assert.Contains("Fornecedor informado não existe.", erros);
    }

    [Fact]
    public void ValidarFornecedorDoProduto_ComFornecedorInativo_RetornaErro()
    {
        using BancoDeTeste banco = new();
        FornecedorService service = new(banco.Contexto);
        Fornecedor fornecedor = CriarFornecedor();
        fornecedor.Ativo = false;
        service.CadastrarFornecedor(fornecedor);

        List<string> erros = service.ValidarFornecedorDoProduto(fornecedor.Id);

        Assert.Contains("Fornecedor informado está inativo.", erros);
    }

    [Fact]
    public void EditarProduto_ComFornecedorAtualizado_PersisteNovoFornecedor()
    {
        using BancoDeTeste banco = new();
        FornecedorService fornecedorService = new(banco.Contexto);
        ProdutoService produtoService = new(banco.Contexto);
        Fornecedor primeiroFornecedor = CriarFornecedor(codigo: 100);
        Fornecedor segundoFornecedor = CriarFornecedor(codigo: 200, documento: "22222222000122");
        fornecedorService.CadastrarFornecedor(primeiroFornecedor);
        fornecedorService.CadastrarFornecedor(segundoFornecedor);
        banco.Contexto.Produtos.Add(CriarProduto(codigo: 101, fornecedorId: primeiroFornecedor.Id));
        banco.Contexto.SaveChanges();
        Produto produtoAtualizado = CriarProduto(codigo: 101, fornecedorId: segundoFornecedor.Id);

        bool editado = produtoService.EditarProduto(101, produtoAtualizado);

        Assert.True(editado);
        Assert.Equal(segundoFornecedor.Id, banco.Contexto.Produtos.Single().FornecedorId);
    }

    private static Fornecedor CriarFornecedor(int codigo = 100, string documento = "11111111000111")
    {
        return new Fornecedor
        {
            Codigo = codigo,
            Nome = "Fornecedor de teste",
            Documento = documento,
            Email = "fornecedor@teste.com",
            Ativo = true,
        };
    }

    private static Produto CriarProduto(int codigo, int? fornecedorId = null)
    {
        return new Produto
        {
            Codigo = codigo,
            Nome = "Produto de teste",
            PrecoUnitario = 10m,
            QuantidadeEstoque = 1,
            CategoriaId = 1,
            FornecedorId = fornecedorId,
        };
    }

    private sealed class BancoDeTeste : IDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");

        public BancoDeTeste()
        {
            connection.Open();
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            Contexto = new AppDbContext(options);
            Contexto.Database.EnsureCreated();
            Contexto.Categorias.Add(new Categoria { Nome = "Categoria de teste" });
            Contexto.SaveChanges();
        }

        public AppDbContext Contexto { get; }

        public void Dispose()
        {
            Contexto.Dispose();
            connection.Dispose();
        }
    }
}