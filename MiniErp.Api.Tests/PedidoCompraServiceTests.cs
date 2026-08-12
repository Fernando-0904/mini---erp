using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Data;
using MiniErp.Api.DTOs;
using MiniErp.Api.Models;
using MiniErp.Api.Services;
using Xunit;

namespace MiniErp.Api.Tests;

public class PedidoCompraServiceTests
{
    [Fact]
    public async Task CriarPedidoAsync_ComDadosValidos_PersistePedidoAberto()
    {
        using BancoDeTeste banco = new();
        PedidoCompraService service = banco.CriarService();

        PedidoCompraRequest request = new()
        {
            FornecedorId = banco.FornecedorAtivo.Id,
            Itens =
            [
                new PedidoCompraItemRequest { ProdutoCodigo = banco.ProdutoA.Codigo, Quantidade = 3 },
                new PedidoCompraItemRequest { ProdutoCodigo = banco.ProdutoB.Codigo, Quantidade = 2 },
            ]
        };

        (PedidoCompraResponse? pedido, List<string> erros) = await service.CriarPedidoAsync(request);

        Assert.Empty(erros);
        Assert.NotNull(pedido);
        Assert.Equal("PendenteAprovacao", pedido!.Status);
        Assert.Equal(2, pedido.Itens.Count);
        Assert.Equal(0, banco.Contexto.MovimentacoesEstoque.Count());
    }

    [Fact]
    public async Task CriarPedidoAsync_ComFornecedorInexistente_RetornaErro()
    {
        using BancoDeTeste banco = new();
        PedidoCompraService service = banco.CriarService();

        PedidoCompraRequest request = new()
        {
            FornecedorId = 999,
            Itens = [new PedidoCompraItemRequest { ProdutoCodigo = banco.ProdutoA.Codigo, Quantidade = 1 }]
        };

        (PedidoCompraResponse? pedido, List<string> erros) = await service.CriarPedidoAsync(request);

        Assert.Null(pedido);
        Assert.Contains("Fornecedor não encontrado.", erros);
    }

    [Fact]
    public async Task ReceberPedidoAsync_ComPedidoAberto_AtualizaEstoqueEMovimentacoes()
    {
        using BancoDeTeste banco = new();
        PedidoCompraService service = banco.CriarService();

        PedidoCompraRequest request = new()
        {
            FornecedorId = banco.FornecedorAtivo.Id,
            Itens =
            [
                new PedidoCompraItemRequest { ProdutoCodigo = banco.ProdutoA.Codigo, Quantidade = 4 },
                new PedidoCompraItemRequest { ProdutoCodigo = banco.ProdutoB.Codigo, Quantidade = 1 },
            ]
        };

        (PedidoCompraResponse? criado, List<string> errosCriacao) = await service.CriarPedidoAsync(request);
        Assert.Empty(errosCriacao);
        Assert.NotNull(criado);

        (PedidoCompraResponse? aprovado, string erroAprovacao) = await service.AprovarPedidoAsync(criado!.Id);
        Assert.Equal(string.Empty, erroAprovacao);
        Assert.NotNull(aprovado);
        Assert.Equal("Aprovado", aprovado!.Status);

        (PedidoCompraResponse? recebido, string erroRecebimento) = await service.ReceberPedidoAsync(criado!.Id);

        Assert.Equal(string.Empty, erroRecebimento);
        Assert.NotNull(recebido);
        Assert.Equal("Recebido", recebido!.Status);

        Produto produtoA = banco.Contexto.Produtos.Single(item => item.Codigo == banco.ProdutoA.Codigo);
        Produto produtoB = banco.Contexto.Produtos.Single(item => item.Codigo == banco.ProdutoB.Codigo);

        Assert.Equal(9, produtoA.QuantidadeEstoque);
        Assert.Equal(4, produtoB.QuantidadeEstoque);
        Assert.Equal(2, banco.Contexto.MovimentacoesEstoque.Count());
    }

    [Fact]
    public async Task ReceberPedidoAsync_SemAprovacao_RetornaErro()
    {
        using BancoDeTeste banco = new();
        PedidoCompraService service = banco.CriarService();

        PedidoCompraRequest request = new()
        {
            FornecedorId = banco.FornecedorAtivo.Id,
            Itens = [new PedidoCompraItemRequest { ProdutoCodigo = banco.ProdutoA.Codigo, Quantidade = 2 }]
        };

        (PedidoCompraResponse? criado, List<string> errosCriacao) = await service.CriarPedidoAsync(request);
        Assert.Empty(errosCriacao);
        Assert.NotNull(criado);

        (PedidoCompraResponse? recebido, string erroRecebimento) = await service.ReceberPedidoAsync(criado!.Id);

        Assert.Null(recebido);
        Assert.Equal("Apenas pedidos aprovados podem ser recebidos.", erroRecebimento);
    }

    [Fact]
    public async Task RejeitarPedidoAsync_ComPedidoPendente_AtualizaStatus()
    {
        using BancoDeTeste banco = new();
        PedidoCompraService service = banco.CriarService();

        PedidoCompraRequest request = new()
        {
            FornecedorId = banco.FornecedorAtivo.Id,
            Itens = [new PedidoCompraItemRequest { ProdutoCodigo = banco.ProdutoA.Codigo, Quantidade = 1 }]
        };

        (PedidoCompraResponse? criado, List<string> errosCriacao) = await service.CriarPedidoAsync(request);
        Assert.Empty(errosCriacao);
        Assert.NotNull(criado);

        (PedidoCompraResponse? rejeitado, string erroRejeicao) = await service.RejeitarPedidoAsync(criado!.Id, "Fornecedor sem prazo de entrega confiável.");

        Assert.Equal(string.Empty, erroRejeicao);
        Assert.NotNull(rejeitado);
        Assert.Equal("Rejeitado", rejeitado!.Status);
        Assert.Equal("Fornecedor sem prazo de entrega confiável.", rejeitado.MotivoRejeicao);
        Assert.NotNull(rejeitado.RejeitadoEmUtc);
    }

    [Fact]
    public async Task RejeitarPedidoAsync_SemMotivo_RetornaErro()
    {
        using BancoDeTeste banco = new();
        PedidoCompraService service = banco.CriarService();

        PedidoCompraRequest request = new()
        {
            FornecedorId = banco.FornecedorAtivo.Id,
            Itens = [new PedidoCompraItemRequest { ProdutoCodigo = banco.ProdutoA.Codigo, Quantidade = 1 }]
        };

        (PedidoCompraResponse? criado, List<string> errosCriacao) = await service.CriarPedidoAsync(request);
        Assert.Empty(errosCriacao);
        Assert.NotNull(criado);

        (PedidoCompraResponse? rejeitado, string erroRejeicao) = await service.RejeitarPedidoAsync(criado!.Id, "   ");

        Assert.Null(rejeitado);
        Assert.Equal("O motivo da rejeição é obrigatório.", erroRejeicao);
    }

    [Fact]
    public async Task ReceberPedidoAsync_QuandoFalhaItemNoMeio_DesfazMovimentacoesParciais()
    {
        using BancoDeTeste banco = new();
        PedidoCompraService service = banco.CriarService();

        PedidoCompraRequest request = new()
        {
            FornecedorId = banco.FornecedorAtivo.Id,
            Itens =
            [
                new PedidoCompraItemRequest { ProdutoCodigo = banco.ProdutoA.Codigo, Quantidade = 2 },
                new PedidoCompraItemRequest { ProdutoCodigo = banco.ProdutoB.Codigo, Quantidade = 1 },
            ]
        };

        (PedidoCompraResponse? criado, List<string> errosCriacao) = await service.CriarPedidoAsync(request);
        Assert.Empty(errosCriacao);
        Assert.NotNull(criado);

        (PedidoCompraResponse? aprovado, string erroAprovacao) = await service.AprovarPedidoAsync(criado!.Id);
        Assert.Equal(string.Empty, erroAprovacao);
        Assert.NotNull(aprovado);

        PedidoCompraItem itemCorrompido = banco.Contexto.PedidosCompra
            .Include(pedido => pedido.Itens)
            .Single(pedido => pedido.Id == criado.Id)
            .Itens
            .Single(item => item.ProdutoCodigo == banco.ProdutoB.Codigo);

        itemCorrompido.Quantidade = 0;
        banco.Contexto.SaveChanges();

        (PedidoCompraResponse? recebido, string erroRecebimento) = await service.ReceberPedidoAsync(criado.Id);

        Assert.Null(recebido);
        Assert.Contains("Não foi possível receber o pedido", erroRecebimento);

        Produto produtoA = banco.Contexto.Produtos.Single(item => item.Codigo == banco.ProdutoA.Codigo);
        Produto produtoB = banco.Contexto.Produtos.Single(item => item.Codigo == banco.ProdutoB.Codigo);

        Assert.Equal(5, produtoA.QuantidadeEstoque);
        Assert.Equal(3, produtoB.QuantidadeEstoque);
        Assert.Equal(0, banco.Contexto.MovimentacoesEstoque.Count());

        PedidoCompra pedido = banco.Contexto.PedidosCompra.Single(item => item.Id == criado.Id);
        Assert.Equal(PedidoCompraStatus.Aprovado, pedido.Status);
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

            Categoria categoria = new() { Nome = "Categoria base" };
            Contexto.Categorias.Add(categoria);
            Contexto.SaveChanges();

            FornecedorAtivo = new Fornecedor
            {
                Codigo = 100,
                Nome = "Fornecedor ativo",
                Documento = "12345678000199",
                Ativo = true,
            };
            Contexto.Fornecedores.Add(FornecedorAtivo);
            Contexto.SaveChanges();

            ProdutoA = new Produto
            {
                Codigo = 1001,
                Nome = "Produto A",
                PrecoUnitario = 10m,
                QuantidadeEstoque = 5,
                CategoriaId = categoria.Id,
            };

            ProdutoB = new Produto
            {
                Codigo = 1002,
                Nome = "Produto B",
                PrecoUnitario = 20m,
                QuantidadeEstoque = 3,
                CategoriaId = categoria.Id,
            };

            Contexto.Produtos.AddRange(ProdutoA, ProdutoB);
            Contexto.SaveChanges();
        }

        public AppDbContext Contexto { get; }
        public Fornecedor FornecedorAtivo { get; }
        public Produto ProdutoA { get; }
        public Produto ProdutoB { get; }

        public PedidoCompraService CriarService()
        {
            MovimentacaoEstoqueService movimentacao = new(Contexto);
            return new PedidoCompraService(Contexto, movimentacao);
        }

        public void Dispose()
        {
            Contexto.Dispose();
            connection.Dispose();
        }
    }
}
