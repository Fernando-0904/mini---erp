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
            .OrderBy(p => p.Codigo)
            .ToList();
    }

    public Produto? BuscarPorCodigo(int codigo)
    {
        return contexto.Produtos
            .FirstOrDefault(p => p.Codigo == codigo);
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
        produtoExistente.QuantidadeEstoque = produtoAtualizado.QuantidadeEstoque;

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