using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MiniErp.Api.Data;
using MiniErp.Api.Models;
using Xunit;

namespace MiniErp.Api.Tests;

public class RelatoriosIntegrationTests
{
    [Fact]
    public async Task R01_DeveListarProdutoAbaixoDoEstoqueMinimo()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        await AutenticarAdministrador(client);

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            AppDbContext contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Categoria categoria = new() { Nome = $"Categoria R01 {Guid.NewGuid():N}" };
            contexto.Categorias.Add(categoria);
            contexto.SaveChanges();

            contexto.Produtos.Add(new Produto
            {
                Codigo = 8101,
                Nome = "Produto abaixo",
                PrecoUnitario = 10m,
                QuantidadeEstoque = 1,
                EstoqueMinimo = 3,
                CategoriaId = categoria.Id
            });
            contexto.SaveChanges();
        }

        HttpResponseMessage response = await client.GetAsync("/relatorios/produtos-estoque-baixo");
        List<ProdutoEstoqueBaixoDto>? itens = await response.Content.ReadFromJsonAsync<List<ProdutoEstoqueBaixoDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(itens);
        Assert.Contains(itens, item => item.Codigo == 8101 && item.QuantidadeEstoque <= item.EstoqueMinimo);
    }

    [Fact]
    public async Task R02_NaoDeveListarProdutoAcimaDoEstoqueMinimo()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        await AutenticarAdministrador(client);

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            AppDbContext contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Categoria categoria = new() { Nome = $"Categoria R02 {Guid.NewGuid():N}" };
            contexto.Categorias.Add(categoria);
            contexto.SaveChanges();

            contexto.Produtos.Add(new Produto
            {
                Codigo = 8102,
                Nome = "Produto acima",
                PrecoUnitario = 10m,
                QuantidadeEstoque = 10,
                EstoqueMinimo = 2,
                CategoriaId = categoria.Id
            });
            contexto.SaveChanges();
        }

        HttpResponseMessage response = await client.GetAsync("/relatorios/produtos-estoque-baixo");
        List<ProdutoEstoqueBaixoDto>? itens = await response.Content.ReadFromJsonAsync<List<ProdutoEstoqueBaixoDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(itens);
        Assert.DoesNotContain(itens, item => item.Codigo == 8102);
    }

    [Fact]
    public async Task R03_DeveListarProdutoSemEstoque()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        await AutenticarAdministrador(client);

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            AppDbContext contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Categoria categoria = new() { Nome = $"Categoria R03 {Guid.NewGuid():N}" };
            contexto.Categorias.Add(categoria);
            contexto.SaveChanges();

            contexto.Produtos.AddRange(
                new Produto
                {
                    Codigo = 8103,
                    Nome = "Sem estoque",
                    PrecoUnitario = 11m,
                    QuantidadeEstoque = 0,
                    EstoqueMinimo = 1,
                    CategoriaId = categoria.Id
                },
                new Produto
                {
                    Codigo = 8104,
                    Nome = "Com estoque",
                    PrecoUnitario = 11m,
                    QuantidadeEstoque = 2,
                    EstoqueMinimo = 1,
                    CategoriaId = categoria.Id
                });
            contexto.SaveChanges();
        }

        HttpResponseMessage response = await client.GetAsync("/relatorios/produtos-sem-estoque");
        List<ProdutoSemEstoqueDto>? itens = await response.Content.ReadFromJsonAsync<List<ProdutoSemEstoqueDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(itens);
        Assert.Contains(itens, item => item.Codigo == 8103 && item.QuantidadeEstoque == 0);
        Assert.DoesNotContain(itens, item => item.Codigo == 8104);
    }

    [Fact]
    public async Task R04_DeveCalcularValorTotalPorCategoria()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        await AutenticarAdministrador(client);

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            AppDbContext contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Categoria categoriaA = new() { Nome = $"Categoria A R04 {Guid.NewGuid():N}" };
            Categoria categoriaB = new() { Nome = $"Categoria B R04 {Guid.NewGuid():N}" };
            contexto.Categorias.AddRange(categoriaA, categoriaB);
            contexto.SaveChanges();

            contexto.Produtos.AddRange(
                new Produto
                {
                    Codigo = 8105,
                    Nome = "Produto A1",
                    PrecoUnitario = 10m,
                    QuantidadeEstoque = 2,
                    EstoqueMinimo = 1,
                    CategoriaId = categoriaA.Id
                },
                new Produto
                {
                    Codigo = 8106,
                    Nome = "Produto A2",
                    PrecoUnitario = 5m,
                    QuantidadeEstoque = 1,
                    EstoqueMinimo = 1,
                    CategoriaId = categoriaA.Id
                },
                new Produto
                {
                    Codigo = 8107,
                    Nome = "Produto B1",
                    PrecoUnitario = 7m,
                    QuantidadeEstoque = 3,
                    EstoqueMinimo = 1,
                    CategoriaId = categoriaB.Id
                });
            contexto.SaveChanges();
        }

        HttpResponseMessage response = await client.GetAsync("/relatorios/valor-estoque-por-categoria");
        List<ValorEstoquePorCategoriaDto>? itens = await response.Content.ReadFromJsonAsync<List<ValorEstoquePorCategoriaDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(itens);
        Assert.Contains(itens, item => item.Categoria.StartsWith("Categoria A R04", StringComparison.Ordinal) && item.ValorTotal == 25m);
        Assert.Contains(itens, item => item.Categoria.StartsWith("Categoria B R04", StringComparison.Ordinal) && item.ValorTotal == 21m);
    }

    [Fact]
    public async Task R05_DeveListarProdutoSemFornecedor()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        await AutenticarAdministrador(client);

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            AppDbContext contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Categoria categoria = new() { Nome = $"Categoria R05 {Guid.NewGuid():N}" };
            contexto.Categorias.Add(categoria);
            contexto.SaveChanges();

            contexto.Produtos.Add(new Produto
            {
                Codigo = 8108,
                Nome = "Sem fornecedor",
                PrecoUnitario = 12m,
                QuantidadeEstoque = 1,
                EstoqueMinimo = 1,
                CategoriaId = categoria.Id,
                FornecedorId = null
            });
            contexto.SaveChanges();
        }

        HttpResponseMessage response = await client.GetAsync("/relatorios/produtos-sem-fornecedor");
        List<ProdutoSemFornecedorDto>? itens = await response.Content.ReadFromJsonAsync<List<ProdutoSemFornecedorDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(itens);
        Assert.Contains(itens, item => item.Codigo == 8108);
    }

    [Fact]
    public async Task R06_NaoDeveListarProdutoComFornecedor()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        await AutenticarAdministrador(client);

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            AppDbContext contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Categoria categoria = new() { Nome = $"Categoria R06 {Guid.NewGuid():N}" };
            Fornecedor fornecedor = new()
            {
                Codigo = 9906,
                Nome = "Fornecedor R06",
                Documento = $"DOC-R06-{Guid.NewGuid():N}".Substring(0, 20),
                Email = "fornecedor.r06@teste.com",
                Telefone = "11999999999",
                Ativo = true
            };
            contexto.Categorias.Add(categoria);
            contexto.Fornecedores.Add(fornecedor);
            contexto.SaveChanges();

            contexto.Produtos.Add(new Produto
            {
                Codigo = 8109,
                Nome = "Com fornecedor",
                PrecoUnitario = 20m,
                QuantidadeEstoque = 1,
                EstoqueMinimo = 1,
                CategoriaId = categoria.Id,
                FornecedorId = fornecedor.Id
            });
            contexto.SaveChanges();
        }

        HttpResponseMessage response = await client.GetAsync("/relatorios/produtos-sem-fornecedor");
        List<ProdutoSemFornecedorDto>? itens = await response.Content.ReadFromJsonAsync<List<ProdutoSemFornecedorDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(itens);
        Assert.DoesNotContain(itens, item => item.Codigo == 8109);
    }

    [Fact]
    public async Task R07_DeveListarUltimasMovimentacoesOrdenadasPorData()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        await AutenticarAdministrador(client);

        DateTime maisAntiga = new(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime maisRecente = new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            AppDbContext contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Categoria categoria = new() { Nome = $"Categoria R07 {Guid.NewGuid():N}" };
            contexto.Categorias.Add(categoria);
            contexto.SaveChanges();

            Produto produto = new()
            {
                Codigo = 8110,
                Nome = "Produto movimentado",
                PrecoUnitario = 9m,
                QuantidadeEstoque = 3,
                EstoqueMinimo = 1,
                CategoriaId = categoria.Id
            };
            contexto.Produtos.Add(produto);
            contexto.SaveChanges();

            contexto.MovimentacoesEstoque.AddRange(
                new MovimentacaoEstoque
                {
                    ProdutoCodigo = produto.Codigo,
                    Tipo = TipoMovimentacaoEstoque.Entrada,
                    Quantidade = 3,
                    SaldoAnterior = 0,
                    SaldoNovo = 3,
                    DataMovimentacaoUtc = maisAntiga
                },
                new MovimentacaoEstoque
                {
                    ProdutoCodigo = produto.Codigo,
                    Tipo = TipoMovimentacaoEstoque.Saida,
                    Quantidade = 1,
                    SaldoAnterior = 3,
                    SaldoNovo = 2,
                    DataMovimentacaoUtc = maisRecente
                });
            contexto.SaveChanges();
        }

        HttpResponseMessage response = await client.GetAsync("/relatorios/ultimas-movimentacoes");
        List<UltimaMovimentacaoDto>? itens = await response.Content.ReadFromJsonAsync<List<UltimaMovimentacaoDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(itens);
        Assert.True(itens.Count >= 2);
        Assert.Equal(maisRecente, itens[0].DataMovimentacaoUtc);
        Assert.Equal(maisAntiga, itens[1].DataMovimentacaoUtc);
    }

    [Fact]
    public async Task R08_DeveRespeitarLimiteDasUltimasMovimentacoes()
    {
        using MiniErpApiFactory factory = new();
        using HttpClient client = factory.CriarCliente();
        await AutenticarAdministrador(client);

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            AppDbContext contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Categoria categoria = new() { Nome = $"Categoria R08 {Guid.NewGuid():N}" };
            contexto.Categorias.Add(categoria);
            contexto.SaveChanges();

            Produto produto = new()
            {
                Codigo = 8111,
                Nome = "Produto limite",
                PrecoUnitario = 8m,
                QuantidadeEstoque = 5,
                EstoqueMinimo = 1,
                CategoriaId = categoria.Id
            };
            contexto.Produtos.Add(produto);
            contexto.SaveChanges();

            contexto.MovimentacoesEstoque.AddRange(
                new MovimentacaoEstoque
                {
                    ProdutoCodigo = produto.Codigo,
                    Tipo = TipoMovimentacaoEstoque.Entrada,
                    Quantidade = 2,
                    SaldoAnterior = 0,
                    SaldoNovo = 2,
                    DataMovimentacaoUtc = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc)
                },
                new MovimentacaoEstoque
                {
                    ProdutoCodigo = produto.Codigo,
                    Tipo = TipoMovimentacaoEstoque.Entrada,
                    Quantidade = 2,
                    SaldoAnterior = 2,
                    SaldoNovo = 4,
                    DataMovimentacaoUtc = new DateTime(2026, 8, 1, 11, 0, 0, DateTimeKind.Utc)
                },
                new MovimentacaoEstoque
                {
                    ProdutoCodigo = produto.Codigo,
                    Tipo = TipoMovimentacaoEstoque.Saida,
                    Quantidade = 1,
                    SaldoAnterior = 4,
                    SaldoNovo = 3,
                    DataMovimentacaoUtc = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc)
                });
            contexto.SaveChanges();
        }

        HttpResponseMessage response = await client.GetAsync("/relatorios/ultimas-movimentacoes?limite=2");
        List<UltimaMovimentacaoDto>? itens = await response.Content.ReadFromJsonAsync<List<UltimaMovimentacaoDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(itens);
        Assert.Equal(2, itens.Count);
    }

    private static async Task AutenticarAdministrador(HttpClient client)
    {
        string token = await ObterTokenAntiforgery(client);
        HttpResponseMessage response = await PostComToken(client, "/auth/login", new
        {
            email = "admin@mini-erp.com",
            senha = "123456"
        }, token);

        response.EnsureSuccessStatusCode();
    }

    private static async Task<string> ObterTokenAntiforgery(HttpClient client)
    {
        CsrfResponse? response = await client.GetFromJsonAsync<CsrfResponse>("/auth/csrf");

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));

        return response.Token;
    }

    private static async Task<HttpResponseMessage> PostComToken(HttpClient client, string rota, object? payload, string token)
    {
        HttpRequestMessage request = new(HttpMethod.Post, rota);
        request.Headers.Add("X-CSRF-TOKEN", token);

        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        return await client.SendAsync(request);
    }

    private sealed class CsrfResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    private sealed class ProdutoEstoqueBaixoDto
    {
        public int Codigo { get; set; }
        public int QuantidadeEstoque { get; set; }
        public int EstoqueMinimo { get; set; }
    }

    private sealed class ProdutoSemEstoqueDto
    {
        public int Codigo { get; set; }
        public int QuantidadeEstoque { get; set; }
    }

    private sealed class ValorEstoquePorCategoriaDto
    {
        public string Categoria { get; set; } = string.Empty;
        public decimal ValorTotal { get; set; }
    }

    private sealed class ProdutoSemFornecedorDto
    {
        public int Codigo { get; set; }
    }

    private sealed class UltimaMovimentacaoDto
    {
        public DateTime DataMovimentacaoUtc { get; set; }
    }
}