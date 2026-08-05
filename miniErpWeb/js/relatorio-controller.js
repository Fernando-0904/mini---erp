function inicializarRelatoriosController() {
    if (!(elementos.formularioRelatorios instanceof HTMLFormElement) ||
        !(elementos.campoTipoRelatorio instanceof HTMLSelectElement) ||
        !(elementos.cabecalhoTabelaRelatorios instanceof HTMLElement) ||
        !(elementos.tabelaRelatorios instanceof HTMLElement)) {
        return;
    }

    const descricaoRelatorio = document.getElementById("descricaoRelatorioAtual");
    const botaoGerarRelatorio = elementos.formularioRelatorios.querySelector("button[type='submit']");
    const botaoExportarRelatorio = elementos.botaoExportarRelatorio;

    elementos.formularioRelatorios.addEventListener("submit", async function (evento) {
        evento.preventDefault();
        await executarComBotaoCarregando(
            botaoGerarRelatorio,
            "Carregando...",
            async function () {
                await carregarRelatorioSelecionado();
            });
    });

    elementos.campoTipoRelatorio.addEventListener("change", function () {
        atualizarVisibilidadeLimite();
    });

    if (botaoExportarRelatorio instanceof HTMLButtonElement) {
        botaoExportarRelatorio.addEventListener("click", async function () {
            await executarComBotaoCarregando(
                botaoExportarRelatorio,
                "Exportando...",
                async function () {
                    await exportarRelatorioSelecionado();
                });
        });
    }

    atualizarVisibilidadeLimite();
    carregarRelatorioSelecionado();

    function atualizarVisibilidadeLimite() {
        const mostrarLimite = elementos.campoTipoRelatorio.value === "ultimas-movimentacoes";

        if (elementos.campoLimiteRelatorio instanceof HTMLInputElement) {
            elementos.campoLimiteRelatorio.disabled = !mostrarLimite;
        }
    }

    async function carregarRelatorioSelecionado() {
        try {
            const resultado = await buscarDadosRelatorio();
            configurarCabecalho(resultado.colunas);
            renderizarLinhas(resultado.linhas);

            if (descricaoRelatorio instanceof HTMLElement) {
                descricaoRelatorio.textContent = resultado.descricao;
            }

            exibirMensagem("", "sucesso");
        } catch (erro) {
            configurarCabecalho(["Informação"]);
            renderizarLinhas([]);
            exibirMensagem(erro instanceof Error ? erro.message : "Não foi possível carregar o relatório.", "erro");
        }
    }

    async function exportarRelatorioSelecionado() {
        const tipoRelatorio = elementos.campoTipoRelatorio.value;
        const limite = obterLimiteRelatorioSelecionado();
        const arquivo = await exportarRelatorioCsvApi(tipoRelatorio, limite);

        baixarArquivo(arquivo.blob, arquivo.nomeArquivo);
        exibirMensagem("Relatório exportado com sucesso.", "sucesso");
    }

    async function buscarDadosRelatorio() {
        const tipoRelatorio = elementos.campoTipoRelatorio.value;

        if (tipoRelatorio === "produtos-estoque-baixo") {
            const itens = await listarRelatorioProdutosEstoqueBaixoApi();
            return {
                descricao: "Produtos com saldo menor ou igual ao estoque mínimo.",
                colunas: ["Código", "Nome", "Categoria", "Saldo", "Estoque mínimo"],
                linhas: itens.map(function (item) {
                    return [
                        item.codigo,
                        item.nome,
                        item.categoria,
                        item.quantidadeEstoque,
                        item.estoqueMinimo
                    ];
                })
            };
        }

        if (tipoRelatorio === "produtos-sem-estoque") {
            const itens = await listarRelatorioProdutosSemEstoqueApi();
            return {
                descricao: "Produtos cujo saldo está zerado.",
                colunas: ["Código", "Nome", "Categoria", "Saldo"],
                linhas: itens.map(function (item) {
                    return [
                        item.codigo,
                        item.nome,
                        item.categoria,
                        item.quantidadeEstoque
                    ];
                })
            };
        }

        if (tipoRelatorio === "valor-estoque-por-categoria") {
            const itens = await listarRelatorioValorEstoquePorCategoriaApi();
            return {
                descricao: "Valor total em estoque agrupado por categoria.",
                colunas: ["Categoria", "Valor total"],
                linhas: itens.map(function (item) {
                    return [
                        item.categoria,
                        formatarMoeda(item.valorTotal)
                    ];
                })
            };
        }

        if (tipoRelatorio === "produtos-sem-fornecedor") {
            const itens = await listarRelatorioProdutosSemFornecedorApi();
            return {
                descricao: "Produtos sem fornecedor vinculado.",
                colunas: ["Código", "Nome", "Categoria"],
                linhas: itens.map(function (item) {
                    return [
                        item.codigo,
                        item.nome,
                        item.categoria
                    ];
                })
            };
        }

        const limite = obterLimiteRelatorioSelecionado();

        const itens = await listarRelatorioUltimasMovimentacoesApi(limite);
        return {
            descricao: "Movimentações mais recentes de estoque.",
            colunas: ["Produto", "Tipo", "Quantidade", "Saldo anterior", "Saldo novo", "Data"],
            linhas: itens.map(function (item) {
                return [
                    item.produto,
                    item.tipo,
                    item.quantidade,
                    item.saldoAnterior,
                    item.saldoNovo,
                    formatarDataRelatorio(item.dataMovimentacaoUtc)
                ];
            })
        };
    }

    function obterLimiteRelatorioSelecionado() {
        const limitePadrao = 10;

        if (!(elementos.campoLimiteRelatorio instanceof HTMLInputElement)) {
            return limitePadrao;
        }

        const valorInformado = Number(elementos.campoLimiteRelatorio.value);

        if (Number.isFinite(valorInformado) && valorInformado > 0) {
            return Math.trunc(valorInformado);
        }

        return limitePadrao;
    }

    function baixarArquivo(blob, nomeArquivo) {
        const urlTemporaria = URL.createObjectURL(blob);
        const link = document.createElement("a");

        link.href = urlTemporaria;
        link.download = nomeArquivo || "relatorio.csv";
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(urlTemporaria);
    }

    function configurarCabecalho(colunas) {
        elementos.cabecalhoTabelaRelatorios.innerHTML = "";

        const linha = document.createElement("tr");

        for (const coluna of colunas) {
            const celula = document.createElement("th");
            celula.textContent = coluna;
            linha.appendChild(celula);
        }

        elementos.cabecalhoTabelaRelatorios.appendChild(linha);
    }

    function renderizarLinhas(linhas) {
        elementos.tabelaRelatorios.innerHTML = "";

        if (!Array.isArray(linhas) || linhas.length === 0) {
            const linhaVazia = document.createElement("tr");
            const celulaVazia = document.createElement("td");

            celulaVazia.colSpan = Math.max(1, elementos.cabecalhoTabelaRelatorios.querySelectorAll("th").length);
            celulaVazia.textContent = "Nenhum dado encontrado para este relatório.";
            linhaVazia.appendChild(celulaVazia);
            elementos.tabelaRelatorios.appendChild(linhaVazia);
            return;
        }

        for (const linha of linhas) {
            const linhaTabela = document.createElement("tr");

            for (const valor of linha) {
                const celula = document.createElement("td");
                celula.textContent = valor === null || valor === undefined ? "" : String(valor);
                linhaTabela.appendChild(celula);
            }

            elementos.tabelaRelatorios.appendChild(linhaTabela);
        }
    }

    function formatarDataRelatorio(dataIso) {
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
