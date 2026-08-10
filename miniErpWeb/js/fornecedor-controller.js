function inicializarFornecedorController() {
    const fornecedores = [];
    let idFornecedorEmEdicao = null;

    carregarFornecedores();

    elementos.formularioFornecedor.addEventListener("submit", async function (event) {
        event.preventDefault();

        await executarComBotaoCarregando(
            elementos.botaoSalvarFornecedor,
            idFornecedorEmEdicao === null ? "Cadastrando..." : "Salvando...",
            async function () {
                const codigoTexto = elementos.campoFornecedorCodigo.value.trim();
                const nome = elementos.campoFornecedorNome.value.trim();
                const documento = elementos.campoFornecedorDocumento.value.trim();
                const email = elementos.campoFornecedorEmail.value.trim();
                const telefone = elementos.campoFornecedorTelefone.value.trim();
                const codigo = Number(codigoTexto);

                if (!validarFornecedor(codigoTexto, nome, documento, email, codigo)) {
                    return;
                }

                const fornecedor = {
                    codigo: codigo,
                    nome: nome,
                    documento: documento,
                    email: email,
                    telefone: telefone,
                    ativo: elementos.campoFornecedorAtivo.checked
                };

                try {
                    if (idFornecedorEmEdicao === null) {
                        const fornecedorCadastrado = await cadastrarFornecedorApi(fornecedor);
                        fornecedores.push(fornecedorCadastrado);
                        exibirMensagem("Fornecedor cadastrado com sucesso.", "sucesso");
                    } else {
                        const fornecedorEditado = await editarFornecedorApi(idFornecedorEmEdicao, fornecedor);
                        aplicarAtualizacaoFornecedorNoArray(idFornecedorEmEdicao, fornecedorEditado);
                        limparModoEdicaoFornecedor();
                        exibirMensagem("Fornecedor editado com sucesso.", "sucesso");
                    }

                    aplicarFiltroRapidoFornecedores();
                    await atualizarFornecedoresDoProduto();
                    elementos.formularioFornecedor.reset();
                    elementos.campoFornecedorCodigo.focus();
                } catch (erro) {
                    exibirMensagem(erro.message, "erro");
                }
            });
    });

    elementos.botaoLimparFornecedor.addEventListener("click", function () {
        limparModoEdicaoFornecedor();
        exibirMensagem("", "");
    });

    if (elementos.campoFiltroRapidoFornecedores instanceof HTMLInputElement) {
        elementos.campoFiltroRapidoFornecedores.addEventListener("input", function () {
            aplicarFiltroRapidoFornecedores();
        });
    }

    function validarFornecedor(codigoTexto, nome, documento, email, codigo) {
        if (codigoTexto === "" || Number.isNaN(codigo) || !Number.isInteger(codigo) || codigo <= 0) {
            exibirMensagem("Informe um código de fornecedor válido.", "erro");
            elementos.campoFornecedorCodigo.focus();
            return false;
        }

        if (nome === "") {
            exibirMensagem("Informe o nome do fornecedor.", "erro");
            elementos.campoFornecedorNome.focus();
            return false;
        }

        if (documento === "") {
            exibirMensagem("Informe o documento do fornecedor.", "erro");
            elementos.campoFornecedorDocumento.focus();
            return false;
        }

        if (email !== "" && !elementos.campoFornecedorEmail.validity.valid) {
            exibirMensagem("Informe um e-mail válido.", "erro");
            elementos.campoFornecedorEmail.focus();
            return false;
        }

        return true;
    }

    async function carregarFornecedores() {
        try {
            const fornecedoresApi = await listarFornecedoresApi();

            fornecedores.length = 0;
            fornecedores.push(...fornecedoresApi);
        } catch (erro) {
            exibirMensagem(erro.message, "erro");
        }

        aplicarFiltroRapidoFornecedores();
    }

    function aplicarFiltroRapidoFornecedores() {
        const termo = normalizarTextoParaBusca(String(elementos.campoFiltroRapidoFornecedores?.value || ""));

        if (termo === "") {
            atualizarTabelaFornecedores(fornecedores, editarFornecedor, inativarFornecedor, removerFornecedor);
            return;
        }

        const filtrados = filtrarFornecedoresPorTermo(termo);

        atualizarTabelaFornecedores(fornecedores, editarFornecedor, inativarFornecedor, removerFornecedor, {
            lista: filtrados,
            mensagemVazia: "Nenhum fornecedor encontrado para este filtro.",
            textoAcaoVazia: "Limpar filtro",
            acaoVazia: function () {
                if (elementos.campoFiltroRapidoFornecedores instanceof HTMLInputElement) {
                    elementos.campoFiltroRapidoFornecedores.value = "";
                    elementos.campoFiltroRapidoFornecedores.focus();
                }

                atualizarTabelaFornecedores(fornecedores, editarFornecedor, inativarFornecedor, removerFornecedor);
            }
        });
    }

    function filtrarFornecedoresPorTermo(termo) {
        return fornecedores.filter(function (fornecedor) {
            const codigo = String(fornecedor.codigo);
            const nome = normalizarTextoParaBusca(fornecedor.nome);
            const documento = normalizarTextoParaBusca(fornecedor.documento);
            const email = normalizarTextoParaBusca(fornecedor.email);
            const status = fornecedor.ativo ? "ativo" : "inativo";

            return codigo.includes(termo)
                || nome.includes(termo)
                || documento.includes(termo)
                || email.includes(termo)
                || status.includes(termo);
        });
    }

    function editarFornecedor(id) {
        const fornecedor = fornecedores.find(function (item) {
            return item.id === id;
        });

        if (fornecedor === undefined) {
            exibirMensagem("Fornecedor não encontrado para edição.", "erro");
            return;
        }

        idFornecedorEmEdicao = id;
        preencherFormularioEdicaoFornecedor(fornecedor);
        elementos.botaoSalvarFornecedor.textContent = "Salvar alteração";
        elementos.campoFornecedorCodigo.focus();

        exibirMensagem("Edite os dados do fornecedor e salve a alteração.", "sucesso");
    }

    function preencherFormularioEdicaoFornecedor(fornecedor) {
        elementos.campoFornecedorCodigo.value = fornecedor.codigo;
        elementos.campoFornecedorNome.value = fornecedor.nome;
        elementos.campoFornecedorDocumento.value = fornecedor.documento;
        elementos.campoFornecedorEmail.value = fornecedor.email;
        elementos.campoFornecedorTelefone.value = fornecedor.telefone;
        elementos.campoFornecedorAtivo.checked = fornecedor.ativo;
    }

    async function removerFornecedor(id) {
        const indiceFornecedor = fornecedores.findIndex(function (item) {
            return item.id === id;
        });

        if (indiceFornecedor === -1) {
            exibirMensagem("Fornecedor não encontrado para remoção.", "erro");
            return;
        }

        const fornecedor = fornecedores[indiceFornecedor];

        if (!confirm(`Deseja realmente remover o fornecedor ${fornecedor.codigo} - ${fornecedor.nome}? Esta ação não pode ser desfeita.`)) {
            return;
        }

        try {
            await removerFornecedorApi(id);
            fornecedores.splice(indiceFornecedor, 1);
            aplicarFiltroRapidoFornecedores();
            await atualizarFornecedoresDoProduto();
            exibirMensagem("Fornecedor removido com sucesso.", "sucesso");
        } catch (erro) {
            exibirMensagem(erro.message, "erro");
        }
    }

    async function inativarFornecedor(id) {
        const indiceFornecedor = fornecedores.findIndex(function (item) {
            return item.id === id;
        });

        if (indiceFornecedor === -1) {
            exibirMensagem("Fornecedor não encontrado para inativação.", "erro");
            return;
        }

        const fornecedor = fornecedores[indiceFornecedor];

        if (!confirm(`Deseja inativar o fornecedor ${fornecedor.codigo} - ${fornecedor.nome}?`)) {
            return;
        }

        try {
            const fornecedorInativado = await inativarFornecedorApi(id);
            fornecedores[indiceFornecedor] = fornecedorInativado;
            aplicarFiltroRapidoFornecedores();
            await atualizarFornecedoresDoProduto();
            exibirMensagem("Fornecedor inativado com sucesso.", "sucesso");
        } catch (erro) {
            exibirMensagem(erro.message, "erro");
        }
    }

    function limparModoEdicaoFornecedor() {
        idFornecedorEmEdicao = null;
        elementos.botaoSalvarFornecedor.textContent = "Cadastrar fornecedor";
    }

    function aplicarAtualizacaoFornecedorNoArray(idFornecedor, fornecedorAtualizado) {
        const indiceFornecedor = fornecedores.findIndex(function (item) {
            return item.id === idFornecedor;
        });

        if (indiceFornecedor >= 0) {
            fornecedores[indiceFornecedor] = fornecedorAtualizado;
        }
    }

    async function atualizarFornecedoresDoProduto() {
        if (typeof window.recarregarFornecedoresDoProduto === "function") {
            await window.recarregarFornecedoresDoProduto();
        }
    }
}