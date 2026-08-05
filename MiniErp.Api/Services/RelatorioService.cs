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

    public async Task<List<AlertaOperacionalResponse>> ListarAlertasOperacionaisAsync(int diasSemMovimentacao = 30)
    {
        List<ProdutoAlertaInterno> produtos = await contexto.Produtos
            .AsNoTracking()
            .Select(produto => new ProdutoAlertaInterno
            {
                Codigo = produto.Codigo,
                Nome = produto.Nome,
                QuantidadeEstoque = produto.QuantidadeEstoque,
                EstoqueMinimo = produto.EstoqueMinimo,
                FornecedorId = produto.FornecedorId,
                FornecedorNome = produto.Fornecedor != null ? produto.Fornecedor.Nome : string.Empty
            })
            .ToListAsync();

        Dictionary<int, DateTime> ultimaMovimentacaoPorProduto = await contexto.MovimentacoesEstoque
            .AsNoTracking()
            .GroupBy(movimentacao => movimentacao.ProdutoCodigo)
            .Select(grupo => new { ProdutoCodigo = grupo.Key, Data = grupo.Max(movimentacao => movimentacao.DataMovimentacaoUtc) })
            .ToDictionaryAsync(item => item.ProdutoCodigo, item => item.Data);

        DateTime hojeUtc = DateTime.UtcNow;
        List<AlertaOperacionalResponse> alertas = [];

        foreach (ProdutoAlertaInterno produto in produtos)
        {
            string nomeProduto = $"{produto.Codigo} - {produto.Nome}";

            if (produto.QuantidadeEstoque == 0)
            {
                alertas.Add(new AlertaOperacionalResponse
                {
                    Prioridade = "Crítico",
                    Titulo = "Produto sem estoque",
                    Produto = nomeProduto,
                    Detalhe = "Sem saldo disponível para venda/consumo.",
                    Acao = new AlertaOperacionalAcaoResponse
                    {
                        Label = "Repor estoque",
                        Href = $"movimentacoes.html?produtoCodigo={produto.Codigo}&acao=entrada&autoHistorico=1"
                    }
                });
            }
            else if (produto.EstoqueMinimo > 0 && produto.QuantidadeEstoque <= produto.EstoqueMinimo)
            {
                alertas.Add(new AlertaOperacionalResponse
                {
                    Prioridade = "Atenção",
                    Titulo = "Estoque abaixo do mínimo",
                    Produto = nomeProduto,
                    Detalhe = $"Saldo {produto.QuantidadeEstoque} (mínimo {produto.EstoqueMinimo}).",
                    Acao = new AlertaOperacionalAcaoResponse
                    {
                        Label = "Planejar reposição",
                        Href = $"movimentacoes.html?produtoCodigo={produto.Codigo}&acao=entrada&autoHistorico=1"
                    }
                });
            }

            if (produto.FornecedorId is null || string.IsNullOrWhiteSpace(produto.FornecedorNome))
            {
                alertas.Add(new AlertaOperacionalResponse
                {
                    Prioridade = "Atenção",
                    Titulo = "Produto sem fornecedor",
                    Produto = nomeProduto,
                    Detalhe = "Vincule um fornecedor para agilizar futuras reposições.",
                    Acao = new AlertaOperacionalAcaoResponse
                    {
                        Label = "Editar produto",
                        Href = $"produtos.html?codigoEdicao={produto.Codigo}"
                    }
                });
            }

            if (!ultimaMovimentacaoPorProduto.TryGetValue(produto.Codigo, out DateTime ultimaMovimentacaoUtc))
            {
                if (produto.QuantidadeEstoque > 0)
                {
                    alertas.Add(new AlertaOperacionalResponse
                    {
                        Prioridade = "Informativo",
                        Titulo = "Produto sem histórico",
                        Produto = nomeProduto,
                        Detalhe = "Sem movimentações registradas até o momento.",
                        Acao = new AlertaOperacionalAcaoResponse
                        {
                            Label = "Ver histórico",
                            Href = $"movimentacoes.html?produtoCodigo={produto.Codigo}&autoHistorico=1"
                        }
                    });
                }

                continue;
            }

            int diasSemMovimentacaoProduto = (int)Math.Floor((hojeUtc - ultimaMovimentacaoUtc).TotalDays);

            if (diasSemMovimentacaoProduto > diasSemMovimentacao)
            {
                alertas.Add(new AlertaOperacionalResponse
                {
                    Prioridade = "Informativo",
                    Titulo = "Sem movimentação recente",
                    Produto = nomeProduto,
                    Detalhe = $"Última movimentação há {diasSemMovimentacaoProduto} dias.",
                    Acao = new AlertaOperacionalAcaoResponse
                    {
                        Label = "Analisar histórico",
                        Href = $"movimentacoes.html?produtoCodigo={produto.Codigo}&autoHistorico=1"
                    }
                });
            }
        }

        return alertas
            .OrderBy(alerta => PesoPrioridade(alerta.Prioridade))
            .ThenBy(alerta => alerta.Produto)
            .ToList();
    }

    public async Task<List<AuditoriaEventoResponse>> ListarAuditoriaAsync(int limite)
    {
        int limiteAjustado = limite <= 0 ? 30 : Math.Min(limite, 200);

        return await contexto.AuditoriaEventos
            .AsNoTracking()
            .OrderByDescending(evento => evento.DataUtc)
            .Take(limiteAjustado)
            .Select(evento => new AuditoriaEventoResponse
            {
                Id = evento.Id,
                Acao = evento.Acao,
                Entidade = evento.Entidade,
                EntidadeId = evento.EntidadeId,
                Descricao = evento.Descricao,
                UsuarioId = evento.UsuarioId,
                UsuarioEmail = evento.UsuarioEmail,
                DataUtc = evento.DataUtc
            })
            .ToListAsync();
    }

    private static int PesoPrioridade(string prioridade)
    {
        if (string.Equals(prioridade, "Crítico", StringComparison.Ordinal))
        {
            return 0;
        }

        if (string.Equals(prioridade, "Atenção", StringComparison.Ordinal))
        {
            return 1;
        }

        return 2;
    }

    private sealed class ProdutoAlertaInterno
    {
        public int Codigo { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int QuantidadeEstoque { get; set; }
        public int EstoqueMinimo { get; set; }
        public int? FornecedorId { get; set; }
        public string FornecedorNome { get; set; } = string.Empty;
    }
}