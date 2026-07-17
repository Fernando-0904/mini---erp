function inicializarProdutoController() {
    const produtos = [];
    let codigoProdutoEmEdicao = null;

    window.recarregarProdutosNaTela = atualizarProdutosDaApi;
    window.recarregarCategoriasDoProduto = carregarCategoriasDoProduto;

    carregarProdutos();
    carregarCategoriasDoProduto();

    elementos.formulario.addEventListener("submit", async function (event) {
        event.preventDefault();

        const codigoTexto = elementos.campoCodigo.value.trim();
        const nome = elementos.campoNome.value.trim();
        const precoTexto = elementos.campoPreco.value.trim();
        const quantidadeTexto = elementos.campoQuantidade.value.trim();
        const estoqueMinimoTexto = elementos.campoEstoqueMinimo.value.trim();
        const categoriaIdTexto = elementos.campoCategoriaProduto.value;

        const codigo = Number(codigoTexto);
        const preco = Number(precoTexto);
        const quantidade = Number(quantidadeTexto);
        const estoqueMinimo = estoqueMinimoTexto === "" ? 0 : Number(estoqueMinimoTexto);
        const categoriaId = Number(categoriaIdTexto);

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
                categoriaId: categoriaId
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
                categoriaId: categoriaId
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

        atualizarTabela(produtos, editarProduto, removerProduto);
        atualizarIndicadores(produtos);
        elementos.formulario.reset();
        elementos.campoCodigo.focus();
    });

    elementos.botaoLimparFormulario.addEventListener("click", function () {
        limparModoEdicao();
        exibirMensagem("", "");
    });

    elementos.botaoBuscar.addEventListener("click", function () {
        buscarProduto();
    });

    elementos.formularioBuscaProduto.addEventListener("submit", function (event) {
        event.preventDefault();
        buscarProduto();
    });

    elementos.botaoLimparBusca.addEventListener("click", function () {
        limparBusca();
    });

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
        atualizarTabela(produtos, editarProduto, removerProduto);
        exibirMensagem("", "");
        elementos.campoCodigoBusca.focus();
    }

    async function buscarProduto() {
        const termoBusca = elementos.campoCodigoBusca.value.trim().toLowerCase();

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
            const nomeProduto = produto.nome.toLowerCase();

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

        elementos.campoCodigo.value = produtoEncontrado.codigo;
        elementos.campoNome.value = produtoEncontrado.nome;
        elementos.campoPreco.value = produtoEncontrado.preco;
        elementos.campoQuantidade.value = produtoEncontrado.quantidade;
        elementos.campoEstoqueMinimo.value = produtoEncontrado.estoqueMinimo;
        elementos.campoCategoriaProduto.value = produtoEncontrado.categoriaId;

        elementos.campoCodigo.disabled = true;
        elementos.campoQuantidade.disabled = true;
        elementos.botaoSalvarProduto.textContent = "Salvar alteração";
        elementos.campoNome.focus();

        exibirMensagem("Edite os dados do produto. Para alterar o estoque, registre uma movimentação.", "sucesso");
    }

    function limparModoEdicao() {
        codigoProdutoEmEdicao = null;
        elementos.campoCodigo.disabled = false;
        elementos.campoQuantidade.disabled = false;
        elementos.botaoSalvarProduto.textContent = "Cadastrar produto";
    }

    async function removerProduto(codigo) {
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

        try {
            await removerProdutoApi(codigo);

            produtos.splice(indiceProduto, 1);
            exibirMensagem("Produto removido com sucesso pela API.", "sucesso");
        } catch (erro) {
            exibirMensagem(erro.message, "erro");
            return;
        }

        atualizarTabela(produtos, editarProduto, removerProduto);
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

        produtos.length = 0;

        for (const produto of produtosApi) {
            produtos.push(converterProdutoApiParaTela(produto));
        }

        atualizarTabela(produtos, editarProduto, removerProduto);
        atualizarIndicadores(produtos);
    }

    async function carregarCategoriasDoProduto() {
        try {
            const categorias = await listarCategoriasApi();
            atualizarSelectCategorias(categorias, elementos.campoCategoriaProduto.value);
        } catch {
            atualizarSelectCategorias([], "");
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
            categoriaNome: produtoApi.categoria ? produtoApi.categoria.nome : "Sem categoria"
        };
    }

    function converterProdutoTelaParaApi(produto) {
        return {
            codigo: produto.codigo,
            nome: produto.nome,
            precoUnitario: produto.preco,
            quantidadeEstoque: produto.quantidade,
            estoqueMinimo: produto.estoqueMinimo,
            categoriaId: produto.categoriaId
        };
    }

    function aplicarDadosProduto(produto, novosDados) {
        produto.nome = novosDados.nome;
        produto.preco = novosDados.preco;
        produto.quantidade = novosDados.quantidade;
        produto.estoqueMinimo = typeof novosDados.estoqueMinimo === "number" ? novosDados.estoqueMinimo : 0;
        produto.categoriaId = novosDados.categoriaId;
        produto.categoriaNome = novosDados.categoriaNome;
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
