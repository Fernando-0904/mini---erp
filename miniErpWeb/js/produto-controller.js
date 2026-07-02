function inicializarProdutoController() {
    const produtos = [];
    let codigoProdutoEmEdicao = null;

    carregarProdutos();

    elementos.formulario.addEventListener("submit", async function (event) {
        event.preventDefault();

        const codigoTexto = elementos.campoCodigo.value.trim();
        const nome = elementos.campoNome.value.trim();
        const precoTexto = elementos.campoPreco.value.trim();
        const quantidadeTexto = elementos.campoQuantidade.value.trim();

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

            try {
                const produtoCadastrado = await cadastrarProdutoApi(converterProdutoTelaParaApi(produto));

                produtos.push(converterProdutoApiParaTela(produtoCadastrado));
                exibirMensagem("Produto cadastrado com sucesso pela API.", "sucesso");
            } catch (erro) {
                if (!(erro instanceof TypeError)) {
                    exibirMensagem(erro.message, "erro");
                    return;
                }

                produtos.push(produto);
                exibirMensagem("API indisponível. Produto cadastrado no navegador.", "erro");
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
                quantidade: quantidade
            };

            try {
                const produtoEditado = await editarProdutoApi(
                    codigoProdutoEmEdicao,
                    converterProdutoTelaParaApi(produtoAtualizado)
                );

                aplicarDadosProduto(produtoParaEditar, converterProdutoApiParaTela(produtoEditado));
                exibirMensagem("Produto editado com sucesso pela API.", "sucesso");
            } catch (erro) {
                if (!(erro instanceof TypeError)) {
                    exibirMensagem(erro.message, "erro");
                    return;
                }

                aplicarDadosProduto(produtoParaEditar, produtoAtualizado);
                exibirMensagem("API indisponível. Produto editado no navegador.", "erro");
            }

            limparModoEdicao();
        }

        salvarProdutos();
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

    function validarProduto(codigoTexto, nome, precoTexto, quantidadeTexto, codigo, preco, quantidade) {
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

                atualizarTabela([produtoEncontrado], editarProduto, removerProduto);
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

        elementos.campoCodigo.disabled = true;
        elementos.botaoSalvarProduto.textContent = "Salvar alteração";
        elementos.campoNome.focus();

        exibirMensagem("Edite os dados do produto e salve a alteração.", "sucesso");
    }

    function limparModoEdicao() {
        codigoProdutoEmEdicao = null;
        elementos.campoCodigo.disabled = false;
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
            if (!(erro instanceof TypeError)) {
                exibirMensagem(erro.message, "erro");
                return;
            }

            produtos.splice(indiceProduto, 1);
            exibirMensagem("API indisponível. Produto removido no navegador.", "erro");
        }

        salvarProdutos();
        atualizarTabela(produtos, editarProduto, removerProduto);
        atualizarIndicadores(produtos);
    }

    function salvarProdutos() {
        salvarProdutosNoStorage(produtos);
    }

    async function carregarProdutos() {
        try {
            const produtosApi = await listarProdutosApi();

            for (const produto of produtosApi) {
                produtos.push(converterProdutoApiParaTela(produto));
            }

            atualizarTabela(produtos, editarProduto, removerProduto);
            atualizarIndicadores(produtos);
            return;
        } catch {
            exibirMensagem("API indisponível. Os dados foram carregados do navegador.", "erro");
        }

        const produtosSalvos = carregarProdutosDoStorage();

        for (const produto of produtosSalvos) {
            produtos.push(produto);
        }

        atualizarTabela(produtos, editarProduto, removerProduto);
        atualizarIndicadores(produtos);
    }

    function converterProdutoApiParaTela(produtoApi) {
        return {
            codigo: produtoApi.codigo,
            nome: produtoApi.nome,
            preco: produtoApi.precoUnitario,
            quantidade: produtoApi.quantidadeEstoque
        };
    }

    function converterProdutoTelaParaApi(produto) {
        return {
            codigo: produto.codigo,
            nome: produto.nome,
            precoUnitario: produto.preco,
            quantidadeEstoque: produto.quantidade
        };
    }

    function aplicarDadosProduto(produto, novosDados) {
        produto.nome = novosDados.nome;
        produto.preco = novosDados.preco;
        produto.quantidade = novosDados.quantidade;
    }
}
