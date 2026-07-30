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
        const parametros = new URLSearchParams(window.location.search);
        const produtoCodigo = parametros.get("produtoCodigo");
        const acao = parametros.get("acao");
        const autoHistorico = parametros.get("autoHistorico");

        if (produtoCodigo !== null && produtoCodigo.trim() !== "") {
            elementos.campoMovimentacaoCodigo.value = produtoCodigo;
        }

        if (autoHistorico === "1") {
            const codigo = obterCodigoMovimentacao();

            if (codigo !== null) {
                await carregarHistorico(codigo, false);
            }
        }

        if (acao === "entrada") {
            elementos.campoMovimentacaoQuantidade.focus();
            exibirMensagem("Código carregado. Informe a quantidade para registrar a entrada.", "sucesso");
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