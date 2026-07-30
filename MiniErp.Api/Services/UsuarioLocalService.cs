using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Data;
using MiniErp.Api.DTOs;
using MiniErp.Api.Models;

namespace MiniErp.Api.Services;

public class UsuarioLocalService
{
    private const int IteracoesHash = 100_000;
    private readonly AppDbContext contexto;

    public UsuarioLocalService(AppDbContext contexto)
    {
        this.contexto = contexto;
    }

    public (UsuarioResponse? Usuario, string Erro) Cadastrar(string nome, string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length < 3)
        {
            return (null, "Informe um nome com pelo menos 3 caracteres.");
        }

        nome = nome.Trim();

        if (nome.Length > 80)
        {
            return (null, "O nome deve possuir no máximo 80 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return (null, "Informe um e-mail válido.");
        }

        email = email.Trim().ToLowerInvariant();

        if (email.Length > 254 ||
            !MailAddress.TryCreate(email, out MailAddress? endereco) ||
            !endereco.Address.Equals(email, StringComparison.OrdinalIgnoreCase))
        {
            return (null, "Informe um e-mail válido.");
        }

        if (string.IsNullOrEmpty(senha) || senha.Length < 8)
        {
            return (null, "A senha deve possuir pelo menos 8 caracteres.");
        }

        if (senha.Length > 128)
        {
            return (null, "A senha deve possuir no máximo 128 caracteres.");
        }

        if (contexto.Usuarios.Any(usuario => usuario.Email == email))
        {
            return (null, "Já existe uma conta com este e-mail.");
        }

        Usuario usuario = CriarUsuario(nome, email, senha, "Usuário");
        contexto.Usuarios.Add(usuario);

        try
        {
            contexto.SaveChanges();
            return (MapearUsuario(usuario), string.Empty);
        }
        catch (DbUpdateException ex) when (IsConstraintViolation(ex))
        {
            return (null, "Já existe uma conta com este e-mail.");
        }
    }

    public UsuarioResponse? Autenticar(string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(senha))
        {
            return null;
        }

        email = email.Trim().ToLowerInvariant();
        Usuario? usuario = contexto.Usuarios
            .AsNoTracking()
            .SingleOrDefault(item => item.Email == email);

        if (usuario is null)
        {
            return null;
        }

        byte[] hashInformado = GerarHash(senha, usuario.SenhaSalt);
        return CryptographicOperations.FixedTimeEquals(hashInformado, usuario.SenhaHash)
            ? MapearUsuario(usuario)
            : null;
    }

    private static Usuario CriarUsuario(string nome, string email, string senha, string perfil)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        return new Usuario
        {
            Nome = nome,
            Email = email,
            Perfil = perfil,
            SenhaSalt = salt,
            SenhaHash = GerarHash(senha, salt),
            CriadoEmUtc = DateTime.UtcNow
        };
    }

    private static byte[] GerarHash(string senha, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            senha,
            salt,
            IteracoesHash,
            HashAlgorithmName.SHA256,
            32);
    }

    private static UsuarioResponse MapearUsuario(Usuario usuario)
    {
        return new UsuarioResponse
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Perfil = usuario.Perfil
        };
    }

    private static bool IsConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException is SqliteException sqliteException &&
               sqliteException.SqliteErrorCode == 19;
    }
}
