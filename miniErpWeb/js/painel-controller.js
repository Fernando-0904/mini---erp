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
                    quantidade: produto.quantidadeEstoque,
                    estoqueMinimo: produto.estoqueMinimo || 0,
                    fornecedorId: produto.fornecedorId,
                    fornecedorNome: produto.fornecedor ? produto.fornecedor.nome : ""
                };
            });

            const ultimaMovimentacaoPorProduto = await carregarUltimasMovimentacoes(produtos);
            const alertas = montarAlertasOperacionais(produtos, ultimaMovimentacaoPorProduto);

            atualizarIndicadores(produtos);
            atualizarResumoAlertasPainel(alertas);
            atualizarTabelaAlertasPainel(alertas);
        } catch (erro) {
            exibirMensagem(erro.message, "erro");
        }
    }

    async function carregarUltimasMovimentacoes(produtos) {
        const resultado = {};

        const consultas = produtos.map(async function (produto) {
            try {
                const movimentacoes = await listarMovimentacoesApi(produto.codigo);

                if (!Array.isArray(movimentacoes) || movimentacoes.length === 0) {
                    resultado[produto.codigo] = null;
                    return;
                }

                const movimentosOrdenados = movimentacoes
                    .filter(function (movimento) {
                        return movimento && movimento.dataMovimentacaoUtc;
                    })
                    .sort(function (a, b) {
                        return new Date(b.dataMovimentacaoUtc) - new Date(a.dataMovimentacaoUtc);
                    });

                resultado[produto.codigo] = movimentosOrdenados.length > 0 ? movimentosOrdenados[0].dataMovimentacaoUtc : null;
            } catch {
                resultado[produto.codigo] = null;
            }
        });

        await Promise.all(consultas);
        return resultado;
    }

    function montarAlertasOperacionais(produtos, ultimaMovimentacaoPorProduto) {
        const hoje = new Date();
        const diasSemMovimentoLimite = 30;
        const alertas = [];

        for (const produto of produtos) {
            const nomeProduto = `${produto.codigo} - ${produto.nome}`;

            if (produto.quantidade === 0) {
                alertas.push({
                    prioridade: "Crítico",
                    titulo: "Produto sem estoque",
                    produto: nomeProduto,
                    detalhe: "Sem saldo disponível para venda/consumo.",
                    acao: {
                        label: "Repor estoque",
                        href: `movimentacoes.html?produtoCodigo=${produto.codigo}&acao=entrada&autoHistorico=1`
                    }
                });
            } else if (produto.estoqueMinimo > 0 && produto.quantidade <= produto.estoqueMinimo) {
                alertas.push({
                    prioridade: "Atenção",
                    titulo: "Estoque abaixo do mínimo",
                    produto: nomeProduto,
                    detalhe: `Saldo ${produto.quantidade} (mínimo ${produto.estoqueMinimo}).`,
                    acao: {
                        label: "Planejar reposição",
                        href: `movimentacoes.html?produtoCodigo=${produto.codigo}&acao=entrada&autoHistorico=1`
                    }
                });
            }

            if (!produto.fornecedorId || !produto.fornecedorNome) {
                alertas.push({
                    prioridade: "Atenção",
                    titulo: "Produto sem fornecedor",
                    produto: nomeProduto,
                    detalhe: "Vincule um fornecedor para agilizar futuras reposições.",
                    acao: {
                        label: "Editar produto",
                        href: `produtos.html?codigoEdicao=${produto.codigo}`
                    }
                });
            }

            const ultimaMovimentacao = ultimaMovimentacaoPorProduto[produto.codigo];

            if (!ultimaMovimentacao && produto.quantidade > 0) {
                alertas.push({
                    prioridade: "Informativo",
                    titulo: "Produto sem histórico",
                    produto: nomeProduto,
                    detalhe: "Sem movimentações registradas até o momento.",
                    acao: {
                        label: "Ver histórico",
                        href: `movimentacoes.html?produtoCodigo=${produto.codigo}&autoHistorico=1`
                    }
                });
                continue;
            }

            if (ultimaMovimentacao) {
                const diasSemMovimento = calcularDiferencaDias(hoje, new Date(ultimaMovimentacao));

                if (diasSemMovimento > diasSemMovimentoLimite) {
                    alertas.push({
                        prioridade: "Informativo",
                        titulo: "Sem movimentação recente",
                        produto: nomeProduto,
                        detalhe: `Última movimentação há ${diasSemMovimento} dias.`,
                        acao: {
                            label: "Analisar histórico",
                            href: `movimentacoes.html?produtoCodigo=${produto.codigo}&autoHistorico=1`
                        }
                    });
                }
            }
        }

        return alertas.sort(function (a, b) {
            return pesoPrioridade(a.prioridade) - pesoPrioridade(b.prioridade);
        });
    }

    function calcularDiferencaDias(dataAtual, dataAnterior) {
        if (Number.isNaN(dataAnterior.getTime())) {
            return 0;
        }

        const diferencaMs = dataAtual.getTime() - dataAnterior.getTime();
        return Math.floor(diferencaMs / (1000 * 60 * 60 * 24));
    }

    function pesoPrioridade(prioridade) {
        if (prioridade === "Crítico") {
            return 0;
        }

        if (prioridade === "Atenção") {
            return 1;
        }

        return 2;
    }
}