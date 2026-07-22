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
const MENSAGEM_ERRO_INESPERADO = "Ocorreu um erro inesperado. Tente novamente.";

async function executarRequisicaoApi(caminho, opcoes, mensagemErroPadrao) {
    let resposta;

    try {
        resposta = await fetch(`${API_BASE_URL}${caminho}`, opcoes);
    } catch {
        throw new Error("Não foi possível conectar à API. Verifique se ela está em execução e tente novamente.");
    }

    return tratarRespostaApi(resposta, mensagemErroPadrao);
}

async function tratarRespostaApi(resposta, mensagemErroPadrao) {
    if (resposta.ok) {
        if (resposta.status === 204) {
            return null;
        }

        return resposta.json();
    }

    let mensagemErro = resposta.status >= 500
        ? MENSAGEM_ERRO_INESPERADO
        : mensagemErroPadrao;

    try {
        const erro = await resposta.json();
        const mensagemExtraida = extrairMensagemErroApi(erro);

        if (resposta.status < 500 && mensagemExtraida !== "") {
            mensagemErro = mensagemExtraida;
        }
    } catch {
        // Mantém a mensagem padrão se a API não retornar JSON.
    }

    throw new Error(normalizarMensagemErroUsuario(mensagemErro));
}

function normalizarMensagemErroUsuario(mensagem) {
    if (typeof mensagem !== "string" || mensagem.trim() === "") {
        return MENSAGEM_ERRO_INESPERADO;
    }

    if (/failed to fetch|networkerror|network error|typeerror|internal server error|server error/i.test(mensagem)) {
        if (/failed to fetch|networkerror|network error/i.test(mensagem)) {
            return "Não foi possível conectar à API. Verifique se ela está em execução e tente novamente.";
        }

        return MENSAGEM_ERRO_INESPERADO;
    }

    return mensagem;
}

function extrairMensagemErroApi(erro) {
    if (Array.isArray(erro)) {
        return erro.join(" ");
    }

    if (typeof erro === "string") {
        return erro;
    }

    if (erro !== null && typeof erro === "object") {
        if (typeof erro.detail === "string") {
            return erro.detail;
        }

        if (typeof erro.title === "string" && !/internal server error|server error/i.test(erro.title)) {
            return erro.title;
        }

        if (erro.errors !== null && typeof erro.errors === "object") {
            return Object.values(erro.errors)
                .flatMap(function (mensagens) {
                    return Array.isArray(mensagens) ? mensagens : [mensagens];
                })
                .filter(function (mensagem) {
                    return typeof mensagem === "string";
                })
                .join(" ");
        }
    }

    return "";
}

async function listarProdutosApi() {
    return executarRequisicaoApi("/produtos", undefined, "Erro ao listar produtos na API.");
}

async function buscarProdutoPorCodigoApi(codigo) {
    return executarRequisicaoApi(`/produtos/${codigo}`, undefined, "Produto não encontrado na API.");
}

async function cadastrarProdutoApi(produto) {
    return executarRequisicaoApi("/produtos", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(produto),
    }, "Erro ao cadastrar produto na API.");
}

async function editarProdutoApi(codigo, produto) {
    return executarRequisicaoApi(`/produtos/${codigo}`, {
        method: "PUT",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(produto),
    }, "Erro ao editar produto na API.");
}

async function removerProdutoApi(codigo) {
    return executarRequisicaoApi(`/produtos/${codigo}`, {
        method: "DELETE",
    }, "Erro ao remover produto na API.");
}

async function listarMovimentacoesApi(codigo) {
    return executarRequisicaoApi(`/produtos/${codigo}/movimentacoes`, undefined, "Erro ao listar movimentações na API.");
}

async function registrarEntradaEstoqueApi(codigo, quantidade) {
    return executarRequisicaoApi(`/produtos/${codigo}/movimentacoes/entrada`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ quantidade }),
    }, "Erro ao registrar entrada de estoque na API.");
}

async function registrarSaidaEstoqueApi(codigo, quantidade) {
    return executarRequisicaoApi(`/produtos/${codigo}/movimentacoes/saida`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ quantidade }),
    }, "Erro ao registrar saída de estoque na API.");
}

async function listarProdutosComEstoqueBaixoApi(categoriaId) {
    const caminho = categoriaId === null || categoriaId === undefined
        ? "/produtos/estoque-baixo"
        : `/produtos/estoque-baixo?categoriaId=${encodeURIComponent(categoriaId)}`;

    return executarRequisicaoApi(caminho, undefined, "Erro ao listar produtos com estoque baixo na API.");
}

async function listarProdutosSemEstoqueApi(categoriaId) {
    const caminho = categoriaId === null || categoriaId === undefined
        ? "/produtos/sem-estoque"
        : `/produtos/sem-estoque?categoriaId=${encodeURIComponent(categoriaId)}`;

    return executarRequisicaoApi(caminho, undefined, "Erro ao listar produtos sem estoque na API.");
}

async function listarCategoriasApi() {
    return executarRequisicaoApi("/categorias", undefined, "Erro ao listar categorias na API.");
}

async function buscarCategoriaPorIdApi(id) {
    return executarRequisicaoApi(`/categorias/${id}`, undefined, "Categoria não encontrada na API.");
}

async function cadastrarCategoriaApi(categoria) {
    return executarRequisicaoApi("/categorias", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(categoria),
    }, "Erro ao cadastrar categoria na API.");
}

async function editarCategoriaApi(id, categoria) {
    return executarRequisicaoApi(`/categorias/${id}`, {
        method: "PUT",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(categoria),
    }, "Erro ao editar categoria na API.");
}

async function removerCategoriaApi(id) {
    return executarRequisicaoApi(`/categorias/${id}`, {
        method: "DELETE",
    }, "Erro ao remover categoria na API.");
}

async function listarFornecedoresApi() {
    return executarRequisicaoApi("/fornecedores", undefined, "Erro ao listar fornecedores na API.");
}

async function buscarFornecedorPorIdApi(id) {
    return executarRequisicaoApi(`/fornecedores/${id}`, undefined, "Fornecedor não encontrado na API.");
}

async function cadastrarFornecedorApi(fornecedor) {
    return executarRequisicaoApi("/fornecedores", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(fornecedor),
    }, "Erro ao cadastrar fornecedor na API.");
}

async function editarFornecedorApi(id, fornecedor) {
    return executarRequisicaoApi(`/fornecedores/${id}`, {
        method: "PUT",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(fornecedor),
    }, "Erro ao editar fornecedor na API.");
}

async function removerFornecedorApi(id) {
    return executarRequisicaoApi(`/fornecedores/${id}`, {
        method: "DELETE",
    }, "Erro ao remover fornecedor na API.");
}

async function inativarFornecedorApi(id) {
    return executarRequisicaoApi(`/fornecedores/${id}/inativar`, {
        method: "PATCH",
    }, "Erro ao inativar fornecedor na API.");
}