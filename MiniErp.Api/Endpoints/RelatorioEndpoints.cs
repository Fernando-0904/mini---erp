using MiniErp.Api.Services;

namespace MiniErp.Api.Endpoints;

internal static class RelatorioEndpoints
{
    internal static void MapRelatorioEndpoints(this WebApplication app, string politicaAdministrar)
    {
        app.MapGet("/relatorios/alertas-operacionais", async (RelatorioService relatorioService) =>
        {
            return Results.Ok(await relatorioService.ListarAlertasOperacionaisAsync());
        });

        app.MapGet("/relatorios/auditoria", async (int? limite, RelatorioService relatorioService) =>
        {
            return Results.Ok(await relatorioService.ListarAuditoriaAsync(limite ?? 30));
        }).RequireAuthorization(politicaAdministrar);

        app.MapGet("/relatorios/produtos-estoque-baixo", async (RelatorioService relatorioService) =>
        {
            return Results.Ok(await relatorioService.ListarProdutosEstoqueBaixoAsync());
        });

        app.MapGet("/relatorios/produtos-sem-estoque", async (RelatorioService relatorioService) =>
        {
            return Results.Ok(await relatorioService.ListarProdutosSemEstoqueAsync());
        });

        app.MapGet("/relatorios/valor-estoque-por-categoria", async (RelatorioService relatorioService) =>
        {
            return Results.Ok(await relatorioService.ListarValorEstoquePorCategoriaAsync());
        });

        app.MapGet("/relatorios/produtos-sem-fornecedor", async (RelatorioService relatorioService) =>
        {
            return Results.Ok(await relatorioService.ListarProdutosSemFornecedorAsync());
        });

        app.MapGet("/relatorios/ultimas-movimentacoes", async (int? limite, RelatorioService relatorioService) =>
        {
            return Results.Ok(await relatorioService.ListarUltimasMovimentacoesAsync(limite ?? 10));
        });

        app.MapGet("/relatorios/exportar", async (string tipo, int? limite, RelatorioService relatorioService, HttpContext context) =>
        {
            if (string.IsNullOrWhiteSpace(tipo))
            {
                return ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Dados inválidos.",
                    "Informe o tipo do relatório para exportação.");
            }

            string tipoNormalizado = tipo.Trim().ToLowerInvariant();

            return tipoNormalizado switch
            {
                "produtos-estoque-baixo" => ApiHttpHelpers.CriarArquivoCsv(
                    "relatorio-produtos-estoque-baixo",
                    ApiHttpHelpers.GerarCsv(
                        ["codigo", "nome", "categoria", "quantidadeEstoque", "estoqueMinimo"],
                        (await relatorioService.ListarProdutosEstoqueBaixoAsync())
                            .Select(item =>
                                new[] { item.Codigo.ToString(), item.Nome, item.Categoria, item.QuantidadeEstoque.ToString(), item.EstoqueMinimo.ToString() }))),

                "produtos-sem-estoque" => ApiHttpHelpers.CriarArquivoCsv(
                    "relatorio-produtos-sem-estoque",
                    ApiHttpHelpers.GerarCsv(
                        ["codigo", "nome", "categoria", "quantidadeEstoque"],
                        (await relatorioService.ListarProdutosSemEstoqueAsync())
                            .Select(item =>
                                new[] { item.Codigo.ToString(), item.Nome, item.Categoria, item.QuantidadeEstoque.ToString() }))),

                "valor-estoque-por-categoria" => ApiHttpHelpers.CriarArquivoCsv(
                    "relatorio-valor-estoque-por-categoria",
                    ApiHttpHelpers.GerarCsv(
                        ["categoria", "valorTotal"],
                        (await relatorioService.ListarValorEstoquePorCategoriaAsync())
                            .Select(item =>
                                new[] { item.Categoria, item.ValorTotal.ToString(System.Globalization.CultureInfo.InvariantCulture) }))),

                "produtos-sem-fornecedor" => ApiHttpHelpers.CriarArquivoCsv(
                    "relatorio-produtos-sem-fornecedor",
                    ApiHttpHelpers.GerarCsv(
                        ["codigo", "nome", "categoria"],
                        (await relatorioService.ListarProdutosSemFornecedorAsync())
                            .Select(item =>
                                new[] { item.Codigo.ToString(), item.Nome, item.Categoria }))),

                "ultimas-movimentacoes" => ApiHttpHelpers.CriarArquivoCsv(
                    "relatorio-ultimas-movimentacoes",
                    ApiHttpHelpers.GerarCsv(
                        ["produto", "tipo", "quantidade", "saldoAnterior", "saldoNovo", "dataMovimentacaoUtc"],
                        (await relatorioService.ListarUltimasMovimentacoesAsync(limite ?? 10))
                            .Select(item =>
                                new[]
                                {
                                    item.Produto,
                                    item.Tipo,
                                    item.Quantidade.ToString(),
                                    item.SaldoAnterior.ToString(),
                                    item.SaldoNovo.ToString(),
                                    item.DataMovimentacaoUtc.ToString("O")
                                }))),

                _ => ApiHttpHelpers.CriarProblem(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Dados inválidos.",
                    "Tipo de relatório não suportado para exportação.")
            };
        });
    }
}
