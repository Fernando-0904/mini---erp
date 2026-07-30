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

inicializarLoginVisual();
