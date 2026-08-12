inicializarAplicacao();

async function inicializarAplicacao() {
	let usuario = null;
	let erroSessao = "";

	configurarTratamentoGlobalErros();

	inicializarStatusConexaoSistema();

	try {
		usuario = await obterSessaoAtualApi();
	} catch (erro) {
		erroSessao = erro instanceof Error
			? erro.message
			: "Não foi possível verificar sua sessão.";
	}

	inicializarLoginVisual(usuario);

	window.addEventListener("miniErp:sessao-expirada", function () {
		mostrarAcessoRestrito("Sua sessão expirou. Entre novamente para continuar.");
	});

	if (usuario === null) {
		mostrarAcessoRestrito(erroSessao);
		return;
	}

	inicializarControllersDaPagina();
}

function inicializarControllersDaPagina() {
	inicializarControllerComIsolamento(elementos.quantidadeProdutos !== null, "inicializarPainelController", "painel");
	inicializarControllerComIsolamento(elementos.formulario !== null, "inicializarProdutoController", "produtos");
	inicializarControllerComIsolamento(elementos.formularioCategoria !== null, "inicializarCategoriaController", "categorias");
	inicializarControllerComIsolamento(elementos.formularioFornecedor !== null, "inicializarFornecedorController", "fornecedores");
	inicializarControllerComIsolamento(elementos.formularioMovimentacaoEstoque !== null, "inicializarMovimentacaoController", "movimentacoes");
	inicializarControllerComIsolamento(elementos.tabelaEstoqueBaixo !== null, "inicializarEstoqueBaixoController", "estoque-baixo");
	inicializarControllerComIsolamento(elementos.formularioRelatorios !== null, "inicializarRelatoriosController", "relatorios");
	inicializarControllerComIsolamento(elementos.formularioAuditoria !== null, "inicializarAuditoriaController", "auditoria");
	inicializarControllerComIsolamento(elementos.formularioPedidoCompra !== null, "inicializarComprasController", "compras");
	inicializarControllerComIsolamento(Boolean(elementos.formularioBuscaGlobal), "inicializarBuscaGlobalController", "busca-global");
}

function inicializarControllerComIsolamento(deveInicializar, nomeInicializador, nomeController) {
	if (!deveInicializar) {
		return;
	}

	const inicializador = typeof window[nomeInicializador] === "function"
		? window[nomeInicializador]
		: null;

	if (inicializador === null) {
		console.warn("Controller não carregado para a página:", nomeController, nomeInicializador);
		return;
	}

	try {
		inicializador();
	} catch (erro) {
		console.error("Falha ao inicializar controller:", nomeController, erro);

		if (typeof exibirMensagem === "function") {
			exibirMensagem("Alguns recursos da tela não puderam ser carregados agora. Recarregue para tentar novamente.", "aviso");
		}
	}
}

function configurarTratamentoGlobalErros() {
	if (typeof window === "undefined") {
		return;
	}

	if (window.__miniErpTratamentoErrosConfigurado === true) {
		return;
	}

	window.__miniErpTratamentoErrosConfigurado = true;

	window.addEventListener("error", function (evento) {
		console.error("Erro global do frontend:", evento.error || evento.message);
	});

	window.addEventListener("unhandledrejection", function (evento) {
		console.error("Promise rejeitada sem tratamento:", evento.reason);
	});
}

function mostrarAcessoRestrito(mensagemErro = "") {
	const conteudoPrincipal = document.querySelector("main");

	if (!(conteudoPrincipal instanceof HTMLElement)) {
		return;
	}

	const painel = document.createElement("section");
	painel.className = "acesso-restrito";
	painel.setAttribute("aria-live", "polite");

	const marcador = document.createElement("span");
	marcador.className = "acesso-restrito-marcador";
	marcador.textContent = "Área protegida";

	const titulo = document.createElement("h3");
	titulo.textContent = "Entre para acessar o Mini ERP";

	const descricao = document.createElement("p");
	descricao.textContent = mensagemErro ||
		"Seus dados operacionais estão protegidos. Use uma conta válida para continuar.";

	const botao = document.createElement("button");
	botao.type = "button";
	botao.textContent = "Abrir acesso";
	botao.addEventListener("click", function (evento) {
		evento.stopPropagation();
		const botaoLogin = document.getElementById("botaoLogin");

		if (botaoLogin instanceof HTMLButtonElement) {
			botaoLogin.click();
		}
	});

	painel.append(marcador, titulo, descricao, botao);
	conteudoPrincipal.replaceChildren(painel);
}
