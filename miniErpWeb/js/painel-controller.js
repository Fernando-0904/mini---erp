function inicializarPainelController() {
    carregarIndicadores();

    async function carregarIndicadores() {
        const resultados = await Promise.allSettled([
            listarProdutosApi(),
            listarAlertasOperacionaisApi(),
            listarPedidosCompraApi(),
            listarRelatorioUltimasMovimentacoesApi(5)
        ]);

        const produtosApi = obterResultadoOuPadrao(resultados[0], []);
        const alertasApi = obterResultadoOuPadrao(resultados[1], []);
        const pedidosApi = obterResultadoOuPadrao(resultados[2], []);
        const movimentacoesRecentesApi = obterResultadoOuPadrao(resultados[3], []);

        const produtos = produtosApi.map(function (produto) {
            return {
                codigo: produto.codigo,
                nome: produto.nome,
                preco: produto.precoUnitario,
                quantidade: produto.quantidadeEstoque
            };
        });
        const alertas = normalizarAlertas(alertasApi);
        const pedidosAbertos = contarPedidosAbertos(pedidosApi);
        const movimentacoesRecentes = Array.isArray(movimentacoesRecentesApi)
            ? movimentacoesRecentesApi
            : [];

        atualizarIndicadores(produtos);
        atualizarResumoAlertasPainel(alertas);
        atualizarTabelaAlertasPainel(alertas);
        atualizarResumoOperacionalPainel(pedidosAbertos, movimentacoesRecentes.length);
        atualizarTabelaAtividadePainel(movimentacoesRecentes);

        if (resultados.some(function (resultado) { return resultado.status === "rejected"; })) {
            exibirMensagem("Parte dos dados do painel está temporariamente indisponível. Tente atualizar em instantes.", "aviso");
        }
    }

    function obterResultadoOuPadrao(resultado, valorPadrao) {
        if (resultado && resultado.status === "fulfilled") {
            return resultado.value;
        }

        return valorPadrao;
    }

    function contarPedidosAbertos(pedidos) {
        if (!Array.isArray(pedidos)) {
            return 0;
        }

        const statusEmAberto = new Set(["aberto", "pendenteaprovacao", "aprovado"]);

        return pedidos.filter(function (pedido) {
            const status = typeof pedido.status === "string" ? pedido.status.toLowerCase() : "";
            return statusEmAberto.has(status);
        }).length;
    }

    function normalizarAlertas(alertas) {
        if (!Array.isArray(alertas)) {
            return [];
        }

        return alertas.map(function (alerta) {
            return {
                prioridade: alerta.prioridade || "Informativo",
                titulo: alerta.titulo || "Alerta",
                produto: alerta.produto || "",
                detalhe: alerta.detalhe || "",
                acao: alerta.acao && alerta.acao.href && alerta.acao.label
                    ? {
                        href: alerta.acao.href,
                        label: alerta.acao.label
                    }
                    : null
            };
        });
    }
}