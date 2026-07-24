(function () {
    const CHAVE_TEMA = "miniErpTema";
    const TEMA_CLARO = "claro";
    const TEMA_ESCURO = "escuro";

    document.addEventListener("DOMContentLoaded", function () {
        const botaoTema = document.getElementById("botaoTema");
        const temaAtual = document.documentElement.classList.contains("tema-escuro")
            ? TEMA_ESCURO
            : TEMA_CLARO;

        sincronizarBotao(temaAtual, botaoTema);

        if (botaoTema) {
            botaoTema.addEventListener("click", function () {
                const temaAtivo = document.documentElement.classList.contains("tema-escuro")
                    ? TEMA_ESCURO
                    : TEMA_CLARO;
                const proximoTema = temaAtivo === TEMA_ESCURO ? TEMA_CLARO : TEMA_ESCURO;

                aplicarTema(proximoTema, botaoTema);
            });
        }
    });

    function aplicarTema(tema, botaoTema) {
        const modoEscuroAtivo = tema === TEMA_ESCURO;

        document.documentElement.classList.toggle("tema-escuro", modoEscuroAtivo);
        localStorage.setItem(CHAVE_TEMA, modoEscuroAtivo ? TEMA_ESCURO : TEMA_CLARO);

        sincronizarBotao(tema, botaoTema);
    }

    function sincronizarBotao(tema, botaoTema) {
        if (!botaoTema) {
            return;
        }

        const modoEscuroAtivo = tema === TEMA_ESCURO;

        botaoTema.setAttribute("aria-pressed", modoEscuroAtivo ? "true" : "false");
        botaoTema.setAttribute(
            "title",
            modoEscuroAtivo ? "Ativar modo claro" : "Ativar modo escuro"
        );
        botaoTema.setAttribute(
            "aria-label",
            modoEscuroAtivo ? "Ativar modo claro" : "Ativar modo escuro"
        );
    }
})();
