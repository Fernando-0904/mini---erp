function inicializarBuscaGlobalController() {
	if (!elementos.formularioBuscaGlobal
		|| !elementos.campoTermoBuscaGlobal
		|| !elementos.campoFiltroTipoBuscaGlobal
		|| !elementos.campoAbrirMelhorBuscaGlobal
		|| !elementos.botaoBuscarGlobal
		|| !elementos.botaoLimparBuscaGlobal
		|| !elementos.listaAtalhosBuscaGlobal) {
		return;
	}

	let ultimoDisparoPorEnter = false;

	elementos.formularioBuscaGlobal.addEventListener("submit", async function (event) {
		event.preventDefault();
		const abrirMelhorResultado = ultimoDisparoPorEnter && elementos.campoAbrirMelhorBuscaGlobal.checked;
		ultimoDisparoPorEnter = false;
		await executarComBotaoCarregando(
			elementos.botaoBuscarGlobal,
			"Buscando...",
			async function () {
				await buscarGlobal({ abrirMelhorResultado: abrirMelhorResultado });
			});
	});

	elementos.campoTermoBuscaGlobal.addEventListener("keydown", function (event) {
		if (event.key === "Enter") {
			ultimoDisparoPorEnter = true;
		}
	});

	elementos.botaoBuscarGlobal.addEventListener("click", async function () {
		ultimoDisparoPorEnter = false;
		await executarComBotaoCarregando(
			elementos.botaoBuscarGlobal,
			"Buscando...",
			async function () {
				await buscarGlobal({ abrirMelhorResultado: false });
			});
	});

	elementos.botaoLimparBuscaGlobal.addEventListener("click", function () {
		limparBuscaGlobal();
	});

	elementos.campoFiltroTipoBuscaGlobal.addEventListener("change", async function () {
		if (elementos.campoTermoBuscaGlobal.value.trim() === "") {
			return;
		}

		await buscarGlobal({ abrirMelhorResultado: false, silencioso: true });
	});
}

async function buscarGlobal(opcoes) {
	const configuracao = opcoes || { abrirMelhorResultado: false, silencioso: false };
	const termoOriginal = elementos.campoTermoBuscaGlobal.value.trim();
	const termo = normalizarTextoBusca(termoOriginal);
	const filtroTipo = elementos.campoFiltroTipoBuscaGlobal.value;

	if (termo === "") {
		exibirMensagem("Informe um termo para realizar a busca global.", "erro");
		elementos.campoTermoBuscaGlobal.focus();
		return;
	}

	try {
		const resultados = await montarResultadosBuscaGlobal(termo);
		const resultadosFiltrados = aplicarFiltroPorTipo(resultados, filtroTipo);

		atualizarResumoBuscaGlobal(resultadosFiltrados);
		atualizarTabelaProdutosBuscaGlobal(resultadosFiltrados.produtos);
		atualizarTabelaCategoriasBuscaGlobal(resultadosFiltrados.categorias);
		atualizarTabelaFornecedoresBuscaGlobal(resultadosFiltrados.fornecedores);
		atualizarAtalhosBuscaGlobal(resultados, termoOriginal);

		if (configuracao.abrirMelhorResultado) {
			const melhorResultado = obterMelhorResultado(resultadosFiltrados);

			if (melhorResultado !== null) {
				window.location.href = melhorResultado.acaoHref;
				return;
			}
		}

		if (!configuracao.silencioso) {
			exibirMensagem("Busca global concluída com sucesso.", "sucesso");
		}
	} catch (erro) {
		exibirMensagem(erro.message, "erro");
	}
}

function limparBuscaGlobal() {
	elementos.formularioBuscaGlobal.reset();
	elementos.campoFiltroTipoBuscaGlobal.value = "todos";
	elementos.campoAbrirMelhorBuscaGlobal.checked = true;
	atualizarResumoBuscaGlobal({ produtos: [], categorias: [], fornecedores: [] });
	atualizarTabelaProdutosBuscaGlobal([]);
	atualizarTabelaCategoriasBuscaGlobal([]);
	atualizarTabelaFornecedoresBuscaGlobal([]);
	atualizarAtalhosBuscaGlobal({ produtos: [], categorias: [], fornecedores: [] }, "");
	exibirMensagem("", "");
	elementos.campoTermoBuscaGlobal.focus();
}

async function montarResultadosBuscaGlobal(termo) {
	const [produtosApi, categoriasApi, fornecedoresApi] = await Promise.all([
		listarProdutosApi(),
		listarCategoriasApi(),
		listarFornecedoresApi()
	]);

	const produtosFiltrados = produtosApi.filter(function (produto) {
		const codigoTexto = normalizarTextoBusca(String(produto.codigo || ""));
		const nome = normalizarTextoBusca(String(produto.nome || ""));
		const categoriaNome = produto.categoria && produto.categoria.nome
			? normalizarTextoBusca(String(produto.categoria.nome))
			: "sem categoria";
		const fornecedorNome = produto.fornecedor && produto.fornecedor.nome
			? normalizarTextoBusca(String(produto.fornecedor.nome))
			: "sem fornecedor";

		return codigoTexto.includes(termo)
			|| nome.includes(termo)
			|| categoriaNome.includes(termo)
			|| fornecedorNome.includes(termo);
	}).map(function (produto) {
		const categoriaNome = produto.categoria && produto.categoria.nome ? produto.categoria.nome : "Sem categoria";
		const fornecedorNome = produto.fornecedor && produto.fornecedor.nome ? produto.fornecedor.nome : "Sem fornecedor";

		return {
			...produto,
			tipo: "produtos",
			score: calcularPontuacao(termo, [String(produto.codigo || ""), String(produto.nome || ""), categoriaNome, fornecedorNome]),
			nomeOrdenacao: String(produto.nome || ""),
			acaoHref: `produtos.html?codigoEdicao=${produto.codigo}`,
			acaoLabel: "Abrir produto"
		};
	});

	const categoriasFiltradas = categoriasApi.filter(function (categoria) {
		const idTexto = normalizarTextoBusca(String(categoria.id || ""));
		const nome = normalizarTextoBusca(String(categoria.nome || ""));

		return idTexto.includes(termo) || nome.includes(termo);
	}).map(function (categoria) {
		return {
			...categoria,
			tipo: "categorias",
			score: calcularPontuacao(termo, [String(categoria.id || ""), String(categoria.nome || "")]),
			nomeOrdenacao: String(categoria.nome || ""),
			actionTermo: String(categoria.nome || ""),
			acaoHref: `estoque-baixo.html?categoria=${encodeURIComponent(categoria.id)}`,
			acaoLabel: "Ver estoque"
		};
	});

	const fornecedoresFiltrados = fornecedoresApi.filter(function (fornecedor) {
		const codigoTexto = normalizarTextoBusca(String(fornecedor.codigo || ""));
		const nome = normalizarTextoBusca(String(fornecedor.nome || ""));
		const documento = normalizarTextoBusca(String(fornecedor.documento || ""));
		const email = normalizarTextoBusca(String(fornecedor.email || ""));

		return codigoTexto.includes(termo)
			|| nome.includes(termo)
			|| documento.includes(termo)
			|| email.includes(termo);
	}).map(function (fornecedor) {
		return {
			...fornecedor,
			tipo: "fornecedores",
			score: calcularPontuacao(termo, [String(fornecedor.codigo || ""), String(fornecedor.nome || ""), String(fornecedor.documento || ""), String(fornecedor.email || "")]),
			nomeOrdenacao: String(fornecedor.nome || ""),
			acaoHref: `fornecedores.html`,
			acaoLabel: "Abrir fornecedores"
		};
	});

	return {
		produtos: ordenarPorRelevancia(produtosFiltrados),
		categorias: ordenarPorRelevancia(categoriasFiltradas),
		fornecedores: ordenarPorRelevancia(fornecedoresFiltrados)
	};
}

function aplicarFiltroPorTipo(resultados, filtroTipo) {
	if (filtroTipo === "produtos") {
		return { produtos: resultados.produtos, categorias: [], fornecedores: [] };
	}

	if (filtroTipo === "categorias") {
		return { produtos: [], categorias: resultados.categorias, fornecedores: [] };
	}

	if (filtroTipo === "fornecedores") {
		return { produtos: [], categorias: [], fornecedores: resultados.fornecedores };
	}

	return resultados;
}

function atualizarAtalhosBuscaGlobal(resultados, termo) {
	elementos.listaAtalhosBuscaGlobal.innerHTML = "";

	const atalhos = [];

	if (resultados.produtos.length > 0) {
		const produto = resultados.produtos[0];
		atalhos.push({
			texto: `Melhor produto: ${produto.codigo} - ${produto.nome}`,
			href: produto.acaoHref,
			label: "Abrir"
		});
	}

	if (resultados.categorias.length > 0) {
		const categoria = resultados.categorias[0];
		atalhos.push({
			texto: `Categoria mais relevante: ${categoria.nome}`,
			href: categoria.acaoHref,
			label: "Ver estoque"
		});
	}

	if (resultados.fornecedores.length > 0) {
		const fornecedor = resultados.fornecedores[0];
		atalhos.push({
			texto: `Fornecedor mais relevante: ${fornecedor.nome}`,
			href: `produtos.html?busca=${encodeURIComponent(termo)}`,
			label: "Buscar produtos"
		});
	}

	if (atalhos.length === 0) {
		const itemVazio = document.createElement("li");
		itemVazio.textContent = "Nenhum atalho disponível para este termo.";
		elementos.listaAtalhosBuscaGlobal.appendChild(itemVazio);
		return;
	}

	for (const atalho of atalhos) {
		const item = document.createElement("li");
		const texto = document.createElement("span");
		const link = document.createElement("a");

		texto.textContent = atalho.texto;
		link.href = atalho.href;
		link.className = "acao-rapida-link";
		link.textContent = atalho.label;

		item.appendChild(texto);
		item.appendChild(document.createTextNode(" "));
		item.appendChild(link);
		elementos.listaAtalhosBuscaGlobal.appendChild(item);
	}
}

function obterMelhorResultado(resultados) {
	const todos = [];
	todos.push(...resultados.produtos);
	todos.push(...resultados.categorias);
	todos.push(...resultados.fornecedores);

	if (todos.length === 0) {
		return null;
	}

	const ordenados = ordenarPorRelevancia(todos);
	return ordenados[0];
}

function ordenarPorRelevancia(itens) {
	return itens.slice().sort(function (a, b) {
		if (b.score !== a.score) {
			return b.score - a.score;
		}

		return String(a.nomeOrdenacao || "").localeCompare(String(b.nomeOrdenacao || ""), "pt-BR", { sensitivity: "base" });
	});
}

function calcularPontuacao(termo, campos) {
	const termoLimpo = normalizarTextoBusca(termo);
	if (termoLimpo === "") {
		return 0;
	}

	const tokens = termoLimpo.split(/\s+/).filter(function (token) {
		return token !== "";
	});

	let pontuacao = 0;

	for (const campoBruto of campos) {
		const campo = normalizarTextoBusca(String(campoBruto || ""));

		if (campo === termoLimpo) {
			pontuacao += 120;
			continue;
		}

		if (campo.startsWith(termoLimpo)) {
			pontuacao += 70;
		}

		if (campo.includes(termoLimpo)) {
			pontuacao += 35;
		}

		for (const token of tokens) {
			if (campo === token) {
				pontuacao += 40;
			} else if (campo.startsWith(token)) {
				pontuacao += 18;
			} else if (campo.includes(token)) {
				pontuacao += 10;
			}
		}
	}

	return pontuacao;
}

function normalizarTextoBusca(texto) {
	return String(texto || "")
		.normalize("NFD")
		.replace(/[\u0300-\u036f]/g, "")
		.toLowerCase()
		.trim();
}

function atualizarResumoBuscaGlobal(resultados) {
	const totalProdutos = resultados.produtos.length;
	const totalCategorias = resultados.categorias.length;
	const totalFornecedores = resultados.fornecedores.length;
	const totalGeral = totalProdutos + totalCategorias + totalFornecedores;

	elementos.totalProdutosGlobal.textContent = String(totalProdutos);
	elementos.totalCategoriasGlobal.textContent = String(totalCategorias);
	elementos.totalFornecedoresGlobal.textContent = String(totalFornecedores);
	elementos.totalResultadosGlobal.textContent = String(totalGeral);
}

function atualizarTabelaProdutosBuscaGlobal(produtos) {
	elementos.tabelaResultadoProdutos.innerHTML = "";

	if (produtos.length === 0) {
		elementos.tabelaResultadoProdutos.appendChild(
			criarLinhaVaziaBuscaGlobal("Nenhum produto encontrado. Tente outro termo ou altere o filtro.", 6)
		);
		return;
	}

	for (const produto of produtos) {
		const linha = document.createElement("tr");
		const categoria = produto.categoria && produto.categoria.nome ? produto.categoria.nome : "Sem categoria";
		const fornecedor = produto.fornecedor && produto.fornecedor.nome ? produto.fornecedor.nome : "Sem fornecedor";

		linha.appendChild(criarCelulaBuscaGlobal(produto.codigo));
		linha.appendChild(criarCelulaBuscaGlobal(produto.nome));
		linha.appendChild(criarCelulaBuscaGlobal(categoria));
		linha.appendChild(criarCelulaBuscaGlobal(fornecedor));
		linha.appendChild(criarCelulaBuscaGlobal(formatarMoeda(produto.precoUnitario || 0)));
		linha.appendChild(criarCelulaAcaoBuscaGlobal(produto.acaoHref, produto.acaoLabel));

		elementos.tabelaResultadoProdutos.appendChild(linha);
	}
}

function atualizarTabelaCategoriasBuscaGlobal(categorias) {
	elementos.tabelaResultadoCategorias.innerHTML = "";

	if (categorias.length === 0) {
		elementos.tabelaResultadoCategorias.appendChild(
			criarLinhaVaziaBuscaGlobal("Nenhuma categoria encontrada para este termo.", 3)
		);
		return;
	}

	for (const categoria of categorias) {
		const linha = document.createElement("tr");

		linha.appendChild(criarCelulaBuscaGlobal(categoria.id));
		linha.appendChild(criarCelulaBuscaGlobal(categoria.nome));
		linha.appendChild(criarCelulaAcaoBuscaGlobal(categoria.acaoHref, categoria.acaoLabel));

		elementos.tabelaResultadoCategorias.appendChild(linha);
	}
}

function atualizarTabelaFornecedoresBuscaGlobal(fornecedores) {
	elementos.tabelaResultadoFornecedores.innerHTML = "";

	if (fornecedores.length === 0) {
		elementos.tabelaResultadoFornecedores.appendChild(
			criarLinhaVaziaBuscaGlobal("Nenhum fornecedor encontrado para este termo.", 6)
		);
		return;
	}

	for (const fornecedor of fornecedores) {
		const linha = document.createElement("tr");

		linha.appendChild(criarCelulaBuscaGlobal(fornecedor.codigo));
		linha.appendChild(criarCelulaBuscaGlobal(fornecedor.nome));
		linha.appendChild(criarCelulaBuscaGlobal(fornecedor.documento));
		linha.appendChild(criarCelulaBuscaGlobal(fornecedor.email || "-"));
		linha.appendChild(criarCelulaBuscaGlobal(fornecedor.ativo ? "Ativo" : "Inativo"));
		linha.appendChild(criarCelulaAcaoBuscaGlobal(fornecedor.acaoHref, fornecedor.acaoLabel));

		elementos.tabelaResultadoFornecedores.appendChild(linha);
	}
}

function criarLinhaVaziaBuscaGlobal(texto, colSpan) {
	const linha = document.createElement("tr");
	const celula = document.createElement("td");

	celula.colSpan = colSpan;
	celula.textContent = texto;
	linha.appendChild(celula);

	return linha;
}

function criarCelulaBuscaGlobal(texto) {
	const celula = document.createElement("td");
	celula.textContent = texto;
	return celula;
}

function criarCelulaAcaoBuscaGlobal(href, textoLink) {
	const celula = document.createElement("td");
	const link = document.createElement("a");

	link.href = href;
	link.className = "acao-rapida-link";
	link.textContent = textoLink;

	celula.appendChild(link);
	return celula;
}
