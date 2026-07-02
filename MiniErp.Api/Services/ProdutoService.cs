using System.Text.Json;
using MiniErp.Api.Models;

namespace MiniErp.Api.Services;

public class ProdutoService
{
    private readonly string caminhoArquivo;
    private readonly List<Produto> produtos;

    public ProdutoService(IWebHostEnvironment ambiente)
    {
        string pastaBaseApi = ambiente.ContentRootPath;

        if (!Directory.Exists(Path.Combine(pastaBaseApi, "Services")))
        {
            pastaBaseApi = Path.Combine(pastaBaseApi, "MiniErp.Api");
        }

        string pastaDados = Path.Combine(pastaBaseApi, "Dados");

        Directory.CreateDirectory(pastaDados);

        caminhoArquivo = Path.Combine(pastaDados, "produtos.json");
        produtos = CarregarProdutosDoArquivo();
    }

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
        SalvarProdutosNoArquivo();

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

        SalvarProdutosNoArquivo();

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
        SalvarProdutosNoArquivo();

        return true;
    }

    private List<Produto> CarregarProdutosDoArquivo()
    {
        if (!File.Exists(caminhoArquivo))
        {
            return new List<Produto>();
        }

        try
        {
            string json = File.ReadAllText(caminhoArquivo);
            List<Produto>? produtosSalvos = JsonSerializer.Deserialize<List<Produto>>(json);

            return produtosSalvos ?? new List<Produto>();
        }
        catch
        {
            return new List<Produto>();
        }
    }

    private void SalvarProdutosNoArquivo()
    {
        JsonSerializerOptions opcoes = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(produtos, opcoes);
        File.WriteAllText(caminhoArquivo, json);
    }
}
