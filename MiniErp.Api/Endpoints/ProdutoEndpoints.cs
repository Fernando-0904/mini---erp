using Microsoft.AspNetCore.Antiforgery;
using MiniErp.Api.DTOs;
using MiniErp.Api.Models;
using MiniErp.Api.Security;
using MiniErp.Api.Services;

namespace MiniErp.Api.Endpoints;

internal static class ProdutoEndpoints
{
    internal static void MapProdutoEndpoints(this WebApplication app, string politicaOperar, string politicaAdministrar)
    {
        app.MapGet("/produtos", (ProdutoService produtoService) =>
        {
            return Results.Ok(produtoService.ListarProdutos());
        });

        app.MapGet("/produtos/estoque-baixo", (int? categoriaId, ProdutoService produtoService) =>
        {
            return Results.Ok(produtoService.ListarProdutosComEstoqueBaixo(categoriaId));
        });

        app.MapGet("/produtos/sem-estoque", (int? categoriaId, ProdutoService produtoService) =>
        {
            return Results.Ok(produtoService.ListarProdutosSemEstoque(categoriaId));
        });

        app.MapGet("/produtos/{codigo:int}", (int codigo, ProdutoService produtoService, HttpContext context) =>
        {
            Produto? produto = produtoService.BuscarPorCodigo(codigo);

            if (produto == null)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status404NotFound,
                    "Recurso não encontrado.",
                    "Produto não encontrado.");
            }

            return Results.Ok(produto);
        });

        app.MapPost("/produtos", async (ProdutoRequest request, ProdutoService produtoService, CategoriaService categoriaService, FornecedorService fornecedorService, AuditoriaService auditoriaService, HttpContext context) =>
        {
            Produto produto = ApiHttpHelpers.MapearProdutoRequest(request);
            List<string> erros = produtoService.ValidarProduto(produto);
            erros.AddRange(categoriaService.ValidarCategoriaDoProduto(produto.CategoriaId));
            erros.AddRange(fornecedorService.ValidarFornecedorDoProduto(produto.FornecedorId));

            if (erros.Count > 0)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Dados inválidos.",
                    string.Join(" ", erros));
            }

            bool cadastrado = produtoService.CadastrarProduto(produto);

            if (!cadastrado)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status409Conflict,
                    "Conflito de dados.",
                    "Já existe um produto com esse código.");
            }

            await auditoriaService.RegistrarAsync(
                context,
                "Cadastro",
                "Produto",
                produto.Codigo.ToString(),
                $"Produto {produto.Codigo} - {produto.Nome} cadastrado.",
                new { produto.Codigo, produto.Nome, produto.CategoriaId, produto.FornecedorId });

            return Results.Created($"/produtos/{produto.Codigo}", produto);
        }).RequireAuthorization(politicaOperar).RequireAntiforgery();

        app.MapPut("/produtos/{codigo:int}", async (int codigo, ProdutoRequest request, ProdutoService produtoService, CategoriaService categoriaService, FornecedorService fornecedorService, AuditoriaService auditoriaService, HttpContext context) =>
        {
            Produto produtoAtualizado = ApiHttpHelpers.MapearProdutoRequest(request);
            Produto? produtoExistente = produtoService.BuscarPorCodigo(codigo);

            if (produtoExistente == null)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status404NotFound,
                    "Recurso não encontrado.",
                    "Produto não encontrado.");
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
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Dados inválidos.",
                    string.Join(" ", erros));
            }

            bool editado = produtoService.EditarProduto(codigo, produtoAtualizado);

            if (!editado)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status404NotFound,
                    "Recurso não encontrado.",
                    "Produto não encontrado.");
            }

            await auditoriaService.RegistrarAsync(
                context,
                "Edição",
                "Produto",
                codigo.ToString(),
                $"Produto {codigo} atualizado.",
                new { produtoAtualizado.Nome, produtoAtualizado.PrecoUnitario, produtoAtualizado.EstoqueMinimo, produtoAtualizado.CategoriaId, produtoAtualizado.FornecedorId });

            return Results.Ok(produtoAtualizado);
        }).RequireAuthorization(politicaOperar).RequireAntiforgery();

        app.MapDelete("/produtos/{codigo:int}", async (int codigo, ProdutoService produtoService, AuditoriaService auditoriaService, HttpContext context) =>
        {
            Produto? produtoExistente = produtoService.BuscarPorCodigo(codigo);

            if (produtoExistente == null)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status404NotFound,
                    "Recurso não encontrado.",
                    "Produto não encontrado.");
            }

            bool removido = produtoService.RemoverProduto(codigo);

            if (!removido)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status404NotFound,
                    "Recurso não encontrado.",
                    "Produto não encontrado.");
            }

            await auditoriaService.RegistrarAsync(
                context,
                "Remoção",
                "Produto",
                codigo.ToString(),
                $"Produto {codigo} removido.",
                new { produtoExistente.Nome, produtoExistente.CategoriaId, produtoExistente.FornecedorId });

            return Results.NoContent();
        }).RequireAuthorization(politicaAdministrar).RequireAntiforgery();

        app.MapGet(
            "/produtos/{codigo:int}/movimentacoes",
            (int codigo, ProdutoService produtoService, MovimentacaoEstoqueService movimentacaoService, HttpContext context) =>
            {
                Produto? produto = produtoService.BuscarPorCodigo(codigo);

                if (produto == null)
                {
                    return ApiHttpHelpers.CriarProblem(
                        context,
                        StatusCodes.Status404NotFound,
                        "Recurso não encontrado.",
                        "Produto não encontrado.");
                }

                return Results.Ok(movimentacaoService.ListarMovimentacoesPorProduto(codigo));
            });

        app.MapPost(
            "/produtos/{codigo:int}/movimentacoes/entrada",
            async (int codigo, MovimentacaoEstoqueRequest request, MovimentacaoEstoqueService movimentacaoService, AuditoriaService auditoriaService, HttpContext context) =>
            {
                bool movimentado = movimentacaoService.RegistrarEntrada(codigo, request.Quantidade, out MovimentacaoEstoque? movimentacao, out string erro);

                if (!movimentado)
                {
                    if (erro == "Produto não encontrado.")
                    {
                        return ApiHttpHelpers.CriarProblem(
                            context,
                            StatusCodes.Status404NotFound,
                            "Recurso não encontrado.",
                            erro);
                    }

                    return ApiHttpHelpers.CriarProblem(
                        context,
                        StatusCodes.Status400BadRequest,
                        "Dados inválidos.",
                        erro);
                }

                await auditoriaService.RegistrarAsync(
                    context,
                    "Movimentação",
                    "Estoque",
                    codigo.ToString(),
                    $"Entrada de {request.Quantidade} unidade(s) no produto {codigo}.",
                    new { Tipo = "Entrada", request.Quantidade, movimentacao!.SaldoAnterior, movimentacao.SaldoNovo });

                return Results.Created($"/produtos/{codigo}/movimentacoes/{movimentacao!.Id}", movimentacao);
            }).RequireAuthorization(politicaOperar).RequireAntiforgery();

        app.MapPost(
            "/produtos/{codigo:int}/movimentacoes/saida",
            async (int codigo, MovimentacaoEstoqueRequest request, MovimentacaoEstoqueService movimentacaoService, AuditoriaService auditoriaService, HttpContext context) =>
            {
                bool movimentado = movimentacaoService.RegistrarSaida(codigo, request.Quantidade, out MovimentacaoEstoque? movimentacao, out string erro);

                if (!movimentado)
                {
                    if (erro == "Produto não encontrado.")
                    {
                        return ApiHttpHelpers.CriarProblem(
                            context,
                            StatusCodes.Status404NotFound,
                            "Recurso não encontrado.",
                            erro);
                    }

                    return ApiHttpHelpers.CriarProblem(
                        context,
                        StatusCodes.Status400BadRequest,
                        "Dados inválidos.",
                        erro);
                }

                await auditoriaService.RegistrarAsync(
                    context,
                    "Movimentação",
                    "Estoque",
                    codigo.ToString(),
                    $"Saída de {request.Quantidade} unidade(s) no produto {codigo}.",
                    new { Tipo = "Saída", request.Quantidade, movimentacao!.SaldoAnterior, movimentacao.SaldoNovo });

                return Results.Created($"/produtos/{codigo}/movimentacoes/{movimentacao!.Id}", movimentacao);
            }).RequireAuthorization(politicaOperar).RequireAntiforgery();
    }
}
