function inicializarProdutoController() {
    const produtos = [];
    let codigoProdutoEmEdicao = null;
    let contextoUrlAplicado = false;

    window.recarregarProdutosNaTela = atualizarProdutosDaApi;
    window.recarregarCategoriasDoProduto = carregarCategoriasDoProduto;
    window.recarregarFornecedoresDoProduto = carregarFornecedoresDoProduto;

    carregarProdutos();
    carregarCategoriasDoProduto();
    carregarFornecedoresDoProduto();

    elementos.formulario.addEventListener("submit", async function (event) {
        event.preventDefault();

        await executarComBotaoCarregando(
            elementos.botaoSalvarProduto,
            codigoProdutoEmEdicao === null ? "Cadastrando..." : "Salvando...",
            async function () {
                const codigoTexto = elementos.campoCodigo.value.trim();
                const nome = elementos.campoNome.value.trim();
                const precoTexto = elementos.campoPreco.value.trim();
                const quantidadeTexto = elementos.campoQuantidade.value.trim();
                const estoqueMinimoTexto = elementos.campoEstoqueMinimo.value.trim();
                const categoriaIdTexto = elementos.campoCategoriaProduto.value;
                const fornecedorIdTexto = elementos.campoFornecedorProduto.value;

                const codigo = Number(codigoTexto);
                const preco = Number(precoTexto);
                const quantidade = Number(quantidadeTexto);
                const estoqueMinimo = estoqueMinimoTexto === "" ? 0 : Number(estoqueMinimoTexto);
                const categoriaId = Number(categoriaIdTexto);
                const fornecedorId = fornecedorIdTexto === "" ? null : Number(fornecedorIdTexto);

                if (!validarProduto(codigoTexto, nome, precoTexto, quantidadeTexto, estoqueMinimoTexto, categoriaIdTexto, codigo, preco, quantidade, estoqueMinimo, categoriaId)) {
                    return;
                }

                if (codigoProdutoEmEdicao === null) {
                    const produto = {
                        codigo: codigo,
                        nome: nome,
                        preco: preco,
                        quantidade: quantidade,
                        estoqueMinimo: estoqueMinimo,
                        categoriaId: categoriaId,
                        fornecedorId: fornecedorId
                    };

                    try {
                        const produtoCadastrado = await cadastrarProdutoApi(converterProdutoTelaParaApi(produto));

                        upsertProdutoNoArray(converterProdutoApiParaTela(produtoCadastrado));
                        exibirMensagem("Produto cadastrado com sucesso pela API.", "sucesso");
                    } catch (erro) {
                        exibirMensagem(erro.message, "erro");
                        return;
                    }
                } else {
                    const produtoParaEditar = produtos.find(function (produto) {
                        return produto.codigo === codigoProdutoEmEdicao;
                    });

                    if (produtoParaEditar === undefined) {
                        exibirMensagem("Produto não encontrado para edição.", "erro");
                        return;
                    }

                    const produtoAtualizado = {
                        codigo: codigo,
                        nome: nome,
                        preco: preco,
                        quantidade: quantidade,
                        estoqueMinimo: estoqueMinimo,
                        categoriaId: categoriaId,
                        fornecedorId: fornecedorId
                    };

                    try {
                        const produtoEditado = await editarProdutoApi(
                            codigoProdutoEmEdicao,
                            converterProdutoTelaParaApi(produtoAtualizado)
                        );

                        aplicarDadosProduto(produtoParaEditar, converterProdutoApiParaTela(produtoEditado));
                        exibirMensagem("Produto editado com sucesso pela API.", "sucesso");
                    } catch (erro) {
                        exibirMensagem(erro.message, "erro");
                        return;
                    }

                    limparModoEdicao();
                }

                aplicarFiltroRapido();
                atualizarIndicadores(produtos);
                elementos.formulario.reset();
                elementos.campoCodigo.focus();
            });
    });

    elementos.botaoLimparFormulario.addEventListener("click", function () {
        limparModoEdicao();
        exibirMensagem("", "");
    });

    elementos.botaoBuscar.addEventListener("click", async function () {
        await buscarProdutoComCarregamento();
    });

    elementos.formularioBuscaProduto.addEventListener("submit", async function (event) {
        event.preventDefault();
        await buscarProdutoComCarregamento();
    });

    elementos.botaoLimparBusca.addEventListener("click", function () {
        limparBusca();
    });

    if (elementos.campoFiltroRapidoProdutos instanceof HTMLInputElement) {
        elementos.campoFiltroRapidoProdutos.addEventListener("input", function () {
            aplicarFiltroRapido();
        });
    }

    function validarProduto(codigoTexto, nome, precoTexto, quantidadeTexto, estoqueMinimoTexto, categoriaIdTexto, codigo, preco, quantidade, estoqueMinimo, categoriaId) {
        if (codigoTexto === "" || Number.isNaN(codigo) || !Number.isInteger(codigo) || codigo <= 0) {
            exibirMensagem("Informe um código válido.", "erro");
            elementos.campoCodigo.focus();
            return false;
        }

        if (nome === "") {
            exibirMensagem("Informe o nome do produto.", "erro");
            elementos.campoNome.focus();
            return false;
        }

        if (precoTexto === "" || Number.isNaN(preco) || preco <= 0) {
            exibirMensagem("Informe um preço válido.", "erro");
            elementos.campoPreco.focus();
            return false;
        }

        if (quantidadeTexto === "" || Number.isNaN(quantidade) || !Number.isInteger(quantidade)) {
            exibirMensagem("Informe a quantidade do produto.", "erro");
            elementos.campoQuantidade.focus();
            return false;
        }

        if (quantidade < 0) {
            exibirMensagem("A quantidade não pode ser negativa.", "erro");
            elementos.campoQuantidade.focus();
            return false;
        }

        if (estoqueMinimoTexto !== "" && (Number.isNaN(estoqueMinimo) || !Number.isInteger(estoqueMinimo))) {
            exibirMensagem("Informe um estoque mínimo válido.", "erro");
            elementos.campoEstoqueMinimo.focus();
            return false;
        }

        if (estoqueMinimo < 0) {
            exibirMensagem("O estoque mínimo não pode ser negativo.", "erro");
            elementos.campoEstoqueMinimo.focus();
            return false;
        }

        if (categoriaIdTexto === "" || !Number.isInteger(categoriaId) || categoriaId <= 0) {
            exibirMensagem("Selecione uma categoria válida.", "erro");
            elementos.campoCategoriaProduto.focus();
            return false;
        }

        if (codigoProdutoEmEdicao === null) {
            for (const produto of produtos) {
                if (produto.codigo === codigo) {
                    exibirMensagem("Já existe um produto com esse código.", "erro");
                    elementos.campoCodigo.focus();
                    return false;
                }
            }
        }

        return true;
    }

    function limparBusca() {
        elementos.campoCodigoBusca.value = "";
        aplicarFiltroRapido();
        exibirMensagem("", "");
        elementos.campoCodigoBusca.focus();
    }

    function aplicarFiltroRapido() {
        const termoFiltro = normalizarTextoParaBusca(elementos.campoFiltroRapidoProdutos?.value);

        if (termoFiltro === "") {
            atualizarTabela(produtos, editarProduto, removerProduto);
            return;
        }

        const listaFiltrada = filtrarProdutosPorTermo(termoFiltro);

        atualizarTabela(produtos, editarProduto, removerProduto, {
            lista: listaFiltrada,
            mensagemVazia: "Nenhum produto encontrado para este filtro.",
            textoAcaoVazia: "Limpar filtro",
            acaoVazia: function () {
                if (elementos.campoFiltroRapidoProdutos instanceof HTMLInputElement) {
                    elementos.campoFiltroRapidoProdutos.value = "";
                    elementos.campoFiltroRapidoProdutos.focus();
                }

                atualizarTabela(produtos, editarProduto, removerProduto);
            }
        });
    }

    function filtrarProdutosPorTermo(termoFiltro) {
        return produtos.filter(function (produto) {
            const codigo = String(produto.codigo);
            const nome = normalizarTextoParaBusca(produto.nome);
            const categoria = normalizarTextoParaBusca(produto.categoriaNome);
            const fornecedor = normalizarTextoParaBusca(produto.fornecedorNome);

            return codigo.includes(termoFiltro)
                || nome.includes(termoFiltro)
                || categoria.includes(termoFiltro)
                || fornecedor.includes(termoFiltro);
        });
    }

    async function buscarProdutoComCarregamento() {
        await executarComBotaoCarregando(
            elementos.botaoBuscar,
            "Buscando...",
            async function () {
                await buscarProduto();
            });
    }

    async function buscarProduto(termoBuscaInformado) {
        const termoBusca = normalizarTextoParaBusca(
            typeof termoBuscaInformado === "string" ? termoBuscaInformado : elementos.campoCodigoBusca.value
        );

        if (termoBusca === "") {
            exibirMensagem("Informe um código ou nome para buscar.", "erro");
            elementos.campoCodigoBusca.focus();
            return;
        }

        const codigoBuscado = Number(termoBusca);
        const buscaPorCodigo = !Number.isNaN(codigoBuscado) && codigoBuscado > 0;

        if (buscaPorCodigo) {
            try {
                const produtoApi = await buscarProdutoPorCodigoApi(codigoBuscado);
                const produtoEncontrado = converterProdutoApiParaTela(produtoApi);
                const produtoSincronizado = upsertProdutoNoArray(produtoEncontrado);

                atualizarTabela([produtoSincronizado], editarProduto, removerProduto);
                exibirMensagem("Busca concluída: 1 resultado(s).", "sucesso");
                return;
            } catch (erro) {
                if (!(erro instanceof TypeError)) {
                    atualizarTabela([], editarProduto, removerProduto);
                    exibirMensagem("Nenhum produto encontrado.", "erro");
                    return;
                }
            }
        }

        const resultados = produtos.filter(function (produto) {
            const nomeProduto = normalizarTextoParaBusca(produto.nome);

            if (buscaPorCodigo && produto.codigo === codigoBuscado) {
                return true;
            }

            return nomeProduto.includes(termoBusca);
        });

        if (resultados.length === 0) {
            atualizarTabela([], editarProduto, removerProduto);
            exibirMensagem("Nenhum produto encontrado.", "erro");
            return;
        }

        atualizarTabela(resultados, editarProduto, removerProduto);
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

        preencherFormularioEdicaoProduto(produtoEncontrado);

        elementos.campoCodigo.disabled = true;
        elementos.campoQuantidade.disabled = true;
        elementos.botaoSalvarProduto.textContent = "Salvar alteração";
        elementos.campoNome.focus();

        exibirMensagem("Edite os dados do produto. Para alterar o estoque, registre uma movimentação.", "sucesso");
    }

    function preencherFormularioEdicaoProduto(produto) {
        elementos.campoCodigo.value = produto.codigo;
        elementos.campoNome.value = produto.nome;
        elementos.campoPreco.value = produto.preco;
        elementos.campoQuantidade.value = produto.quantidade;
        elementos.campoEstoqueMinimo.value = produto.estoqueMinimo;
        elementos.campoCategoriaProduto.value = produto.categoriaId;
        elementos.campoFornecedorProduto.value = produto.fornecedorId || "";
    }

    function limparModoEdicao() {
        codigoProdutoEmEdicao = null;
        elementos.campoCodigo.disabled = false;
        elementos.campoQuantidade.disabled = false;
        elementos.botaoSalvarProduto.textContent = "Cadastrar produto";
    }

    async function removerProduto(codigo) {
        const indiceProduto = produtos.findIndex(function (produto) {
            return produto.codigo === codigo;
        });

        if (indiceProduto === -1) {
            exibirMensagem("Produto não encontrado para remoção.", "erro");
            return;
        }

        const produto = produtos[indiceProduto];
        const confirmarRemocao = confirm(
            `Deseja realmente remover o produto ${produto.codigo} - ${produto.nome}? Esta ação não pode ser desfeita.`
        );

        if (!confirmarRemocao) {
            return;
        }

        try {
            await removerProdutoApi(codigo);

            produtos.splice(indiceProduto, 1);
            exibirMensagem("Produto removido com sucesso pela API.", "sucesso");
        } catch (erro) {
            exibirMensagem(erro.message, "erro");
            return;
        }

        aplicarFiltroRapido();
        atualizarIndicadores(produtos);
    }

    async function carregarProdutos() {
        try {
            await atualizarProdutosDaApi();
        } catch (erro) {
            exibirMensagem(erro.message, "erro");
        }
    }

    async function atualizarProdutosDaApi() {
        const produtosApi = await listarProdutosApi();
        const produtosConvertidos = produtosApi.map(function (produto) {
            return converterProdutoApiParaTela(produto);
        });

        produtos.length = 0;
        produtos.push(...produtosConvertidos);

        aplicarFiltroRapido();
        atualizarIndicadores(produtos);

        if (!contextoUrlAplicado) {
            await aplicarContextoDaUrl();
            contextoUrlAplicado = true;
        }
    }

    async function aplicarContextoDaUrl() {
        const parametros = new URLSearchParams(window.location.search);
        const codigoEdicaoTexto = parametros.get("codigoEdicao");
        const buscaTexto = parametros.get("busca");

        if (codigoEdicaoTexto !== null && codigoEdicaoTexto.trim() !== "") {
            const codigoEdicao = Number(codigoEdicaoTexto);

            if (Number.isInteger(codigoEdicao) && codigoEdicao > 0) {
                editarProduto(codigoEdicao);
                return;
            }
        }

        if (buscaTexto !== null && buscaTexto.trim() !== "") {
            elementos.campoCodigoBusca.value = buscaTexto;
            await buscarProduto(buscaTexto);
        }
    }

    async function carregarCategoriasDoProduto() {
        try {
            const categorias = await listarCategoriasApi();
            atualizarSelectCategorias(categorias, elementos.campoCategoriaProduto.value);
        } catch {
            atualizarSelectCategorias([], "");
        }
    }

    async function carregarFornecedoresDoProduto() {
        try {
            const fornecedores = await listarFornecedoresApi();
            atualizarSelectFornecedores(fornecedores, elementos.campoFornecedorProduto.value);
        } catch {
            atualizarSelectFornecedores([], "");
        }
    }

    function converterProdutoApiParaTela(produtoApi) {
        return {
            codigo: produtoApi.codigo,
            nome: produtoApi.nome,
            preco: produtoApi.precoUnitario,
            quantidade: produtoApi.quantidadeEstoque,
            estoqueMinimo: typeof produtoApi.estoqueMinimo === "number" ? produtoApi.estoqueMinimo : 0,
            categoriaId: produtoApi.categoriaId,
            categoriaNome: produtoApi.categoria ? produtoApi.categoria.nome : "Sem categoria",
            fornecedorId: produtoApi.fornecedorId,
            fornecedorNome: produtoApi.fornecedor ? produtoApi.fornecedor.nome : "Sem fornecedor"
        };
    }

    function converterProdutoTelaParaApi(produto) {
        return {
            codigo: produto.codigo,
            nome: produto.nome,
            precoUnitario: produto.preco,
            quantidadeEstoque: produto.quantidade,
            estoqueMinimo: produto.estoqueMinimo,
            categoriaId: produto.categoriaId,
            fornecedorId: produto.fornecedorId
        };
    }

    function aplicarDadosProduto(produto, novosDados) {
        produto.nome = novosDados.nome;
        produto.preco = novosDados.preco;
        produto.quantidade = novosDados.quantidade;
        produto.estoqueMinimo = typeof novosDados.estoqueMinimo === "number" ? novosDados.estoqueMinimo : 0;
        produto.categoriaId = novosDados.categoriaId;
        produto.categoriaNome = novosDados.categoriaNome;
        produto.fornecedorId = novosDados.fornecedorId;
        produto.fornecedorNome = novosDados.fornecedorNome;
    }

    function upsertProdutoNoArray(produto) {
        const produtoExistente = produtos.find(function (item) {
            return item.codigo === produto.codigo;
        });

        if (produtoExistente !== undefined) {
            aplicarDadosProduto(produtoExistente, produto);
            return produtoExistente;
        }

        produtos.push(produto);
        return produto;
    }
}
