namespace MiniErp.Api.Services;

public interface IEmailService
{
    Task EnviarConfirmacaoEmailAsync(string para, string nome, string link, DateTime expiraEmUtc);
    Task EnviarRedefinicaoSenhaAsync(string para, string nome, string link, DateTime expiraEmUtc);
}

public sealed class EmailSimuladoService : IEmailService
{
    private readonly List<EmailSimulado> emails = [];
    private readonly object syncRoot = new();
    private readonly ILogger<EmailSimuladoService> logger;

    public EmailSimuladoService(ILogger<EmailSimuladoService> logger)
    {
        this.logger = logger;
    }

    public Task EnviarConfirmacaoEmailAsync(string para, string nome, string link, DateTime expiraEmUtc)
    {
        Registrar(para, "Confirme seu e-mail no Mini ERP", nome, link, expiraEmUtc);
        return Task.CompletedTask;
    }

    public Task EnviarRedefinicaoSenhaAsync(string para, string nome, string link, DateTime expiraEmUtc)
    {
        Registrar(para, "Redefina sua senha no Mini ERP", nome, link, expiraEmUtc);
        return Task.CompletedTask;
    }

    public IReadOnlyList<EmailSimulado> Listar()
    {
        lock (syncRoot)
        {
            return emails
                .OrderByDescending(email => email.CriadoEmUtc)
                .ToList();
        }
    }

    private void Registrar(string para, string assunto, string nome, string link, DateTime expiraEmUtc)
    {
        EmailSimulado email = new()
        {
            Para = para,
            Assunto = assunto,
            Nome = nome,
            Link = link,
            ExpiraEmUtc = expiraEmUtc,
            CriadoEmUtc = DateTime.UtcNow
        };

        lock (syncRoot)
        {
            emails.Add(email);
        }

        logger.LogInformation(
            "E-MAIL SIMULADO | Para: {Para} | Assunto: {Assunto} | Link: {Link} | Expira em UTC: {ExpiraEmUtc}",
            email.Para,
            email.Assunto,
            email.Link,
            email.ExpiraEmUtc);
    }
}

public sealed class EmailSimulado
{
    public string Para { get; set; } = string.Empty;
    public string Assunto { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public DateTime ExpiraEmUtc { get; set; }
    public DateTime CriadoEmUtc { get; set; }
}
