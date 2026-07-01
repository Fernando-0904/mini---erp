using MiniErp.Api.Models;

namespace MiniErp.Api.Services;

public class ProdutoService
{
    private readonly List<Produto> produtos = new List<Produto>();

    public List<Produto> ListarProdutos()
    {
        return produtos;
    }

    public Produto? BuscarPorCodigo(int codigo)
    {
        foreach (Produto produto in produtos)
        {
            if (produto.Codigo == codigo)
            {
                return produto;
            }
        }

        return null;
    }

    public bool CadastrarProduto(Produto produto)
    {
        if (BuscarPorCodigo(produto.Codigo) != null)
        {
            return false;
        }

        produtos.Add(produto);
        return true;
    }

    public bool EditarProduto(int codigo, Produto produtoAtualizado)
    {
        Produto? produto = BuscarPorCodigo(codigo);

        if (produto == null)
        {
            return false;
        }

        produto.Nome = produtoAtualizado.Nome;
        produto.PrecoUnitario = produtoAtualizado.PrecoUnitario;
        produto.QuantidadeEstoque = produtoAtualizado.QuantidadeEstoque;

        return true;
    }

    public bool RemoverProduto(int codigo)
    {
        Produto? produto = BuscarPorCodigo(codigo);

        if (produto == null)
        {
            return false;
        }

        produtos.Remove(produto);
        return true;
    }
}
