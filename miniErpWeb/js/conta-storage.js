const CHAVE_USUARIO_LOGADO = "miniErpUsuarioLogadoSqlite";

try {
	localStorage.removeItem("miniErpUsuarioLogado");
} catch {
	// A indisponibilidade do armazenamento não impede o uso da tela de acesso.
}

function carregarUsuarioLogado() {
	try {
		const valor = localStorage.getItem(CHAVE_USUARIO_LOGADO);

		if (typeof valor !== "string" || valor.trim() === "") {
			return null;
		}

		const usuario = JSON.parse(valor);
		return usuario !== null && typeof usuario === "object" ? usuario : null;
	} catch {
		return null;
	}
}

function salvarUsuarioLogado(usuario) {
	try {
		localStorage.setItem(CHAVE_USUARIO_LOGADO, JSON.stringify(usuario));
	} catch {
		// A sessão continua ativa nesta página mesmo se o armazenamento não estiver disponível.
	}
}

function limparUsuarioLogado() {
	try {
		localStorage.removeItem(CHAVE_USUARIO_LOGADO);
	} catch {
		// A sessão em memória já foi encerrada pelo controller.
	}
}
