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
    public void CadastrarProduto_ComDadosValidos_RetornaTrue()
    {
        using BancoDeTeste banco = new();
        ProdutoService service = new(banco.Contexto);

        bool cadastrado = service.CadastrarProduto(CriarProduto(codigo: 101));

        Assert.True(cadastrado);
        Assert.Single(banco.Contexto.Produtos);
    }

    [Fact]
    public void ValidarProduto_SemCategoria_RetornaErro()
    {
        using BancoDeTeste banco = new();
        ProdutoService service = new(banco.Contexto);
        Produto produto = CriarProduto(codigo: 101);
        produto.CategoriaId = 0;

        List<string> erros = service.ValidarProduto(produto);
        CategoriaService categoriaService = new(banco.Contexto);
        erros.AddRange(categoriaService.ValidarCategoriaDoProduto(produto.CategoriaId));

        Assert.Contains("Informe uma categoria válida.", erros);
    }

    [Fact]
    public void ValidarCategoriaDoProduto_ComCategoriaInexistente_RetornaErro()
    {
        using BancoDeTeste banco = new();
        CategoriaService service = new(banco.Contexto);

        List<string> erros = service.ValidarCategoriaDoProduto(999);

        Assert.Contains("Categoria informada não existe.", erros);
    }

    [Fact]
    public void ValidarProduto_ComPrecoInvalido_RetornaErro()
    {
        using BancoDeTeste banco = new();
        ProdutoService service = new(banco.Contexto);
        Produto produto = CriarProduto(codigo: 101);
        produto.PrecoUnitario = 0;

        List<string> erros = service.ValidarProduto(produto);

        Assert.Contains("O preço unitário deve ser maior que zero.", erros);
    }

    [Fact]
    public void ValidarProduto_ComQuantidadeNegativa_RetornaErro()
    {
        using BancoDeTeste banco = new();
        ProdutoService service = new(banco.Contexto);
        Produto produto = CriarProduto(codigo: 101, quantidadeEstoque: -1);

        List<string> erros = service.ValidarProduto(produto);

        Assert.Contains("A quantidade em estoque não pode ser negativa.", erros);
    }

    [Fact]
    public void ValidarProduto_ComEstoqueMinimoNegativo_RetornaErro()
    {
        using BancoDeTeste banco = new();
        ProdutoService service = new(banco.Contexto);
        Produto produto = CriarProduto(codigo: 101);
        produto.EstoqueMinimo = -1;

        List<string> erros = service.ValidarProduto(produto);

        Assert.Contains("O estoque mínimo não pode ser negativo.", erros);
    }

    [Fact]
    public void EditarProduto_ComQuantidadeDiferente_PreservaEstoqueAtual()
    {
        using BancoDeTeste banco = new();
        banco.Contexto.Produtos.Add(CriarProduto(codigo: 101, quantidadeEstoque: 5));
        banco.Contexto.SaveChanges();
        ProdutoService service = new(banco.Contexto);
        Produto produtoAtualizado = CriarProduto(codigo: 101, quantidadeEstoque: 20);
        produtoAtualizado.Nome = "Produto atualizado";

        bool editado = service.EditarProduto(101, produtoAtualizado);

        Assert.True(editado);
        Produto produtoPersistido = banco.Contexto.Produtos.Single();
        Assert.Equal("Produto atualizado", produtoPersistido.Nome);
        Assert.Equal(5, produtoPersistido.QuantidadeEstoque);
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

    [Fact]
    public void RegistrarSaida_ComQuantidadeValida_AtualizaEstoqueECriaHistorico()
    {
        using BancoDeTeste banco = new();
        banco.Contexto.Produtos.Add(CriarProduto(codigo: 101, quantidadeEstoque: 5));
        banco.Contexto.SaveChanges();
        MovimentacaoEstoqueService service = new(banco.Contexto);

        bool registrado = service.RegistrarSaida(101, 2, out MovimentacaoEstoque? movimentacao, out string erro);

        Assert.True(registrado);
        Assert.Equal(string.Empty, erro);
        Assert.NotNull(movimentacao);
        Assert.Equal(3, banco.Contexto.Produtos.Single().QuantidadeEstoque);
        Assert.Equal(TipoMovimentacaoEstoque.Saida, movimentacao.Tipo);
        Assert.Equal(5, movimentacao.SaldoAnterior);
        Assert.Equal(3, movimentacao.SaldoNovo);
    }

    [Fact]
    public void ListarMovimentacoesPorProduto_ComHistorico_RetornaMovimentacoesDoProduto()
    {
        using BancoDeTeste banco = new();
        banco.Contexto.Produtos.Add(CriarProduto(codigo: 101, quantidadeEstoque: 5));
        banco.Contexto.Produtos.Add(CriarProduto(codigo: 102, quantidadeEstoque: 5));
        banco.Contexto.SaveChanges();
        MovimentacaoEstoqueService service = new(banco.Contexto);
        service.RegistrarEntrada(101, 2, out _, out _);
        service.RegistrarEntrada(102, 3, out _, out _);

        List<MovimentacaoEstoque> movimentacoes = service.ListarMovimentacoesPorProduto(101);

        Assert.Single(movimentacoes);
        Assert.Equal(101, movimentacoes[0].ProdutoCodigo);
        Assert.Equal(TipoMovimentacaoEstoque.Entrada, movimentacoes[0].Tipo);
    }

    [Fact]
    public void ListarProdutosComEstoqueBaixo_RetornaAbaixoOuIgualAoMinimoOrdenados()
    {
        using BancoDeTeste banco = new();
        banco.Contexto.Produtos.Add(CriarProduto(codigo: 101, quantidadeEstoque: 2, estoqueMinimo: 3));
        banco.Contexto.Produtos.Add(CriarProduto(codigo: 102, quantidadeEstoque: 0, estoqueMinimo: 0));
        banco.Contexto.Produtos.Add(CriarProduto(codigo: 103, quantidadeEstoque: 5, estoqueMinimo: 3));
        banco.Contexto.SaveChanges();
        ProdutoService service = new(banco.Contexto);

        List<Produto> produtos = service.ListarProdutosComEstoqueBaixo();

        Assert.Equal(new[] { 102, 101 }, produtos.Select(produto => produto.Codigo));
    }

    [Fact]
    public void ListarProdutosComEstoqueBaixo_ComFiltroDeCategoria_RetornaSomenteCategoriaSelecionada()
    {
        using BancoDeTeste banco = new();
        Categoria outraCategoria = new() { Nome = "Outra categoria" };
        banco.Contexto.Categorias.Add(outraCategoria);
        banco.Contexto.SaveChanges();
        banco.Contexto.Produtos.Add(CriarProduto(codigo: 101, quantidadeEstoque: 1, estoqueMinimo: 2));
        banco.Contexto.Produtos.Add(CriarProduto(codigo: 102, quantidadeEstoque: 1, estoqueMinimo: 2, categoriaId: outraCategoria.Id));
        banco.Contexto.SaveChanges();
        ProdutoService service = new(banco.Contexto);

        List<Produto> produtos = service.ListarProdutosComEstoqueBaixo(outraCategoria.Id);

        Assert.Single(produtos);
        Assert.Equal(102, produtos[0].Codigo);
    }

    [Fact]
    public void ListarProdutosSemEstoque_RetornaSomenteProdutosComSaldoZero()
    {
        using BancoDeTeste banco = new();
        banco.Contexto.Produtos.Add(CriarProduto(codigo: 101, quantidadeEstoque: 0));
        banco.Contexto.Produtos.Add(CriarProduto(codigo: 102, quantidadeEstoque: 1));
        banco.Contexto.SaveChanges();
        ProdutoService service = new(banco.Contexto);

        List<Produto> produtos = service.ListarProdutosSemEstoque();

        Assert.Single(produtos);
        Assert.Equal(101, produtos[0].Codigo);
        Assert.Equal(0, produtos[0].QuantidadeEstoque);
    }

    private static Produto CriarProduto(int codigo, int quantidadeEstoque = 1, int estoqueMinimo = 0, int categoriaId = 1)
    {
        return new Produto
        {
            Codigo = codigo,
            Nome = "Produto de teste",
            PrecoUnitario = 10m,
            QuantidadeEstoque = quantidadeEstoque,
            EstoqueMinimo = estoqueMinimo,
            CategoriaId = categoriaId,
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