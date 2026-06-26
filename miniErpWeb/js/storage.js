const CHAVE_PRODUTOS = "produtos";

function salvarProdutosNoStorage(produtos) {
    localStorage.setItem(CHAVE_PRODUTOS, JSON.stringify(produtos));
}

function carregarProdutosDoStorage() {
    const dadosSalvos = localStorage.getItem(CHAVE_PRODUTOS);

    if (dadosSalvos === null) {
        return [];
    }

    try {
        const produtosSalvos = JSON.parse(dadosSalvos);

        if (!Array.isArray(produtosSalvos)) {
            localStorage.removeItem(CHAVE_PRODUTOS);
            return [];
        }

        return produtosSalvos;
    } catch {
        localStorage.removeItem(CHAVE_PRODUTOS);
        return [];
    }
}
