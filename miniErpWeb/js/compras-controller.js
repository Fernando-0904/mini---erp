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

    elementos.botaoAdicionarItemPedido?.addEventListener("click", function () {
        adicionarItem();
    });

    elementos.formularioPedidoCompra.addEventListener("submit", async function (evento) {
        evento.preventDefault();

        await executarComBotaoCarregando(botaoSalvarPedido, "Salvando...", async function () {
            await salvarPedido();
        });
    });

    elementos.campoFiltroStatusPedidoCompra?.addEventListener("change", function () {
        aplicarFiltroStatus();
    });

    carregarDadosIniciais();

    async function carregarDadosIniciais() {
        try {
            const [produtosApi, fornecedoresApi] = await Promise.all([
                listarProdutosApi(),
                listarFornecedoresApi()
            ]);

            produtos = Array.isArray(produtosApi) ? produtosApi : [];
            fornecedores = Array.isArray(fornecedoresApi) ? fornecedoresApi.filter(function (item) { return item.ativo; }) : [];

            preencherSelectFornecedores();
            preencherSelectProdutos();
            renderizarItensPedido();
            await carregarPedidos();
        } catch (erro) {
            produtos = [];
            fornecedores = [];
            pedidosCache = [];

            preencherSelectFornecedores();
            preencherSelectProdutos();
            renderizarItensPedido();
            renderizarEstadoErroPedidos(erro);
            exibirMensagem(montarMensagemErro(erro, "Erro ao carregar dados de compras."), "erro");
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
            exibirMensagem(montarMensagemErro(erro, "Erro ao carregar pedidos de compra."), "erro");
        }
    }

    function renderizarEstadoErroPedidos(erro) {
        elementos.tabelaPedidosCompra.innerHTML = "";

        const linhaErro = criarLinhaEstadoVazio(
            7,
            montarMensagemErro(erro, "Não foi possível carregar os pedidos agora."),
            "Tentar novamente",
            function () {
                carregarDadosIniciais();
            }
        );

        elementos.tabelaPedidosCompra.appendChild(linhaErro);
    }

    function montarMensagemErro(erro, fallback) {
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

    function aplicarFiltroStatus() {
        const statusSelecionado = typeof elementos.campoFiltroStatusPedidoCompra?.value === "string"
            ? elementos.campoFiltroStatusPedidoCompra.value.trim()
            : "";

        if (statusSelecionado === "") {
            renderizarPedidos(pedidosCache);
            return;
        }

        const pedidosFiltrados = pedidosCache.filter(function (pedido) {
            return pedido.status === statusSelecionado;
        });

        renderizarPedidos(pedidosFiltrados);
    }

    function preencherSelectFornecedores() {
        if (!(elementos.campoFornecedorPedidoCompra instanceof HTMLSelectElement)) {
            return;
        }

        elementos.campoFornecedorPedidoCompra.innerHTML = "";

        const opcaoPadrao = document.createElement("option");
        opcaoPadrao.value = "";
        opcaoPadrao.textContent = "Selecione um fornecedor";
        elementos.campoFornecedorPedidoCompra.appendChild(opcaoPadrao);

        for (const fornecedor of fornecedores) {
            const opcao = document.createElement("option");
            opcao.value = String(fornecedor.id);
            opcao.textContent = `${fornecedor.codigo} - ${fornecedor.nome}`;
            elementos.campoFornecedorPedidoCompra.appendChild(opcao);
        }
    }

    function preencherSelectProdutos() {
        if (!(elementos.campoProdutoPedidoCompra instanceof HTMLSelectElement)) {
            return;
        }

        elementos.campoProdutoPedidoCompra.innerHTML = "";

        const opcaoPadrao = document.createElement("option");
        opcaoPadrao.value = "";
        opcaoPadrao.textContent = "Selecione um produto";
        elementos.campoProdutoPedidoCompra.appendChild(opcaoPadrao);

        for (const produto of produtos) {
            const opcao = document.createElement("option");
            opcao.value = String(produto.codigo);
            opcao.textContent = `${produto.codigo} - ${produto.nome}`;
            elementos.campoProdutoPedidoCompra.appendChild(opcao);
        }
    }

    function adicionarItem() {
        const produtoCodigo = Number(elementos.campoProdutoPedidoCompra?.value || "");
        const quantidade = Number(elementos.campoQuantidadeItemPedidoCompra?.value || "");

        if (!Number.isInteger(produtoCodigo) || produtoCodigo <= 0) {
            exibirMensagem("Selecione um produto para adicionar no pedido.", "erro");
            return;
        }

        if (!Number.isInteger(quantidade) || quantidade <= 0) {
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
            const linhaVazia = criarLinhaEstadoVazio(
                5,
                "Nenhum item adicionado ao pedido.",
                "Selecionar produto",
                function () {
                    elementos.campoProdutoPedidoCompra?.focus();
                }
            );

            elementos.tabelaItensPedidoCompra.appendChild(linhaVazia);
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

            const celulaAcao = document.createElement("td");
            const botaoRemover = document.createElement("button");
            botaoRemover.type = "button";
            botaoRemover.textContent = "Remover";
            botaoRemover.addEventListener("click", function () {
                removerItem(item.produtoCodigo);
            });
            celulaAcao.appendChild(botaoRemover);
            linha.appendChild(celulaAcao);

            elementos.tabelaItensPedidoCompra.appendChild(linha);
        }
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
        const fornecedorId = Number(elementos.campoFornecedorPedidoCompra?.value || "");

        if (!Number.isInteger(fornecedorId) || fornecedorId <= 0) {
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
            exibirMensagem("Pedido de compra criado com sucesso.", "sucesso");
            await carregarPedidos();
        } catch (erro) {
            exibirMensagem(erro instanceof Error ? erro.message : "Não foi possível criar o pedido.", "erro");
        }
    }

    function renderizarPedidos(pedidos) {
        elementos.tabelaPedidosCompra.innerHTML = "";

        if (pedidos.length === 0) {
            const linhaVazia = criarLinhaEstadoVazio(
                7,
                "Nenhum pedido de compra cadastrado.",
                "Criar pedido",
                function () {
                    elementos.campoFornecedorPedidoCompra?.focus();
                }
            );

            elementos.tabelaPedidosCompra.appendChild(linhaVazia);
            return;
        }

        for (const pedido of pedidos) {
            const linha = document.createElement("tr");
            const resumoItens = (pedido.itens || [])
                .map(function (item) {
                    return `${item.produtoCodigo}(${item.quantidade})`;
                })
                .join(", ");

            linha.appendChild(criarCelula(pedido.id));
            linha.appendChild(criarCelula(pedido.fornecedorNome));
            linha.appendChild(criarCelula(pedido.status));
            linha.appendChild(criarCelula(resumoItens));
            linha.appendChild(criarCelula(formatarMoeda(pedido.valorTotal || 0)));
            linha.appendChild(criarCelula(formatarData(pedido.criadoEmUtc)));

            const celulaAcoes = document.createElement("td");
            if (pedido.status === "Aberto") {
                const botaoReceber = document.createElement("button");
                botaoReceber.type = "button";
                botaoReceber.textContent = "Receber";
                botaoReceber.addEventListener("click", async function () {
                    await receberPedido(pedido.id);
                });
                celulaAcoes.appendChild(botaoReceber);
            } else {
                celulaAcoes.textContent = "Concluído";
            }

            linha.appendChild(celulaAcoes);
            elementos.tabelaPedidosCompra.appendChild(linha);
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

    function formatarData(dataIso) {
        if (typeof dataIso !== "string" || dataIso.trim() === "") {
            return "";
        }

        const data = new Date(dataIso);

        if (Number.isNaN(data.getTime())) {
            return dataIso;
        }

        return data.toLocaleString("pt-BR");
    }
}
