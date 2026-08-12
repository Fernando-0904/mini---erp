const COLSPAN_ITENS_PEDIDO = 6;
const COLSPAN_TABELA_PEDIDOS = 7;

function inicializarComprasController() {
    if (!(elementos.formularioPedidoCompra instanceof HTMLFormElement) ||
        !(elementos.tabelaPedidosCompra instanceof HTMLElement)) {
        return;
    }

    const itensPedido = [];
    let produtos = [];
    let fornecedores = [];
    let pedidosCache = [];
    const botaoSalvarPedido = elementos.formularioPedidoCompra.querySelector("button[type='submit']");

    elementos.botaoAdicionarItemPedido?.addEventListener("click", adicionarItem);
    elementos.campoFiltroStatusPedidoCompra?.addEventListener("change", aplicarFiltroStatus);

    elementos.formularioPedidoCompra.addEventListener("submit", async function (evento) {
        evento.preventDefault();

        await executarComBotaoCarregando(botaoSalvarPedido, "Salvando...", async function () {
            await salvarPedido();
        });
    });

    carregarDadosIniciais();

    async function carregarDadosIniciais() {
        try {
            const [produtosApi, fornecedoresApi] = await Promise.all([
                listarProdutosApi(),
                listarFornecedoresApi()
            ]);

            produtos = Array.isArray(produtosApi) ? produtosApi : [];
            fornecedores = Array.isArray(fornecedoresApi)
                ? fornecedoresApi.filter(function (item) { return item.ativo; })
                : [];

            preencherSelectProdutos();
            preencherSelectFornecedores();
            renderizarItensPedido();
            await carregarPedidos();
        } catch (erro) {
            produtos = [];
            fornecedores = [];
            pedidosCache = [];

            preencherSelectProdutos();
            preencherSelectFornecedores();
            renderizarItensPedido();
            renderizarEstadoErroPedidos(erro);
            exibirMensagem(montarMensagemErroComProtocolo(erro, "Erro ao carregar dados de compras."), "erro");
        }
    }

    async function carregarPedidos() {
        try {
            const pedidos = await listarPedidosCompraApi();
            pedidosCache = Array.isArray(pedidos) ? pedidos : [];
            aplicarFiltroStatus();
        } catch (erro) {
            pedidosCache = [];
            renderizarEstadoErroPedidos(erro);
            exibirMensagem(montarMensagemErroComProtocolo(erro, "Erro ao carregar pedidos de compra."), "erro");
        }
    }

    function renderizarEstadoErroPedidos(erro) {
        if (!(elementos.tabelaPedidosCompra instanceof HTMLElement)) {
            return;
        }

        elementos.tabelaPedidosCompra.innerHTML = "";
        elementos.tabelaPedidosCompra.appendChild(criarLinhaEstadoVazio(
            COLSPAN_TABELA_PEDIDOS,
            montarMensagemErroComProtocolo(erro, "Não foi possível carregar os pedidos agora."),
            "Tentar novamente",
            function () {
                carregarDadosIniciais();
            }
        ));
    }

    function aplicarFiltroStatus() {
        const statusSelecionado = typeof elementos.campoFiltroStatusPedidoCompra?.value === "string"
            ? elementos.campoFiltroStatusPedidoCompra.value.trim()
            : "";
        const pedidosFiltrados = statusSelecionado === ""
            ? pedidosCache
            : pedidosCache.filter(function (pedido) {
                return pedido.status === statusSelecionado;
            });

        renderizarPedidos(pedidosFiltrados);
    }

    function preencherSelectProdutos() {
        preencherSelectGenerico(
            elementos.campoProdutoPedidoCompra,
            "Selecione um produto",
            produtos,
            function (produto) {
                return String(produto.codigo);
            },
            function (produto) {
                return `${produto.codigo} - ${produto.nome}`;
            }
        );
    }

    function preencherSelectFornecedores() {
        preencherSelectGenerico(
            elementos.campoFornecedorPedidoCompra,
            "Selecione um fornecedor",
            fornecedores,
            function (fornecedor) {
                return String(fornecedor.id);
            },
            function (fornecedor) {
                return `${fornecedor.codigo} - ${fornecedor.nome}`;
            }
        );
    }

    function adicionarItem() {
        const produtoCodigo = lerInteiroPositivo(elementos.campoProdutoPedidoCompra?.value);
        const quantidade = lerInteiroPositivo(elementos.campoQuantidadeItemPedidoCompra?.value);

        if (produtoCodigo === null) {
            exibirMensagem("Selecione um produto para adicionar no pedido.", "erro");
            return;
        }

        if (quantidade === null) {
            exibirMensagem("Informe uma quantidade válida para o item.", "erro");
            return;
        }

        const produto = produtos.find(function (item) {
            return item.codigo === produtoCodigo;
        });

        if (!produto) {
            exibirMensagem("Produto selecionado não encontrado.", "erro");
            return;
        }

        const itemExistente = itensPedido.find(function (item) {
            return item.produtoCodigo === produtoCodigo;
        });

        if (itemExistente) {
            itemExistente.quantidade += quantidade;
        } else {
            itensPedido.push({
                produtoCodigo,
                produtoNome: produto.nome,
                quantidade,
                precoUnitario: produto.precoUnitario
            });
        }

        if (elementos.campoQuantidadeItemPedidoCompra instanceof HTMLInputElement) {
            elementos.campoQuantidadeItemPedidoCompra.value = "";
        }

        renderizarItensPedido();
        exibirMensagem("Item adicionado ao pedido.", "sucesso");
    }

    function renderizarItensPedido() {
        if (!(elementos.tabelaItensPedidoCompra instanceof HTMLElement)) {
            return;
        }

        elementos.tabelaItensPedidoCompra.innerHTML = "";

        if (itensPedido.length === 0) {
            elementos.tabelaItensPedidoCompra.appendChild(criarLinhaEstadoVazio(
                COLSPAN_ITENS_PEDIDO,
                "Nenhum item adicionado ao pedido.",
                "Selecionar produto",
                function () {
                    elementos.campoProdutoPedidoCompra?.focus();
                }
            ));
            return;
        }

        for (const item of itensPedido) {
            const linha = document.createElement("tr");
            const valorTotal = item.precoUnitario * item.quantidade;

            linha.appendChild(criarCelula(item.produtoCodigo));
            linha.appendChild(criarCelula(item.produtoNome));
            linha.appendChild(criarCelula(item.quantidade));
            linha.appendChild(criarCelula(formatarMoeda(item.precoUnitario)));
            linha.appendChild(criarCelula(formatarMoeda(valorTotal)));
            linha.appendChild(criarCelulaAcaoRemoverItem(item.produtoCodigo));

            elementos.tabelaItensPedidoCompra.appendChild(linha);
        }
    }

    function criarCelulaAcaoRemoverItem(produtoCodigo) {
        const celulaAcao = document.createElement("td");
        const botaoRemover = document.createElement("button");

        botaoRemover.type = "button";
        botaoRemover.textContent = "Remover";
        botaoRemover.addEventListener("click", function () {
            removerItem(produtoCodigo);
        });

        celulaAcao.appendChild(botaoRemover);
        return celulaAcao;
    }

    function removerItem(produtoCodigo) {
        const indice = itensPedido.findIndex(function (item) {
            return item.produtoCodigo === produtoCodigo;
        });

        if (indice >= 0) {
            itensPedido.splice(indice, 1);
            renderizarItensPedido();
        }
    }

    async function salvarPedido() {
        const fornecedorId = lerInteiroPositivo(elementos.campoFornecedorPedidoCompra?.value);

        if (fornecedorId === null) {
            exibirMensagem("Selecione um fornecedor para criar o pedido.", "erro");
            return;
        }

        if (itensPedido.length === 0) {
            exibirMensagem("Adicione ao menos um item para criar o pedido.", "erro");
            return;
        }

        const payload = {
            fornecedorId,
            itens: itensPedido.map(function (item) {
                return {
                    produtoCodigo: item.produtoCodigo,
                    quantidade: item.quantidade
                };
            })
        };

        try {
            await criarPedidoCompraApi(payload);
            itensPedido.length = 0;
            renderizarItensPedido();
            elementos.formularioPedidoCompra.reset();
            exibirMensagem("Pedido de compra criado e enviado para aprovação.", "sucesso");
            await carregarPedidos();
        } catch (erro) {
            exibirMensagem(erro instanceof Error ? erro.message : "Não foi possível criar o pedido.", "erro");
        }
    }

    function renderizarPedidos(pedidos) {
        if (!(elementos.tabelaPedidosCompra instanceof HTMLElement)) {
            return;
        }

        elementos.tabelaPedidosCompra.innerHTML = "";

        if (pedidos.length === 0) {
            elementos.tabelaPedidosCompra.appendChild(criarLinhaEstadoVazio(
                COLSPAN_TABELA_PEDIDOS,
                "Nenhum pedido de compra cadastrado.",
                "Criar pedido",
                function () {
                    elementos.campoFornecedorPedidoCompra?.focus();
                }
            ));
            return;
        }

        for (const pedido of pedidos) {
            elementos.tabelaPedidosCompra.appendChild(criarLinhaPedido(pedido));
        }
    }

    function criarLinhaPedido(pedido) {
        const linha = document.createElement("tr");

        linha.appendChild(criarCelula(pedido.id));
        linha.appendChild(criarCelula(pedido.fornecedorNome));
        linha.appendChild(criarCelula(formatarStatusPedido(pedido.status)));
        linha.appendChild(criarCelula(montarResumoItensPedido(pedido.itens)));
        linha.appendChild(criarCelula(formatarMoeda(pedido.valorTotal || 0)));
        linha.appendChild(criarCelula(formatarDataPedido(pedido.criadoEmUtc)));
        linha.appendChild(criarCelulaAcaoPedido(pedido));

        return linha;
    }

    function criarCelulaAcaoPedido(pedido) {
        const celulaAcoes = document.createElement("td");

        if (pedido.status === "PendenteAprovacao" || pedido.status === "Aberto") {
            const botaoAprovar = document.createElement("button");
            botaoAprovar.type = "button";
            botaoAprovar.textContent = "Aprovar";
            botaoAprovar.addEventListener("click", async function () {
                await aprovarPedido(pedido.id);
            });
            celulaAcoes.appendChild(botaoAprovar);

            const botaoRejeitar = document.createElement("button");
            botaoRejeitar.type = "button";
            botaoRejeitar.textContent = "Rejeitar";
            botaoRejeitar.addEventListener("click", async function () {
                await rejeitarPedido(pedido.id);
            });
            celulaAcoes.appendChild(botaoRejeitar);

            return celulaAcoes;
        }

        if (pedido.status === "Aprovado") {
            const botaoReceber = document.createElement("button");
            botaoReceber.type = "button";
            botaoReceber.textContent = "Receber";
            botaoReceber.addEventListener("click", async function () {
                await receberPedido(pedido.id);
            });
            celulaAcoes.appendChild(botaoReceber);
            return celulaAcoes;
        }

        celulaAcoes.textContent = "Concluído";

        return celulaAcoes;
    }

    async function aprovarPedido(pedidoId) {
        const confirmar = confirm(`Confirma a aprovação do pedido ${pedidoId}?`);

        if (!confirmar) {
            return;
        }

        try {
            await aprovarPedidoCompraApi(pedidoId);
            exibirMensagem("Pedido aprovado com sucesso.", "sucesso");
            await carregarPedidos();
        } catch (erro) {
            exibirMensagem(erro instanceof Error ? erro.message : "Não foi possível aprovar o pedido.", "erro");
        }
    }

    async function rejeitarPedido(pedidoId) {
        const confirmar = confirm(`Confirma a rejeição do pedido ${pedidoId}?`);

        if (!confirmar) {
            return;
        }

        try {
            await rejeitarPedidoCompraApi(pedidoId);
            exibirMensagem("Pedido rejeitado com sucesso.", "sucesso");
            await carregarPedidos();
        } catch (erro) {
            exibirMensagem(erro instanceof Error ? erro.message : "Não foi possível rejeitar o pedido.", "erro");
        }
    }

    async function receberPedido(pedidoId) {
        const confirmar = confirm(`Confirma o recebimento do pedido ${pedidoId}? Esta ação dará entrada no estoque.`);

        if (!confirmar) {
            return;
        }

        try {
            await receberPedidoCompraApi(pedidoId);
            exibirMensagem("Pedido recebido e estoque atualizado com sucesso.", "sucesso");
            await carregarPedidos();
        } catch (erro) {
            exibirMensagem(erro instanceof Error ? erro.message : "Não foi possível receber o pedido.", "erro");
        }
    }
}

function preencherSelectGenerico(select, textoPadrao, lista, obterValor, obterTexto) {
    if (!(select instanceof HTMLSelectElement)) {
        return;
    }

    select.innerHTML = "";

    const opcaoPadrao = document.createElement("option");
    opcaoPadrao.value = "";
    opcaoPadrao.textContent = textoPadrao;
    select.appendChild(opcaoPadrao);

    for (const item of lista) {
        const opcao = document.createElement("option");
        opcao.value = obterValor(item);
        opcao.textContent = obterTexto(item);
        select.appendChild(opcao);
    }
}

function lerInteiroPositivo(valor) {
    const numero = Number(valor || "");

    if (!Number.isInteger(numero) || numero <= 0) {
        return null;
    }

    return numero;
}

function montarMensagemErroComProtocolo(erro, fallback) {
    const mensagemBase = erro instanceof Error && erro.message
        ? erro.message
        : fallback;
    const correlationId = erro !== null && typeof erro === "object" && typeof erro.correlationId === "string"
        ? erro.correlationId.trim()
        : "";

    if (correlationId === "") {
        return mensagemBase;
    }

    return `${mensagemBase} Protocolo: ${correlationId}.`;
}

function montarResumoItensPedido(itens) {
    if (!Array.isArray(itens) || itens.length === 0) {
        return "";
    }

    return itens.map(function (item) {
        return `${item.produtoCodigo}(${item.quantidade})`;
    }).join(", ");
}

function formatarDataPedido(dataIso) {
    if (typeof dataIso !== "string" || dataIso.trim() === "") {
        return "";
    }

    const data = new Date(dataIso);

    if (Number.isNaN(data.getTime())) {
        return dataIso;
    }

    return data.toLocaleString("pt-BR");
}

function formatarStatusPedido(status) {
    switch (status) {
        case "PendenteAprovacao":
            return "Pendente de aprovação";
        case "Aprovado":
            return "Aprovado";
        case "Rejeitado":
            return "Rejeitado";
        case "Recebido":
            return "Recebido";
        case "Aberto":
            return "Aberto";
        default:
            return status || "-";
    }
}
