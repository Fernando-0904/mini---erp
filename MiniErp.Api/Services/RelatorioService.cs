using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Data;
using MiniErp.Api.DTOs;

namespace MiniErp.Api.Services;

public class RelatorioService
{
    private readonly AppDbContext contexto;

    public RelatorioService(AppDbContext contexto)
    {
        this.contexto = contexto;
    }

    public async Task<List<ProdutoEstoqueBaixoResponse>> ListarProdutosEstoqueBaixoAsync()
    {
        return await contexto.Produtos
            .AsNoTracking()
            .Where(produto => produto.QuantidadeEstoque <= produto.EstoqueMinimo)
            .OrderBy(produto => produto.QuantidadeEstoque)
            .ThenBy(produto => produto.Nome)
            .Select(produto => new ProdutoEstoqueBaixoResponse
            {
                Codigo = produto.Codigo,
                Nome = produto.Nome,
                QuantidadeEstoque = produto.QuantidadeEstoque,
                EstoqueMinimo = produto.EstoqueMinimo,
                Categoria = produto.Categoria != null
                    ? produto.Categoria.Nome
                    : "Sem categoria"
            })
            .ToListAsync();
    }

    public async Task<List<ProdutoSemEstoqueResponse>> ListarProdutosSemEstoqueAsync()
    {
        return await contexto.Produtos
            .AsNoTracking()
            .Where(produto => produto.QuantidadeEstoque == 0)
            .OrderBy(produto => produto.Nome)
            .Select(produto => new ProdutoSemEstoqueResponse
            {
                Codigo = produto.Codigo,
                Nome = produto.Nome,
                QuantidadeEstoque = produto.QuantidadeEstoque,
                Categoria = produto.Categoria != null
                    ? produto.Categoria.Nome
                    : "Sem categoria"
            })
            .ToListAsync();
    }

    public async Task<List<ValorEstoquePorCategoriaResponse>> ListarValorEstoquePorCategoriaAsync()
    {
        return await contexto.Categorias
            .AsNoTracking()
            .Select(categoria => new ValorEstoquePorCategoriaResponse
            {
                Categoria = categoria.Nome,
                ValorTotal = contexto.Produtos
                    .AsNoTracking()
                    .Where(produto => produto.CategoriaId == categoria.Id)
                    .Select(produto => (decimal?)produto.PrecoUnitario * produto.QuantidadeEstoque)
                    .Sum() ?? 0m
            })
            .OrderByDescending(item => item.ValorTotal)
            .ThenBy(item => item.Categoria)
            .ToListAsync();
    }

    public async Task<List<ProdutoSemFornecedorResponse>> ListarProdutosSemFornecedorAsync()
    {
        return await contexto.Produtos
            .AsNoTracking()
            .Where(produto => produto.FornecedorId == null)
            .OrderBy(produto => produto.Nome)
            .Select(produto => new ProdutoSemFornecedorResponse
            {
                Codigo = produto.Codigo,
                Nome = produto.Nome,
                Categoria = produto.Categoria != null
                    ? produto.Categoria.Nome
                    : "Sem categoria"
            })
            .ToListAsync();
    }

    public async Task<List<UltimaMovimentacaoResponse>> ListarUltimasMovimentacoesAsync(int limite)
    {
        int limiteAjustado = limite <= 0 ? 10 : Math.Min(limite, 100);

        return await contexto.MovimentacoesEstoque
            .AsNoTracking()
            .OrderByDescending(movimentacao => movimentacao.DataMovimentacaoUtc)
            .Take(limiteAjustado)
            .Select(movimentacao => new UltimaMovimentacaoResponse
            {
                Produto = movimentacao.Produto != null
                    ? movimentacao.Produto.Nome
                    : string.Empty,
                Tipo = movimentacao.Tipo.ToString(),
                Quantidade = movimentacao.Quantidade,
                SaldoAnterior = movimentacao.SaldoAnterior,
                SaldoNovo = movimentacao.SaldoNovo,
                DataMovimentacaoUtc = movimentacao.DataMovimentacaoUtc
            })
            .ToListAsync();
    }
}