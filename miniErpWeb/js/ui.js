function exibirMensagem(texto, tipo) {
    elementos.mensagem.textContent = texto;
    elementos.mensagem.className = "";

    if (tipo === "sucesso") {
        elementos.mensagem.className = "mensagem-sucesso";
    }

    if (tipo === "erro") {
        elementos.mensagem.className = "mensagem-erro";
    }
}

function formatarMoeda(valor) {
    return valor.toLocaleString("pt-BR", {
        style: "currency",
        currency: "BRL"
    });
}

function atualizarTabela(listaProdutos, aoEditarProduto, aoRemoverProduto) {
    elementos.tabelaProdutos.innerHTML = "";

    if (listaProdutos.length === 0) {
        const linhaVazia = document.createElement("tr");
        const celulaVazia = document.createElement("td");

        celulaVazia.colSpan = 7;
        celulaVazia.textContent = "Nenhum produto cadastrado.";
        linhaVazia.appendChild(celulaVazia);
        elementos.tabelaProdutos.appendChild(linhaVazia);
        return;
    }

    for (const produto of listaProdutos) {
        const linha = document.createElement("tr");
        const valorTotal = produto.preco * produto.quantidade;
        const situacao = obterSituacaoEstoque(produto.quantidade, produto.estoqueMinimo);
        const classeSituacao = obterClasseSituacaoEstoque(produto.quantidade, produto.estoqueMinimo);

        linha.appendChild(criarCelula(produto.codigo));
        linha.appendChild(criarCelula(produto.nome));
        linha.appendChild(criarCelula(formatarMoeda(produto.preco)));
        linha.appendChild(criarCelula(produto.quantidade));
        linha.appendChild(criarCelula(formatarMoeda(valorTotal)));
        linha.appendChild(criarCelulaSituacao(situacao, classeSituacao));
        linha.appendChild(criarCelulaAcoes(produto.codigo, aoEditarProduto, aoRemoverProduto));

        elementos.tabelaProdutos.appendChild(linha);
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

function criarCelulaAcoes(codigo, aoEditarProduto, aoRemoverProduto) {
    const celula = document.createElement("td");
    const botaoEditar = document.createElement("button");
    const botaoRemover = document.createElement("button");

    botaoEditar.type = "button";
    botaoEditar.textContent = "Editar";
    botaoEditar.addEventListener("click", function () {
        aoEditarProduto(codigo);
    });

    botaoRemover.type = "button";
    botaoRemover.textContent = "Remover";
    botaoRemover.addEventListener("click", function () {
        aoRemoverProduto(codigo);
    });

    celula.appendChild(botaoEditar);
    celula.appendChild(document.createTextNode(" "));
    celula.appendChild(botaoRemover);

    return celula;
}

function atualizarIndicadores(produtos) {
    let totalItens = 0;
    let valorTotal = 0;

    for (const produto of produtos) {
        totalItens += produto.quantidade;
        valorTotal += produto.preco * produto.quantidade;
    }

    elementos.quantidadeProdutos.textContent = produtos.length;
    elementos.itensEstoque.textContent = totalItens;
    elementos.valorTotalEstoque.textContent = formatarMoeda(valorTotal);
}

function atualizarTabelaMovimentacoes(movimentacoes) {
    elementos.tabelaMovimentacoes.innerHTML = "";

    if (movimentacoes.length === 0) {
        const linhaVazia = document.createElement("tr");
        const celulaVazia = document.createElement("td");

        celulaVazia.colSpan = 6;
        celulaVazia.textContent = "Nenhuma movimentação registrada.";
        linhaVazia.appendChild(celulaVazia);
        elementos.tabelaMovimentacoes.appendChild(linhaVazia);
        return;
    }

    for (const movimentacao of movimentacoes) {
        const linha = document.createElement("tr");

        linha.appendChild(criarCelula(formatarDataMovimentacao(movimentacao.dataMovimentacaoUtc)));
        linha.appendChild(criarCelula(movimentacao.produtoCodigo));
        linha.appendChild(criarCelula(formatarTipoMovimentacao(movimentacao.tipo)));
        linha.appendChild(criarCelula(movimentacao.quantidade));
        linha.appendChild(criarCelula(movimentacao.saldoAnterior));
        linha.appendChild(criarCelula(movimentacao.saldoNovo));

        elementos.tabelaMovimentacoes.appendChild(linha);
    }
}

function formatarDataMovimentacao(dataMovimentacaoUtc) {
    const data = new Date(dataMovimentacaoUtc);

    if (Number.isNaN(data.getTime())) {
        return "Data indisponível";
    }

    return data.toLocaleString("pt-BR");
}

function formatarTipoMovimentacao(tipo) {
    if (tipo === 1 || tipo === "Entrada") {
        return "Entrada";
    }

    if (tipo === 2 || tipo === "Saida") {
        return "Saída";
    }

    return "Tipo desconhecido";
}

function obterSituacaoEstoque(quantidade, estoqueMinimo) {
    if (quantidade === 0) {
        return "Sem estoque";
    }

    if (typeof estoqueMinimo === "number" && estoqueMinimo > 0 && quantidade <= estoqueMinimo) {
        return "Estoque baixo";
    }

    return "Estoque disponível";
}

function obterClasseSituacaoEstoque(quantidade, estoqueMinimo) {
    if (quantidade === 0) {
        return "status-sem-estoque";
    }

    if (typeof estoqueMinimo === "number" && estoqueMinimo > 0 && quantidade <= estoqueMinimo) {
        return "status-estoque-baixo";
    }

    return "status-disponivel";
}
