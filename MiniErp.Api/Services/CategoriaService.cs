using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Data;
using MiniErp.Api.Models;

namespace MiniErp.Api.Services;

public class CategoriaService
{
    private readonly AppDbContext contexto;

    public CategoriaService(AppDbContext contexto)
    {
        this.contexto = contexto;
    }

    public List<Categoria> ListarCategorias()
    {
        return contexto.Categorias
            .AsNoTracking()
            .OrderBy(categoria => categoria.Nome)
            .ToList();
    }

    public Categoria? BuscarPorId(int id)
    {
        return contexto.Categorias
            .FirstOrDefault(categoria => categoria.Id == id);
    }

    public bool CadastrarCategoria(Categoria categoria)
    {
        string nomeNormalizado = categoria.Nome.Trim();

        if (ExisteCategoriaComNome(nomeNormalizado))
        {
            return false;
        }

        categoria.Nome = nomeNormalizado;
        contexto.Categorias.Add(categoria);

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

    public bool EditarCategoria(int id, Categoria categoriaAtualizada)
    {
        Categoria? categoriaExistente = BuscarPorId(id);

        if (categoriaExistente == null)
        {
            return false;
        }

        string nomeNormalizado = categoriaAtualizada.Nome.Trim();

        bool nomeEmUso = contexto.Categorias
            .Any(categoria => categoria.Id != id && categoria.Nome.ToLower() == nomeNormalizado.ToLower());

        if (nomeEmUso)
        {
            return false;
        }

        categoriaExistente.Nome = nomeNormalizado;

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

    public bool RemoverCategoria(int id)
    {
        Categoria? categoria = BuscarPorId(id);

        if (categoria == null)
        {
            return false;
        }

        contexto.Categorias.Remove(categoria);
        contexto.SaveChanges();
        return true;
    }

    private bool ExisteCategoriaComNome(string nome)
    {
        return contexto.Categorias
            .Any(categoria => categoria.Nome.ToLower() == nome.ToLower());
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        if (ex.InnerException is not SqliteException sqliteEx)
        {
            return false;
        }

        return sqliteEx.SqliteErrorCode == 19;
    }
}
