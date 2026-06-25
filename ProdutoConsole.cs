namespace projetoerp;

public static class ProdutoConsole
{
    public static void ExibirMenu()
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
    }

    public static bool LerOpcao(out int opcao)
    {
        string entradaOpcao = Console.ReadLine()!;

        if (!int.TryParse(entradaOpcao, out opcao))
        {
            opcao = -1;
            Console.WriteLine("Digite apenas números.");
            return false;
        }

        return true;
    }

    public static bool LerCodigo(string mensagem, string mensagemErro, out int codigo)
    {
        Console.Write(mensagem);
        string entradaCodigo = Console.ReadLine()!;

        if (!int.TryParse(entradaCodigo, out codigo))
        {
            Console.WriteLine(mensagemErro);
            return false;
        }

        return true;
    }

    public static bool LerDadosProduto(out string nome, out decimal precoUnitario, out int quantidadeEstoque)
    {
        Console.Write("Nome: ");
        nome = Console.ReadLine()!;

        if (string.IsNullOrWhiteSpace(nome))
        {
            Console.WriteLine("O nome do produto não pode ser vazio.");
            precoUnitario = 0;
            quantidadeEstoque = 0;
            return false;
        }

        Console.Write("Preço unitário: ");
        string entradaPreco = Console.ReadLine()!;

        if (!decimal.TryParse(entradaPreco, out precoUnitario))
        {
            Console.WriteLine("Preço unitário inválido.");
            quantidadeEstoque = 0;
            return false;
        }

        if (precoUnitario <= 0)
        {
            Console.WriteLine("O preço unitário deve ser maior que zero.");
            quantidadeEstoque = 0;
            return false;
        }

        Console.Write("Quantidade em estoque: ");
        string entradaQuantidade = Console.ReadLine()!;

        if (!int.TryParse(entradaQuantidade, out quantidadeEstoque))
        {
            Console.WriteLine("Quantidade em estoque inválida.");
            return false;
        }

        if (quantidadeEstoque < 0)
        {
            Console.WriteLine("A quantidade em estoque não pode ser negativa.");
            return false;
        }

        return true;
    }

    public static bool LerDadosEdicao(out string nome, out decimal precoUnitario, out int quantidadeEstoque)
    {
        Console.Write("Novo nome: ");
        nome = Console.ReadLine()!;

        if (string.IsNullOrWhiteSpace(nome))
        {
            Console.WriteLine("O nome do produto não pode ser vazio.");
            precoUnitario = 0;
            quantidadeEstoque = 0;
            return false;
        }

        Console.Write("Novo preço unitário: ");
        string entradaPreco = Console.ReadLine()!;

        if (!decimal.TryParse(entradaPreco, out precoUnitario))
        {
            Console.WriteLine("Preço unitário inválido.");
            quantidadeEstoque = 0;
            return false;
        }

        if (precoUnitario <= 0)
        {
            Console.WriteLine("O preço unitário deve ser maior que zero.");
            quantidadeEstoque = 0;
            return false;
        }

        Console.Write("Nova quantidade em estoque: ");
        string entradaQuantidade = Console.ReadLine()!;

        if (!int.TryParse(entradaQuantidade, out quantidadeEstoque))
        {
            Console.WriteLine("Quantidade em estoque inválida.");
            return false;
        }

        if (quantidadeEstoque < 0)
        {
            Console.WriteLine("A quantidade em estoque não pode ser negativa.");
            return false;
        }

        return true;
    }

    public static bool LerLimiteEstoque(out int limite)
    {
        Console.WriteLine("Digite o limite do estoque: ");
        string entradaLimite = Console.ReadLine()!;

        if (!int.TryParse(entradaLimite, out limite))
        {
            Console.WriteLine("Limite inválido.");
            return false;
        }

        if (limite < 0)
        {
            Console.WriteLine("O limite de estoque não pode ser negativo.");
            return false;
        }

        return true;
    }

    public static string LerTexto(string mensagem)
    {
        Console.Write(mensagem);
        return Console.ReadLine()!;
    }

    public static bool ConfirmarRemocao(Produto produto)
    {
        Console.Write($"Confirma remover o produto {produto.Nome}? (s/n): ");
        string confirmacaoRemocao = Console.ReadLine()!;

        return confirmacaoRemocao.ToLower() == "s";
    }

    public static void ExibirProduto(Produto produto, bool exibirPreco = true)
    {
        Console.WriteLine($"Código: {produto.Codigo}");
        Console.WriteLine($"Nome: {produto.Nome}");

        if (exibirPreco)
        {
            Console.WriteLine($"Preço Unitário: {produto.PrecoUnitario:F2}");
        }

        Console.WriteLine($"Quantidade em Estoque: {produto.QuantidadeEstoque}");
        Console.WriteLine("-----------------------------");
    }

    public static void ExibirProdutos(List<Produto> produtos, bool exibirPreco = true)
    {
        foreach (Produto produto in produtos)
        {
            ExibirProduto(produto, exibirPreco);
        }
    }
}
