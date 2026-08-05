function inicializarCategoriaController() {
    const categorias = [];
    let idCategoriaEmEdicao = null;

    carregarCategorias();

    elementos.formularioCategoria.addEventListener("submit", async function (event) {
        event.preventDefault();

        await executarComBotaoCarregando(
            elementos.botaoSalvarCategoria,
            idCategoriaEmEdicao === null ? "Cadastrando..." : "Salvando...",
            async function () {
                const idTexto = elementos.campoCategoriaId.value.trim();
                const nome = elementos.campoCategoriaNome.value.trim();
                const id = Number(idTexto);

                if (!validarCategoria(idTexto, nome, id)) {
                    return;
                }

                if (idCategoriaEmEdicao === null) {
                    const categoria = {
                        id: idTexto === "" ? 0 : id,
                        nome: nome
                    };

                    try {
                        const categoriaCadastrada = await cadastrarCategoriaApi(categoria);
                        upsertCategoriaNoArray(categoriaCadastrada);
                        await atualizarCategoriasDoProduto();
                        exibirMensagem("Categoria cadastrada com sucesso.", "sucesso");
                    } catch (erro) {
                        exibirMensagem(erro.message, "erro");
                        return;
                    }
                } else {
                    const categoriaExistente = categorias.find(function (categoria) {
                        return categoria.id === idCategoriaEmEdicao;
                    });

                    if (categoriaExistente === undefined) {
                        exibirMensagem("Categoria não encontrada para edição.", "erro");
                        return;
                    }

                    const categoriaAtualizada = {
                        id: idCategoriaEmEdicao,
                        nome: nome
                    };

                    try {
                        const categoriaEditada = await editarCategoriaApi(idCategoriaEmEdicao, categoriaAtualizada);
                        aplicarDadosCategoria(categoriaExistente, categoriaEditada);
                        await atualizarCategoriasDoProduto();
                        exibirMensagem("Categoria editada com sucesso.", "sucesso");
                    } catch (erro) {
                        exibirMensagem(erro.message, "erro");
                        return;
                    }

                    limparModoEdicaoCategoria();
                }

                aplicarFiltroRapidoCategorias();
                elementos.formularioCategoria.reset();
                elementos.campoCategoriaId.focus();
            });
    });

    elementos.botaoLimparCategoria.addEventListener("click", function () {
        limparModoEdicaoCategoria();
        exibirMensagem("", "");
    });

    if (elementos.campoFiltroRapidoCategorias instanceof HTMLInputElement) {
        elementos.campoFiltroRapidoCategorias.addEventListener("input", function () {
            aplicarFiltroRapidoCategorias();
        });
    }

    function validarCategoria(idTexto, nome, id) {
        if (idCategoriaEmEdicao === null) {
            if (idTexto !== "" && (Number.isNaN(id) || !Number.isInteger(id) || id < 0)) {
                exibirMensagem("Informe um ID válido ou deixe em branco.", "erro");
                elementos.campoCategoriaId.focus();
                return false;
            }
        }

        if (nome === "") {
            exibirMensagem("Informe o nome da categoria.", "erro");
            elementos.campoCategoriaNome.focus();
            return false;
        }

        return true;
    }

    async function carregarCategorias() {
        try {
            const categoriasApi = await listarCategoriasApi();

            categorias.length = 0;

            for (const categoria of categoriasApi) {
                categorias.push(categoria);
            }

            aplicarFiltroRapidoCategorias();
        } catch (erro) {
            aplicarFiltroRapidoCategorias();
            exibirMensagem(erro.message, "erro");
        }
    }

    function aplicarFiltroRapidoCategorias() {
        const termo = normalizarTextoParaBusca(String(elementos.campoFiltroRapidoCategorias?.value || ""));

        if (termo === "") {
            atualizarTabelaCategorias(categorias, editarCategoria, removerCategoria);
            return;
        }

        const filtradas = categorias.filter(function (categoria) {
            const id = String(categoria.id);
            const nome = normalizarTextoParaBusca(categoria.nome);

            return id.includes(termo) || nome.includes(termo);
        });

        atualizarTabelaCategorias(categorias, editarCategoria, removerCategoria, {
            lista: filtradas,
            mensagemVazia: "Nenhuma categoria encontrada para este filtro.",
            textoAcaoVazia: "Limpar filtro",
            acaoVazia: function () {
                if (elementos.campoFiltroRapidoCategorias instanceof HTMLInputElement) {
                    elementos.campoFiltroRapidoCategorias.value = "";
                    elementos.campoFiltroRapidoCategorias.focus();
                }

                atualizarTabelaCategorias(categorias, editarCategoria, removerCategoria);
            }
        });
    }

    function editarCategoria(id) {
        const categoria = categorias.find(function (item) {
            return item.id === id;
        });

        if (categoria === undefined) {
            exibirMensagem("Categoria não encontrada para edição.", "erro");
            return;
        }

        idCategoriaEmEdicao = id;

        elementos.campoCategoriaId.value = categoria.id;
        elementos.campoCategoriaNome.value = categoria.nome;

        elementos.campoCategoriaId.disabled = true;
        elementos.botaoSalvarCategoria.textContent = "Salvar alteração";
        elementos.campoCategoriaNome.focus();

        exibirMensagem("Edite os dados da categoria e salve a alteração.", "sucesso");
    }

    function limparModoEdicaoCategoria() {
        idCategoriaEmEdicao = null;
        elementos.campoCategoriaId.disabled = false;
        elementos.botaoSalvarCategoria.textContent = "Cadastrar categoria";
    }

    async function removerCategoria(id) {
        const confirmarRemocao = confirm("Deseja realmente remover esta categoria?");

        if (!confirmarRemocao) {
            return;
        }

        const indiceCategoria = categorias.findIndex(function (categoria) {
            return categoria.id === id;
        });

        if (indiceCategoria === -1) {
            exibirMensagem("Categoria não encontrada para remoção.", "erro");
            return;
        }

        try {
            await removerCategoriaApi(id);
            categorias.splice(indiceCategoria, 1);
            await atualizarCategoriasDoProduto();
            exibirMensagem("Categoria removida com sucesso.", "sucesso");
        } catch (erro) {
            exibirMensagem(erro.message, "erro");
            return;
        }

        aplicarFiltroRapidoCategorias();
    }

    function upsertCategoriaNoArray(categoria) {
        const categoriaExistente = categorias.find(function (item) {
            return item.id === categoria.id;
        });

        if (categoriaExistente !== undefined) {
            aplicarDadosCategoria(categoriaExistente, categoria);
            return categoriaExistente;
        }

        categorias.push(categoria);
        return categoria;
    }

    function aplicarDadosCategoria(categoriaDestino, categoriaOrigem) {
        categoriaDestino.nome = categoriaOrigem.nome;
    }

    async function atualizarCategoriasDoProduto() {
        if (typeof window.recarregarCategoriasDoProduto === "function") {
            await window.recarregarCategoriasDoProduto();
        }
    }
}

function atualizarTabelaCategorias(listaCategorias, aoEditarCategoria, aoRemoverCategoria, opcoes = {}) {
    const listaRenderizacao = Array.isArray(opcoes.lista) ? opcoes.lista : listaCategorias;
    const mensagemVazia = typeof opcoes.mensagemVazia === "string"
        ? opcoes.mensagemVazia
        : "Nenhuma categoria cadastrada.";
    const textoAcaoVazia = typeof opcoes.textoAcaoVazia === "string"
        ? opcoes.textoAcaoVazia
        : "Cadastrar categoria";
    const acaoVazia = typeof opcoes.acaoVazia === "function"
        ? opcoes.acaoVazia
        : function () {
            focarElementoComSuavidade(elementos.campoCategoriaNome);
        };

    elementos.tabelaCategorias.innerHTML = "";

    if (listaRenderizacao.length === 0) {
        const linhaVazia = criarLinhaEstadoVazio(
            3,
            mensagemVazia,
            textoAcaoVazia,
            acaoVazia
        );

        elementos.tabelaCategorias.appendChild(linhaVazia);
        return;
    }

    for (const categoria of listaRenderizacao) {
        const linha = document.createElement("tr");

        linha.appendChild(criarCelulaCategoria(categoria.id));
        linha.appendChild(criarCelulaCategoria(categoria.nome));
        linha.appendChild(criarCelulaAcoesCategoria(categoria.id, aoEditarCategoria, aoRemoverCategoria));

        elementos.tabelaCategorias.appendChild(linha);
    }
}

function criarCelulaCategoria(texto) {
    const celula = document.createElement("td");
    celula.textContent = texto;
    return celula;
}

function criarCelulaAcoesCategoria(id, aoEditarCategoria, aoRemoverCategoria) {
    const celula = document.createElement("td");
    const botaoEditar = document.createElement("button");
    const botaoRemover = document.createElement("button");

    botaoEditar.type = "button";
    botaoEditar.textContent = "Editar";
    botaoEditar.addEventListener("click", function () {
        aoEditarCategoria(id);
    });

    botaoRemover.type = "button";
    botaoRemover.textContent = "Remover";
    botaoRemover.addEventListener("click", function () {
        aoRemoverCategoria(id);
    });

    celula.appendChild(botaoEditar);
    celula.appendChild(document.createTextNode(" "));
    celula.appendChild(botaoRemover);

    return celula;
}
