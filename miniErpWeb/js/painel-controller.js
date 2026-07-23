function inicializarPainelController() {
    carregarIndicadores();

    async function carregarIndicadores() {
        try {
            const produtosApi = await listarProdutosApi();
            const produtos = produtosApi.map(function (produto) {
                return {
                    codigo: produto.codigo,
                    nome: produto.nome,
                    categoriaNome: produto.categoria ? produto.categoria.nome : "Sem categoria",
                    preco: produto.precoUnitario,
                    quantidade: produto.quantidadeEstoque,
                    estoqueMinimo: produto.estoqueMinimo
                };
            });

            atualizarIndicadores(produtos);
            atualizarAlertasPainel(montarAlertasEstoque(produtos));
        } catch (erro) {
            exibirMensagem(erro.message, "erro");
        }
    }

    function montarAlertasEstoque(produtos) {
        const alertas = [];

        for (const produto of produtos) {
            if (produto.quantidade === 0) {
                alertas.push({
                    codigo: produto.codigo,
                    nome: produto.nome,
                    categoriaNome: produto.categoriaNome,
                    saldo: produto.quantidade,
                    estoqueMinimo: produto.estoqueMinimo,
                    situacao: "Sem estoque",
                    situacaoClasse: "status-sem-estoque"
                });
                continue;
            }

            if (produto.estoqueMinimo > 0 && produto.quantidade <= produto.estoqueMinimo) {
                alertas.push({
                    codigo: produto.codigo,
                    nome: produto.nome,
                    categoriaNome: produto.categoriaNome,
                    saldo: produto.quantidade,
                    estoqueMinimo: produto.estoqueMinimo,
                    situacao: "Estoque baixo",
                    situacaoClasse: "status-estoque-baixo"
                });
            }
        }

        return alertas;
    }
}