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
    List<string> erros = ValidarProduto(produto);

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

app.MapPut("/produtos/{codigo:int}", (int codigo, Produto produtoAtualizado, ProdutoService produtoService) =>
{
    List<string> erros = ValidarProduto(produtoAtualizado);

    if (codigo != produtoAtualizado.Codigo)
    {
        erros.Add("O código da URL deve ser igual ao código do produto.");
    }

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

    return erros;
}
