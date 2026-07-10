using MiniErp.Api.Data;
using MiniErp.Api.Models;

namespace MiniErp.Api.Services;

public class MovimentacaoEstoqueService
{
    private readonly AppDbContext contexto;

    public MovimentacaoEstoqueService(AppDbContext contexto)
    {
        this.contexto = contexto;
    }

    public List<MovimentacaoEstoque> ListarMovimentacoesPorProduto(int codigoProduto)
    {
        return contexto.MovimentacoesEstoque
            .Where(movimentacao => movimentacao.ProdutoCodigo == codigoProduto)
            .OrderByDescending(movimentacao => movimentacao.DataMovimentacaoUtc)
            .ToList();
    }

    public bool RegistrarEntrada(int codigoProduto, int quantidade, out MovimentacaoEstoque? movimentacao, out string erro)
    {
        return RegistrarMovimentacao(codigoProduto, quantidade, TipoMovimentacaoEstoque.Entrada, out movimentacao, out erro);
    }

    public bool RegistrarSaida(int codigoProduto, int quantidade, out MovimentacaoEstoque? movimentacao, out string erro)
    {
        return RegistrarMovimentacao(codigoProduto, quantidade, TipoMovimentacaoEstoque.Saida, out movimentacao, out erro);
    }

    private bool RegistrarMovimentacao(
        int codigoProduto,
        int quantidade,
        TipoMovimentacaoEstoque tipo,
        out MovimentacaoEstoque? movimentacao,
        out string erro)
    {
        movimentacao = null;
        erro = string.Empty;

        if (quantidade <= 0)
        {
            erro = "A quantidade da movimentação deve ser maior que zero.";
            return false;
        }

        Produto? produto = contexto.Produtos.FirstOrDefault(item => item.Codigo == codigoProduto);

        if (produto == null)
        {
            erro = "Produto não encontrado.";
            return false;
        }

        int saldoAnterior = produto.QuantidadeEstoque;
        int saldoNovo = tipo == TipoMovimentacaoEstoque.Entrada
            ? saldoAnterior + quantidade
            : saldoAnterior - quantidade;

        if (saldoNovo < 0)
        {
            erro = "Não é permitido movimentar saída com estoque insuficiente.";
            return false;
        }

        produto.QuantidadeEstoque = saldoNovo;

        movimentacao = new MovimentacaoEstoque
        {
            ProdutoCodigo = codigoProduto,
            Tipo = tipo,
            Quantidade = quantidade,
            SaldoAnterior = saldoAnterior,
            SaldoNovo = saldoNovo,
            DataMovimentacaoUtc = DateTime.UtcNow,
        };

        contexto.MovimentacoesEstoque.Add(movimentacao);
        contexto.SaveChanges();
        return true;
    }
}
