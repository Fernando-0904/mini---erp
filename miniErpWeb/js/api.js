function obterApiBaseUrl() {
    if (typeof window !== "undefined" && typeof window.MINIERP_API_URL === "string" && window.MINIERP_API_URL.trim() !== "") {
        return window.MINIERP_API_URL.trim().replace(/\/$/, "");
    }

    if (typeof window !== "undefined" && window.location && /^https?:$/i.test(window.location.protocol)) {
        return `${window.location.protocol}//${window.location.hostname}:5208`;
    }

    return "http://localhost:5208";
}

const API_BASE_URL = obterApiBaseUrl();

async function tratarRespostaApi(resposta, mensagemErroPadrao) {
    if (resposta.ok) {
        if (resposta.status === 204) {
            return null;
        }

        return resposta.json();
    }

    let mensagemErro = mensagemErroPadrao;

    try {
        const erro = await resposta.json();

        if (Array.isArray(erro)) {
            mensagemErro = erro.join(" ");
        } else if (typeof erro === "string") {
            mensagemErro = erro;
        }
    } catch {
        // Mantém a mensagem padrão se a API não retornar JSON.
    }

    throw new Error(mensagemErro);
}

async function listarProdutosApi() {
    const resposta = await fetch(`${API_BASE_URL}/produtos`);

    return tratarRespostaApi(resposta, "Erro ao listar produtos na API.");
}

async function buscarProdutoPorCodigoApi(codigo) {
    const resposta = await fetch(`${API_BASE_URL}/produtos/${codigo}`);

    return tratarRespostaApi(resposta, "Produto não encontrado na API.");
}

async function cadastrarProdutoApi(produto) {
    const resposta = await fetch(`${API_BASE_URL}/produtos`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(produto),
    });

    return tratarRespostaApi(resposta, "Erro ao cadastrar produto na API.");
}

async function editarProdutoApi(codigo, produto) {
    const resposta = await fetch(`${API_BASE_URL}/produtos/${codigo}`, {
        method: "PUT",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(produto),
    });

    return tratarRespostaApi(resposta, "Erro ao editar produto na API.");
}

async function removerProdutoApi(codigo) {
    const resposta = await fetch(`${API_BASE_URL}/produtos/${codigo}`, {
        method: "DELETE",
    });

    return tratarRespostaApi(resposta, "Erro ao remover produto na API.");
}

async function listarCategoriasApi() {
    const resposta = await fetch(`${API_BASE_URL}/categorias`);

    return tratarRespostaApi(resposta, "Erro ao listar categorias na API.");
}

async function buscarCategoriaPorIdApi(id) {
    const resposta = await fetch(`${API_BASE_URL}/categorias/${id}`);

    return tratarRespostaApi(resposta, "Categoria não encontrada na API.");
}

async function cadastrarCategoriaApi(categoria) {
    const resposta = await fetch(`${API_BASE_URL}/categorias`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(categoria),
    });

    return tratarRespostaApi(resposta, "Erro ao cadastrar categoria na API.");
}

async function editarCategoriaApi(id, categoria) {
    const resposta = await fetch(`${API_BASE_URL}/categorias/${id}`, {
        method: "PUT",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(categoria),
    });

    return tratarRespostaApi(resposta, "Erro ao editar categoria na API.");
}

async function removerCategoriaApi(id) {
    const resposta = await fetch(`${API_BASE_URL}/categorias/${id}`, {
        method: "DELETE",
    });

    return tratarRespostaApi(resposta, "Erro ao remover categoria na API.");
}
