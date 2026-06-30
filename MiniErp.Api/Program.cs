using MiniErp.Api.Models;
using MiniErp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<ProdutoService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/produtos", (ProdutoService produtoService) =>
{
    return Results.Ok(produtoService.ListarProdutos());
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

app.MapPost("/produtos", (Produto produto, ProdutoService produtoService) =>
{
    bool cadastrado = produtoService.CadastrarProduto(produto);

    if (!cadastrado)
    {
        return Results.Conflict("Já existe um produto com esse código.");
    }

    return Results.Created($"/produtos/{produto.Codigo}", produto);
});

app.MapPut("/produtos/{codigo:int}", (int codigo, Produto produtoAtualizado, ProdutoService produtoService) =>
{
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

app.Run();
