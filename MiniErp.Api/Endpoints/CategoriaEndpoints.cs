using Microsoft.AspNetCore.Antiforgery;
using MiniErp.Api.DTOs;
using MiniErp.Api.Models;
using MiniErp.Api.Security;
using MiniErp.Api.Services;

namespace MiniErp.Api.Endpoints;

internal static class CategoriaEndpoints
{
    internal static void MapCategoriaEndpoints(this WebApplication app, string politicaOperar, string politicaAdministrar)
    {
        app.MapGet("/categorias", (CategoriaService categoriaService) =>
        {
            return Results.Ok(categoriaService.ListarCategorias());
        });

        app.MapGet("/categorias/{id:int}", (int id, CategoriaService categoriaService, HttpContext context) =>
        {
            Categoria? categoria = categoriaService.BuscarPorId(id);

            if (categoria == null)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status404NotFound,
                    "Recurso não encontrado.",
                    "Categoria não encontrada.");
            }

            return Results.Ok(categoria);
        });

        app.MapPost("/categorias", async (CategoriaRequest request, CategoriaService categoriaService, AuditoriaService auditoriaService, HttpContext context) =>
        {
            Categoria categoria = ApiHttpHelpers.MapearCategoriaRequest(request);
            List<string> erros = categoriaService.ValidarCategoria(categoria);

            if (erros.Count > 0)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Dados inválidos.",
                    string.Join(" ", erros));
            }

            bool cadastrada = categoriaService.CadastrarCategoria(categoria);

            if (!cadastrada)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status409Conflict,
                    "Conflito de dados.",
                    "Já existe uma categoria com esse nome.");
            }

            await auditoriaService.RegistrarAsync(
                context,
                "Cadastro",
                "Categoria",
                categoria.Id.ToString(),
                $"Categoria {categoria.Id} - {categoria.Nome} cadastrada.",
                new { categoria.Id, categoria.Nome });

            return Results.Created($"/categorias/{categoria.Id}", categoria);
        }).RequireAuthorization(politicaOperar).RequireAntiforgery();

        app.MapPut("/categorias/{id:int}", async (int id, CategoriaRequest request, CategoriaService categoriaService, AuditoriaService auditoriaService, HttpContext context) =>
        {
            Categoria categoriaAtualizada = ApiHttpHelpers.MapearCategoriaRequest(request);
            List<string> erros = categoriaService.ValidarCategoria(categoriaAtualizada);

            if (id != categoriaAtualizada.Id)
            {
                erros.Add("O id da URL deve ser igual ao id da categoria.");
            }

            if (erros.Count > 0)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Dados inválidos.",
                    string.Join(" ", erros));
            }

            bool editada = categoriaService.EditarCategoria(id, categoriaAtualizada);

            if (!editada)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status404NotFound,
                    "Recurso não encontrado.",
                    "Categoria não encontrada ou nome já está em uso.");
            }

            await auditoriaService.RegistrarAsync(
                context,
                "Edição",
                "Categoria",
                id.ToString(),
                $"Categoria {id} atualizada para {categoriaAtualizada.Nome}.",
                new { categoriaAtualizada.Id, categoriaAtualizada.Nome });

            return Results.Ok(categoriaAtualizada);
        }).RequireAuthorization(politicaOperar).RequireAntiforgery();

        app.MapDelete("/categorias/{id:int}", async (int id, CategoriaService categoriaService, AuditoriaService auditoriaService, HttpContext context) =>
        {
            Categoria? categoria = categoriaService.BuscarPorId(id);

            if (categoria == null)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status404NotFound,
                    "Recurso não encontrado.",
                    "Categoria não encontrada.");
            }

            if (categoriaService.PossuiProdutosVinculados(id))
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Operação inválida.",
                    "Não é possível remover uma categoria vinculada a produtos.");
            }

            categoriaService.RemoverCategoria(id);

            await auditoriaService.RegistrarAsync(
                context,
                "Remoção",
                "Categoria",
                id.ToString(),
                $"Categoria {id} - {categoria.Nome} removida.",
                new { categoria.Id, categoria.Nome });

            return Results.NoContent();
        }).RequireAuthorization(politicaAdministrar).RequireAntiforgery();
    }
}
