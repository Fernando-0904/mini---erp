const elementos = {
    formulario: document.getElementById("formProduto"),
    campoCodigo: document.getElementById("codigo"),
    campoNome: document.getElementById("nome"),
    campoPreco: document.getElementById("preco"),
    campoQuantidade: document.getElementById("quantidade"),
    botaoSalvarProduto: document.getElementById("botaoSalvarProduto"),
    tabelaProdutos: document.getElementById("tabelaProdutos"),
    quantidadeProdutos: document.getElementById("quantidadeProdutos"),
    itensEstoque: document.getElementById("itensEstoque"),
    valorTotalEstoque: document.getElementById("valorTotalEstoque"),
    mensagem: document.getElementById("mensagem"),
    formularioBuscaProduto: document.getElementById("formBuscaProduto"),
    campoCodigoBusca: document.getElementById("codigoBusca"),
    botaoBuscar: document.getElementById("botaoBuscar"),
    botaoLimparBusca: document.getElementById("botaoLimparBusca")
};

elementos.botaoLimparFormulario = elementos.formulario.querySelector("button[type='reset']");
