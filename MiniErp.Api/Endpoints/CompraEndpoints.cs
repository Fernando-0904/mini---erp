using Microsoft.AspNetCore.Antiforgery;
using MiniErp.Api.DTOs;
using MiniErp.Api.Security;
using MiniErp.Api.Services;

namespace MiniErp.Api.Endpoints;

internal static class CompraEndpoints
{
    internal static void MapCompraEndpoints(this WebApplication app, string politicaOperar, string politicaAdministrar)
    {
        app.MapGet("/compras/pedidos", async (PedidoCompraService pedidoCompraService) =>
        {
            return Results.Ok(await pedidoCompraService.ListarPedidosAsync());
        }).RequireAuthorization(politicaOperar);

        app.MapPost("/compras/pedidos", async (PedidoCompraRequest request, PedidoCompraService pedidoCompraService, AuditoriaService auditoriaService, HttpContext context) =>
        {
            (PedidoCompraResponse? pedido, List<string> erros) = await pedidoCompraService.CriarPedidoAsync(request);

            if (erros.Count > 0)
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Dados inválidos.",
                    string.Join(" ", erros));
            }

            await auditoriaService.RegistrarAsync(
                context,
                "Cadastro",
                "PedidoCompra",
                pedido!.Id.ToString(),
                $"Pedido de compra {pedido.Id} criado para fornecedor {pedido.FornecedorId}.",
                new { pedido.FornecedorId, QuantidadeItens = pedido.Itens.Count, pedido.ValorTotal });

            return Results.Created($"/compras/pedidos/{pedido.Id}", pedido);
        }).RequireAuthorization(politicaOperar).RequireAntiforgery();

        app.MapPost("/compras/pedidos/{id:int}/receber", async (int id, PedidoCompraService pedidoCompraService, AuditoriaService auditoriaService, HttpContext context) =>
        {
            (PedidoCompraResponse? pedido, string erro) = await pedidoCompraService.ReceberPedidoAsync(id);

            if (!string.IsNullOrWhiteSpace(erro))
            {
                int statusCode = erro.Contains("não encontrado", StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status400BadRequest;

                return ApiHttpHelpers.CriarProblem(
                    context,
                    statusCode,
                    statusCode == StatusCodes.Status404NotFound ? "Recurso não encontrado." : "Operação inválida.",
                    erro);
            }

            await auditoriaService.RegistrarAsync(
                context,
                "Recebimento",
                "PedidoCompra",
                pedido!.Id.ToString(),
                $"Pedido de compra {pedido.Id} recebido e estoque atualizado.",
                new { pedido.FornecedorId, QuantidadeItens = pedido.Itens.Count, pedido.ValorTotal });

            return Results.Ok(pedido);
        }).RequireAuthorization(politicaOperar).RequireAntiforgery();

        app.MapPost("/compras/pedidos/{id:int}/aprovar", async (int id, PedidoCompraService pedidoCompraService, AuditoriaService auditoriaService, HttpContext context) =>
        {
            (PedidoCompraResponse? pedido, string erro) = await pedidoCompraService.AprovarPedidoAsync(id);

            if (!string.IsNullOrWhiteSpace(erro))
            {
                int statusCode = erro.Contains("não encontrado", StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status400BadRequest;

                return ApiHttpHelpers.CriarProblem(
                    context,
                    statusCode,
                    statusCode == StatusCodes.Status404NotFound ? "Recurso não encontrado." : "Operação inválida.",
                    erro);
            }

            await auditoriaService.RegistrarAsync(
                context,
                "Aprovação",
                "PedidoCompra",
                pedido!.Id.ToString(),
                $"Pedido de compra {pedido.Id} aprovado.",
                new { pedido.FornecedorId, QuantidadeItens = pedido.Itens.Count, pedido.ValorTotal });

            return Results.Ok(pedido);
        }).RequireAuthorization(politicaAdministrar).RequireAntiforgery();

        app.MapPost("/compras/pedidos/{id:int}/rejeitar", async (int id, RejeicaoPedidoCompraRequest request, PedidoCompraService pedidoCompraService, AuditoriaService auditoriaService, HttpContext context) =>
        {
            (PedidoCompraResponse? pedido, string erro) = await pedidoCompraService.RejeitarPedidoAsync(id, request.Motivo);

            if (!string.IsNullOrWhiteSpace(erro))
            {
                int statusCode = erro.Contains("não encontrado", StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status400BadRequest;

                return ApiHttpHelpers.CriarProblem(
                    context,
                    statusCode,
                    statusCode == StatusCodes.Status404NotFound ? "Recurso não encontrado." : "Operação inválida.",
                    erro);
            }

            await auditoriaService.RegistrarAsync(
                context,
                "Rejeição",
                "PedidoCompra",
                pedido!.Id.ToString(),
                $"Pedido de compra {pedido.Id} rejeitado.",
                new { pedido.FornecedorId, QuantidadeItens = pedido.Itens.Count, pedido.ValorTotal, pedido.MotivoRejeicao });

            return Results.Ok(pedido);
        }).RequireAuthorization(politicaAdministrar).RequireAntiforgery();
    }
}
