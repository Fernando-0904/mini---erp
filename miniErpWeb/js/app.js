inicializarAplicacao();

async function inicializarAplicacao() {
	let usuario = null;
	let erroSessao = "";

	configurarTratamentoGlobalErros();
	aplicarMelhoriasDeInterface();

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

function aplicarMelhoriasDeInterface() {
	organizarNavegacaoPorModulos();
	inserirBreadcrumb();
	inserirNavegacaoContextual();
	inserirAcoesRapidasCompras();
}

function organizarNavegacaoPorModulos() {
	const cabecalho = document.querySelector("header");
	const navegacao = document.querySelector("header nav[aria-label='Navegação principal']");

	if (!(cabecalho instanceof HTMLElement) || !(navegacao instanceof HTMLElement)) {
		return;
	}

	const linksOriginais = Array.from(navegacao.querySelectorAll("a[href]"));

	if (linksOriginais.length === 0) {
		return;
	}

	document.body.classList.add("layout-modular");
	navegacao.classList.add("nav-modular");
	garantirBotaoMenuModular(cabecalho);

	const grupos = [
		{
			titulo: "Visão Geral",
			itens: ["index.html"]
		},
		{
			titulo: "Cadastros",
			itens: ["produtos.html", "categorias.html", "fornecedores.html"]
		},
		{
			titulo: "Operações",
			itens: ["movimentacoes.html", "compras.html", "estoque-baixo.html"]
		},
		{
			titulo: "Gestão",
			itens: ["relatorios.html", "auditoria.html"]
		},
		{
			titulo: "Utilitários",
			itens: ["busca-global.html"]
		}
	];

	const linksPorArquivo = new Map();

	for (const link of linksOriginais) {
		const arquivo = extrairArquivoDoHref(link.getAttribute("href"));
		if (arquivo !== "") {
			linksPorArquivo.set(arquivo, link);
		}
	}

	navegacao.replaceChildren();

	for (const grupo of grupos) {
		const secao = document.createElement("section");
		secao.className = "nav-modulo";

		const titulo = document.createElement("p");
		titulo.className = "nav-modulo-titulo";
		titulo.textContent = grupo.titulo;
		secao.appendChild(titulo);

		const lista = document.createElement("div");
		lista.className = "nav-modulo-links";

		let temItem = false;

		for (const arquivo of grupo.itens) {
			const link = linksPorArquivo.get(arquivo);
			if (link instanceof HTMLAnchorElement) {
				lista.appendChild(link);
				temItem = true;
			}
		}

		if (temItem) {
			secao.appendChild(lista);
			navegacao.appendChild(secao);
		}
	}

	const linksRestantes = Array.from(linksPorArquivo.entries()).filter(function (entrada) {
		return !grupos.some(function (grupo) {
			return grupo.itens.includes(entrada[0]);
		});
	});

	if (linksRestantes.length > 0) {
		const secaoOutros = document.createElement("section");
		secaoOutros.className = "nav-modulo";

		const tituloOutros = document.createElement("p");
		tituloOutros.className = "nav-modulo-titulo";
		tituloOutros.textContent = "Outros";
		secaoOutros.appendChild(tituloOutros);

		const listaOutros = document.createElement("div");
		listaOutros.className = "nav-modulo-links";

		for (const entrada of linksRestantes) {
			listaOutros.appendChild(entrada[1]);
		}

		secaoOutros.appendChild(listaOutros);
		navegacao.appendChild(secaoOutros);
	}
}

function garantirBotaoMenuModular(cabecalho) {
	const topoMarca = cabecalho.querySelector(".topo-marca");

	if (!(topoMarca instanceof HTMLElement) || topoMarca.querySelector("#botaoMenuModular")) {
		return;
	}

	const botao = document.createElement("button");
	botao.id = "botaoMenuModular";
	botao.type = "button";
	botao.className = "botao-menu-modular";
	botao.setAttribute("aria-label", "Abrir menu de navegação");
	botao.setAttribute("aria-expanded", "false");
	botao.textContent = "Menu";

	botao.addEventListener("click", function () {
		const aberto = document.body.classList.toggle("menu-modular-aberto");
		botao.setAttribute("aria-expanded", aberto ? "true" : "false");
	});

	topoMarca.insertBefore(botao, topoMarca.firstChild);
}

function inserirBreadcrumb() {
	const conteudoPrincipal = document.querySelector("main");

	if (!(conteudoPrincipal instanceof HTMLElement) || conteudoPrincipal.querySelector(".breadcrumb-app")) {
		return;
	}

	const paginaAtual = extrairArquivoDoHref(window.location.pathname);
	const estrutura = obterEstruturaPagina(paginaAtual);

	if (estrutura === null) {
		return;
	}

	const breadcrumb = document.createElement("nav");
	breadcrumb.className = "breadcrumb-app";
	breadcrumb.setAttribute("aria-label", "Navegação da página");

	const trilha = document.createElement("p");
	trilha.className = "breadcrumb-trilha";
	trilha.textContent = `Mini ERP / ${estrutura.modulo} / ${estrutura.nome}`;

	breadcrumb.appendChild(trilha);
	conteudoPrincipal.insertBefore(breadcrumb, conteudoPrincipal.firstChild);
}

function inserirNavegacaoContextual() {
	const conteudoPrincipal = document.querySelector("main");
	if (!(conteudoPrincipal instanceof HTMLElement) || conteudoPrincipal.querySelector(".navegacao-contextual")) {
		return;
	}

	const paginaAtual = extrairArquivoDoHref(window.location.pathname);
	const estrutura = obterEstruturaPagina(paginaAtual);

	if (estrutura === null || estrutura.contexto.length <= 1) {
		return;
	}

	const bloco = document.createElement("section");
	bloco.className = "navegacao-contextual";

	const titulo = document.createElement("h3");
	titulo.textContent = `${estrutura.modulo} - atalhos`;
	bloco.appendChild(titulo);

	const lista = document.createElement("div");
	lista.className = "navegacao-contextual-links";

	for (const item of estrutura.contexto) {
		const link = document.createElement("a");
		link.href = item.arquivo;
		link.className = "atalho-contextual";
		link.textContent = item.nome;
		if (item.arquivo === paginaAtual) {
			link.setAttribute("aria-current", "page");
		}
		lista.appendChild(link);
	}

	bloco.appendChild(lista);

	const referencia = conteudoPrincipal.querySelector("section");
	if (referencia) {
		conteudoPrincipal.insertBefore(bloco, referencia.nextSibling);
	} else {
		conteudoPrincipal.appendChild(bloco);
	}
}

function obterEstruturaPagina(paginaAtual) {
	const estrutura = {
		"index.html": { nome: "Painel", modulo: "Visão Geral", contexto: [{ arquivo: "index.html", nome: "Painel" }] },
		"produtos.html": {
			nome: "Produtos",
			modulo: "Cadastros",
			contexto: [
				{ arquivo: "produtos.html", nome: "Produtos" },
				{ arquivo: "categorias.html", nome: "Categorias" },
				{ arquivo: "fornecedores.html", nome: "Fornecedores" }
			]
		},
		"categorias.html": {
			nome: "Categorias",
			modulo: "Cadastros",
			contexto: [
				{ arquivo: "produtos.html", nome: "Produtos" },
				{ arquivo: "categorias.html", nome: "Categorias" },
				{ arquivo: "fornecedores.html", nome: "Fornecedores" }
			]
		},
		"fornecedores.html": {
			nome: "Fornecedores",
			modulo: "Cadastros",
			contexto: [
				{ arquivo: "produtos.html", nome: "Produtos" },
				{ arquivo: "categorias.html", nome: "Categorias" },
				{ arquivo: "fornecedores.html", nome: "Fornecedores" }
			]
		},
		"movimentacoes.html": {
			nome: "Movimentações",
			modulo: "Operações",
			contexto: [
				{ arquivo: "movimentacoes.html", nome: "Movimentações" },
				{ arquivo: "compras.html", nome: "Compras" },
				{ arquivo: "estoque-baixo.html", nome: "Estoque baixo" }
			]
		},
		"compras.html": {
			nome: "Compras",
			modulo: "Operações",
			contexto: [
				{ arquivo: "movimentacoes.html", nome: "Movimentações" },
				{ arquivo: "compras.html", nome: "Compras" },
				{ arquivo: "estoque-baixo.html", nome: "Estoque baixo" }
			]
		},
		"estoque-baixo.html": {
			nome: "Estoque baixo",
			modulo: "Operações",
			contexto: [
				{ arquivo: "movimentacoes.html", nome: "Movimentações" },
				{ arquivo: "compras.html", nome: "Compras" },
				{ arquivo: "estoque-baixo.html", nome: "Estoque baixo" }
			]
		},
		"relatorios.html": {
			nome: "Relatórios",
			modulo: "Gestão",
			contexto: [
				{ arquivo: "relatorios.html", nome: "Relatórios" },
				{ arquivo: "auditoria.html", nome: "Auditoria" }
			]
		},
		"auditoria.html": {
			nome: "Auditoria",
			modulo: "Gestão",
			contexto: [
				{ arquivo: "relatorios.html", nome: "Relatórios" },
				{ arquivo: "auditoria.html", nome: "Auditoria" }
			]
		},
		"busca-global.html": {
			nome: "Busca global",
			modulo: "Utilitários",
			contexto: [{ arquivo: "busca-global.html", nome: "Busca global" }]
		}
	};

	return estrutura[paginaAtual] || null;
}

function extrairArquivoDoHref(href) {
	if (typeof href !== "string" || href.trim() === "") {
		return "";
	}

	const hrefLimpo = href.split("#")[0].split("?")[0].trim();
	const partes = hrefLimpo.split("/").filter(Boolean);

	if (partes.length === 0) {
		return "index.html";
	}

	return partes[partes.length - 1].toLowerCase();
}

function inserirAcoesRapidasCompras() {
	if (!(elementos.formularioPedidoCompra instanceof HTMLFormElement) ||
		!(elementos.campoFiltroStatusPedidoCompra instanceof HTMLSelectElement)) {
		return;
	}

	const conteudoPrincipal = document.querySelector("main");
	if (!(conteudoPrincipal instanceof HTMLElement) || conteudoPrincipal.querySelector(".acoes-compras-rapidas")) {
		return;
	}

	const bloco = document.createElement("section");
	bloco.className = "acoes-compras-rapidas";

	const titulo = document.createElement("h3");
	titulo.textContent = "Ações rápidas de compras";
	bloco.appendChild(titulo);

	const lista = document.createElement("div");
	lista.className = "navegacao-contextual-links";

	lista.appendChild(criarBotaoAcaoRapida("Novo pedido", function () {
		elementos.campoFornecedorPedidoCompra?.focus();
	}));

	lista.appendChild(criarBotaoAcaoRapida("Pendentes", function () {
		aplicarFiltroCompra("PendenteAprovacao");
	}));

	lista.appendChild(criarBotaoAcaoRapida("Aprovados", function () {
		aplicarFiltroCompra("Aprovado");
	}));

	lista.appendChild(criarBotaoAcaoRapida("Rejeitados", function () {
		aplicarFiltroCompra("Rejeitado");
	}));

	lista.appendChild(criarBotaoAcaoRapida("Todos", function () {
		aplicarFiltroCompra("");
	}));

	bloco.appendChild(lista);

	const primeiraSecao = conteudoPrincipal.querySelector("section");
	if (primeiraSecao) {
		conteudoPrincipal.insertBefore(bloco, primeiraSecao.nextSibling);
	} else {
		conteudoPrincipal.appendChild(bloco);
	}
}

function criarBotaoAcaoRapida(texto, acao) {
	const botao = document.createElement("button");
	botao.type = "button";
	botao.className = "atalho-contextual";
	botao.textContent = texto;
	botao.addEventListener("click", acao);
	return botao;
}

function aplicarFiltroCompra(status) {
	if (!(elementos.campoFiltroStatusPedidoCompra instanceof HTMLSelectElement)) {
		return;
	}

	elementos.campoFiltroStatusPedidoCompra.value = status;
	elementos.campoFiltroStatusPedidoCompra.dispatchEvent(new Event("change"));
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
