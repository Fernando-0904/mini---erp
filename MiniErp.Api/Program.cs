using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Data;
using MiniErp.Api.Models;
using MiniErp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

string[] allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? new[]
    {
        "http://localhost:5500",
        "http://127.0.0.1:5500",
        "https://fernando-0904.github.io"
    };

builder.Services.AddCors(options =>
{
    options.AddPolicy("MiniErpCors", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=Dados/mini-erp.db"));
builder.Services.AddScoped<ProdutoService>();
builder.Services.AddScoped<CategoriaService>();
builder.Services.AddScoped<MovimentacaoEstoqueService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("MiniErpCors");

app.MapGet("/produtos", (ProdutoService produtoService) =>
{
    return Results.Ok(produtoService.ListarProdutos());
});

app.MapGet("/produtos/estoque-baixo", (ProdutoService produtoService) =>
{
    return Results.Ok(produtoService.ListarProdutosComEstoqueBaixo());
});

app.MapGet("/produtos/{codigo:int}", (int codigo, ProdutoService produtoService) =>
{
    Produto? produto = produtoService.BuscarPorCodigo(codigo);

    if (produto == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(produto);
});

app.MapPost("/produtos", (Produto produto, ProdutoService produtoService, CategoriaService categoriaService) =>
{
    List<string> erros = ValidarProduto(produto);
    erros.AddRange(ValidarCategoriaDoProduto(produto, categoriaService));

    if (erros.Count > 0)
    {
        return Results.BadRequest(erros);
    }

    bool cadastrado = produtoService.CadastrarProduto(produto);

    if (!cadastrado)
    {
        return Results.Conflict("Já existe um produto com esse código.");
    }

    return Results.Created($"/produtos/{produto.Codigo}", produto);
});

app.MapPut("/produtos/{codigo:int}", (int codigo, Produto produtoAtualizado, ProdutoService produtoService, CategoriaService categoriaService) =>
{
    Produto? produtoExistente = produtoService.BuscarPorCodigo(codigo);

    if (produtoExistente == null)
    {
        return Results.NotFound();
    }

    List<string> erros = ValidarProduto(produtoAtualizado);

    if (codigo != produtoAtualizado.Codigo)
    {
        erros.Add("O código da URL deve ser igual ao código do produto.");
    }

    if (produtoAtualizado.QuantidadeEstoque != produtoExistente.QuantidadeEstoque)
    {
        erros.Add("A quantidade em estoque deve ser alterada por uma movimentação de entrada ou saída.");
    }

    erros.AddRange(ValidarCategoriaDoProduto(produtoAtualizado, categoriaService));

    if (erros.Count > 0)
    {
        return Results.BadRequest(erros);
    }

    bool editado = produtoService.EditarProduto(codigo, produtoAtualizado);

    if (!editado)
    {
        return Results.NotFound();
    }

    return Results.Ok(produtoAtualizado);
});

app.MapDelete("/produtos/{codigo:int}", (int codigo, ProdutoService produtoService) =>
{
    bool removido = produtoService.RemoverProduto(codigo);

    if (!removido)
    {
        return Results.NotFound();
    }

    return Results.NoContent();
});

app.MapGet(
    "/produtos/{codigo:int}/movimentacoes",
    (int codigo, ProdutoService produtoService, MovimentacaoEstoqueService movimentacaoService) =>
    {
        Produto? produto = produtoService.BuscarPorCodigo(codigo);

        if (produto == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(movimentacaoService.ListarMovimentacoesPorProduto(codigo));
    });

app.MapPost(
    "/produtos/{codigo:int}/movimentacoes/entrada",
    (int codigo, MovimentacaoEstoqueRequest request, MovimentacaoEstoqueService movimentacaoService) =>
    {
        bool movimentado = movimentacaoService.RegistrarEntrada(codigo, request.Quantidade, out MovimentacaoEstoque? movimentacao, out string erro);

        if (!movimentado)
        {
            if (erro == "Produto não encontrado.")
            {
                return Results.NotFound(erro);
            }

            return Results.BadRequest(erro);
        }

        return Results.Created($"/produtos/{codigo}/movimentacoes/{movimentacao!.Id}", movimentacao);
    });

app.MapPost(
    "/produtos/{codigo:int}/movimentacoes/saida",
    (int codigo, MovimentacaoEstoqueRequest request, MovimentacaoEstoqueService movimentacaoService) =>
    {
        bool movimentado = movimentacaoService.RegistrarSaida(codigo, request.Quantidade, out MovimentacaoEstoque? movimentacao, out string erro);

        if (!movimentado)
        {
            if (erro == "Produto não encontrado.")
            {
                return Results.NotFound(erro);
            }

            return Results.BadRequest(erro);
        }

        return Results.Created($"/produtos/{codigo}/movimentacoes/{movimentacao!.Id}", movimentacao);
    });

app.MapGet("/categorias", (CategoriaService categoriaService) =>
{
    return Results.Ok(categoriaService.ListarCategorias());
});

app.MapGet("/categorias/{id:int}", (int id, CategoriaService categoriaService) =>
{
    Categoria? categoria = categoriaService.BuscarPorId(id);

    if (categoria == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(categoria);
});

app.MapPost("/categorias", (Categoria categoria, CategoriaService categoriaService) =>
{
    List<string> erros = ValidarCategoria(categoria);

    if (erros.Count > 0)
    {
        return Results.BadRequest(erros);
    }

    bool cadastrada = categoriaService.CadastrarCategoria(categoria);

    if (!cadastrada)
    {
        return Results.Conflict("Já existe uma categoria com esse nome.");
    }

    return Results.Created($"/categorias/{categoria.Id}", categoria);
});

app.MapPut("/categorias/{id:int}", (int id, Categoria categoriaAtualizada, CategoriaService categoriaService) =>
{
    List<string> erros = ValidarCategoria(categoriaAtualizada);

    if (id != categoriaAtualizada.Id)
    {
        erros.Add("O id da URL deve ser igual ao id da categoria.");
    }

    if (erros.Count > 0)
    {
        return Results.BadRequest(erros);
    }

    bool editada = categoriaService.EditarCategoria(id, categoriaAtualizada);

    if (!editada)
    {
        return Results.NotFound("Categoria não encontrada ou nome já está em uso.");
    }

    return Results.Ok(categoriaAtualizada);
});

app.MapDelete("/categorias/{id:int}", (int id, CategoriaService categoriaService) =>
{
    if (categoriaService.BuscarPorId(id) == null)
    {
        return Results.NotFound();
    }

    if (categoriaService.PossuiProdutosVinculados(id))
    {
        return Results.BadRequest("Não é possível remover uma categoria vinculada a produtos.");
    }

    categoriaService.RemoverCategoria(id);
    return Results.NoContent();
});

app.Run();

static List<string> ValidarProduto(Produto produto)
{
    List<string> erros = new List<string>();

    if (produto.Codigo <= 0)
    {
        erros.Add("O código deve ser maior que zero.");
    }

    if (string.IsNullOrWhiteSpace(produto.Nome))
    {
        erros.Add("O nome é obrigatório.");
    }

    if (produto.PrecoUnitario <= 0)
    {
        erros.Add("O preço unitário deve ser maior que zero.");
    }

    if (produto.QuantidadeEstoque < 0)
    {
        erros.Add("A quantidade em estoque não pode ser negativa.");
    }

    if (produto.EstoqueMinimo < 0)
    {
        erros.Add("O estoque mínimo não pode ser negativo.");
    }

    return erros;
}

static List<string> ValidarCategoriaDoProduto(Produto produto, CategoriaService categoriaService)
{
    List<string> erros = new List<string>();

    if (produto.CategoriaId <= 0)
    {
        erros.Add("Informe uma categoria válida.");
    }
    else if (categoriaService.BuscarPorId(produto.CategoriaId) == null)
    {
        erros.Add("Categoria informada não existe.");
    }

    return erros;
}

static List<string> ValidarCategoria(Categoria categoria)
{
    List<string> erros = new List<string>();

    if (categoria.Id < 0)
    {
        erros.Add("O id não pode ser negativo.");
    }

    if (string.IsNullOrWhiteSpace(categoria.Nome))
    {
        erros.Add("O nome é obrigatório.");
    }

    return erros;
}
