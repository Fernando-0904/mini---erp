function inicializarBuscaGlobalController() {
	if (!elementos.formularioBuscaGlobal
		|| !elementos.campoTermoBuscaGlobal
		|| !elementos.botaoBuscarGlobal
		|| !elementos.botaoLimparBuscaGlobal) {
		return;
	}

	elementos.formularioBuscaGlobal.addEventListener("submit", function (event) {
		event.preventDefault();
		buscarGlobal();
	});

	elementos.botaoBuscarGlobal.addEventListener("click", function () {
		buscarGlobal();
	});

	elementos.botaoLimparBuscaGlobal.addEventListener("click", function () {
		limparBuscaGlobal();
	});
}

async function buscarGlobal() {
	const termo = elementos.campoTermoBuscaGlobal.value.trim().toLowerCase();

	if (termo === "") {
		exibirMensagem("Informe um termo para realizar a busca global.", "erro");
		elementos.campoTermoBuscaGlobal.focus();
		return;
	}

	try {
		const resultados = await montarResultadosBuscaGlobal(termo);

		atualizarResumoBuscaGlobal(resultados);
		atualizarTabelaProdutosBuscaGlobal(resultados.produtos);
		atualizarTabelaCategoriasBuscaGlobal(resultados.categorias);
		atualizarTabelaFornecedoresBuscaGlobal(resultados.fornecedores);

		exibirMensagem("Busca global concluida com sucesso.", "sucesso");
	} catch (erro) {
		exibirMensagem(erro.message, "erro");
	}
}

function limparBuscaGlobal() {
	elementos.formularioBuscaGlobal.reset();
	atualizarResumoBuscaGlobal({ produtos: [], categorias: [], fornecedores: [] });
	atualizarTabelaProdutosBuscaGlobal([]);
	atualizarTabelaCategoriasBuscaGlobal([]);
	atualizarTabelaFornecedoresBuscaGlobal([]);
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
		const codigoTexto = String(produto.codigo || "").toLowerCase();
		const nome = String(produto.nome || "").toLowerCase();
		const categoriaNome = produto.categoria && produto.categoria.nome
			? String(produto.categoria.nome).toLowerCase()
			: "sem categoria";
		const fornecedorNome = produto.fornecedor && produto.fornecedor.nome
			? String(produto.fornecedor.nome).toLowerCase()
			: "sem fornecedor";

		return codigoTexto.includes(termo)
			|| nome.includes(termo)
			|| categoriaNome.includes(termo)
			|| fornecedorNome.includes(termo);
	});

	const categoriasFiltradas = categoriasApi.filter(function (categoria) {
		const idTexto = String(categoria.id || "").toLowerCase();
		const nome = String(categoria.nome || "").toLowerCase();

		return idTexto.includes(termo) || nome.includes(termo);
	});

	const fornecedoresFiltrados = fornecedoresApi.filter(function (fornecedor) {
		const codigoTexto = String(fornecedor.codigo || "").toLowerCase();
		const nome = String(fornecedor.nome || "").toLowerCase();
		const documento = String(fornecedor.documento || "").toLowerCase();
		const email = String(fornecedor.email || "").toLowerCase();

		return codigoTexto.includes(termo)
			|| nome.includes(termo)
			|| documento.includes(termo)
			|| email.includes(termo);
	});

	return {
		produtos: produtosFiltrados,
		categorias: categoriasFiltradas,
		fornecedores: fornecedoresFiltrados
	};
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
			criarLinhaVaziaBuscaGlobal("Nenhum produto encontrado.", 5)
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

		elementos.tabelaResultadoProdutos.appendChild(linha);
	}
}

function atualizarTabelaCategoriasBuscaGlobal(categorias) {
	elementos.tabelaResultadoCategorias.innerHTML = "";

	if (categorias.length === 0) {
		elementos.tabelaResultadoCategorias.appendChild(
			criarLinhaVaziaBuscaGlobal("Nenhuma categoria encontrada.", 2)
		);
		return;
	}

	for (const categoria of categorias) {
		const linha = document.createElement("tr");

		linha.appendChild(criarCelulaBuscaGlobal(categoria.id));
		linha.appendChild(criarCelulaBuscaGlobal(categoria.nome));

		elementos.tabelaResultadoCategorias.appendChild(linha);
	}
}

function atualizarTabelaFornecedoresBuscaGlobal(fornecedores) {
	elementos.tabelaResultadoFornecedores.innerHTML = "";

	if (fornecedores.length === 0) {
		elementos.tabelaResultadoFornecedores.appendChild(
			criarLinhaVaziaBuscaGlobal("Nenhum fornecedor encontrado.", 5)
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
