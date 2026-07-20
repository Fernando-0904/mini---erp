function inicializarEstoqueBaixoController() {
    carregarCategorias();
    carregarRelatorio();

    elementos.campoCategoriaEstoqueBaixo.addEventListener("change", function () {
        carregarRelatorio();
    });

    async function carregarCategorias() {
        try {
            const categorias = await listarCategoriasApi();
            atualizarSelectCategoriasEstoqueBaixo(categorias, elementos.campoCategoriaEstoqueBaixo.value);
        } catch (erro) {
            atualizarSelectCategoriasEstoqueBaixo([], "");
            exibirMensagem(erro.message, "erro");
        }
    }

    async function carregarRelatorio() {
        const categoriaIdTexto = elementos.campoCategoriaEstoqueBaixo.value;
        const categoriaId = categoriaIdTexto === "" ? null : Number(categoriaIdTexto);

        try {
            const produtos = await listarProdutosComEstoqueBaixoApi(categoriaId);
            atualizarTabelaEstoqueBaixo(produtos);
        } catch (erro) {
            atualizarTabelaEstoqueBaixo([]);
            exibirMensagem(erro.message, "erro");
        }
    }
}