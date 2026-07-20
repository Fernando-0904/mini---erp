function inicializarPainelController() {
    carregarIndicadores();

    async function carregarIndicadores() {
        try {
            const produtosApi = await listarProdutosApi();
            const produtos = produtosApi.map(function (produto) {
                return {
                    preco: produto.precoUnitario,
                    quantidade: produto.quantidadeEstoque
                };
            });

            atualizarIndicadores(produtos);
        } catch (erro) {
            exibirMensagem(erro.message, "erro");
        }
    }
}