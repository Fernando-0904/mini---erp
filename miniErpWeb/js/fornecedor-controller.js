function inicializarFornecedorController() {
    const fornecedores = [];
    let idFornecedorEmEdicao = null;

    carregarFornecedores();

    elementos.formularioFornecedor.addEventListener("submit", async function (event) {
        event.preventDefault();

        const codigoTexto = elementos.campoFornecedorCodigo.value.trim();
        const nome = elementos.campoFornecedorNome.value.trim();
        const documento = elementos.campoFornecedorDocumento.value.trim();
        const email = elementos.campoFornecedorEmail.value.trim();
        const codigo = Number(codigoTexto);

        if (!validarFornecedor(codigoTexto, nome, documento, email, codigo)) {
            return;
        }

        const fornecedor = {
            codigo: codigo,
            nome: nome,
            documento: documento,
            email: email,
            ativo: elementos.campoFornecedorAtivo.checked
        };

        try {
            if (idFornecedorEmEdicao === null) {
                const fornecedorCadastrado = await cadastrarFornecedorApi(fornecedor);
                fornecedores.push(fornecedorCadastrado);
                exibirMensagem("Fornecedor cadastrado com sucesso.", "sucesso");
            } else {
                const fornecedorEditado = await editarFornecedorApi(idFornecedorEmEdicao, fornecedor);
                const indiceFornecedor = fornecedores.findIndex(function (item) {
                    return item.id === idFornecedorEmEdicao;
                });

                fornecedores[indiceFornecedor] = fornecedorEditado;
                limparModoEdicaoFornecedor();
                exibirMensagem("Fornecedor editado com sucesso.", "sucesso");
            }

            atualizarTabelaFornecedores(fornecedores, editarFornecedor, removerFornecedor);
            await recarregarFornecedoresDoProduto();
            elementos.formularioFornecedor.reset();
            elementos.campoFornecedorCodigo.focus();
        } catch (erro) {
            exibirMensagem(erro.message, "erro");
        }
    });

    elementos.botaoLimparFornecedor.addEventListener("click", function () {
        limparModoEdicaoFornecedor();
        exibirMensagem("", "");
    });

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

        if (email === "" || !elementos.campoFornecedorEmail.validity.valid) {
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

        atualizarTabelaFornecedores(fornecedores, editarFornecedor, removerFornecedor);
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
        elementos.campoFornecedorCodigo.value = fornecedor.codigo;
        elementos.campoFornecedorNome.value = fornecedor.nome;
        elementos.campoFornecedorDocumento.value = fornecedor.documento;
        elementos.campoFornecedorEmail.value = fornecedor.email;
        elementos.campoFornecedorAtivo.checked = fornecedor.ativo;
        elementos.botaoSalvarFornecedor.textContent = "Salvar alteração";
        elementos.campoFornecedorCodigo.focus();

        exibirMensagem("Edite os dados do fornecedor e salve a alteração.", "sucesso");
    }

    async function removerFornecedor(id) {
        if (!confirm("Deseja realmente remover este fornecedor?")) {
            return;
        }

        const indiceFornecedor = fornecedores.findIndex(function (item) {
            return item.id === id;
        });

        if (indiceFornecedor === -1) {
            exibirMensagem("Fornecedor não encontrado para remoção.", "erro");
            return;
        }

        try {
            await removerFornecedorApi(id);
            fornecedores.splice(indiceFornecedor, 1);
            atualizarTabelaFornecedores(fornecedores, editarFornecedor, removerFornecedor);
            await recarregarFornecedoresDoProduto();
            exibirMensagem("Fornecedor removido com sucesso.", "sucesso");
        } catch (erro) {
            exibirMensagem(erro.message, "erro");
        }
    }

    function limparModoEdicaoFornecedor() {
        idFornecedorEmEdicao = null;
        elementos.botaoSalvarFornecedor.textContent = "Cadastrar fornecedor";
    }
}