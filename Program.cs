using projetoerp;

int opcao = -1;
ProdutoService produtoService = new ProdutoService();

while (opcao != 0)
{
    ProdutoConsole.ExibirMenu();
    ProdutoConsole.LerOpcao(out opcao);

    switch (opcao)
    {
        case 1:
            Console.WriteLine("Cadastrar produto:");

            if (!ProdutoConsole.LerCodigo("Codigo: ", "Codigo deve ser um número inteiro", out int codigo))
            {
                break;
            }

            if (produtoService.BuscarPorCodigo(codigo) != null)
            {
                Console.WriteLine("Código já existe.");
                break;
            }

            if (!ProdutoConsole.LerDadosProduto(out string nome, out decimal precoUnitario, out int quantidadeEstoque))
            {
                break;
            }

            Produto produto = new Produto
            {
                Codigo = codigo,
                Nome = nome,
                PrecoUnitario = precoUnitario,
                QuantidadeEstoque = quantidadeEstoque
            };

            produtoService.CadastrarProduto(produto);
            Console.WriteLine("Produto cadastrado com sucesso!");
            break;
        case 2:
            Console.WriteLine("Listar produtos:");

            List<Produto> produtos = produtoService.ListarProdutos();

            if (produtos.Count == 0)
            {
                Console.WriteLine("Nenhum produto cadastrado.");
                break;
            }

            Console.WriteLine($"Total de produtos cadastrados: {produtos.Count}");
            ProdutoConsole.ExibirProdutos(produtos);
            break;
        case 3:
            Console.WriteLine("Buscar produto por código");

            if (!ProdutoConsole.LerCodigo("Digite o código do produto: ", "Código inválido.", out int codigoBuscado))
            {
                break;
            }

            Produto? produtoEncontradoPorCodigo = produtoService.BuscarPorCodigo(codigoBuscado);

            if (produtoEncontradoPorCodigo == null)
            {
                Console.WriteLine("Produto não encontrado.");
                break;
            }

            ProdutoConsole.ExibirProduto(produtoEncontradoPorCodigo);
            break;
        case 4:
            Console.WriteLine("Calcular valor total do estoque:");

            if (produtoService.ListarProdutos().Count == 0)
            {
                Console.WriteLine("Nenhum produto cadastrado.");
                break;
            }

            Console.WriteLine($"Produtos considerados no cálculo: {produtoService.ListarProdutos().Count}");
            Console.WriteLine($"Valor total do estoque: {produtoService.CalcularValorTotalEstoque():F2}");
            break;
        case 5:
            Console.WriteLine("Editar produto");

            if (produtoService.ListarProdutos().Count == 0)
            {
                Console.WriteLine("Nenhum produto cadastrado.");
                break;
            }

            if (!ProdutoConsole.LerCodigo("Digite o código do produto que deseja editar: ", "Codigo invalido.", out int codigoEditar))
            {
                break;
            }

            Produto? produtoParaEditar = produtoService.BuscarPorCodigo(codigoEditar);

            if (produtoParaEditar == null)
            {
                Console.WriteLine("Produto não encontrado.");
                break;
            }

            Console.WriteLine("Produto encontrado. Proximo passo, editar os dados do produto.");

            if (!ProdutoConsole.LerDadosEdicao(out string novoNome, out decimal novoPrecoUnitario, out int novaQuantidadeEstoque))
            {
                break;
            }

            produtoService.EditarProduto(codigoEditar, novoNome, novoPrecoUnitario, novaQuantidadeEstoque);
            Console.WriteLine("Produto editado com sucesso!");
            break;
        case 6:
            Console.WriteLine("Remover produto");

            if (produtoService.ListarProdutos().Count == 0)
            {
                Console.WriteLine("Nenhum produto cadastrado.");
                break;
            }

            if (!ProdutoConsole.LerCodigo("Digite o código do produto que deseja remover: ", "Codigo invalido.", out int codigoRemover))
            {
                break;
            }

            Produto? produtoParaRemover = produtoService.BuscarPorCodigo(codigoRemover);

            if (produtoParaRemover == null)
            {
                Console.WriteLine("Produto não encontrado.");
                break;
            }

            if (!ProdutoConsole.ConfirmarRemocao(produtoParaRemover))
            {
                Console.WriteLine("Remoção cancelada.");
                break;
            }

            produtoService.RemoverProduto(codigoRemover);
            Console.WriteLine("Produto removido com sucesso!");
            break;
        case 7:
            Console.WriteLine("Resumo do Estoque");

            if (produtoService.ListarProdutos().Count == 0)
            {
                Console.WriteLine("Nenhum produto cadastrado.");
                break;
            }

            Console.WriteLine($"Total de tipos de produtos: {produtoService.ListarProdutos().Count}");
            Console.WriteLine($"Total de itens em estoque: {produtoService.CalcularTotalItensEstoque()}");
            Console.WriteLine($"Valor total do estoque: {produtoService.CalcularValorTotalEstoque():F2}");
            break;
        case 8:
            Console.WriteLine("Produtos com estoque baixo");

            if (produtoService.ListarProdutos().Count == 0)
            {
                Console.WriteLine("Nenhum produto cadastrado.");
                break;
            }

            if (!ProdutoConsole.LerLimiteEstoque(out int limite))
            {
                break;
            }

            List<Produto> produtosComEstoqueBaixo = produtoService.ListarProdutosComEstoqueBaixo(limite);

            if (produtosComEstoqueBaixo.Count == 0)
            {
                Console.WriteLine("Nenhum produto com estoque abaixo do limite.");
                break;
            }

            ProdutoConsole.ExibirProdutos(produtosComEstoqueBaixo, false);
            break;
        case 9:
            Console.WriteLine("Buscar produto por nome");

            if (produtoService.ListarProdutos().Count == 0)
            {
                Console.WriteLine("Nenhum produto cadastrado.");
                break;
            }

            string termoBuscado = ProdutoConsole.LerTexto("Digite o nome do produto: ");

            if (string.IsNullOrWhiteSpace(termoBuscado))
            {
                Console.WriteLine("O nome do produto não pode ser vazio.");
                break;
            }

            List<Produto> produtosEncontradosPorNome = produtoService.BuscarPorNome(termoBuscado);

            if (produtosEncontradosPorNome.Count == 0)
            {
                Console.WriteLine("Nenhum produto encontrado com esse nome.");
                break;
            }

            ProdutoConsole.ExibirProdutos(produtosEncontradosPorNome);
            break;
        case 0:
            Console.WriteLine("Saindo do programa...");
            break;
        default:
            Console.WriteLine("Opção inválida");
            break;
    }

    Console.WriteLine();
}
