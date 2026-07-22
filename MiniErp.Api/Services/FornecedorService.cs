using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Data;
using MiniErp.Api.Models;
using System.Net.Mail;

namespace MiniErp.Api.Services;

public class FornecedorService
{
    private readonly AppDbContext contexto;

    public FornecedorService(AppDbContext contexto)
    {
        this.contexto = contexto;
    }

    public List<Fornecedor> ListarFornecedores()
    {
        return contexto.Fornecedores
            .AsNoTracking()
            .OrderBy(fornecedor => fornecedor.Nome)
            .ToList();
    }

    public Fornecedor? BuscarPorId(int id)
    {
        return contexto.Fornecedores
            .FirstOrDefault(fornecedor => fornecedor.Id == id);
    }

    public List<string> ValidarFornecedorDoProduto(int? fornecedorId)
    {
        List<string> erros = new();

        if (fornecedorId == null)
        {
            return erros;
        }

        Fornecedor? fornecedor = BuscarPorId(fornecedorId.Value);

        if (fornecedor == null)
        {
            erros.Add("Fornecedor informado não existe.");
        }
        else if (!fornecedor.Ativo)
        {
            erros.Add("Fornecedor informado está inativo.");
        }

        return erros;
    }

    public List<string> ValidarFornecedor(Fornecedor fornecedor)
    {
        List<string> erros = new();

        if (fornecedor.Codigo <= 0)
        {
            erros.Add("O código deve ser maior que zero.");
        }

        if (string.IsNullOrWhiteSpace(fornecedor.Nome))
        {
            erros.Add("O nome é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(fornecedor.Documento))
        {
            erros.Add("O documento é obrigatório.");
        }

        if (!string.IsNullOrWhiteSpace(fornecedor.Email) &&
            !MailAddress.TryCreate(fornecedor.Email, out _))
        {
            erros.Add("Informe um e-mail válido.");
        }

        return erros;
    }

    public bool CadastrarFornecedor(Fornecedor fornecedor)
    {
        NormalizarDados(fornecedor);

        if (ExisteFornecedorComCodigo(fornecedor.Codigo) ||
            ExisteFornecedorComDocumento(fornecedor.Documento))
        {
            return false;
        }

        contexto.Fornecedores.Add(fornecedor);

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

    public bool EditarFornecedor(int id, Fornecedor fornecedorAtualizado)
    {
        Fornecedor? fornecedorExistente = BuscarPorId(id);

        if (fornecedorExistente == null)
        {
            return false;
        }

        NormalizarDados(fornecedorAtualizado);

        bool codigoEmUso = contexto.Fornecedores
            .Any(fornecedor => fornecedor.Id != id &&
                               fornecedor.Codigo == fornecedorAtualizado.Codigo);

        bool documentoEmUso = contexto.Fornecedores
            .Any(fornecedor => fornecedor.Id != id &&
                               fornecedor.Documento == fornecedorAtualizado.Documento);

        if (codigoEmUso || documentoEmUso)
        {
            return false;
        }

        fornecedorExistente.Codigo = fornecedorAtualizado.Codigo;
        fornecedorExistente.Nome = fornecedorAtualizado.Nome;
        fornecedorExistente.Documento = fornecedorAtualizado.Documento;
        fornecedorExistente.Email = fornecedorAtualizado.Email;
        fornecedorExistente.Telefone = fornecedorAtualizado.Telefone;
        fornecedorExistente.Ativo = fornecedorAtualizado.Ativo;

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

    public bool RemoverFornecedor(int id)
    {
        Fornecedor? fornecedor = BuscarPorId(id);

        if (fornecedor == null)
        {
            return false;
        }

        contexto.Fornecedores.Remove(fornecedor);
        contexto.SaveChanges();
        return true;
    }

    public bool InativarFornecedor(int id)
    {
        Fornecedor? fornecedor = BuscarPorId(id);

        if (fornecedor == null)
        {
            return false;
        }

        fornecedor.Ativo = false;
        contexto.SaveChanges();
        return true;
    }

    public bool PossuiProdutosVinculados(int fornecedorId)
    {
        return contexto.Produtos.Any(produto => produto.FornecedorId == fornecedorId);
    }

    private bool ExisteFornecedorComCodigo(int codigo)
    {
        return contexto.Fornecedores
            .Any(fornecedor => fornecedor.Codigo == codigo);
    }

    private bool ExisteFornecedorComDocumento(string documento)
    {
        return contexto.Fornecedores
            .Any(fornecedor => fornecedor.Documento == documento);
    }

    private static void NormalizarDados(Fornecedor fornecedor)
    {
        fornecedor.Nome = fornecedor.Nome.Trim();
        fornecedor.Documento = fornecedor.Documento.Trim();
        fornecedor.Email = fornecedor.Email.Trim();
        fornecedor.Telefone = fornecedor.Telefone.Trim();
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException is SqliteException sqliteEx &&
               sqliteEx.SqliteErrorCode == 19;
    }
}
