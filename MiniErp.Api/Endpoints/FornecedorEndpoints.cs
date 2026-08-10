using Microsoft.AspNetCore.Antiforgery;
using MiniErp.Api.DTOs;
using MiniErp.Api.Models;
using MiniErp.Api.Security;
using MiniErp.Api.Services;

namespace MiniErp.Api.Endpoints;

internal static class FornecedorEndpoints
{
    internal static void MapFornecedorEndpoints(this WebApplication app, string politicaOperar, string politicaAdministrar)
    {
        app.MapGet("/fornecedores", (FornecedorService fornecedorService) =>
        {
            return Results.Ok(fornecedorService.ListarFornecedores());
        });

        app.MapGet("/fornecedores/{id:int}", (int id, FornecedorService fornecedorService, HttpContext context) =>
        {
            Fornecedor? fornecedor = fornecedorService.BuscarPorId(id);

            if (fornecedor == null)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status404NotFound,
                    "Recurso não encontrado.",
                    "Fornecedor não encontrado.");
            }

            return Results.Ok(fornecedor);
        });

        app.MapPost("/fornecedores", async (FornecedorRequest request, FornecedorService fornecedorService, AuditoriaService auditoriaService, HttpContext context) =>
        {
            Fornecedor fornecedor = ApiHttpHelpers.MapearFornecedorRequest(request);
            List<string> erros = fornecedorService.ValidarFornecedor(fornecedor);

            if (erros.Count > 0)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Dados inválidos.",
                    string.Join(" ", erros));
            }

            bool cadastrado = fornecedorService.CadastrarFornecedor(fornecedor);

            if (!cadastrado)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status409Conflict,
                    "Conflito de dados.",
                    "Já existe um fornecedor com esse código ou documento.");
            }

            await auditoriaService.RegistrarAsync(
                context,
                "Cadastro",
                "Fornecedor",
                fornecedor.Id.ToString(),
                $"Fornecedor {fornecedor.Codigo} - {fornecedor.Nome} cadastrado.",
                new { fornecedor.Id, fornecedor.Codigo, fornecedor.Nome, fornecedor.Ativo });

            return Results.Created($"/fornecedores/{fornecedor.Id}", fornecedor);
        }).RequireAuthorization(politicaOperar).RequireAntiforgery();

        app.MapPut("/fornecedores/{id:int}", async (int id, FornecedorRequest request, FornecedorService fornecedorService, AuditoriaService auditoriaService, HttpContext context) =>
        {
            if (fornecedorService.BuscarPorId(id) == null)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status404NotFound,
                    "Recurso não encontrado.",
                    "Fornecedor não encontrado.");
            }

            Fornecedor fornecedorAtualizado = ApiHttpHelpers.MapearFornecedorRequest(request);
            List<string> erros = fornecedorService.ValidarFornecedor(fornecedorAtualizado);

            if (erros.Count > 0)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Dados inválidos.",
                    string.Join(" ", erros));
            }

            bool editado = fornecedorService.EditarFornecedor(id, fornecedorAtualizado);

            if (!editado)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status409Conflict,
                    "Conflito de dados.",
                    "Já existe um fornecedor com esse código ou documento.");
            }

            await auditoriaService.RegistrarAsync(
                context,
                "Edição",
                "Fornecedor",
                id.ToString(),
                $"Fornecedor {id} atualizado.",
                new { fornecedorAtualizado.Codigo, fornecedorAtualizado.Nome, fornecedorAtualizado.Ativo });

            return Results.Ok(fornecedorService.BuscarPorId(id));
        }).RequireAuthorization(politicaOperar).RequireAntiforgery();

        app.MapPatch("/fornecedores/{id:int}/inativar", async (int id, FornecedorService fornecedorService, AuditoriaService auditoriaService, HttpContext context) =>
        {
            if (!fornecedorService.InativarFornecedor(id))
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status404NotFound,
                    "Recurso não encontrado.",
                    "Fornecedor não encontrado.");
            }

            Fornecedor? fornecedor = fornecedorService.BuscarPorId(id);

            await auditoriaService.RegistrarAsync(
                context,
                "Inativação",
                "Fornecedor",
                id.ToString(),
                $"Fornecedor {id} inativado.",
                new { fornecedor?.Codigo, fornecedor?.Nome });

            return Results.Ok(fornecedor);
        }).RequireAuthorization(politicaOperar).RequireAntiforgery();

        app.MapDelete("/fornecedores/{id:int}", async (int id, FornecedorService fornecedorService, AuditoriaService auditoriaService, HttpContext context) =>
        {
            Fornecedor? fornecedor = fornecedorService.BuscarPorId(id);

            if (fornecedor == null)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status404NotFound,
                    "Recurso não encontrado.",
                    "Fornecedor não encontrado.");
            }

            if (fornecedorService.PossuiProdutosVinculados(id))
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Operação inválida.",
                    "Não é possível remover um fornecedor vinculado a produtos.");
            }

            fornecedorService.RemoverFornecedor(id);

            await auditoriaService.RegistrarAsync(
                context,
                "Remoção",
                "Fornecedor",
                id.ToString(),
                $"Fornecedor {id} - {fornecedor.Nome} removido.",
                new { fornecedor.Id, fornecedor.Codigo, fornecedor.Nome });

            return Results.NoContent();
        }).RequireAuthorization(politicaAdministrar).RequireAntiforgery();
    }
}
