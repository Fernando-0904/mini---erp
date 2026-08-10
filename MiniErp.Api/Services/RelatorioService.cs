using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Data;
using MiniErp.Api.DTOs;

namespace MiniErp.Api.Services;

public class RelatorioService
{
    private const string PrioridadeCritico = "Crítico";
    private const string PrioridadeAtencao = "Atenção";
    private const string PrioridadeInformativo = "Informativo";
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
            string nomeProduto = CriarNomeProduto(produto);

            AdicionarAlertasEstoque(alertas, produto, nomeProduto);
            AdicionarAlertaFornecedor(alertas, produto, nomeProduto);

            if (!ultimaMovimentacaoPorProduto.TryGetValue(produto.Codigo, out DateTime ultimaMovimentacaoUtc))
            {
                AdicionarAlertaSemHistorico(alertas, produto, nomeProduto);

                continue;
            }

            int diasSemMovimentacaoProduto = (int)Math.Floor((hojeUtc - ultimaMovimentacaoUtc).TotalDays);

            AdicionarAlertaSemMovimentacaoRecente(alertas, produto, nomeProduto, diasSemMovimentacaoProduto, diasSemMovimentacao);
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
        if (string.Equals(prioridade, PrioridadeCritico, StringComparison.Ordinal))
        {
            return 0;
        }

        if (string.Equals(prioridade, PrioridadeAtencao, StringComparison.Ordinal))
        {
            return 1;
        }

        return 2;
    }

    private static string CriarNomeProduto(ProdutoAlertaInterno produto)
    {
        return $"{produto.Codigo} - {produto.Nome}";
    }

    private static void AdicionarAlertasEstoque(List<AlertaOperacionalResponse> alertas, ProdutoAlertaInterno produto, string nomeProduto)
    {
        if (produto.QuantidadeEstoque == 0)
        {
            alertas.Add(new AlertaOperacionalResponse
            {
                Prioridade = PrioridadeCritico,
                Titulo = "Produto sem estoque",
                Produto = nomeProduto,
                Detalhe = "Sem saldo disponível para venda/consumo.",
                Acao = new AlertaOperacionalAcaoResponse
                {
                    Label = "Repor estoque",
                    Href = CriarLinkMovimentacaoEntrada(produto.Codigo)
                }
            });
            return;
        }

        if (produto.EstoqueMinimo > 0 && produto.QuantidadeEstoque <= produto.EstoqueMinimo)
        {
            alertas.Add(new AlertaOperacionalResponse
            {
                Prioridade = PrioridadeAtencao,
                Titulo = "Estoque abaixo do mínimo",
                Produto = nomeProduto,
                Detalhe = $"Saldo {produto.QuantidadeEstoque} (mínimo {produto.EstoqueMinimo}).",
                Acao = new AlertaOperacionalAcaoResponse
                {
                    Label = "Planejar reposição",
                    Href = CriarLinkMovimentacaoEntrada(produto.Codigo)
                }
            });
        }
    }

    private static void AdicionarAlertaFornecedor(List<AlertaOperacionalResponse> alertas, ProdutoAlertaInterno produto, string nomeProduto)
    {
        if (produto.FornecedorId is not null && !string.IsNullOrWhiteSpace(produto.FornecedorNome))
        {
            return;
        }

        alertas.Add(new AlertaOperacionalResponse
        {
            Prioridade = PrioridadeAtencao,
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

    private static void AdicionarAlertaSemHistorico(List<AlertaOperacionalResponse> alertas, ProdutoAlertaInterno produto, string nomeProduto)
    {
        if (produto.QuantidadeEstoque <= 0)
        {
            return;
        }

        alertas.Add(new AlertaOperacionalResponse
        {
            Prioridade = PrioridadeInformativo,
            Titulo = "Produto sem histórico",
            Produto = nomeProduto,
            Detalhe = "Sem movimentações registradas até o momento.",
            Acao = new AlertaOperacionalAcaoResponse
            {
                Label = "Ver histórico",
                Href = CriarLinkHistoricoMovimentacao(produto.Codigo)
            }
        });
    }

    private static void AdicionarAlertaSemMovimentacaoRecente(
        List<AlertaOperacionalResponse> alertas,
        ProdutoAlertaInterno produto,
        string nomeProduto,
        int diasSemMovimentacaoProduto,
        int diasSemMovimentacao)
    {
        if (diasSemMovimentacaoProduto <= diasSemMovimentacao)
        {
            return;
        }

        alertas.Add(new AlertaOperacionalResponse
        {
            Prioridade = PrioridadeInformativo,
            Titulo = "Sem movimentação recente",
            Produto = nomeProduto,
            Detalhe = $"Última movimentação há {diasSemMovimentacaoProduto} dias.",
            Acao = new AlertaOperacionalAcaoResponse
            {
                Label = "Analisar histórico",
                Href = CriarLinkHistoricoMovimentacao(produto.Codigo)
            }
        });
    }

    private static string CriarLinkMovimentacaoEntrada(int produtoCodigo)
    {
        return $"movimentacoes.html?produtoCodigo={produtoCodigo}&acao=entrada&autoHistorico=1";
    }

    private static string CriarLinkHistoricoMovimentacao(int produtoCodigo)
    {
        return $"movimentacoes.html?produtoCodigo={produtoCodigo}&autoHistorico=1";
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