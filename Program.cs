
using projetoerp;

int opcao = -1;
List<Produto> produtos = new List<Produto>();

while (opcao != 0)
{
    Console.WriteLine("=====MENU=====");
    Console.WriteLine("1 - Cadastrar produto");
    Console.WriteLine("2 - Listar produtos");
    Console.WriteLine("3 - Buscar produto por código");
    Console.WriteLine("4 - Calcular valor total do estoque");
    Console.WriteLine("5 - Editar produto");
    Console.WriteLine("6 - Remover produto");
    Console.WriteLine("7 - Resumo do estoque");
    Console.WriteLine("8 - Produtos com estoque baixo");
    Console.WriteLine("9 - Buscar produtos por nome");
    Console.WriteLine("0 - Sair");
    Console.WriteLine("Escolha uma opção");
    string entradaOpcao = Console.ReadLine()!;

    if (!int.TryParse(entradaOpcao, out opcao))
    {
        opcao = -1;
        Console.WriteLine("Digite apenas números.");
    }

    switch (opcao)
    {
        case 1:
            Console.WriteLine("Cadastrar produto:");
            Console.Write("Codigo: ");
            string entradaCodigo = Console.ReadLine()!;

            if (!int.TryParse(entradaCodigo, out int codigo))
            {
                Console.WriteLine("Codigo deve ser um número inteiro");
                break;
            }

            bool codigoJaExiste = false;

            foreach (Produto p in produtos)
            {
                if (p.Codigo == codigo)
                {
                    codigoJaExiste = true;
                    break;
                }
            }

            if (codigoJaExiste)
            {
                Console.WriteLine("Código já existe.");
                break;
            }

            Console.Write("Nome: ");
            string nome = Console.ReadLine()!;

            if (string.IsNullOrWhiteSpace(nome))
            {
                Console.WriteLine("O nome do produto não pode ser vazio.");
                break;
            }

            Console.Write("Preço unitário: ");
            string entradaPreco = Console.ReadLine()!;

            if (!decimal.TryParse(entradaPreco, out decimal precoUnitario))
            {
                Console.WriteLine("Preço unitário inválido.");
                break;
            }

            if (precoUnitario <= 0)
            {
                Console.WriteLine("O preço unitário deve ser maior que zero.");
                break;
            }

            Console.Write("Quantidade em estoque: ");
            string entradaQuantidade = Console.ReadLine()!;

            if (!int.TryParse(entradaQuantidade, out int quantidadeEstoque))
            {
                Console.WriteLine("Quantidade em estoque inválida.");
                break;
            }

            if (quantidadeEstoque < 0)
            {
                Console.WriteLine("A quantidade em estoque não pode ser negativa.");
                break;
            }

            Produto produto = new Produto();

            produto.Codigo = codigo;
            produto.Nome = nome;
            produto.PrecoUnitario = precoUnitario;
            produto.QuantidadeEstoque = quantidadeEstoque;

            produtos.Add(produto);

            Console.WriteLine("Produto cadastrado com sucesso!");

            break;
        case 2:
            Console.WriteLine("Listar produtos:");

            if (produtos.Count == 0)
            {
                Console.WriteLine("Nenhum produto cadastrado.");
                break;
            }

            Console.WriteLine($"Total de produtos cadastrados: {produtos.Count}");

            foreach (Produto p in produtos)
            {
                Console.WriteLine($"Código: {p.Codigo}");
                Console.WriteLine($"Nome: {p.Nome}");
                Console.WriteLine($"Preço Unitário: {p.PrecoUnitario:F2}");
                Console.WriteLine($"Quantidade em Estoque: {p.QuantidadeEstoque}");
                Console.WriteLine("-----------------------------");
            }

            break;
        case 3:
            Console.WriteLine("Buscar produto por código");

            Console.Write("Digite o código do produto: ");
            string entradaCodigoBuscado = Console.ReadLine()!;

            if (!int.TryParse(entradaCodigoBuscado, out int codigoBuscado))
            {
                Console.WriteLine("Código inválido.");
                break;
            }
            bool encontrou = false;
            foreach (Produto p in produtos)
            {
                if (p.Codigo == codigoBuscado)
                {
                    Console.WriteLine($"Código: {p.Codigo}");
                    Console.WriteLine($"Nome: {p.Nome}");
                    Console.WriteLine($"Preço Unitário: {p.PrecoUnitario:F2}");
                    Console.WriteLine($"Quantidade em Estoque: {p.QuantidadeEstoque}");
                    Console.WriteLine("-----------------------------");
                    encontrou = true;
                    break;
                }
            }

            if (!encontrou)
            {
                Console.WriteLine("Produto não encontrado.");
            }

            break;
        case 4:
            Console.WriteLine("Calcular valor total do estoque:");
            if (produtos.Count == 0)
            {
                Console.WriteLine("Nenhum produto cadastrado.");
                break;
            }
            Console.WriteLine($"Produtos considerados no cálculo: {produtos.Count}");
            decimal valorTotal = 0;
            foreach (Produto p in produtos)
            {
                valorTotal += p.PrecoUnitario * p.QuantidadeEstoque;
            }
            Console.WriteLine($"Valor total do estoque: {valorTotal:F2}");
            break;
        case 5:
            Console.WriteLine("Editar produto");
            if (produtos.Count == 0)
            {
                Console.WriteLine("Nenhum produto cadastrado.");
                break;
            }

            Console.WriteLine("Digite o código do produto que deseja editar: ");
            string entradaCodigoEditar = Console.ReadLine()!;
            if (!int.TryParse(entradaCodigoEditar, out int codigoEditar))
            {
                Console.WriteLine("Codigo invalido.");
                break;
            }

            Produto? produtoParaEditar = null;

            foreach (Produto p in produtos)
            {
                if (p.Codigo == codigoEditar)
                {
                    produtoParaEditar = p;
                    break;
                }
            }
            if (produtoParaEditar == null)
            {
                Console.WriteLine("Produto não encontrado.");
                break;
            }

            Console.WriteLine("Produto encontrado. Proximo passo, editar os dados do produto.");

            Console.Write("Novo nome: ");
            string novoNome = Console.ReadLine()!;
            if (string.IsNullOrWhiteSpace(novoNome))
            {
                Console.WriteLine("O nome do produto não pode ser vazio.");
                break;
            }

            produtoParaEditar.Nome = novoNome;
            Console.WriteLine("Nome do produto atualizado com sucesso!");

            Console.Write("Novo preço unitário: ");
            string entradaNovoPreco = Console.ReadLine()!;
            if (!decimal.TryParse(entradaNovoPreco, out decimal novoPrecoUnitario))
            {
                Console.WriteLine("Preço unitário inválido.");
                break;
            }

            if (novoPrecoUnitario <= 0)
            {
                Console.WriteLine("O preço unitário deve ser maior que zero.");
                break;
            }
            produtoParaEditar.PrecoUnitario = novoPrecoUnitario;
            Console.WriteLine("Preço unitário do produto atualizado com sucesso!");

            Console.Write("Nova quantidade em estoque: ");
            string entradaNovaQuantidade = Console.ReadLine()!;
            if (!int.TryParse(entradaNovaQuantidade, out int novaQuantidadeEstoque))
            {
                Console.WriteLine("Quantidade em estoque inválida.");
                break;
            }

            if (novaQuantidadeEstoque < 0)
            {
                Console.WriteLine("A quantidade em estoque não pode ser negativa.");
                break;
            }

            produtoParaEditar.QuantidadeEstoque = novaQuantidadeEstoque;
            Console.WriteLine("Quantidade em estoque do produto atualizada com sucesso!");
            Console.WriteLine("Produto editado com sucesso!");
            break;
        case 6:
            Console.WriteLine("Remover produto");
            if (produtos.Count == 0)
            {
                Console.WriteLine("Nenhum produto cadastrado.");
                break;
            }

            Console.WriteLine("Digite o código do produto que deseja remover: ");
            string entradaCodigoRemover = Console.ReadLine()!;
            if (!int.TryParse(entradaCodigoRemover, out int codigoRemover))
            {
                Console.WriteLine("Codigo invalido.");
                break;
            }

            Produto? produtoParaRemover = null;

            foreach (Produto p in produtos)
            {
                if (p.Codigo == codigoRemover)
                {
                    produtoParaRemover = p;
                    break;
                }
            }
            if (produtoParaRemover == null)
            {
                Console.WriteLine("Produto não encontrado.");
                break;
            }

            Console.Write($"Confirma remover o produto {produtoParaRemover.Nome}? (s/n): ");
            string confirmacaoRemocao = Console.ReadLine()!;

            if (confirmacaoRemocao.ToLower() != "s")
            {
                Console.WriteLine("Remoção cancelada.");
                break;
            }

            produtos.Remove(produtoParaRemover);
            Console.WriteLine("Produto removido com sucesso!");
            break;
        case 7:
            Console.WriteLine("Resumo do Estoque");
            if (produtos.Count == 0)
            {
                Console.WriteLine("Nenhum produto cadastrado.");
                break;
            }

            int totalTipos = produtos.Count;
            int totalItens = 0;
            decimal valorTotalEstoque = 0;

            foreach (Produto p in produtos)
            {
                totalItens += p.QuantidadeEstoque;
                valorTotalEstoque += p.PrecoUnitario * p.QuantidadeEstoque;
            }   

            Console.WriteLine($"Total de tipos de produtos: {totalTipos}");
            Console.WriteLine($"Total de itens em estoque: {totalItens}");
            Console.WriteLine($"Valor total do estoque: {valorTotalEstoque:F2}");
            break;
        case 8:
            Console.WriteLine("Produtos com estoque baixo");

            if (produtos.Count == 0)
            {
                Console.WriteLine("Nenhum produto cadastrado.");
                break;
            }

            Console.WriteLine("Digite o limite do estoque: ");
            string entradaLimite = Console.ReadLine()!;
            if (!int.TryParse(entradaLimite, out int limite))
            {
                Console.WriteLine("Limite inválido.");
                break;
            }

            if (limite < 0)
            {
                Console.WriteLine("O limite de estoque não pode ser negativo.");
                break;
            }

            bool encontrouBaixoEstoque = false;

            foreach (Produto p in produtos)
            {
                if (p.QuantidadeEstoque < limite)
                {
                    Console.WriteLine($"Código: {p.Codigo}");
                    Console.WriteLine($"Nome: {p.Nome}");
                    Console.WriteLine($"Quantidade em Estoque: {p.QuantidadeEstoque}");
                    Console.WriteLine("-----------------------------");
                    encontrouBaixoEstoque = true;
                }
            }

            if (!encontrouBaixoEstoque)
            {
                Console.WriteLine("Nenhum produto com estoque abaixo do limite.");
            }
            break;
        case 9:
            Console.WriteLine("Buscar produto por nome");

            if (produtos.Count == 0)
            {
                Console.WriteLine("Nenhum produto cadastrado.");
                break;
            }

            Console.WriteLine("Digite o nome do produto: ");
            string termoBuscado = Console.ReadLine()!;

            if (string.IsNullOrWhiteSpace(termoBuscado))
            {
                Console.WriteLine("O nome do produto não pode ser vazio.");
                break;
            }

            bool encontrouProduto = false;
            string termoNormalizado = termoBuscado.Trim().ToLower();

            foreach (Produto p in produtos)
            {
                if (p.Nome.Trim().ToLower().Contains(termoNormalizado))
                {
                    Console.WriteLine($"Código: {p.Codigo}");
                    Console.WriteLine($"Nome: {p.Nome}");
                    Console.WriteLine($"Preço Unitário: {p.PrecoUnitario:F2}");
                    Console.WriteLine($"Quantidade em Estoque: {p.QuantidadeEstoque}");
                    Console.WriteLine("-----------------------------");
                    encontrouProduto = true;
                }
            }

            if (!encontrouProduto)
            {
                Console.WriteLine("Nenhum produto encontrado com esse nome.");
            }

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
