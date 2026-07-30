namespace MiniErp.Api.Services;

public interface IEmailService
{
    Task EnviarConfirmacaoEmailAsync(string para, string nome, string link, DateTime expiraEmUtc);
    Task EnviarRedefinicaoSenhaAsync(string para, string nome, string link, DateTime expiraEmUtc);
}
