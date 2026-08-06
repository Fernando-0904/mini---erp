function inicializarAuditoriaController() {
    if (!(elementos.formularioAuditoria instanceof HTMLFormElement) ||
        !(elementos.tabelaAuditoria instanceof HTMLElement)) {
        return;
    }

    const botaoFiltrar = elementos.formularioAuditoria.querySelector("button[type='submit']");

    elementos.formularioAuditoria.addEventListener("submit", async function (evento) {
        evento.preventDefault();

        await executarComBotaoCarregando(botaoFiltrar, "Filtrando...", async function () {
            await carregarAuditoria();
        });
    });

    carregarAuditoria();

    async function carregarAuditoria() {
        const limite = obterLimite();

        try {
            const eventos = await listarAuditoriaApi(limite);
            renderizarEventos(Array.isArray(eventos) ? eventos : []);
            exibirMensagem("", "sucesso");
        } catch (erro) {
            renderizarEventos([]);
            exibirMensagem(erro instanceof Error ? erro.message : "Não foi possível carregar a auditoria.", "erro");
        }
    }

    function obterLimite() {
        if (!(elementos.campoLimiteAuditoria instanceof HTMLInputElement)) {
            return 50;
        }

        const valor = Number(elementos.campoLimiteAuditoria.value);
        return Number.isFinite(valor) && valor > 0 ? Math.trunc(valor) : 50;
    }

    function renderizarEventos(eventos) {
        elementos.tabelaAuditoria.innerHTML = "";

        if (eventos.length === 0) {
            const linhaVazia = criarLinhaEstadoVazio(
                6,
                "Nenhum evento de auditoria encontrado.",
                "Recarregar",
                function () {
                    carregarAuditoria();
                }
            );

            elementos.tabelaAuditoria.appendChild(linhaVazia);
            return;
        }

        for (const evento of eventos) {
            const linha = document.createElement("tr");

            linha.appendChild(criarCelula(formatarData(evento.dataUtc)));
            linha.appendChild(criarCelula(evento.usuarioEmail || "Sistema"));
            linha.appendChild(criarCelula(evento.acao || ""));
            linha.appendChild(criarCelula(`${evento.entidade || ""} ${evento.entidadeId || ""}`.trim()));
            linha.appendChild(criarCelula(evento.descricao || ""));
            linha.appendChild(criarCelula(evento.id));

            elementos.tabelaAuditoria.appendChild(linha);
        }
    }

    function formatarData(dataUtc) {
        if (typeof dataUtc !== "string" || dataUtc.trim() === "") {
            return "";
        }

        const data = new Date(dataUtc);

        if (Number.isNaN(data.getTime())) {
            return dataUtc;
        }

        return data.toLocaleString("pt-BR");
    }
}
