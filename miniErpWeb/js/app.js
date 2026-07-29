inicializarAplicacao();

async function inicializarAplicacao() {
	let usuario = null;
	let erroSessao = "";

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
	if (elementos.quantidadeProdutos !== null) {
		inicializarPainelController();
	}

	if (elementos.formulario !== null) {
		inicializarProdutoController();
	}

	if (elementos.formularioCategoria !== null) {
		inicializarCategoriaController();
	}

	if (elementos.formularioFornecedor !== null) {
		inicializarFornecedorController();
	}

	if (elementos.formularioMovimentacaoEstoque !== null) {
		inicializarMovimentacaoController();
	}

	if (elementos.tabelaEstoqueBaixo !== null) {
		inicializarEstoqueBaixoController();
	}

	if (elementos.formularioBuscaGlobal) {
		inicializarBuscaGlobalController();
	}
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
