using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Data;
using MiniErp.Api.Models;
using MiniErp.Api.Services;
using Xunit;

namespace MiniErp.Api.Tests;

public class ProdutoEEstoqueTests
{
    [Fact]
    public void CalcularValorTotal_ComPrecoEQuantidade_RetornaValorEsperado()
    {
        Produto produto = new()
        {
            PrecoUnitario = 19.90m,
            QuantidadeEstoque = 3,
        };

        decimal valorTotal = produto.CalcularValorTotal();

        Assert.Equal(59.70m, valorTotal);
    }

    [Fact]
    public void CadastrarProduto_ComCodigoDuplicado_RetornaFalse()
    {
        using BancoDeTeste banco = new();
        ProdutoService service = new(banco.Contexto);
        Produto primeiroProduto = CriarProduto(codigo: 101);
        Produto produtoDuplicado = CriarProduto(codigo: 101);

        bool primeiroCadastro = service.CadastrarProduto(primeiroProduto);
        bool segundoCadastro = service.CadastrarProduto(produtoDuplicado);

        Assert.True(primeiroCadastro);
        Assert.False(segundoCadastro);
        Assert.Single(banco.Contexto.Produtos);
    }

    [Fact]
    public void RegistrarEntrada_ComQuantidadeValida_AtualizaEstoqueECriaHistorico()
    {
        using BancoDeTeste banco = new();
        banco.Contexto.Produtos.Add(CriarProduto(codigo: 101, quantidadeEstoque: 5));
        banco.Contexto.SaveChanges();
        MovimentacaoEstoqueService service = new(banco.Contexto);

        bool registrado = service.RegistrarEntrada(101, 4, out MovimentacaoEstoque? movimentacao, out string erro);

        Assert.True(registrado);
        Assert.Equal(string.Empty, erro);
        Assert.NotNull(movimentacao);
        Assert.Equal(9, banco.Contexto.Produtos.Single().QuantidadeEstoque);
        Assert.Equal(TipoMovimentacaoEstoque.Entrada, movimentacao.Tipo);
        Assert.Equal(5, movimentacao.SaldoAnterior);
        Assert.Equal(9, movimentacao.SaldoNovo);
        Assert.Single(banco.Contexto.MovimentacoesEstoque);
    }

    [Fact]
    public void RegistrarSaida_MaiorQueEstoque_BloqueiaMovimentacao()
    {
        using BancoDeTeste banco = new();
        banco.Contexto.Produtos.Add(CriarProduto(codigo: 101, quantidadeEstoque: 5));
        banco.Contexto.SaveChanges();
        MovimentacaoEstoqueService service = new(banco.Contexto);

        bool registrado = service.RegistrarSaida(101, 6, out MovimentacaoEstoque? movimentacao, out string erro);

        Assert.False(registrado);
        Assert.Null(movimentacao);
        Assert.Equal("Não é permitido movimentar saída com estoque insuficiente.", erro);
        Assert.Equal(5, banco.Contexto.Produtos.Single().QuantidadeEstoque);
        Assert.Empty(banco.Contexto.MovimentacoesEstoque);
    }

    private static Produto CriarProduto(int codigo, int quantidadeEstoque = 1)
    {
        return new Produto
        {
            Codigo = codigo,
            Nome = "Produto de teste",
            PrecoUnitario = 10m,
            QuantidadeEstoque = quantidadeEstoque,
            CategoriaId = 1,
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