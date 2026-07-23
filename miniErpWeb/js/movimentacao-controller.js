function inicializarMovimentacaoController() {
    aplicarContextoDaUrl();

    elementos.botaoRegistrarEntrada.addEventListener("click", function () {
        registrarMovimentacao("entrada");
    });

    elementos.botaoRegistrarSaida.addEventListener("click", function () {
        registrarMovimentacao("saida");
    });

    elementos.botaoBuscarHistorico.addEventListener("click", async function () {
        const codigo = obterCodigoMovimentacao();

        if (codigo === null) {
            return;
        }

        await carregarHistorico(codigo);
    });

    elementos.formularioMovimentacaoEstoque.addEventListener("submit", function (event) {
        event.preventDefault();
    });

    async function aplicarContextoDaUrl() {
        if (typeof window === "undefined" || window.location === undefined) {
            return;
        }

        const parametros = new URLSearchParams(window.location.search);
        const codigoTexto = parametros.get("produtoCodigo");

        if (codigoTexto !== null && codigoTexto.trim() !== "") {
            elementos.campoMovimentacaoCodigo.value = codigoTexto;
        }

        const acao = parametros.get("acao");

        if (acao === "entrada") {
            elementos.campoMovimentacaoQuantidade.focus();
            exibirMensagem("Ação rápida de reposição ativa. Informe a quantidade e registre a entrada.", "sucesso");
        }

        if (parametros.get("autoHistorico") === "1") {
            const codigo = obterCodigoMovimentacao();

            if (codigo !== null) {
                await carregarHistorico(codigo, false);
            }
        }
    }

    async function registrarMovimentacao(tipo) {
        const codigo = obterCodigoMovimentacao();
        const quantidade = obterQuantidadeMovimentacao();

        if (codigo === null || quantidade === null) {
            return;
        }

        try {
            if (tipo === "entrada") {
                await registrarEntradaEstoqueApi(codigo, quantidade);
            } else {
                await registrarSaidaEstoqueApi(codigo, quantidade);
            }

            if (typeof window.recarregarProdutosNaTela === "function") {
                await window.recarregarProdutosNaTela();
            }
            await carregarHistorico(codigo, false);
            elementos.campoMovimentacaoQuantidade.value = "";
            const descricaoTipo = tipo === "entrada" ? "entrada" : "saída";
            exibirMensagem(`Movimentação de ${descricaoTipo} registrada com sucesso.`, "sucesso");
        } catch (erro) {
            exibirMensagem(erro.message, "erro");
        }
    }

    function obterCodigoMovimentacao() {
        const codigoTexto = elementos.campoMovimentacaoCodigo.value.trim();
        const codigo = Number(codigoTexto);

        if (codigoTexto === "" || !Number.isInteger(codigo) || codigo <= 0) {
            exibirMensagem("Informe um código de produto válido.", "erro");
            elementos.campoMovimentacaoCodigo.focus();
            return null;
        }

        return codigo;
    }

    function obterQuantidadeMovimentacao() {
        const quantidadeTexto = elementos.campoMovimentacaoQuantidade.value.trim();
        const quantidade = Number(quantidadeTexto);

        if (quantidadeTexto === "" || !Number.isInteger(quantidade) || quantidade <= 0) {
            exibirMensagem("Informe uma quantidade inteira maior que zero.", "erro");
            elementos.campoMovimentacaoQuantidade.focus();
            return null;
        }

        return quantidade;
    }

    async function carregarHistorico(codigo, exibirMensagemSucesso = true) {
        try {
            const movimentacoes = await listarMovimentacoesApi(codigo);

            atualizarTabelaMovimentacoes(movimentacoes);

            if (exibirMensagemSucesso) {
                exibirMensagem("Histórico carregado com sucesso.", "sucesso");
            }
        } catch (erro) {
            exibirMensagem(erro.message, "erro");
        }
    }
}