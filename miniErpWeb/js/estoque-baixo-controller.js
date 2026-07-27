function inicializarEstoqueBaixoController() {
    carregarCategorias();
    aplicarCategoriaDaUrl();
    carregarRelatorio();

    elementos.campoCategoriaEstoqueBaixo.addEventListener("change", function () {
        carregarRelatorio();
    });

    async function carregarCategorias() {
        try {
            const categorias = await listarCategoriasApi();
            atualizarSelectCategoriasEstoqueBaixo(categorias, elementos.campoCategoriaEstoqueBaixo.value);
            aplicarCategoriaDaUrl();
        } catch (erro) {
            atualizarSelectCategoriasEstoqueBaixo([], "");
            exibirMensagem(erro.message, "erro");
        }
    }

    function aplicarCategoriaDaUrl() {
        const parametros = new URLSearchParams(window.location.search);
        const categoriaTexto = parametros.get("categoria");

        if (categoriaTexto === null || categoriaTexto.trim() === "") {
            return;
        }

        elementos.campoCategoriaEstoqueBaixo.value = categoriaTexto;
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