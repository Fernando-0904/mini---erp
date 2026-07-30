using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Data;
using MiniErp.Api.DTOs;
using MiniErp.Api.Models;

namespace MiniErp.Api.Services;

public class UsuarioLocalService
{
    private const int IteracoesHash = 100_000;
    private static readonly TimeSpan DuracaoConfirmacaoEmail = TimeSpan.FromHours(24);
    private static readonly TimeSpan DuracaoRedefinicaoSenha = TimeSpan.FromHours(2);
    private readonly AppDbContext contexto;
    private readonly IEmailService emailService;
    private readonly IConfiguration configuration;

    public UsuarioLocalService(AppDbContext contexto, IEmailService emailService, IConfiguration configuration)
    {
        this.contexto = contexto;
        this.emailService = emailService;
        this.configuration = configuration;
    }

    public async Task<(UsuarioResponse? Usuario, string Erro)> CadastrarAsync(string nome, string email, string senha)
    {
        string erro = ValidarDadosCadastro(ref nome, ref email, senha);

        if (!string.IsNullOrEmpty(erro))
        {
            return (null, erro);
        }

        if (contexto.Usuarios.Any(usuario => usuario.Email == email))
        {
            return (null, "Já existe uma conta com este e-mail.");
        }

        Usuario usuario = CriarUsuario(nome, email, senha, "Usuário", emailConfirmado: false);
        contexto.Usuarios.Add(usuario);

        try
        {
            contexto.SaveChanges();
        }
        catch (DbUpdateException ex) when (IsConstraintViolation(ex))
        {
            return (null, "Já existe uma conta com este e-mail.");
        }

        await GerarEEnviarTokenAsync(usuario, TokenUsuarioTipo.ConfirmacaoEmail);
        return (MapearUsuario(usuario), string.Empty);
    }

    public ResultadoAutenticacao Autenticar(string email, string senha)
    {
        if (!NormalizarEmail(ref email) || string.IsNullOrEmpty(senha))
        {
            return ResultadoAutenticacao.CredenciaisInvalidas();
        }

        Usuario? usuario = contexto.Usuarios
            .AsNoTracking()
            .SingleOrDefault(item => item.Email == email);

        if (usuario is null)
        {
            return ResultadoAutenticacao.CredenciaisInvalidas();
        }

        byte[] hashInformado = GerarHash(senha, usuario.SenhaSalt);
        if (!CryptographicOperations.FixedTimeEquals(hashInformado, usuario.SenhaHash))
        {
            return ResultadoAutenticacao.CredenciaisInvalidas();
        }

        return usuario.EmailConfirmado
            ? ResultadoAutenticacao.Sucesso(MapearUsuario(usuario))
            : ResultadoAutenticacao.EmailPendente();
    }

    public async Task ReenviarConfirmacaoEmailAsync(string email)
    {
        if (!NormalizarEmail(ref email))
        {
            return;
        }

        Usuario? usuario = contexto.Usuarios.SingleOrDefault(item => item.Email == email);

        if (usuario is null || usuario.EmailConfirmado)
        {
            return;
        }

        await GerarEEnviarTokenAsync(usuario, TokenUsuarioTipo.ConfirmacaoEmail);
    }

    public bool ConfirmarEmail(string token)
    {
        TokenUsuario? tokenUsuario = BuscarTokenValido(token, TokenUsuarioTipo.ConfirmacaoEmail);

        if (tokenUsuario is null || tokenUsuario.Usuario is null)
        {
            return false;
        }

        DateTime agora = DateTime.UtcNow;
        tokenUsuario.Usuario.EmailConfirmado = true;
        tokenUsuario.Usuario.EmailConfirmadoEmUtc = agora;
        tokenUsuario.UsadoEmUtc = agora;
        contexto.SaveChanges();
        return true;
    }

    public async Task SolicitarRedefinicaoSenhaAsync(string email)
    {
        if (!NormalizarEmail(ref email))
        {
            return;
        }

        Usuario? usuario = contexto.Usuarios.SingleOrDefault(item => item.Email == email);

        if (usuario is null || !usuario.EmailConfirmado)
        {
            return;
        }

        await GerarEEnviarTokenAsync(usuario, TokenUsuarioTipo.RedefinicaoSenha);
    }

    public bool RedefinirSenha(string token, string novaSenha)
    {
        if (!SenhaValida(novaSenha, out _))
        {
            return false;
        }

        TokenUsuario? tokenUsuario = BuscarTokenValido(token, TokenUsuarioTipo.RedefinicaoSenha);

        if (tokenUsuario is null || tokenUsuario.Usuario is null)
        {
            return false;
        }

        byte[] salt = RandomNumberGenerator.GetBytes(16);
        tokenUsuario.Usuario.SenhaSalt = salt;
        tokenUsuario.Usuario.SenhaHash = GerarHash(novaSenha, salt);
        tokenUsuario.UsadoEmUtc = DateTime.UtcNow;
        contexto.SaveChanges();
        return true;
    }

    private async Task GerarEEnviarTokenAsync(Usuario usuario, TokenUsuarioTipo tipo)
    {
        DateTime agora = DateTime.UtcNow;
        DateTime expiraEmUtc = agora + ObterDuracaoToken(tipo);
        string token = GerarTokenSeguro();

        foreach (TokenUsuario tokenPendente in contexto.TokensUsuario
            .Where(item => item.UsuarioId == usuario.Id && item.Tipo == tipo && item.UsadoEmUtc == null))
        {
            tokenPendente.UsadoEmUtc = agora;
        }

        contexto.TokensUsuario.Add(new TokenUsuario
        {
            UsuarioId = usuario.Id,
            Tipo = tipo,
            TokenHash = GerarHashToken(token),
            CriadoEmUtc = agora,
            ExpiraEmUtc = expiraEmUtc
        });
        contexto.SaveChanges();

        string link = CriarLinkFrontend(tipo, token);

        if (tipo == TokenUsuarioTipo.ConfirmacaoEmail)
        {
            await emailService.EnviarConfirmacaoEmailAsync(usuario.Email, usuario.Nome, link, expiraEmUtc);
            return;
        }

        await emailService.EnviarRedefinicaoSenhaAsync(usuario.Email, usuario.Nome, link, expiraEmUtc);
    }

    private TokenUsuario? BuscarTokenValido(string token, TokenUsuarioTipo tipo)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        byte[] hash = GerarHashToken(token);
        DateTime agora = DateTime.UtcNow;

        return contexto.TokensUsuario
            .Include(item => item.Usuario)
            .Where(item => item.Tipo == tipo && item.UsadoEmUtc == null && item.ExpiraEmUtc > agora)
            .AsEnumerable()
            .SingleOrDefault(item => CryptographicOperations.FixedTimeEquals(item.TokenHash, hash));
    }

    private string CriarLinkFrontend(TokenUsuarioTipo tipo, string token)
    {
        string frontendBaseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:5500";
        frontendBaseUrl = frontendBaseUrl.TrimEnd('/');
        string pagina = tipo == TokenUsuarioTipo.ConfirmacaoEmail
            ? "confirmar-email.html"
            : "redefinir-senha.html";
        return $"{frontendBaseUrl}/{pagina}?token={Uri.EscapeDataString(token)}";
    }

    private static TimeSpan ObterDuracaoToken(TokenUsuarioTipo tipo)
    {
        return tipo == TokenUsuarioTipo.ConfirmacaoEmail
            ? DuracaoConfirmacaoEmail
            : DuracaoRedefinicaoSenha;
    }

    private static Usuario CriarUsuario(string nome, string email, string senha, string perfil, bool emailConfirmado)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        DateTime agora = DateTime.UtcNow;
        return new Usuario
        {
            Nome = nome,
            Email = email,
            Perfil = perfil,
            SenhaSalt = salt,
            SenhaHash = GerarHash(senha, salt),
            EmailConfirmado = emailConfirmado,
            EmailConfirmadoEmUtc = emailConfirmado ? agora : null,
            CriadoEmUtc = agora
        };
    }

    private static string ValidarDadosCadastro(ref string nome, ref string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length < 3)
        {
            return "Informe um nome com pelo menos 3 caracteres.";
        }

        nome = nome.Trim();

        if (nome.Length > 80)
        {
            return "O nome deve possuir no máximo 80 caracteres.";
        }

        if (!NormalizarEmail(ref email))
        {
            return "Informe um e-mail válido.";
        }

        return SenhaValida(senha, out string erro) ? string.Empty : erro;
    }

    private static bool NormalizarEmail(ref string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        email = email.Trim().ToLowerInvariant();

        return email.Length <= 254 &&
               MailAddress.TryCreate(email, out MailAddress? endereco) &&
               endereco.Address.Equals(email, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SenhaValida(string senha, out string erro)
    {
        if (string.IsNullOrEmpty(senha) || senha.Length < 8)
        {
            erro = "A senha deve possuir pelo menos 8 caracteres.";
            return false;
        }

        if (senha.Length > 128)
        {
            erro = "A senha deve possuir no máximo 128 caracteres.";
            return false;
        }

        erro = string.Empty;
        return true;
    }

    private static string GerarTokenSeguro()
    {
        return WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    }

    private static byte[] GerarHashToken(string token)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(token));
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

public sealed class ResultadoAutenticacao
{
    private ResultadoAutenticacao(UsuarioResponse? usuario, bool emailNaoConfirmado)
    {
        Usuario = usuario;
        EmailNaoConfirmado = emailNaoConfirmado;
    }

    public UsuarioResponse? Usuario { get; }
    public bool EmailNaoConfirmado { get; }

    public static ResultadoAutenticacao Sucesso(UsuarioResponse usuario)
    {
        return new ResultadoAutenticacao(usuario, false);
    }

    public static ResultadoAutenticacao CredenciaisInvalidas()
    {
        return new ResultadoAutenticacao(null, false);
    }

    public static ResultadoAutenticacao EmailPendente()
    {
        return new ResultadoAutenticacao(null, true);
    }
}
