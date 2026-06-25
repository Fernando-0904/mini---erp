const produtos = [];
let codigoProdutoEmEdicao = null;
const formulario = document.getElementById("formProduto");
const campoCodigo = document.getElementById("codigo");
const campoNome = document.getElementById("nome");
const campoPreco = document.getElementById("preco");
const campoQuantidade = document.getElementById("quantidade");
const botaoSalvarProduto = document.getElementById("botaoSalvarProduto");
const botaoLimparFormulario = formulario.querySelector("button[type='reset']");
const tabelaProdutos = document.getElementById("tabelaProdutos");
const quantidadeProdutos = document.getElementById("quantidadeProdutos");
const itensEstoque = document.getElementById("itensEstoque");
const valorTotalEstoque = document.getElementById("valorTotalEstoque");
const mensagem = document.getElementById("mensagem");
const formularioBuscaProduto = document.getElementById("formBuscaProduto");
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

    if (codigoProdutoEmEdicao === null) {
        const produto = {
            codigo: codigo,
            nome: nome,
            preco: preco,
            quantidade: quantidade
        };

        produtos.push(produto);
        exibirMensagem("Produto cadastrado com sucesso.", "sucesso");
    } else {
        const produtoParaEditar = produtos.find(function (produto) {
            return produto.codigo === codigoProdutoEmEdicao;
        });

        if (produtoParaEditar === undefined) {
            exibirMensagem("Produto não encontrado para edição.", "erro");
            return;
        }

        produtoParaEditar.nome = nome;
        produtoParaEditar.preco = preco;
        produtoParaEditar.quantidade = quantidade;

        limparModoEdicao();
        exibirMensagem("Produto editado com sucesso.", "sucesso");
    }

    salvarProdutos();
    atualizarTabela(produtos);
    atualizarIndicadores();
    formulario.reset();
    campoCodigo.focus();
});

botaoLimparFormulario.addEventListener("click", function () {
    limparModoEdicao();
    exibirMensagem("", "");
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

    if (codigoProdutoEmEdicao === null) {
        for (const produto of produtos) {
            if (produto.codigo === codigo) {
                exibirMensagem("Já existe um produto com esse código.", "erro");
                campoCodigo.focus();
                return false;
            }
        }
    }

    return true;
}

botaoBuscar.addEventListener("click", function () {
    buscarProduto();
});

formularioBuscaProduto.addEventListener("submit", function (event) {
    event.preventDefault();
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
        const linhaVazia = document.createElement("tr");
        const celulaVazia = document.createElement("td");

        celulaVazia.colSpan = 7;
        celulaVazia.textContent = "Nenhum produto cadastrado.";
        linhaVazia.appendChild(celulaVazia);
        tabelaProdutos.appendChild(linhaVazia);
        return;
    }

    for (const produto of listaProdutos) {
        const linha = document.createElement("tr");
        const valorTotal = produto.preco * produto.quantidade;
        const situacao = obterSituacaoEstoque(produto.quantidade);
        const classeSituacao = obterClasseSituacaoEstoque(produto.quantidade);

        linha.appendChild(criarCelula(produto.codigo));
        linha.appendChild(criarCelula(produto.nome));
        linha.appendChild(criarCelula(formatarMoeda(produto.preco)));
        linha.appendChild(criarCelula(produto.quantidade));
        linha.appendChild(criarCelula(formatarMoeda(valorTotal)));
        linha.appendChild(criarCelulaSituacao(situacao, classeSituacao));
        linha.appendChild(criarCelulaAcoes(produto.codigo));

        tabelaProdutos.appendChild(linha);
    }
}

function criarCelula(texto) {
    const celula = document.createElement("td");
    celula.textContent = texto;
    return celula;
}

function criarCelulaSituacao(situacao, classeSituacao) {
    const celula = document.createElement("td");
    const textoSituacao = document.createElement("span");

    textoSituacao.className = classeSituacao;
    textoSituacao.textContent = situacao;
    celula.appendChild(textoSituacao);

    return celula;
}

function criarCelulaAcoes(codigo) {
    const celula = document.createElement("td");
    const botaoEditar = document.createElement("button");
    const botaoRemover = document.createElement("button");

    botaoEditar.type = "button";
    botaoEditar.textContent = "Editar";
    botaoEditar.addEventListener("click", function () {
        editarProduto(codigo);
    });

    botaoRemover.type = "button";
    botaoRemover.textContent = "Remover";
    botaoRemover.addEventListener("click", function () {
        removerProduto(codigo);
    });

    celula.appendChild(botaoEditar);
    celula.appendChild(document.createTextNode(" "));
    celula.appendChild(botaoRemover);

    return celula;
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
    const termoBusca = campoCodigoBusca.value.trim().toLowerCase();

    if (termoBusca === "") {
        exibirMensagem("Informe um código ou nome para buscar.", "erro");
        campoCodigoBusca.focus();
        return;
    }

    const codigoBuscado = Number(termoBusca);
    const buscaPorCodigo = !Number.isNaN(codigoBuscado) && codigoBuscado > 0;

    const resultados = produtos.filter(function (produto) {
        const nomeProduto = produto.nome.toLowerCase();

        if (buscaPorCodigo && produto.codigo === codigoBuscado) {
            return true;
        }

        return nomeProduto.includes(termoBusca);
    });

    if (resultados.length === 0) {
        atualizarTabela([]);
        exibirMensagem("Nenhum produto encontrado.", "erro");
        return;
    }

    atualizarTabela(resultados);
    exibirMensagem("Busca concluída: " + resultados.length + " resultado(s).", "sucesso");
}

function editarProduto(codigo) {
    const produtoEncontrado = produtos.find(function (produto) {
        return produto.codigo === codigo;
    });

    if (produtoEncontrado === undefined) {
        exibirMensagem("Produto não encontrado para edição.", "erro");
        return;
    }

    codigoProdutoEmEdicao = codigo;

    campoCodigo.value = produtoEncontrado.codigo;
    campoNome.value = produtoEncontrado.nome;
    campoPreco.value = produtoEncontrado.preco;
    campoQuantidade.value = produtoEncontrado.quantidade;

    campoCodigo.disabled = true;
    botaoSalvarProduto.textContent = "Salvar alteração";
    campoNome.focus();

    exibirMensagem("Edite os dados do produto e salve a alteração.", "sucesso");
}

function limparModoEdicao() {
    codigoProdutoEmEdicao = null;
    campoCodigo.disabled = false;
    botaoSalvarProduto.textContent = "Cadastrar produto";
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
