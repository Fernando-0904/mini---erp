using MiniErp.Api.DTOs;

namespace MiniErp.Api.Services;

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
