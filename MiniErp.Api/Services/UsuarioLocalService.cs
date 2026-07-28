using System.Net.Mail;
using System.Security.Cryptography;
using MiniErp.Api.DTOs;

namespace MiniErp.Api.Services;

public class UsuarioLocalService
{
    private const int IteracoesHash = 100_000;
    private readonly Dictionary<string, ContaLocal> contas = new(StringComparer.OrdinalIgnoreCase);
    private readonly object bloqueio = new();

    public UsuarioLocalService()
    {
        AdicionarConta("Administrador", "admin@mini-erp.com", "123456", "Admin");
    }

    public (UsuarioResponse? Usuario, string Erro) Cadastrar(string nome, string email, string senha)
    {
        nome = nome.Trim();
        email = email.Trim().ToLowerInvariant();

        if (nome.Length < 3)
        {
            return (null, "Informe um nome com pelo menos 3 caracteres.");
        }

        if (!MailAddress.TryCreate(email, out _))
        {
            return (null, "Informe um e-mail válido.");
        }

        if (senha.Length < 8)
        {
            return (null, "A senha deve possuir pelo menos 8 caracteres.");
        }

        lock (bloqueio)
        {
            if (contas.ContainsKey(email))
            {
                return (null, "Já existe uma conta com este e-mail.");
            }

            ContaLocal conta = CriarConta(nome, email, senha, "Usuário");
            contas.Add(email, conta);
            return (MapearUsuario(conta), string.Empty);
        }
    }

    public UsuarioResponse? Autenticar(string email, string senha)
    {
        email = email.Trim().ToLowerInvariant();
        ContaLocal? conta;

        lock (bloqueio)
        {
            contas.TryGetValue(email, out conta);
        }

        if (conta is null)
        {
            return null;
        }

        byte[] hashInformado = GerarHash(senha, conta.Salt);
        return CryptographicOperations.FixedTimeEquals(hashInformado, conta.SenhaHash)
            ? MapearUsuario(conta)
            : null;
    }

    private void AdicionarConta(string nome, string email, string senha, string perfil)
    {
        contas[email] = CriarConta(nome, email, senha, perfil);
    }

    private static ContaLocal CriarConta(string nome, string email, string senha, string perfil)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        return new ContaLocal(nome, email, perfil, salt, GerarHash(senha, salt));
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

    private static UsuarioResponse MapearUsuario(ContaLocal conta)
    {
        return new UsuarioResponse
        {
            Nome = conta.Nome,
            Email = conta.Email,
            Perfil = conta.Perfil
        };
    }

    private sealed record ContaLocal(
        string Nome,
        string Email,
        string Perfil,
        byte[] Salt,
        byte[] SenhaHash);
}
