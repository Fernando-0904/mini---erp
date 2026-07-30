inicializarPaginaTokenAutenticacao();

function inicializarPaginaTokenAutenticacao() {
    const mensagem = document.getElementById("mensagemAuthToken");
    const formularioRedefinicao = document.getElementById("formRedefinirSenha");
    const campoNovaSenha = document.getElementById("novaSenha");
    const campoConfirmarSenha = document.getElementById("confirmarNovaSenha");
    const token = new URLSearchParams(window.location.search).get("token") || "";

    if (!(mensagem instanceof HTMLElement)) {
        return;
    }

    if (document.body.dataset.authPage === "confirmar-email") {
        confirmarEmail(token, mensagem);
        return;
    }

    if (!(formularioRedefinicao instanceof HTMLFormElement)) {
        return;
    }

    if (token === "") {
        mostrarMensagem(mensagem, "Link inválido ou incompleto.", "erro");
        formularioRedefinicao.hidden = true;
        return;
    }

    formularioRedefinicao.addEventListener("submit", async function (evento) {
        evento.preventDefault();
        const novaSenha = campoNovaSenha instanceof HTMLInputElement ? campoNovaSenha.value : "";
        const confirmarSenha = campoConfirmarSenha instanceof HTMLInputElement ? campoConfirmarSenha.value : "";

        if (novaSenha.length < 8) {
            mostrarMensagem(mensagem, "A nova senha deve possuir pelo menos 8 caracteres.", "erro");
            return;
        }

        if (novaSenha !== confirmarSenha) {
            mostrarMensagem(mensagem, "As senhas informadas não são iguais.", "erro");
            return;
        }

        const botao = formularioRedefinicao.querySelector("button[type='submit']");
        const textoOriginal = botao instanceof HTMLButtonElement ? botao.textContent : "Redefinir senha";

        if (botao instanceof HTMLButtonElement) {
            botao.disabled = true;
            botao.textContent = "Redefinindo...";
        }

        mostrarMensagem(mensagem, "Validando token e atualizando senha...", "info");

        try {
            const resultado = await redefinirSenhaApi(token, novaSenha);
            mostrarMensagem(mensagem, resultado.mensagem || "Senha redefinida com sucesso.", "sucesso");
            formularioRedefinicao.reset();
            formularioRedefinicao.hidden = true;
        } catch (erro) {
            mostrarMensagem(mensagem, erro instanceof Error ? erro.message : "Não foi possível redefinir sua senha.", "erro");
        } finally {
            if (botao instanceof HTMLButtonElement) {
                botao.disabled = false;
                botao.textContent = textoOriginal;
            }
        }
    });
}

async function confirmarEmail(token, mensagem) {
    if (token === "") {
        mostrarMensagem(mensagem, "Link inválido ou incompleto.", "erro");
        return;
    }

    mostrarMensagem(mensagem, "Confirmando seu e-mail...", "info");

    try {
        const resultado = await confirmarEmailApi(token);
        mostrarMensagem(mensagem, resultado.mensagem || "E-mail confirmado com sucesso.", "sucesso");
    } catch (erro) {
        mostrarMensagem(mensagem, erro instanceof Error ? erro.message : "Não foi possível confirmar seu e-mail.", "erro");
    }
}

function mostrarMensagem(elemento, texto, tipo) {
    elemento.textContent = texto;
    elemento.className = `auth-token-mensagem auth-token-${tipo}`;
}
