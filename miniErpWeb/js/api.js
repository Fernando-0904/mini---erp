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
const MENSAGEM_ERRO_CONEXAO = "Não foi possível conectar ao sistema agora. Tente novamente mais tarde.";
const METODOS_HTTP_SEGUROS = new Set(["GET", "HEAD", "OPTIONS", "TRACE"]);
const TENTATIVAS_REDE_METODO_SEGURO = 2;
const ESPERA_REDE_MS = 350;
let tokenAntiforgery = null;

function notificarStatusConexaoSistema(status) {
    if (typeof window === "undefined") {
        return;
    }

    window.dispatchEvent(new CustomEvent("miniErp:status-conexao", {
        detail: { status }
    }));
}

class ErroApi extends Error {
    constructor(mensagem, status, correlationId = null) {
        super(mensagem);
        this.name = "ErroApi";
        this.status = status;
        this.correlationId = correlationId;
    }
}

async function executarRequisicaoApi(caminho, opcoes, mensagemErroPadrao, notificarSessaoExpirada = true, permitirNovaTentativaCsrf = true) {
    let resposta;
    const opcoesRequisicao = {
        ...(opcoes || {}),
        credentials: "include",
    };
    const metodo = (opcoesRequisicao.method || "GET").toUpperCase();
    const totalTentativas = METODOS_HTTP_SEGUROS.has(metodo)
        ? TENTATIVAS_REDE_METODO_SEGURO
        : 1;

    if (!METODOS_HTTP_SEGUROS.has(metodo)) {
        const token = await obterTokenAntiforgeryApi();
        opcoesRequisicao.headers = {
            ...(opcoesRequisicao.headers || {}),
            "X-CSRF-TOKEN": token,
        };
    }

    for (let tentativa = 1; tentativa <= totalTentativas; tentativa += 1) {
        try {
            resposta = await fetch(`${API_BASE_URL}${caminho}`, opcoesRequisicao);
            notificarStatusConexaoSistema("online");
            break;
        } catch {
            notificarStatusConexaoSistema("offline");

            if (tentativa >= totalTentativas) {
                throw new Error(MENSAGEM_ERRO_CONEXAO);
            }

            await esperar(ESPERA_REDE_MS * tentativa);
        }
    }

    if (!(resposta instanceof Response)) {
        throw new Error(MENSAGEM_ERRO_CONEXAO);
    }

    if (!METODOS_HTTP_SEGUROS.has(metodo) &&
        permitirNovaTentativaCsrf &&
        await respostaIndicaErroAntiforgery(resposta)) {
        invalidarTokenAntiforgery();
        return executarRequisicaoApi(caminho, opcoes, mensagemErroPadrao, notificarSessaoExpirada, false);
    }

    return tratarRespostaApi(resposta, mensagemErroPadrao, notificarSessaoExpirada);
}

function esperar(tempoMs) {
    return new Promise(function (resolve) {
        setTimeout(resolve, tempoMs);
    });
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

    let correlationId = resposta.headers.get("X-Correlation-Id");
    let mensagemErro = resposta.status >= 500
        ? "Não foi possível concluir a operação agora. Tente novamente mais tarde."
        : mensagemErroPadrao;

    if (resposta.status === 401 && notificarSessaoExpirada) {
        mensagemErro = "Sua sessão expirou. Entre novamente para continuar.";
        window.dispatchEvent(new CustomEvent("miniErp:sessao-expirada"));
    }

    if (resposta.status === 403) {
        mensagemErro = "Seu perfil não tem permissão para executar esta operação.";
    }

    try {
        const erro = await resposta.json();
        if (!correlationId && erro !== null && typeof erro === "object") {
            if (typeof erro.correlationId === "string" && erro.correlationId.trim() !== "") {
                correlationId = erro.correlationId.trim();
            } else if (
                erro.extensions !== null &&
                typeof erro.extensions === "object" &&
                typeof erro.extensions.correlationId === "string" &&
                erro.extensions.correlationId.trim() !== ""
            ) {
                correlationId = erro.extensions.correlationId.trim();
            }
        }

        const mensagemExtraida = extrairMensagemErroApi(erro);

        if (resposta.status < 500 && resposta.status !== 403) {
            mensagemErro = resolverMensagemErroAmigavel(mensagemExtraida, mensagemErroPadrao, resposta.status);
        }
    } catch {
        mensagemErro = resolverMensagemErroAmigavel("", mensagemErroPadrao, resposta.status);
    }

    throw new ErroApi(normalizarMensagemErroUsuario(mensagemErro), resposta.status, correlationId);
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
        notificarStatusConexaoSistema("online");
    } catch {
        notificarStatusConexaoSistema("offline");
        throw new Error(MENSAGEM_ERRO_CONEXAO);
    }

    if (!resposta.ok) {
        throw new ErroApi("Não foi possível validar sua sessão agora. Atualize a página e tente novamente.", resposta.status);
    }

    const dados = await resposta.json();

    if (typeof dados.token !== "string" || dados.token === "") {
        throw new Error("Não foi possível validar sua sessão agora. Atualize a página e tente novamente.");
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
            return MENSAGEM_ERRO_CONEXAO;
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

function resolverMensagemErroAmigavel(mensagemApi, mensagemPadrao, status) {
    const mensagemBase = (typeof mensagemApi === "string" ? mensagemApi.trim() : "");
    const mensagemLower = mensagemBase.toLowerCase();

    if (mensagemLower.includes("token de segurança") || mensagemLower.includes("csrf") || mensagemLower.includes("antiforgery")) {
        return "Sua sessão de segurança expirou. Atualize a página e tente novamente.";
    }

    if (mensagemLower.includes("estoque insuficiente") || mensagemLower.includes("saldo insuficiente")) {
        return "Não foi possível registrar a saída porque o saldo em estoque é insuficiente.";
    }

    if (mensagemLower.includes("produto não encontrado")) {
        return "Não encontramos esse produto. Confira o código e tente novamente.";
    }

    if (mensagemLower.includes("categoria não encontrada") || mensagemLower.includes("categoria inexistente")) {
        return "A categoria informada não existe mais. Atualize os dados e tente novamente.";
    }

    if (mensagemLower.includes("fornecedor informado não existe")) {
        return "O fornecedor informado não foi encontrado. Atualize os dados e tente novamente.";
    }

    if (mensagemLower.includes("fornecedor informado está inativo")) {
        return "Esse fornecedor está inativo. Escolha um fornecedor ativo para continuar.";
    }

    if (mensagemLower.includes("já existe um produto com esse código")) {
        return "Já existe um produto com esse código. Use outro código para continuar.";
    }

    if (mensagemLower.includes("já existe uma categoria com esse nome")) {
        return "Já existe uma categoria com esse nome. Informe um nome diferente.";
    }

    if (mensagemLower.includes("já existe um fornecedor com esse código ou documento")) {
        return "Já existe fornecedor com esse código ou documento. Revise os dados e tente novamente.";
    }

    if (mensagemLower.includes("e-mail e senha são obrigatórios")) {
        return "Informe e-mail e senha para entrar.";
    }

    if (mensagemLower.includes("confirme seu e-mail antes de entrar")) {
        return "Seu e-mail ainda não foi confirmado. Verifique sua caixa de entrada e confirme antes de entrar.";
    }

    if (mensagemLower.includes("token inválido") || mensagemLower.includes("token de confirmação inválido")) {
        return "O link usado não é mais válido. Solicite um novo link e tente novamente.";
    }

    if (mensagemBase !== "" && !mensagemPareceTecnica(mensagemBase)) {
        return mensagemBase;
    }

    if (status === 404) {
        return "Não encontramos o registro solicitado.";
    }

    if (status === 409) {
        return "Já existe um registro com esses dados. Revise e tente novamente.";
    }

    if (status === 400) {
        return "Não foi possível concluir a operação. Revise os dados informados e tente novamente.";
    }

    if (status === 401) {
        return "Sua sessão expirou. Entre novamente para continuar.";
    }

    if (status === 403) {
        return "Seu perfil não tem permissão para executar esta operação.";
    }

    return mensagemPadrao;
}

function mensagemPareceTecnica(mensagem) {
    return /exception|stack trace|sql|sqlite|invalidoperation|object reference|inner exception|at\s+[a-z0-9_.]+\(/i.test(mensagem);
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

async function listarRelatorioProdutosEstoqueBaixoApi() {
    return executarRequisicaoApi("/relatorios/produtos-estoque-baixo", undefined, "Erro ao carregar o relatório de produtos com estoque baixo.");
}

async function listarRelatorioProdutosSemEstoqueApi() {
    return executarRequisicaoApi("/relatorios/produtos-sem-estoque", undefined, "Erro ao carregar o relatório de produtos sem estoque.");
}

async function listarRelatorioValorEstoquePorCategoriaApi() {
    return executarRequisicaoApi("/relatorios/valor-estoque-por-categoria", undefined, "Erro ao carregar o relatório de valor por categoria.");
}

async function listarRelatorioProdutosSemFornecedorApi() {
    return executarRequisicaoApi("/relatorios/produtos-sem-fornecedor", undefined, "Erro ao carregar o relatório de produtos sem fornecedor.");
}

async function listarRelatorioUltimasMovimentacoesApi(limite) {
    const limiteNumerico = Number(limite);
    const caminho = Number.isFinite(limiteNumerico)
        ? `/relatorios/ultimas-movimentacoes?limite=${encodeURIComponent(limiteNumerico)}`
        : "/relatorios/ultimas-movimentacoes";

    return executarRequisicaoApi(caminho, undefined, "Erro ao carregar o relatório das últimas movimentações.");
}

async function listarAuditoriaApi(limite) {
    const limiteNumerico = Number(limite);
    const caminho = Number.isFinite(limiteNumerico)
        ? `/relatorios/auditoria?limite=${encodeURIComponent(limiteNumerico)}`
        : "/relatorios/auditoria";

    return executarRequisicaoApi(caminho, undefined, "Erro ao carregar os eventos de auditoria.");
}

async function listarAlertasOperacionaisApi() {
    return executarRequisicaoApi("/relatorios/alertas-operacionais", undefined, "Erro ao carregar os alertas operacionais.");
}

async function exportarRelatorioCsvApi(tipoRelatorio, limite) {
    const parametros = new URLSearchParams();
    parametros.set("tipo", tipoRelatorio);

    if (Number.isFinite(Number(limite)) && Number(limite) > 0) {
        parametros.set("limite", String(Math.trunc(Number(limite))));
    }

    let resposta;

    try {
        resposta = await fetch(`${API_BASE_URL}/relatorios/exportar?${parametros.toString()}`, {
            credentials: "include",
            cache: "no-store"
        });
    } catch {
        throw new Error("Não foi possível conectar à API. Verifique se ela está em execução e tente novamente.");
    }

    if (!resposta.ok) {
        await tratarRespostaApi(resposta, "Não foi possível exportar o relatório.");
    }

    const blob = await resposta.blob();
    const contentDisposition = resposta.headers.get("Content-Disposition") || "";
    const nomeArquivo = extrairNomeArquivoContentDisposition(contentDisposition)
        || `relatorio-${tipoRelatorio}.csv`;

    return { blob, nomeArquivo };
}

function extrairNomeArquivoContentDisposition(contentDisposition) {
    const correspondenciaUtf8 = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);

    if (correspondenciaUtf8 && correspondenciaUtf8[1]) {
        return decodeURIComponent(correspondenciaUtf8[1].trim().replace(/^"|"$/g, ""));
    }

    const correspondenciaPadrao = contentDisposition.match(/filename=([^;]+)/i);

    if (correspondenciaPadrao && correspondenciaPadrao[1]) {
        return correspondenciaPadrao[1].trim().replace(/^"|"$/g, "");
    }

    return "";
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

async function listarPedidosCompraApi() {
    return executarRequisicaoApi("/compras/pedidos", undefined, "Erro ao listar pedidos de compra.");
}

async function criarPedidoCompraApi(pedido) {
    return executarRequisicaoApi("/compras/pedidos", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(pedido),
    }, "Erro ao criar pedido de compra.");
}

async function receberPedidoCompraApi(id) {
    return executarRequisicaoApi(`/compras/pedidos/${id}/receber`, {
        method: "POST",
    }, "Erro ao receber pedido de compra.");
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
