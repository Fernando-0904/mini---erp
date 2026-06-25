namespace projetoerp;

public class ProdutoService
{
    private readonly List<Produto> produtos = new List<Produto>();

    public List<Produto> ListarProdutos()
    {
        return produtos;
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

    public List<Produto> BuscarPorNome(string termoBuscado)
    {
        List<Produto> produtosEncontrados = new List<Produto>();
        string termoNormalizado = termoBuscado.Trim().ToLower();

        foreach (Produto produto in produtos)
        {
            if (produto.Nome.Trim().ToLower().Contains(termoNormalizado))
            {
                produtosEncontrados.Add(produto);
            }
        }

        return produtosEncontrados;
    }

    public bool EditarProduto(int codigo, string novoNome, decimal novoPrecoUnitario, int novaQuantidadeEstoque)
    {
        Produto? produto = BuscarPorCodigo(codigo);

        if (produto == null)
        {
            return false;
        }

        produto.Nome = novoNome;
        produto.PrecoUnitario = novoPrecoUnitario;
        produto.QuantidadeEstoque = novaQuantidadeEstoque;

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

    public decimal CalcularValorTotalEstoque()
    {
        decimal valorTotal = 0;

        foreach (Produto produto in produtos)
        {
            valorTotal += produto.CalcularValorTotal();
        }

        return valorTotal;
    }

    public int CalcularTotalItensEstoque()
    {
        int totalItens = 0;

        foreach (Produto produto in produtos)
        {
            totalItens += produto.QuantidadeEstoque;
        }

        return totalItens;
    }

    public List<Produto> ListarProdutosComEstoqueBaixo(int limite)
    {
        List<Produto> produtosComEstoqueBaixo = new List<Produto>();

        foreach (Produto produto in produtos)
        {
            if (produto.QuantidadeEstoque < limite)
            {
                produtosComEstoqueBaixo.Add(produto);
            }
        }

        return produtosComEstoqueBaixo;
    }
}
