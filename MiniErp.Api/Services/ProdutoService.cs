using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Data;
using MiniErp.Api.Models;

namespace MiniErp.Api.Services;

public class ProdutoService
{
    private readonly AppDbContext contexto;

    public ProdutoService(AppDbContext contexto)
    {
        this.contexto = contexto;
    }

    public List<Produto> ListarProdutos()
    {
        return contexto.Produtos
            .AsNoTracking()
            .Include(produto => produto.Categoria)
            .Include(produto => produto.Fornecedor)
            .OrderBy(produto => produto.Codigo)
            .ToList();
    }

    public List<Produto> ListarProdutosComEstoqueBaixo(int? categoriaId = null)
    {
        IQueryable<Produto> consulta = contexto.Produtos
            .AsNoTracking()
            .Include(produto => produto.Categoria)
            .Include(produto => produto.Fornecedor)
            .Where(produto => produto.QuantidadeEstoque <= produto.EstoqueMinimo);

        if (categoriaId.HasValue)
        {
            consulta = consulta.Where(produto => produto.CategoriaId == categoriaId.Value);
        }

        return consulta
            .OrderBy(produto => produto.QuantidadeEstoque)
            .ThenBy(produto => produto.Codigo)
            .ToList();
    }

    public Produto? BuscarPorCodigo(int codigo)
    {
        return contexto.Produtos
            .Include(produto => produto.Categoria)
            .Include(produto => produto.Fornecedor)
            .FirstOrDefault(produto => produto.Codigo == codigo);
    }

    public List<string> ValidarProduto(Produto produto)
    {
        List<string> erros = new();

        if (produto.Codigo <= 0)
        {
            erros.Add("O código deve ser maior que zero.");
        }

        if (string.IsNullOrWhiteSpace(produto.Nome))
        {
            erros.Add("O nome é obrigatório.");
        }

        if (produto.PrecoUnitario <= 0)
        {
            erros.Add("O preço unitário deve ser maior que zero.");
        }

        if (produto.QuantidadeEstoque < 0)
        {
            erros.Add("A quantidade em estoque não pode ser negativa.");
        }

        if (produto.EstoqueMinimo < 0)
        {
            erros.Add("O estoque mínimo não pode ser negativo.");
        }

        return erros;
    }

    public bool CadastrarProduto(Produto produto)
    {
        if (BuscarPorCodigo(produto.Codigo) != null)
        {
            return false;
        }

        contexto.Produtos.Add(produto);

        try
        {
            contexto.SaveChanges();
            return true;
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            return false;
        }
    }

    public bool EditarProduto(int codigo, Produto produtoAtualizado)
    {
        Produto? produtoExistente = BuscarPorCodigo(codigo);

        if (produtoExistente == null)
        {
            return false;
        }

        produtoExistente.Nome = produtoAtualizado.Nome;
        produtoExistente.PrecoUnitario = produtoAtualizado.PrecoUnitario;
        produtoExistente.EstoqueMinimo = produtoAtualizado.EstoqueMinimo;
        produtoExistente.CategoriaId = produtoAtualizado.CategoriaId;
        produtoExistente.FornecedorId = produtoAtualizado.FornecedorId;

        contexto.SaveChanges();
        return true;
    }

    public bool RemoverProduto(int codigo)
    {
        Produto? produtoExistente = BuscarPorCodigo(codigo);

        if (produtoExistente == null)
        {
            return false;
        }

        contexto.Produtos.Remove(produtoExistente);
        contexto.SaveChanges();
        return true;
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        if (ex.InnerException is not SqliteException sqliteEx)
        {
            return false;
        }

        // SQLite uses constraint violation (19) for duplicate primary key.
        return sqliteEx.SqliteErrorCode == 19;
    }
}