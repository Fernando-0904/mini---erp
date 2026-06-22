const produtos = [];
const formulario = document.getElementById("formProduto");
const campoCodigo = document.getElementById("codigo");
const campoNome = document.getElementById("nome");
const campoPreco = document.getElementById("preco");
const campoQuantidade = document.getElementById("quantidade");
const tabelaProdutos = document.getElementById("tabelaProdutos");
const quantidadeProdutos = document.getElementById("quantidadeProdutos");
const itensEstoque = document.getElementById("itensEstoque");
const valorTotalEstoque = document.getElementById("valorTotalEstoque");
const mensagem = document.getElementById("mensagem");
const campoCodigoBusca = document.getElementById("codigoBusca");
const botaoBuscar = document.getElementById("botaoBuscar");
const botaoLimparBusca = document.getElementById("botaoLimparBusca");

carregarProdutos();

formulario.addEventListener("submit", function (event) {
    event.preventDefault();

    const codigoTexto = campoCodigo.value.trim();
    const nome = campoNome.value.trim();
    const precoTexto = campoPreco.value.trim();
    const quantidadeTexto = campoQuantidade.value.trim();

    const codigo = Number(codigoTexto);
    const preco = Number(precoTexto);
    const quantidade = Number(quantidadeTexto);

    if (!validarProduto(codigoTexto, nome, precoTexto, quantidadeTexto, codigo, preco, quantidade)) {
        return;
    }

    const produto = {
        codigo: codigo,
        nome: nome,
        preco: preco,
        quantidade: quantidade
    };

    produtos.push(produto);
    salvarProdutos();
    atualizarTabela(produtos);
    atualizarIndicadores();
    exibirMensagem("Produto cadastrado com sucesso.", "sucesso");
    formulario.reset();
    campoCodigo.focus();
});

function validarProduto(codigoTexto, nome, precoTexto, quantidadeTexto, codigo, preco, quantidade) {
    if (codigoTexto === "" || codigo <= 0) {
        exibirMensagem("Informe um código válido.", "erro");
        campoCodigo.focus();
        return false;
    }

    if (nome === "") {
        exibirMensagem("Informe o nome do produto.", "erro");
        campoNome.focus();
        return false;
    }

    if (precoTexto === "" || preco <= 0) {
        exibirMensagem("Informe um preço válido.", "erro");
        campoPreco.focus();
        return false;
    }

    if (quantidadeTexto === "") {
        exibirMensagem("Informe a quantidade do produto.", "erro");
        campoQuantidade.focus();
        return false;
    }

    if (quantidade < 0) {
        exibirMensagem("A quantidade não pode ser negativa.", "erro");
        campoQuantidade.focus();
        return false;
    }

    for (const produto of produtos) {
        if (produto.codigo === codigo) {
            exibirMensagem("Já existe um produto com esse código.", "erro");
            campoCodigo.focus();
            return false;
        }
    }

    return true;
}

botaoBuscar.addEventListener("click", function () {
    buscarProduto();
});

botaoLimparBusca.addEventListener("click", function () {
    limparBusca();
});

function limparBusca() {
    campoCodigoBusca.value = "";
    atualizarTabela(produtos);
    exibirMensagem("", "");
    campoCodigoBusca.focus();
}

function exibirMensagem(texto, tipo) {
    mensagem.textContent = texto;
    mensagem.className = "";

    if (tipo === "sucesso") {
        mensagem.className = "mensagem-sucesso";
    }

    if (tipo === "erro") {
        mensagem.className = "mensagem-erro";
    }
}

function formatarMoeda(valor) {
    return valor.toLocaleString("pt-BR", {
        style: "currency",
        currency: "BRL"
    });
}

function atualizarTabela(listaProdutos) {
    tabelaProdutos.innerHTML = "";

    if (listaProdutos.length === 0) {
        tabelaProdutos.innerHTML = "<tr><td colspan=\"7\">Nenhum produto cadastrado.</td></tr>";
        return;
    }

    for (const produto of listaProdutos) {
        const linha = document.createElement("tr");
        const valorTotal = produto.preco * produto.quantidade;
        const situacao = obterSituacaoEstoque(produto.quantidade);
        const classeSituacao = obterClasseSituacaoEstoque(produto.quantidade);

        linha.innerHTML =
            "<td>" + produto.codigo + "</td>" +
            "<td>" + produto.nome + "</td>" +
            "<td>" + formatarMoeda(produto.preco) + "</td>" +
            "<td>" + produto.quantidade + "</td>" +
            "<td>" + formatarMoeda(valorTotal) + "</td>" +
            "<td><span class=\"" + classeSituacao + "\">" + situacao + "</span></td>" +
            "<td><button type=\"button\" onclick=\"removerProduto(" + produto.codigo + ")\">Remover</button></td>";

        tabelaProdutos.appendChild(linha);
    }
}

function atualizarIndicadores() {
    let totalItens = 0;
    let valorTotal = 0;

    for (const produto of produtos) {
        totalItens += produto.quantidade;
        valorTotal += produto.preco * produto.quantidade;
    }

    quantidadeProdutos.textContent = produtos.length;
    itensEstoque.textContent = totalItens;
    valorTotalEstoque.textContent = formatarMoeda(valorTotal);
}

function obterSituacaoEstoque(quantidade) {
    if (quantidade === 0) {
        return "Sem estoque";
    }

    if (quantidade <= 5) {
        return "Estoque baixo";
    }

    return "Estoque disponível";
}

function obterClasseSituacaoEstoque(quantidade) {
    if (quantidade === 0) {
        return "status-sem-estoque";
    }

    if (quantidade <= 5) {
        return "status-estoque-baixo";
    }

    return "status-disponivel";
}

function buscarProduto() {
    const codigoBuscadoTexto = campoCodigoBusca.value.trim();
    const codigoBuscado = Number(codigoBuscadoTexto);

    if (codigoBuscadoTexto === "" || codigoBuscado <= 0) {
        exibirMensagem("Informe um código válido para buscar.", "erro");
        campoCodigoBusca.focus();
        return;
    }

    for (const produto of produtos) {
        if (produto.codigo === codigoBuscado) {
            atualizarTabela([produto]);
            exibirMensagem("Produto encontrado.", "sucesso");
            return;
        }
    }

    exibirMensagem("Produto não encontrado.", "erro");
}

function removerProduto(codigo) {
    const confirmarRemocao = confirm("Deseja realmente remover este produto?");

    if (!confirmarRemocao) {
        return;
    }

    const indiceProduto = produtos.findIndex(function (produto) {
        return produto.codigo === codigo;
    });

    if (indiceProduto === -1) {
        exibirMensagem("Produto não encontrado para remoção.", "erro");
        return;
    }

    produtos.splice(indiceProduto, 1);
    salvarProdutos();
    atualizarTabela(produtos);
    atualizarIndicadores();
    exibirMensagem("Produto removido com sucesso.", "sucesso");
}

function salvarProdutos() {
    localStorage.setItem("produtos", JSON.stringify(produtos));
}

function carregarProdutos() {
    const dadosSalvos = localStorage.getItem("produtos");

    if (dadosSalvos === null) {
        atualizarTabela(produtos);
        atualizarIndicadores();
        return;
    }

    const produtosSalvos = JSON.parse(dadosSalvos);

    for (const produto of produtosSalvos) {
        produtos.push(produto);
    }

    atualizarTabela(produtos);
    atualizarIndicadores();
}