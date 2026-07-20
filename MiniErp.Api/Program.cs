using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Data;
using MiniErp.Api.DTOs;
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
builder.Services.AddScoped<FornecedorService>();
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

app.MapGet("/produtos/estoque-baixo", (int? categoriaId, ProdutoService produtoService) =>
{
    return Results.Ok(produtoService.ListarProdutosComEstoqueBaixo(categoriaId));
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

app.MapPost("/produtos", (ProdutoRequest request, ProdutoService produtoService, CategoriaService categoriaService, FornecedorService fornecedorService) =>
{
    Produto produto = MapearProdutoRequest(request);
    List<string> erros = produtoService.ValidarProduto(produto);
    erros.AddRange(categoriaService.ValidarCategoriaDoProduto(produto.CategoriaId));
    erros.AddRange(fornecedorService.ValidarFornecedorDoProduto(produto.FornecedorId));

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

app.MapPut("/produtos/{codigo:int}", (int codigo, ProdutoRequest request, ProdutoService produtoService, CategoriaService categoriaService, FornecedorService fornecedorService) =>
{
    Produto produtoAtualizado = MapearProdutoRequest(request);
    Produto? produtoExistente = produtoService.BuscarPorCodigo(codigo);

    if (produtoExistente == null)
    {
        return Results.NotFound();
    }

    List<string> erros = produtoService.ValidarProduto(produtoAtualizado);

    if (codigo != produtoAtualizado.Codigo)
    {
        erros.Add("O código da URL deve ser igual ao código do produto.");
    }

    if (produtoAtualizado.QuantidadeEstoque != produtoExistente.QuantidadeEstoque)
    {
        erros.Add("A quantidade em estoque deve ser alterada por uma movimentação de entrada ou saída.");
    }

    erros.AddRange(categoriaService.ValidarCategoriaDoProduto(produtoAtualizado.CategoriaId));
    erros.AddRange(fornecedorService.ValidarFornecedorDoProduto(produtoAtualizado.FornecedorId));

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

app.MapPost("/categorias", (CategoriaRequest request, CategoriaService categoriaService) =>
{
    Categoria categoria = MapearCategoriaRequest(request);
    List<string> erros = categoriaService.ValidarCategoria(categoria);

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

app.MapPut("/categorias/{id:int}", (int id, CategoriaRequest request, CategoriaService categoriaService) =>
{
    Categoria categoriaAtualizada = MapearCategoriaRequest(request);
    List<string> erros = categoriaService.ValidarCategoria(categoriaAtualizada);

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

app.MapGet("/fornecedores", (FornecedorService fornecedorService) =>
{
    return Results.Ok(fornecedorService.ListarFornecedores());
});

app.MapGet("/fornecedores/{id:int}", (int id, FornecedorService fornecedorService) =>
{
    Fornecedor? fornecedor = fornecedorService.BuscarPorId(id);

    if (fornecedor == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(fornecedor);
});

app.MapPost("/fornecedores", (FornecedorRequest request, FornecedorService fornecedorService) =>
{
    Fornecedor fornecedor = MapearFornecedorRequest(request);
    List<string> erros = fornecedorService.ValidarFornecedor(fornecedor);

    if (erros.Count > 0)
    {
        return Results.BadRequest(erros);
    }

    bool cadastrado = fornecedorService.CadastrarFornecedor(fornecedor);

    if (!cadastrado)
    {
        return Results.Conflict("Já existe um fornecedor com esse código ou documento.");
    }

    return Results.Created($"/fornecedores/{fornecedor.Id}", fornecedor);
});

app.MapPut("/fornecedores/{id:int}", (int id, FornecedorRequest request, FornecedorService fornecedorService) =>
{
    if (fornecedorService.BuscarPorId(id) == null)
    {
        return Results.NotFound();
    }

    Fornecedor fornecedorAtualizado = MapearFornecedorRequest(request);
    List<string> erros = fornecedorService.ValidarFornecedor(fornecedorAtualizado);

    if (erros.Count > 0)
    {
        return Results.BadRequest(erros);
    }

    bool editado = fornecedorService.EditarFornecedor(id, fornecedorAtualizado);

    if (!editado)
    {
        return Results.Conflict("Já existe um fornecedor com esse código ou documento.");
    }

    return Results.Ok(fornecedorService.BuscarPorId(id));
});

app.MapPatch("/fornecedores/{id:int}/inativar", (int id, FornecedorService fornecedorService) =>
{
    if (!fornecedorService.InativarFornecedor(id))
    {
        return Results.NotFound();
    }

    return Results.Ok(fornecedorService.BuscarPorId(id));
});

app.MapDelete("/fornecedores/{id:int}", (int id, FornecedorService fornecedorService) =>
{
    if (fornecedorService.BuscarPorId(id) == null)
    {
        return Results.NotFound();
    }

    if (fornecedorService.PossuiProdutosVinculados(id))
    {
        return Results.BadRequest("Não é possível remover um fornecedor vinculado a produtos.");
    }

    fornecedorService.RemoverFornecedor(id);
    return Results.NoContent();
});

app.Run();

static Produto MapearProdutoRequest(ProdutoRequest request)
{
    return new Produto
    {
        Codigo = request.Codigo,
        Nome = request.Nome,
        PrecoUnitario = request.PrecoUnitario,
        QuantidadeEstoque = request.QuantidadeEstoque,
        EstoqueMinimo = request.EstoqueMinimo,
        CategoriaId = request.CategoriaId,
        FornecedorId = request.FornecedorId
    };
}

static Categoria MapearCategoriaRequest(CategoriaRequest request)
{
    return new Categoria
    {
        Id = request.Id,
        Nome = request.Nome
    };
}

static Fornecedor MapearFornecedorRequest(FornecedorRequest request)
{
    return new Fornecedor
    {
        Codigo = request.Codigo,
        Nome = request.Nome,
        Documento = request.Documento,
        Email = request.Email,
        Telefone = request.Telefone,
        Ativo = request.Ativo
    };
}
