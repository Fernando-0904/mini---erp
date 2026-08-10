let temporizadorMensagemSucesso = null;
let tokenMensagemAtual = 0;
const DURACAO_MENSAGEM_SUCESSO_MS = 4000;
const CLASSE_TIPO_MENSAGEM = {
    sucesso: "mensagem-sucesso",
    erro: "mensagem-erro",
    aviso: "mensagem-aviso",
    info: "mensagem-info"
};

function inicializarStatusConexaoSistema() {
    if (document.getElementById("statusConexaoSistema") instanceof HTMLElement) {
        return;
    }

    const cabecalho = document.querySelector("header");

    if (!(cabecalho instanceof HTMLElement)) {
        return;
    }

    const indicador = document.createElement("div");
    indicador.id = "statusConexaoSistema";
    indicador.className = "status-conexao status-conexao-verificando";
    indicador.setAttribute("role", "status");
    indicador.setAttribute("aria-live", "polite");

    const ponto = document.createElement("span");
    ponto.className = "status-conexao-ponto";
    ponto.setAttribute("aria-hidden", "true");

    const texto = document.createElement("span");
    texto.className = "status-conexao-texto";
    texto.textContent = "Conexão com o sistema: verificando";

    indicador.append(ponto, texto);
    cabecalho.insertBefore(indicador, cabecalho.querySelector("nav"));

    window.addEventListener("miniErp:status-conexao", function (evento) {
        const status = evento instanceof CustomEvent && evento.detail !== null && typeof evento.detail === "object"
            ? evento.detail.status
            : "verificando";

        atualizarStatusConexaoSistema(status);
    });

    window.addEventListener("online", function () {
        atualizarStatusConexaoSistema("verificando");
    });

    window.addEventListener("offline", function () {
        atualizarStatusConexaoSistema("offline");
    });
}

function atualizarStatusConexaoSistema(status) {
    const indicador = document.getElementById("statusConexaoSistema");

    if (!(indicador instanceof HTMLElement)) {
        return;
    }

    const texto = indicador.querySelector(".status-conexao-texto");

    if (!(texto instanceof HTMLElement)) {
        return;
    }

    indicador.classList.remove("status-conexao-online", "status-conexao-offline", "status-conexao-verificando");

    if (status === "online") {
        indicador.classList.add("status-conexao-online");
        texto.textContent = "Conexão com o sistema: online";
        return;
    }

    if (status === "offline") {
        indicador.classList.add("status-conexao-offline");
        texto.textContent = "Conexão com o sistema: indisponível";
        return;
    }

    indicador.classList.add("status-conexao-verificando");
    texto.textContent = "Conexão com o sistema: verificando";
}

function normalizarTipoMensagem(tipo) {
    if (typeof tipo !== "string") {
        return "info";
    }

    const tipoNormalizado = tipo.trim().toLowerCase();

    if (Object.prototype.hasOwnProperty.call(CLASSE_TIPO_MENSAGEM, tipoNormalizado)) {
        return tipoNormalizado;
    }

    return "info";
}

function exibirMensagem(texto, tipo) {
    if (!(elementos.mensagem instanceof HTMLElement)) {
        return;
    }

    tokenMensagemAtual += 1;
    const tokenLocal = tokenMensagemAtual;
    const tipoNormalizado = normalizarTipoMensagem(tipo);
    const textoNormalizado = typeof texto === "string" ? texto.trim() : "";

    if (temporizadorMensagemSucesso !== null) {
        clearTimeout(temporizadorMensagemSucesso);
        temporizadorMensagemSucesso = null;
    }

    elementos.mensagem.textContent = textoNormalizado;
    elementos.mensagem.className = "";

    if (textoNormalizado === "") {
        return;
    }

    elementos.mensagem.className = `mensagem ${CLASSE_TIPO_MENSAGEM[tipoNormalizado]}`;

    if (tipoNormalizado === "sucesso") {
        temporizadorMensagemSucesso = setTimeout(function () {
            if (tokenLocal !== tokenMensagemAtual) {
                return;
            }

            elementos.mensagem.textContent = "";
            elementos.mensagem.className = "";
            temporizadorMensagemSucesso = null;
        }, DURACAO_MENSAGEM_SUCESSO_MS);
    }
}

function formatarMoeda(valor) {
    return valor.toLocaleString("pt-BR", {
        style: "currency",
        currency: "BRL"
    });
}

function normalizarTextoParaBusca(texto) {
    return String(texto || "")
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .toLowerCase()
        .trim();
}

async function executarComBotaoCarregando(botao, textoCarregando, acaoAsync) {
    if (typeof acaoAsync !== "function") {
        return;
    }

    if (!(botao instanceof HTMLButtonElement)) {
        return acaoAsync();
    }

    if (botao.dataset.carregando === "1") {
        return;
    }

    const textoOriginal = botao.textContent;

    botao.dataset.carregando = "1";
    botao.disabled = true;
    botao.setAttribute("aria-busy", "true");

    if (typeof textoCarregando === "string" && textoCarregando.trim() !== "") {
        botao.textContent = textoCarregando;
    }

    try {
        return await acaoAsync();
    } finally {
        botao.disabled = false;
        botao.removeAttribute("aria-busy");

        if (typeof textoCarregando === "string" && botao.textContent === textoCarregando) {
            botao.textContent = textoOriginal;
        }

        delete botao.dataset.carregando;
    }
}

function focarElementoComSuavidade(elemento) {
    if (!(elemento instanceof HTMLElement)) {
        return;
    }

    elemento.focus();
    elemento.scrollIntoView({
        behavior: "smooth",
        block: "center"
    });
}

function criarLinhaEstadoVazio(colSpan, mensagem, textoAcao, aoExecutarAcao) {
    const linhaVazia = document.createElement("tr");
    const celulaVazia = document.createElement("td");
    const conteudo = document.createElement("div");
    const textoMensagem = document.createElement("p");

    celulaVazia.colSpan = colSpan;
    celulaVazia.className = "estado-vazio-celula";
    conteudo.className = "estado-vazio";
    textoMensagem.className = "estado-vazio-mensagem";
    textoMensagem.textContent = mensagem;

    conteudo.appendChild(textoMensagem);

    if (typeof aoExecutarAcao === "function" && typeof textoAcao === "string" && textoAcao.trim() !== "") {
        const botaoAcao = document.createElement("button");

        botaoAcao.type = "button";
        botaoAcao.className = "estado-vazio-acao";
        botaoAcao.textContent = textoAcao;
        botaoAcao.addEventListener("click", aoExecutarAcao);
        conteudo.appendChild(botaoAcao);
    }

    celulaVazia.appendChild(conteudo);
    linhaVazia.appendChild(celulaVazia);

    return linhaVazia;
}

function atualizarTabela(listaProdutos, aoEditarProduto, aoRemoverProduto, opcoes = {}) {
    const listaRenderizacao = Array.isArray(opcoes.lista) ? opcoes.lista : listaProdutos;
    const mensagemVazia = typeof opcoes.mensagemVazia === "string"
        ? opcoes.mensagemVazia
        : "Nenhum produto cadastrado.";
    const textoAcaoVazia = typeof opcoes.textoAcaoVazia === "string"
        ? opcoes.textoAcaoVazia
        : "Cadastrar produto";
    const acaoVazia = typeof opcoes.acaoVazia === "function"
        ? opcoes.acaoVazia
        : function () {
            const campoPrincipal = elementos.campoNome instanceof HTMLElement
                ? elementos.campoNome
                : elementos.campoCodigo;

            focarElementoComSuavidade(campoPrincipal);
        };

    elementos.tabelaProdutos.innerHTML = "";

    if (listaRenderizacao.length === 0) {
        const linhaVazia = criarLinhaEstadoVazio(
            9,
            mensagemVazia,
            textoAcaoVazia,
            acaoVazia
        );

        elementos.tabelaProdutos.appendChild(linhaVazia);
        return;
    }

    for (const produto of listaRenderizacao) {
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

function atualizarTabelaFornecedores(fornecedores, aoEditarFornecedor, aoInativarFornecedor, aoRemoverFornecedor, opcoes = {}) {
    const listaRenderizacao = Array.isArray(opcoes.lista) ? opcoes.lista : fornecedores;
    const mensagemVazia = typeof opcoes.mensagemVazia === "string"
        ? opcoes.mensagemVazia
        : "Nenhum fornecedor cadastrado.";
    const textoAcaoVazia = typeof opcoes.textoAcaoVazia === "string"
        ? opcoes.textoAcaoVazia
        : "Cadastrar fornecedor";
    const acaoVazia = typeof opcoes.acaoVazia === "function"
        ? opcoes.acaoVazia
        : function () {
            const campoPrincipal = elementos.campoFornecedorCodigo instanceof HTMLElement
                ? elementos.campoFornecedorCodigo
                : elementos.campoFornecedorNome;

            focarElementoComSuavidade(campoPrincipal);
        };

    elementos.tabelaFornecedores.innerHTML = "";

    if (listaRenderizacao.length === 0) {
        const linhaVazia = criarLinhaEstadoVazio(
            7,
            mensagemVazia,
            textoAcaoVazia,
            acaoVazia
        );

        elementos.tabelaFornecedores.appendChild(linhaVazia);
        return;
    }

    for (const fornecedor of listaRenderizacao) {
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
        const linhaVazia = criarLinhaEstadoVazio(
            6,
            "Nenhum produto com estoque baixo.",
            "Ir para produtos",
            function () {
                window.location.href = "produtos.html";
            }
        );

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

function atualizarResumoAlertasPainel(alertas) {
    if (elementos.quantidadeAlertasCriticos === null || elementos.quantidadeAlertasTotal === null) {
        return;
    }

    const totalCriticos = alertas.filter(function (alerta) {
        return alerta.prioridade === "Crítico";
    }).length;

    elementos.quantidadeAlertasCriticos.textContent = String(totalCriticos);
    elementos.quantidadeAlertasTotal.textContent = String(alertas.length);
}

function atualizarTabelaAlertasPainel(alertas) {
    if (elementos.tabelaAlertasPainel === null) {
        return;
    }

    elementos.tabelaAlertasPainel.innerHTML = "";

    if (alertas.length === 0) {
        const linhaVazia = document.createElement("tr");
        const celulaVazia = document.createElement("td");

        celulaVazia.colSpan = 5;
        celulaVazia.textContent = "Nenhum alerta operacional no momento.";
        linhaVazia.appendChild(celulaVazia);
        elementos.tabelaAlertasPainel.appendChild(linhaVazia);
        return;
    }

    for (const alerta of alertas) {
        const linha = document.createElement("tr");

        linha.appendChild(criarCelulaPrioridade(alerta.prioridade));
        linha.appendChild(criarCelula(alerta.titulo));
        linha.appendChild(criarCelula(alerta.produto));
        linha.appendChild(criarCelula(alerta.detalhe));
        linha.appendChild(criarCelulaAcaoRapida(alerta));

        elementos.tabelaAlertasPainel.appendChild(linha);
    }
}

function criarCelulaPrioridade(prioridade) {
    const celula = document.createElement("td");
    const marcador = document.createElement("span");

    marcador.className = "prioridade-alerta";

    if (prioridade === "Crítico") {
        marcador.className += " prioridade-critica";
    } else if (prioridade === "Atenção") {
        marcador.className += " prioridade-atencao";
    } else {
        marcador.className += " prioridade-info";
    }

    marcador.textContent = prioridade;
    celula.appendChild(marcador);

    return celula;
}

function criarCelulaAcaoRapida(alerta) {
    const celula = document.createElement("td");

    if (!alerta.acao || !alerta.acao.href || !alerta.acao.label) {
        celula.textContent = "Sem ação";
        return celula;
    }

    const link = document.createElement("a");

    link.href = alerta.acao.href;
    link.className = "acao-rapida-link";
    link.textContent = alerta.acao.label;

    celula.appendChild(link);
    return celula;
}

function atualizarTabelaMovimentacoes(movimentacoes) {
    elementos.tabelaMovimentacoes.innerHTML = "";

    if (movimentacoes.length === 0) {
        const linhaVazia = criarLinhaEstadoVazio(
            6,
            "Nenhuma movimentação registrada para o filtro atual.",
            "Registrar movimentação",
            function () {
                focarElementoComSuavidade(elementos.campoMovimentacaoCodigo);
            }
        );

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
