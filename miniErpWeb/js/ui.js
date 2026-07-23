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

        celulaVazia.colSpan = 9;
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
        linha.appendChild(criarCelula(produto.categoriaNome));
        linha.appendChild(criarCelula(produto.fornecedorNome));
        linha.appendChild(criarCelula(formatarMoeda(produto.preco)));
        linha.appendChild(criarCelula(produto.quantidade));
        linha.appendChild(criarCelula(formatarMoeda(valorTotal)));
        linha.appendChild(criarCelulaSituacao(situacao, classeSituacao));
        linha.appendChild(criarCelulaAcoes(produto.codigo, aoEditarProduto, aoRemoverProduto));

        elementos.tabelaProdutos.appendChild(linha);
    }
}

function atualizarSelectCategorias(categorias, categoriaSelecionadaId) {
    elementos.campoCategoriaProduto.innerHTML = "";

    const opcaoPadrao = document.createElement("option");
    opcaoPadrao.value = "";
    opcaoPadrao.textContent = "Selecione uma categoria";
    elementos.campoCategoriaProduto.appendChild(opcaoPadrao);

    for (const categoria of categorias) {
        const opcao = document.createElement("option");

        opcao.value = categoria.id;
        opcao.textContent = categoria.nome;
        elementos.campoCategoriaProduto.appendChild(opcao);
    }

    elementos.campoCategoriaProduto.value = categoriaSelecionadaId || "";
}

function atualizarSelectFornecedores(fornecedores, fornecedorSelecionadoId) {
    elementos.campoFornecedorProduto.innerHTML = "";

    const opcaoPadrao = document.createElement("option");
    opcaoPadrao.value = "";
    opcaoPadrao.textContent = "Sem fornecedor";
    elementos.campoFornecedorProduto.appendChild(opcaoPadrao);

    for (const fornecedor of fornecedores) {
        if (!fornecedor.ativo) {
            continue;
        }

        const opcao = document.createElement("option");

        opcao.value = fornecedor.id;
        opcao.textContent = fornecedor.nome;
        elementos.campoFornecedorProduto.appendChild(opcao);
    }

    elementos.campoFornecedorProduto.value = fornecedorSelecionadoId || "";
}

function atualizarTabelaFornecedores(fornecedores, aoEditarFornecedor, aoInativarFornecedor, aoRemoverFornecedor) {
    elementos.tabelaFornecedores.innerHTML = "";

    if (fornecedores.length === 0) {
        const linhaVazia = document.createElement("tr");
        const celulaVazia = document.createElement("td");

        celulaVazia.colSpan = 7;
        celulaVazia.textContent = "Nenhum fornecedor cadastrado.";
        linhaVazia.appendChild(celulaVazia);
        elementos.tabelaFornecedores.appendChild(linhaVazia);
        return;
    }

    for (const fornecedor of fornecedores) {
        const linha = document.createElement("tr");

        linha.appendChild(criarCelula(fornecedor.codigo));
        linha.appendChild(criarCelula(fornecedor.nome));
        linha.appendChild(criarCelula(fornecedor.documento));
        linha.appendChild(criarCelula(fornecedor.email));
        linha.appendChild(criarCelula(fornecedor.telefone));
        linha.appendChild(criarCelula(fornecedor.ativo ? "Ativo" : "Inativo"));
        linha.appendChild(criarCelulaAcoesFornecedor(fornecedor.id, fornecedor.ativo, aoEditarFornecedor, aoInativarFornecedor, aoRemoverFornecedor));

        elementos.tabelaFornecedores.appendChild(linha);
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

function criarCelulaAcoesFornecedor(id, ativo, aoEditarFornecedor, aoInativarFornecedor, aoRemoverFornecedor) {
    const celula = document.createElement("td");
    const botaoEditar = document.createElement("button");
    const botaoRemover = document.createElement("button");

    botaoEditar.type = "button";
    botaoEditar.textContent = "Editar";
    botaoEditar.addEventListener("click", function () {
        aoEditarFornecedor(id);
    });

    celula.appendChild(botaoEditar);

    if (ativo) {
        const botaoInativar = document.createElement("button");

        botaoInativar.type = "button";
        botaoInativar.textContent = "Inativar";
        botaoInativar.addEventListener("click", function () {
            aoInativarFornecedor(id);
        });

        celula.appendChild(document.createTextNode(" "));
        celula.appendChild(botaoInativar);
    }

    botaoRemover.type = "button";
    botaoRemover.textContent = "Remover";
    botaoRemover.addEventListener("click", function () {
        aoRemoverFornecedor(id);
    });

    celula.appendChild(document.createTextNode(" "));
    celula.appendChild(botaoRemover);

    return celula;
}

function atualizarSelectCategoriasEstoqueBaixo(categorias, categoriaSelecionadaId) {
    elementos.campoCategoriaEstoqueBaixo.innerHTML = "";

    const opcaoPadrao = document.createElement("option");
    opcaoPadrao.value = "";
    opcaoPadrao.textContent = "Todas as categorias";
    elementos.campoCategoriaEstoqueBaixo.appendChild(opcaoPadrao);

    for (const categoria of categorias) {
        const opcao = document.createElement("option");

        opcao.value = categoria.id;
        opcao.textContent = categoria.nome;
        elementos.campoCategoriaEstoqueBaixo.appendChild(opcao);
    }

    elementos.campoCategoriaEstoqueBaixo.value = categoriaSelecionadaId || "";
}

function atualizarTabelaEstoqueBaixo(produtos) {
    elementos.tabelaEstoqueBaixo.innerHTML = "";

    if (produtos.length === 0) {
        const linhaVazia = document.createElement("tr");
        const celulaVazia = document.createElement("td");

        celulaVazia.colSpan = 6;
        celulaVazia.textContent = "Nenhum produto com estoque baixo.";
        linhaVazia.appendChild(celulaVazia);
        elementos.tabelaEstoqueBaixo.appendChild(linhaVazia);
        return;
    }

    for (const produto of produtos) {
        const linha = document.createElement("tr");
        const situacao = obterSituacaoEstoque(produto.quantidadeEstoque, produto.estoqueMinimo);
        const classeSituacao = obterClasseSituacaoEstoque(produto.quantidadeEstoque, produto.estoqueMinimo);

        if (produto.quantidadeEstoque === 0) {
            linha.className = "linha-sem-estoque";
        }

        linha.appendChild(criarCelula(produto.codigo));
        linha.appendChild(criarCelula(produto.nome));
        linha.appendChild(criarCelula(produto.categoria ? produto.categoria.nome : "Sem categoria"));
        linha.appendChild(criarCelula(produto.quantidadeEstoque));
        linha.appendChild(criarCelula(produto.estoqueMinimo));
        linha.appendChild(criarCelulaSituacao(situacao, classeSituacao));

        elementos.tabelaEstoqueBaixo.appendChild(linha);
    }
}

function atualizarIndicadores(produtos) {
    if (elementos.quantidadeProdutos === null) {
        return;
    }

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

function atualizarAlertasPainel(alertas) {
    if (elementos.tabelaAlertasPainel === null || elementos.quantidadeAlertasCriticos === null) {
        return;
    }

    elementos.tabelaAlertasPainel.innerHTML = "";

    const totalCriticos = alertas.filter(function (alerta) {
        return alerta.situacao === "Sem estoque";
    }).length;

    elementos.quantidadeAlertasCriticos.textContent = totalCriticos;

    if (alertas.length === 0) {
        const linhaVazia = document.createElement("tr");
        const celulaVazia = document.createElement("td");

        celulaVazia.colSpan = 7;
        celulaVazia.textContent = "Nenhum alerta de estoque no momento.";
        linhaVazia.appendChild(celulaVazia);
        elementos.tabelaAlertasPainel.appendChild(linhaVazia);
        return;
    }

    for (const alerta of alertas) {
        const linha = document.createElement("tr");

        linha.appendChild(criarCelula(alerta.codigo));
        linha.appendChild(criarCelula(alerta.nome));
        linha.appendChild(criarCelula(alerta.categoriaNome));
        linha.appendChild(criarCelula(alerta.saldo));
        linha.appendChild(criarCelula(alerta.estoqueMinimo));
        linha.appendChild(criarCelulaSituacao(alerta.situacao, alerta.situacaoClasse));
        linha.appendChild(criarCelulaAcaoReposicao(alerta.codigo));

        elementos.tabelaAlertasPainel.appendChild(linha);
    }
}

function criarCelulaAcaoReposicao(codigoProduto) {
    const celula = document.createElement("td");
    const link = document.createElement("a");

    link.href = `movimentacoes.html?produtoCodigo=${encodeURIComponent(codigoProduto)}&acao=entrada&autoHistorico=1`;
    link.textContent = "Repor estoque";
    link.className = "acao-repor-estoque";

    celula.appendChild(link);
    return celula;
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
