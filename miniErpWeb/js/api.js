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
const METODOS_HTTP_SEGUROS = new Set(["GET", "HEAD", "OPTIONS", "TRACE"]);
let tokenAntiforgery = null;

class ErroApi extends Error {
    constructor(mensagem, status) {
        super(mensagem);
        this.name = "ErroApi";
        this.status = status;
    }
}

async function executarRequisicaoApi(caminho, opcoes, mensagemErroPadrao, notificarSessaoExpirada = true, permitirNovaTentativaCsrf = true) {
    let resposta;
    const opcoesRequisicao = {
        ...(opcoes || {}),
        credentials: "include",
    };
    const metodo = (opcoesRequisicao.method || "GET").toUpperCase();

    if (!METODOS_HTTP_SEGUROS.has(metodo)) {
        const token = await obterTokenAntiforgeryApi();
        opcoesRequisicao.headers = {
            ...(opcoesRequisicao.headers || {}),
            "X-CSRF-TOKEN": token,
        };
    }

    try {
        resposta = await fetch(`${API_BASE_URL}${caminho}`, opcoesRequisicao);
    } catch {
        throw new Error("Não foi possível conectar à API. Verifique se ela está em execução e tente novamente.");
    }

    if (!METODOS_HTTP_SEGUROS.has(metodo) &&
        permitirNovaTentativaCsrf &&
        await respostaIndicaErroAntiforgery(resposta)) {
        invalidarTokenAntiforgery();
        return executarRequisicaoApi(caminho, opcoes, mensagemErroPadrao, notificarSessaoExpirada, false);
    }

    return tratarRespostaApi(resposta, mensagemErroPadrao, notificarSessaoExpirada);
}

async function respostaIndicaErroAntiforgery(resposta) {
    if (resposta.status !== 400) {
        return false;
    }

    try {
        const conteudo = await resposta.clone().text();
        return /token de segurança ausente ou inválido/i.test(conteudo);
    } catch {
        return false;
    }
}

async function tratarRespostaApi(resposta, mensagemErroPadrao, notificarSessaoExpirada = true) {
    if (resposta.ok) {
        if (resposta.status === 204) {
            return null;
        }

        return resposta.json();
    }

    let mensagemErro = resposta.status >= 500 ? MENSAGEM_ERRO_INESPERADO : mensagemErroPadrao;

    if (resposta.status === 401 && notificarSessaoExpirada) {
        mensagemErro = "Sua sessão expirou. Entre novamente para continuar.";
        window.dispatchEvent(new CustomEvent("miniErp:sessao-expirada"));
    }

    try {
        const erro = await resposta.json();
        const mensagemExtraida = extrairMensagemErroApi(erro);

        if (resposta.status < 500 && mensagemExtraida !== "") {
            mensagemErro = mensagemExtraida;
        }
    } catch {
        // Mantém a mensagem padrão se a API não retornar JSON.
    }

    throw new ErroApi(normalizarMensagemErroUsuario(mensagemErro), resposta.status);
}

async function obterTokenAntiforgeryApi() {
    if (typeof tokenAntiforgery === "string" && tokenAntiforgery !== "") {
        return tokenAntiforgery;
    }

    let resposta;

    try {
        resposta = await fetch(`${API_BASE_URL}/auth/csrf`, {
            credentials: "include",
            cache: "no-store",
        });
    } catch {
        throw new Error("Não foi possível conectar à API. Verifique se ela está em execução e tente novamente.");
    }

    if (!resposta.ok) {
        throw new ErroApi("Não foi possível preparar a requisição segura.", resposta.status);
    }

    const dados = await resposta.json();

    if (typeof dados.token !== "string" || dados.token === "") {
        throw new Error("Não foi possível preparar a requisição segura.");
    }

    tokenAntiforgery = dados.token;
    return tokenAntiforgery;
}

function invalidarTokenAntiforgery() {
    tokenAntiforgery = null;
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

async function autenticarUsuarioApi(email, senha) {
    const usuario = await executarRequisicaoApi("/auth/login", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ email, senha }),
    }, "E-mail ou senha inválidos.", false);

    invalidarTokenAntiforgery();
    return usuario;
}

async function cadastrarUsuarioApi(nome, email, senha) {
    const resultado = await executarRequisicaoApi("/auth/cadastro", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ nome, email, senha }),
    }, "Não foi possível criar a conta.");

    invalidarTokenAntiforgery();
    return resultado;
}

async function confirmarEmailApi(token) {
    return executarRequisicaoApi("/auth/confirmar-email", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ token }),
    }, "Não foi possível confirmar seu e-mail.", false);
}

async function reenviarConfirmacaoEmailApi(email) {
    return executarRequisicaoApi("/auth/reenviar-confirmacao", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ email }),
    }, "Não foi possível reenviar a confirmação.", false);
}

async function solicitarRedefinicaoSenhaApi(email) {
    return executarRequisicaoApi("/auth/esqueci-senha", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ email }),
    }, "Não foi possível solicitar a recuperação de senha.", false);
}

async function redefinirSenhaApi(token, novaSenha) {
    return executarRequisicaoApi("/auth/redefinir-senha", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ token, novaSenha }),
    }, "Não foi possível redefinir sua senha.", false);
}

async function obterSessaoAtualApi() {
    let resposta;

    try {
        resposta = await fetch(`${API_BASE_URL}/auth/me`, {
            credentials: "include",
            cache: "no-store",
        });
    } catch {
        throw new Error("Não foi possível conectar à API. Verifique se ela está em execução e tente novamente.");
    }

    if (resposta.status === 401) {
        return null;
    }

    return tratarRespostaApi(resposta, "Não foi possível verificar sua sessão.");
}

async function encerrarSessaoApi() {
    await executarRequisicaoApi("/auth/logout", {
        method: "POST",
    }, "Não foi possível sair da conta.");

    invalidarTokenAntiforgery();
}
