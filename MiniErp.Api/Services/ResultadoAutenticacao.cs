using MiniErp.Api.DTOs;

namespace MiniErp.Api.Services;

public sealed class ResultadoAutenticacao
{
    private ResultadoAutenticacao(UsuarioResponse? usuario, bool emailNaoConfirmado, bool tentativaBloqueada, int retryAfterSeconds)
    {
        Usuario = usuario;
        EmailNaoConfirmado = emailNaoConfirmado;
        TentativaBloqueada = tentativaBloqueada;
        RetryAfterSeconds = retryAfterSeconds;
    }

    public UsuarioResponse? Usuario { get; }
    public bool EmailNaoConfirmado { get; }
    public bool TentativaBloqueada { get; }
    public int RetryAfterSeconds { get; }

    public static ResultadoAutenticacao Sucesso(UsuarioResponse usuario)
    {
        return new ResultadoAutenticacao(usuario, false, false, 0);
    }

    public static ResultadoAutenticacao CredenciaisInvalidas()
    {
        return new ResultadoAutenticacao(null, false, false, 0);
    }

    public static ResultadoAutenticacao EmailPendente()
    {
        return new ResultadoAutenticacao(null, true, false, 0);
    }

    public static ResultadoAutenticacao Bloqueado(int retryAfterSeconds)
    {
        return new ResultadoAutenticacao(null, false, true, Math.Max(retryAfterSeconds, 1));
    }
}
