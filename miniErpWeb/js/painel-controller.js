function inicializarPainelController() {
    carregarIndicadores();

    async function carregarIndicadores() {
        try {
            const produtosApi = await listarProdutosApi();
            const produtos = produtosApi.map(function (produto) {
                return {
                    codigo: produto.codigo,
                    nome: produto.nome,
                    preco: produto.precoUnitario,
                    quantidade: produto.quantidadeEstoque
                };
            });
            const alertasApi = await listarAlertasOperacionaisApi();
            const alertas = normalizarAlertas(alertasApi);

            atualizarIndicadores(produtos);
            atualizarResumoAlertasPainel(alertas);
            atualizarTabelaAlertasPainel(alertas);
        } catch (erro) {
            exibirMensagem(erro.message, "erro");
        }
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