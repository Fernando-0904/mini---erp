function inicializarLoginVisual() {
	const botaoLogin = document.getElementById("botaoLogin");
	const painelLogin = document.getElementById("painelLogin");
	const menuLoginVisual = document.getElementById("menuLoginVisual");
	const formLoginVisual = document.getElementById("formLoginVisual");
	const formCadastroVisual = document.getElementById("formCadastroVisual");
	const avatarLogin = botaoLogin ? botaoLogin.querySelector(".avatar-login") : null;
	const botaoAbrirLogin = painelLogin ? painelLogin.querySelector(".botao-abrir-login") : null;
	const botaoCriarConta = painelLogin ? painelLogin.querySelector(".botao-criar-conta") : null;
	const botaoConfirmarLogin = painelLogin ? painelLogin.querySelector(".botao-confirmar-login") : null;
	const botaoConfirmarCadastro = painelLogin ? painelLogin.querySelector(".botao-confirmar-cadastro") : null;
	const botoesVoltarLogin = painelLogin ? painelLogin.querySelectorAll(".botao-voltar-login") : [];
	const campoLoginEmail = document.getElementById("loginEmail");
	const campoLoginSenha = document.getElementById("loginSenha");
	const campoCadastroNome = document.getElementById("cadastroNome");
	const campoCadastroEmail = document.getElementById("cadastroEmail");
	const campoCadastroSenha = document.getElementById("cadastroSenha");
	const campoCadastroConfirmarSenha = document.getElementById("cadastroConfirmarSenha");
	const tituloMenuLogin = painelLogin ? painelLogin.querySelector(".titulo-menu-login") : null;
	const textoPadraoBotaoLogin = botaoConfirmarLogin instanceof HTMLElement
		? botaoConfirmarLogin.textContent
		: "Entrar";
	const textoPadraoBotaoCadastro = botaoConfirmarCadastro instanceof HTMLElement
		? botaoConfirmarCadastro.textContent
		: "Criar minha conta";
	if (botaoLogin === null || painelLogin === null) {
		return;
	}

	let usuarioLogado = carregarUsuarioLogado();

	let mensagemLoginVisual = document.getElementById("mensagemLoginVisual");
	let contaConectadaVisual = document.getElementById("contaConectadaVisual");

	if (!(contaConectadaVisual instanceof HTMLElement)) {
		contaConectadaVisual = document.createElement("section");
		contaConectadaVisual.id = "contaConectadaVisual";
		contaConectadaVisual.className = "conta-conectada-visual";
		contaConectadaVisual.hidden = true;
		contaConectadaVisual.innerHTML = [
			'<div class="conta-conectada-cabecalho">',
			'  <div class="conta-conectada-avatar" id="contaConectadaAvatar">U</div>',
			'  <div class="conta-conectada-dados">',
			'    <p class="conta-conectada-titulo">Conectado como</p>',
			'    <p class="conta-conectada-nome" id="contaConectadaNome"></p>',
			'    <p class="conta-conectada-email" id="contaConectadaEmail"></p>',
			'  </div>',
			'</div>',
			'<span class="conta-status-pill">Conta conectada</span>',
			'<button type="button" class="botao-sair-conta" id="botaoSairConta">Sair da conta</button>',
			'<div class="confirmar-saida-conta" id="confirmarSaidaConta" hidden>',
			'  <p>Deseja sair desta conta?</p>',
			'  <div class="acoes-confirmar-saida">',
			'    <button type="button" class="botao-confirmar-saida" id="botaoConfirmarSaidaConta">Sim, sair</button>',
			'    <button type="button" class="botao-cancelar-saida" id="botaoCancelarSaidaConta">Cancelar</button>',
			'  </div>',
			'</div>'
		].join("");

		painelLogin.appendChild(contaConectadaVisual);
	}

	const contaConectadaAvatar = contaConectadaVisual.querySelector("#contaConectadaAvatar");
	const contaConectadaNome = contaConectadaVisual.querySelector("#contaConectadaNome");
	const contaConectadaEmail = contaConectadaVisual.querySelector("#contaConectadaEmail");
	const botaoSairConta = contaConectadaVisual.querySelector("#botaoSairConta");
	const confirmarSaidaConta = contaConectadaVisual.querySelector("#confirmarSaidaConta");
	const botaoConfirmarSaidaConta = contaConectadaVisual.querySelector("#botaoConfirmarSaidaConta");
	const botaoCancelarSaidaConta = contaConectadaVisual.querySelector("#botaoCancelarSaidaConta");

	if (!(mensagemLoginVisual instanceof HTMLElement)) {
		mensagemLoginVisual = document.createElement("p");
		mensagemLoginVisual.id = "mensagemLoginVisual";
		mensagemLoginVisual.className = "mensagem-login-visual";
		mensagemLoginVisual.hidden = true;
		mensagemLoginVisual.setAttribute("aria-live", "polite");

		if (tituloMenuLogin instanceof HTMLElement) {
			tituloMenuLogin.insertAdjacentElement("afterend", mensagemLoginVisual);
		} else {
			painelLogin.appendChild(mensagemLoginVisual);
		}
	}

	function limparMensagemLogin() {
		if (!(mensagemLoginVisual instanceof HTMLElement)) {
			return;
		}

		mensagemLoginVisual.hidden = true;
		mensagemLoginVisual.textContent = "";
		mensagemLoginVisual.className = "mensagem-login-visual";
	}

	function limparMensagemGlobal() {
		if (typeof elementos !== "undefined" && elementos.mensagem instanceof HTMLElement) {
			elementos.mensagem.textContent = "";
			elementos.mensagem.className = "";
		}
	}

	function atualizarAvatarConta() {
		if (!(avatarLogin instanceof HTMLElement)) {
			return;
		}

		if (usuarioLogado === null || typeof usuarioLogado.nome !== "string" || usuarioLogado.nome.trim() === "") {
			avatarLogin.textContent = "L";
			botaoLogin.title = "Entrar";
			botaoLogin.setAttribute("aria-label", "Entrar");
			return;
		}

		avatarLogin.textContent = usuarioLogado.nome.trim().charAt(0).toUpperCase();
		botaoLogin.title = "Minha conta";
		botaoLogin.setAttribute("aria-label", "Minha conta");
	}

	function esconderConfirmacaoSaida() {
		if (confirmarSaidaConta instanceof HTMLElement) {
			confirmarSaidaConta.hidden = true;
		}
	}

	function mostrarConfirmacaoSaida() {
		if (confirmarSaidaConta instanceof HTMLElement) {
			confirmarSaidaConta.hidden = false;
		}

		if (botaoConfirmarSaidaConta instanceof HTMLElement) {
			botaoConfirmarSaidaConta.focus();
		}
	}

	function executarLogout() {
		usuarioLogado = null;
		limparUsuarioLogado();
		limparMensagemLogin();
		limparMensagemGlobal();
		esconderConfirmacaoSaida();

		if (campoLoginEmail instanceof HTMLInputElement) {
			campoLoginEmail.value = "";
		}

		if (campoLoginSenha instanceof HTMLInputElement) {
			campoLoginSenha.value = "";
		}

		mostrarMenuInicial();
		atualizarAvatarConta();
	}

	function mostrarContaConectada() {
		limparMensagemLogin();
		esconderConfirmacaoSaida();

		if (menuLoginVisual !== null) {
			menuLoginVisual.hidden = true;
		}

		if (formLoginVisual !== null) {
			formLoginVisual.hidden = true;
		}

		if (formCadastroVisual !== null) {
			formCadastroVisual.hidden = true;
		}

		if (contaConectadaVisual !== null) {
			contaConectadaVisual.hidden = false;
		}

		if (contaConectadaNome instanceof HTMLElement) {
			contaConectadaNome.textContent = typeof usuarioLogado?.nome === "string" && usuarioLogado.nome.trim() !== ""
				? usuarioLogado.nome
				: "Usuário";
		}

		if (contaConectadaAvatar instanceof HTMLElement) {
			contaConectadaAvatar.textContent = typeof usuarioLogado?.nome === "string" && usuarioLogado.nome.trim() !== ""
				? usuarioLogado.nome.trim().charAt(0).toUpperCase()
				: "U";
		}

		if (contaConectadaEmail instanceof HTMLElement) {
			contaConectadaEmail.textContent = typeof usuarioLogado?.email === "string"
				? usuarioLogado.email
				: "";
		}
	}

	function atualizarPainelConta() {
		atualizarAvatarConta();

		if (usuarioLogado !== null) {
			mostrarContaConectada();
			return;
		}

		mostrarMenuInicial();
	}

	function atualizarMensagemLogin(texto, tipo) {
		if (!(mensagemLoginVisual instanceof HTMLElement)) {
			return;
		}

		mensagemLoginVisual.hidden = false;
		mensagemLoginVisual.textContent = texto;

		if (tipo === "sucesso") {
			mensagemLoginVisual.className = "mensagem-login-visual mensagem-sucesso";
			return;
		}

		if (tipo === "erro") {
			mensagemLoginVisual.className = "mensagem-login-visual mensagem-erro";
			return;
		}

		mensagemLoginVisual.className = "mensagem-login-visual mensagem-info";
	}

	function mostrarMenuInicial() {
		limparMensagemLogin();
		esconderConfirmacaoSaida();

		if (contaConectadaVisual !== null) {
			contaConectadaVisual.hidden = true;
		}

		if (menuLoginVisual !== null) {
			menuLoginVisual.hidden = false;
		}

		if (formLoginVisual !== null) {
			formLoginVisual.hidden = true;
		}

		if (formCadastroVisual !== null) {
			formCadastroVisual.hidden = true;
		}
	}

	function mostrarFormularioLogin() {
		if (usuarioLogado !== null) {
			mostrarContaConectada();
			return;
		}

		limparMensagemLogin();

		if (menuLoginVisual !== null) {
			menuLoginVisual.hidden = true;
		}

		if (formLoginVisual !== null) {
			formLoginVisual.hidden = false;
		}

		if (formCadastroVisual !== null) {
			formCadastroVisual.hidden = true;
		}

		if (campoLoginEmail !== null) {
			campoLoginEmail.focus();
		}
	}

	function mostrarFormularioCadastro() {
		if (usuarioLogado !== null) {
			mostrarContaConectada();
			return;
		}

		limparMensagemLogin();

		if (menuLoginVisual !== null) {
			menuLoginVisual.hidden = true;
		}

		if (formLoginVisual !== null) {
			formLoginVisual.hidden = true;
		}

		if (formCadastroVisual !== null) {
			formCadastroVisual.hidden = false;
		}

		if (campoCadastroNome instanceof HTMLInputElement) {
			campoCadastroNome.focus();
		}
	}

	botaoLogin.addEventListener("click", function () {
		const aberto = !painelLogin.hidden;

		painelLogin.hidden = aberto;
		botaoLogin.setAttribute("aria-expanded", aberto ? "false" : "true");

		if (aberto) {
			esconderConfirmacaoSaida();
		} else {
			atualizarPainelConta();
		}
	});

	if (botaoAbrirLogin instanceof HTMLElement) {
		botaoAbrirLogin.addEventListener("click", function () {
			mostrarFormularioLogin();
		});
	}

	if (botaoCriarConta instanceof HTMLElement) {
		botaoCriarConta.addEventListener("click", function () {
			mostrarFormularioCadastro();
		});
	}

	botoesVoltarLogin.forEach(function (botaoVoltarLogin) {
		botaoVoltarLogin.addEventListener("click", function () {
			mostrarMenuInicial();
		});
	});

	function exibirMensagemLogin(texto, tipo) {
		atualizarMensagemLogin(texto, tipo);

		if (tipo === "sucesso") {
			return;
		}

		if (typeof exibirMensagem === "function") {
			try {
				exibirMensagem(texto, tipo);
				return;
			} catch {
				// Ignora falhas da área de mensagem global e preserva feedback no painel.
			}
		}
	}

	if (botaoSairConta instanceof HTMLElement) {
		botaoSairConta.addEventListener("click", function () {
			mostrarConfirmacaoSaida();
		});
	}

	if (botaoCancelarSaidaConta instanceof HTMLElement) {
		botaoCancelarSaidaConta.addEventListener("click", function () {
			esconderConfirmacaoSaida();
		});
	}

	if (botaoConfirmarSaidaConta instanceof HTMLElement) {
		botaoConfirmarSaidaConta.addEventListener("click", function () {
			executarLogout();
		});
	}

	if (formLoginVisual instanceof HTMLFormElement && botaoConfirmarLogin instanceof HTMLElement) {
		formLoginVisual.addEventListener("submit", async function (evento) {
			evento.preventDefault();
			const email = campoLoginEmail ? campoLoginEmail.value.trim() : "";
			const senha = campoLoginSenha ? campoLoginSenha.value : "";

			if (email === "" || senha === "") {
				exibirMensagemLogin("Preencha e-mail e senha.", "erro");
				return;
			}

			if (campoLoginEmail instanceof HTMLInputElement && !campoLoginEmail.validity.valid) {
				exibirMensagemLogin("Informe um e-mail válido.", "erro");
				return;
			}

			atualizarMensagemLogin("Validando acesso...", "info");
			botaoConfirmarLogin.textContent = "Entrando...";
			botaoConfirmarLogin.setAttribute("disabled", "disabled");

			try {
				const usuario = await autenticarUsuarioApi(email, senha);
				usuarioLogado = usuario;
				salvarUsuarioLogado(usuarioLogado);
				limparMensagemLogin();
				limparMensagemGlobal();
				mostrarContaConectada();
				atualizarAvatarConta();

				if (campoLoginSenha !== null) {
					campoLoginSenha.value = "";
				}
			} catch (erro) {
				const mensagem = erro instanceof Error
					? erro.message
					: "Não foi possível fazer login. Tente novamente.";
				exibirMensagemLogin(mensagem, "erro");
			} finally {
				botaoConfirmarLogin.textContent = textoPadraoBotaoLogin;
				botaoConfirmarLogin.removeAttribute("disabled");
			}
		});
	}

	if (formCadastroVisual instanceof HTMLFormElement && botaoConfirmarCadastro instanceof HTMLElement) {
		formCadastroVisual.addEventListener("submit", async function (evento) {
			evento.preventDefault();
			const nome = campoCadastroNome instanceof HTMLInputElement ? campoCadastroNome.value.trim() : "";
			const email = campoCadastroEmail instanceof HTMLInputElement ? campoCadastroEmail.value.trim() : "";
			const senha = campoCadastroSenha instanceof HTMLInputElement ? campoCadastroSenha.value : "";
			const confirmarSenha = campoCadastroConfirmarSenha instanceof HTMLInputElement
				? campoCadastroConfirmarSenha.value
				: "";

			if (nome.length < 3) {
				exibirMensagemLogin("Informe seu nome completo.", "erro");
				return;
			}

			if (!(campoCadastroEmail instanceof HTMLInputElement) || !campoCadastroEmail.validity.valid) {
				exibirMensagemLogin("Informe um e-mail válido.", "erro");
				return;
			}

			if (senha.length < 8) {
				exibirMensagemLogin("Crie uma senha com pelo menos 8 caracteres.", "erro");
				return;
			}

			if (senha !== confirmarSenha) {
				exibirMensagemLogin("As senhas informadas não são iguais.", "erro");
				return;
			}

			atualizarMensagemLogin("Criando sua conta...", "info");
			botaoConfirmarCadastro.textContent = "Criando conta...";
			botaoConfirmarCadastro.setAttribute("disabled", "disabled");

			try {
				const usuario = await cadastrarUsuarioApi(nome, email, senha);
				usuarioLogado = usuario;
				salvarUsuarioLogado(usuarioLogado);
				formCadastroVisual.reset();
				limparMensagemLogin();
				limparMensagemGlobal();
				mostrarContaConectada();
				atualizarAvatarConta();
			} catch (erro) {
				const mensagem = erro instanceof Error
					? erro.message
					: "Não foi possível criar a conta. Tente novamente.";
				exibirMensagemLogin(mensagem, "erro");
			} finally {
				botaoConfirmarCadastro.textContent = textoPadraoBotaoCadastro;
				botaoConfirmarCadastro.removeAttribute("disabled");
			}
		});
	}

	atualizarPainelConta();

	document.addEventListener("click", function (evento) {
		const alvo = evento.target;

		if (!(alvo instanceof Element)) {
			return;
		}

		const clicouNoPainel = painelLogin.contains(alvo);
		const clicouNoBotao = botaoLogin.contains(alvo);

		if (!clicouNoPainel && !clicouNoBotao) {
			painelLogin.hidden = true;
			botaoLogin.setAttribute("aria-expanded", "false");
			esconderConfirmacaoSaida();
		}
	});
}
