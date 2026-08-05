using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using MiniErp.Api.Data;
using MiniErp.Api.DTOs;
using MiniErp.Api.Models;
using MiniErp.Api.Security;
using MiniErp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

const string PerfilAdministrador = "Administrador";
const string PerfilAdminLegado = "Admin";
const string PerfilOperador = "Operador";
const string PerfilUsuarioLegado = "Usuário";
const string PoliticaOperar = "PodeOperar";
const string PoliticaAdministrar = "PodeAdministrar";

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
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "MiniErp.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = builder.Environment.IsDevelopment()
            ? SameSiteMode.Strict
            : SameSiteMode.None;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = async context =>
        {
            await EscreverProblemAsync(
                context.HttpContext,
                StatusCodes.Status401Unauthorized,
                "Sessão expirada ou não autenticada.",
                "Faça login para continuar.");
        };
        options.Events.OnRedirectToAccessDenied = async context =>
        {
            await EscreverProblemAsync(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                "Acesso negado.",
                "Seu perfil não tem permissão para executar esta operação.");
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.AddPolicy(PoliticaOperar, policy => policy.RequireRole(
        PerfilAdministrador,
        PerfilAdminLegado,
        PerfilOperador,
        PerfilUsuarioLegado));
    options.AddPolicy(PoliticaAdministrar, policy => policy.RequireRole(
        PerfilAdministrador,
        PerfilAdminLegado));
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "MiniErp.Csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = builder.Environment.IsDevelopment()
        ? SameSiteMode.Strict
        : SameSiteMode.None;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});
string databaseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=Dados/mini-erp.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(databaseConnectionString));
builder.Services.AddScoped<ProdutoService>();
builder.Services.AddScoped<CategoriaService>();
builder.Services.AddScoped<FornecedorService>();
builder.Services.AddScoped<MovimentacaoEstoqueService>();
builder.Services.AddScoped<RelatorioService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<UsuarioLocalService>();
builder.Services.AddSingleton<EmailSimuladoService>();
builder.Services.AddSingleton<IEmailService>(serviceProvider => serviceProvider.GetRequiredService<EmailSimuladoService>());
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        IExceptionHandlerFeature? exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        ILoggerFactory loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger("GlobalExceptionHandler");

        logger.LogError(
            exceptionFeature?.Error,
            "Erro não tratado na API. CorrelationId: {CorrelationId}",
            context.TraceIdentifier);

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        ProblemDetails problemDetails = new()
        {
            Title = "Erro inesperado.",
            Detail = "Ocorreu um erro inesperado. Tente novamente e informe o código de correlação se o problema persistir.",
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://httpstatuses.com/500",
            Instance = context.Request.Path
        };
        problemDetails.Extensions["correlationId"] = context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Correlation-Id"] = context.TraceIdentifier;
    await next();
});

app.UseStatusCodePages(async statusCodeContext =>
{
    HttpContext context = statusCodeContext.HttpContext;

    if (context.Response.HasStarted)
    {
        return;
    }

    if (context.Response.ContentLength is > 0)
    {
        return;
    }

    int statusCode = context.Response.StatusCode;

    if (statusCode == StatusCodes.Status404NotFound)
    {
        await EscreverProblemAsync(
            context,
            StatusCodes.Status404NotFound,
            "Recurso não encontrado.",
            "O recurso solicitado não foi localizado.");
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("MiniErpCors");
app.UseAuthentication();
app.UseAuthorization();

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
        return CriarProblem(
            context,
            StatusCodes.Status404NotFound,
            "Recurso não encontrado.",
            "Produto não encontrado.");
    }

    return Results.Ok(produto);
});

app.MapPost("/produtos", async (ProdutoRequest request, ProdutoService produtoService, CategoriaService categoriaService, FornecedorService fornecedorService, AuditoriaService auditoriaService, HttpContext context) =>
{
    Produto produto = MapearProdutoRequest(request);
    List<string> erros = produtoService.ValidarProduto(produto);
    erros.AddRange(categoriaService.ValidarCategoriaDoProduto(produto.CategoriaId));
    erros.AddRange(fornecedorService.ValidarFornecedorDoProduto(produto.FornecedorId));

    if (erros.Count > 0)
    {
        return CriarProblem(
            context,
            StatusCodes.Status400BadRequest,
            "Dados inválidos.",
            string.Join(" ", erros));
    }

    bool cadastrado = produtoService.CadastrarProduto(produto);

    if (!cadastrado)
    {
        return CriarProblem(
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
}).RequireAuthorization(PoliticaOperar).RequireAntiforgery();

app.MapPut("/produtos/{codigo:int}", async (int codigo, ProdutoRequest request, ProdutoService produtoService, CategoriaService categoriaService, FornecedorService fornecedorService, AuditoriaService auditoriaService, HttpContext context) =>
{
    Produto produtoAtualizado = MapearProdutoRequest(request);
    Produto? produtoExistente = produtoService.BuscarPorCodigo(codigo);

    if (produtoExistente == null)
    {
        return CriarProblem(
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
        return CriarProblem(
            context,
            StatusCodes.Status400BadRequest,
            "Dados inválidos.",
            string.Join(" ", erros));
    }

    bool editado = produtoService.EditarProduto(codigo, produtoAtualizado);

    if (!editado)
    {
        return CriarProblem(
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
}).RequireAuthorization(PoliticaOperar).RequireAntiforgery();

app.MapDelete("/produtos/{codigo:int}", async (int codigo, ProdutoService produtoService, AuditoriaService auditoriaService, HttpContext context) =>
{
    Produto? produtoExistente = produtoService.BuscarPorCodigo(codigo);

    if (produtoExistente == null)
    {
        return CriarProblem(
            context,
            StatusCodes.Status404NotFound,
            "Recurso não encontrado.",
            "Produto não encontrado.");
    }

    bool removido = produtoService.RemoverProduto(codigo);

    if (!removido)
    {
        return CriarProblem(
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
}).RequireAuthorization(PoliticaAdministrar).RequireAntiforgery();

app.MapGet(
    "/produtos/{codigo:int}/movimentacoes",
    (int codigo, ProdutoService produtoService, MovimentacaoEstoqueService movimentacaoService, HttpContext context) =>
    {
        Produto? produto = produtoService.BuscarPorCodigo(codigo);

        if (produto == null)
        {
            return CriarProblem(
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
                return CriarProblem(
                    context,
                    StatusCodes.Status404NotFound,
                    "Recurso não encontrado.",
                    erro);
            }

            return CriarProblem(
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
    }).RequireAuthorization(PoliticaOperar).RequireAntiforgery();

app.MapPost(
    "/produtos/{codigo:int}/movimentacoes/saida",
    async (int codigo, MovimentacaoEstoqueRequest request, MovimentacaoEstoqueService movimentacaoService, AuditoriaService auditoriaService, HttpContext context) =>
    {
        bool movimentado = movimentacaoService.RegistrarSaida(codigo, request.Quantidade, out MovimentacaoEstoque? movimentacao, out string erro);

        if (!movimentado)
        {
            if (erro == "Produto não encontrado.")
            {
                return CriarProblem(
                    context,
                    StatusCodes.Status404NotFound,
                    "Recurso não encontrado.",
                    erro);
            }

            return CriarProblem(
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
    }).RequireAuthorization(PoliticaOperar).RequireAntiforgery();

app.MapGet("/categorias", (CategoriaService categoriaService) =>
{
    return Results.Ok(categoriaService.ListarCategorias());
});

app.MapGet("/categorias/{id:int}", (int id, CategoriaService categoriaService, HttpContext context) =>
{
    Categoria? categoria = categoriaService.BuscarPorId(id);

    if (categoria == null)
    {
        return CriarProblem(
            context,
            StatusCodes.Status404NotFound,
            "Recurso não encontrado.",
            "Categoria não encontrada.");
    }

    return Results.Ok(categoria);
});

app.MapPost("/categorias", async (CategoriaRequest request, CategoriaService categoriaService, AuditoriaService auditoriaService, HttpContext context) =>
{
    Categoria categoria = MapearCategoriaRequest(request);
    List<string> erros = categoriaService.ValidarCategoria(categoria);

    if (erros.Count > 0)
    {
        return CriarProblem(
            context,
            StatusCodes.Status400BadRequest,
            "Dados inválidos.",
            string.Join(" ", erros));
    }

    bool cadastrada = categoriaService.CadastrarCategoria(categoria);

    if (!cadastrada)
    {
        return CriarProblem(
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
}).RequireAuthorization(PoliticaOperar).RequireAntiforgery();

app.MapPut("/categorias/{id:int}", async (int id, CategoriaRequest request, CategoriaService categoriaService, AuditoriaService auditoriaService, HttpContext context) =>
{
    Categoria categoriaAtualizada = MapearCategoriaRequest(request);
    List<string> erros = categoriaService.ValidarCategoria(categoriaAtualizada);

    if (id != categoriaAtualizada.Id)
    {
        erros.Add("O id da URL deve ser igual ao id da categoria.");
    }

    if (erros.Count > 0)
    {
        return CriarProblem(
            context,
            StatusCodes.Status400BadRequest,
            "Dados inválidos.",
            string.Join(" ", erros));
    }

    bool editada = categoriaService.EditarCategoria(id, categoriaAtualizada);

    if (!editada)
    {
        return CriarProblem(
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
}).RequireAuthorization(PoliticaOperar).RequireAntiforgery();

app.MapDelete("/categorias/{id:int}", async (int id, CategoriaService categoriaService, AuditoriaService auditoriaService, HttpContext context) =>
{
    Categoria? categoria = categoriaService.BuscarPorId(id);

    if (categoria == null)
    {
        return CriarProblem(
            context,
            StatusCodes.Status404NotFound,
            "Recurso não encontrado.",
            "Categoria não encontrada.");
    }

    if (categoriaService.PossuiProdutosVinculados(id))
    {
        return CriarProblem(
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
}).RequireAuthorization(PoliticaAdministrar).RequireAntiforgery();

app.MapGet("/fornecedores", (FornecedorService fornecedorService) =>
{
    return Results.Ok(fornecedorService.ListarFornecedores());
});

app.MapGet("/fornecedores/{id:int}", (int id, FornecedorService fornecedorService, HttpContext context) =>
{
    Fornecedor? fornecedor = fornecedorService.BuscarPorId(id);

    if (fornecedor == null)
    {
        return CriarProblem(
            context,
            StatusCodes.Status404NotFound,
            "Recurso não encontrado.",
            "Fornecedor não encontrado.");
    }

    return Results.Ok(fornecedor);
});

app.MapPost("/fornecedores", async (FornecedorRequest request, FornecedorService fornecedorService, AuditoriaService auditoriaService, HttpContext context) =>
{
    Fornecedor fornecedor = MapearFornecedorRequest(request);
    List<string> erros = fornecedorService.ValidarFornecedor(fornecedor);

    if (erros.Count > 0)
    {
        return CriarProblem(
            context,
            StatusCodes.Status400BadRequest,
            "Dados inválidos.",
            string.Join(" ", erros));
    }

    bool cadastrado = fornecedorService.CadastrarFornecedor(fornecedor);

    if (!cadastrado)
    {
        return CriarProblem(
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
}).RequireAuthorization(PoliticaOperar).RequireAntiforgery();

app.MapPut("/fornecedores/{id:int}", async (int id, FornecedorRequest request, FornecedorService fornecedorService, AuditoriaService auditoriaService, HttpContext context) =>
{
    if (fornecedorService.BuscarPorId(id) == null)
    {
        return CriarProblem(
            context,
            StatusCodes.Status404NotFound,
            "Recurso não encontrado.",
            "Fornecedor não encontrado.");
    }

    Fornecedor fornecedorAtualizado = MapearFornecedorRequest(request);
    List<string> erros = fornecedorService.ValidarFornecedor(fornecedorAtualizado);

    if (erros.Count > 0)
    {
        return CriarProblem(
            context,
            StatusCodes.Status400BadRequest,
            "Dados inválidos.",
            string.Join(" ", erros));
    }

    bool editado = fornecedorService.EditarFornecedor(id, fornecedorAtualizado);

    if (!editado)
    {
        return CriarProblem(
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
}).RequireAuthorization(PoliticaOperar).RequireAntiforgery();

app.MapPatch("/fornecedores/{id:int}/inativar", async (int id, FornecedorService fornecedorService, AuditoriaService auditoriaService, HttpContext context) =>
{
    if (!fornecedorService.InativarFornecedor(id))
    {
        return CriarProblem(
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
}).RequireAuthorization(PoliticaOperar).RequireAntiforgery();

app.MapDelete("/fornecedores/{id:int}", async (int id, FornecedorService fornecedorService, AuditoriaService auditoriaService, HttpContext context) =>
{
    Fornecedor? fornecedor = fornecedorService.BuscarPorId(id);

    if (fornecedor == null)
    {
        return CriarProblem(
            context,
            StatusCodes.Status404NotFound,
            "Recurso não encontrado.",
            "Fornecedor não encontrado.");
    }

    if (fornecedorService.PossuiProdutosVinculados(id))
    {
        return CriarProblem(
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
}).RequireAuthorization(PoliticaAdministrar).RequireAntiforgery();

app.MapGet("/relatorios/alertas-operacionais", async (RelatorioService relatorioService) =>
{
    return Results.Ok(await relatorioService.ListarAlertasOperacionaisAsync());
});

app.MapGet("/relatorios/auditoria", async (int? limite, RelatorioService relatorioService) =>
{
    return Results.Ok(await relatorioService.ListarAuditoriaAsync(limite ?? 30));
}).RequireAuthorization(PoliticaAdministrar);

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
        return CriarProblem(
            context,
            StatusCodes.Status400BadRequest,
            "Dados inválidos.",
            "Informe o tipo do relatório para exportação.");
    }

    string tipoNormalizado = tipo.Trim().ToLowerInvariant();

    return tipoNormalizado switch
    {
        "produtos-estoque-baixo" => CriarArquivoCsv(
            "relatorio-produtos-estoque-baixo",
            GerarCsv(
                ["codigo", "nome", "categoria", "quantidadeEstoque", "estoqueMinimo"],
                (await relatorioService.ListarProdutosEstoqueBaixoAsync())
                    .Select(item =>
                        new[] { item.Codigo.ToString(), item.Nome, item.Categoria, item.QuantidadeEstoque.ToString(), item.EstoqueMinimo.ToString() }))),

        "produtos-sem-estoque" => CriarArquivoCsv(
            "relatorio-produtos-sem-estoque",
            GerarCsv(
                ["codigo", "nome", "categoria", "quantidadeEstoque"],
                (await relatorioService.ListarProdutosSemEstoqueAsync())
                    .Select(item =>
                        new[] { item.Codigo.ToString(), item.Nome, item.Categoria, item.QuantidadeEstoque.ToString() }))),

        "valor-estoque-por-categoria" => CriarArquivoCsv(
            "relatorio-valor-estoque-por-categoria",
            GerarCsv(
                ["categoria", "valorTotal"],
                (await relatorioService.ListarValorEstoquePorCategoriaAsync())
                    .Select(item =>
                        new[] { item.Categoria, item.ValorTotal.ToString(System.Globalization.CultureInfo.InvariantCulture) }))),

        "produtos-sem-fornecedor" => CriarArquivoCsv(
            "relatorio-produtos-sem-fornecedor",
            GerarCsv(
                ["codigo", "nome", "categoria"],
                (await relatorioService.ListarProdutosSemFornecedorAsync())
                    .Select(item =>
                        new[] { item.Codigo.ToString(), item.Nome, item.Categoria }))),

        "ultimas-movimentacoes" => CriarArquivoCsv(
            "relatorio-ultimas-movimentacoes",
            GerarCsv(
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

        _ => CriarProblem(
            context,
            StatusCodes.Status400BadRequest,
            "Dados inválidos.",
            "Tipo de relatório não suportado para exportação.")
    };
});

app.MapGet("/auth/csrf", (HttpContext context, IAntiforgery antiforgery) =>
{
    AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
    context.Response.Headers.CacheControl = "no-store";
    return Results.Ok(new { token = tokens.RequestToken });
})
    .AllowAnonymous();

app.MapPost("/auth/cadastro", async (CadastroUsuarioRequest request, UsuarioLocalService usuarioService, HttpContext context) =>
{
    (UsuarioResponse? usuario, string erro) = await usuarioService.CadastrarAsync(
        request.Nome,
        request.Email,
        request.Senha);

    if (usuario is null)
    {
        return erro == "Já existe uma conta com este e-mail."
            ? CriarProblem(context, StatusCodes.Status409Conflict, "Conflito de dados.", erro)
            : CriarProblem(context, StatusCodes.Status400BadRequest, "Dados inválidos.", erro);
    }

    context.Response.Headers.CacheControl = "no-store";
    return Results.Json(new
    {
        usuario.Email,
        mensagem = "Conta criada. Confirme seu e-mail para entrar."
    }, statusCode: StatusCodes.Status201Created);
})
    .AllowAnonymous()
    .RequireAntiforgery();

app.MapPost("/auth/login", async (LoginRequest request, UsuarioLocalService usuarioService, HttpContext context) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
    {
        return CriarProblem(
            context,
            StatusCodes.Status400BadRequest,
            "Dados inválidos.",
            "E-mail e senha são obrigatórios.");
    }

    ResultadoAutenticacao autenticacao = usuarioService.Autenticar(request.Email, request.Senha);

    if (autenticacao.EmailNaoConfirmado)
    {
        return CriarProblem(
            context,
            StatusCodes.Status400BadRequest,
            "Confirmação pendente.",
            "Confirme seu e-mail antes de entrar.");
    }

    if (autenticacao.Usuario is null)
    {
        return CriarProblem(
            context,
            StatusCodes.Status401Unauthorized,
            "Não autenticado.",
            "E-mail ou senha inválidos.");
    }

    await context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        CriarPrincipal(autenticacao.Usuario),
        CriarPropriedadesAutenticacao());

    context.Response.Headers.CacheControl = "no-store";
    return Results.Ok(autenticacao.Usuario);
})
    .AllowAnonymous()
    .RequireAntiforgery();

app.MapPost("/auth/confirmar-email", (ConfirmarEmailRequest request, UsuarioLocalService usuarioService, HttpContext context) =>
{
    bool confirmado = usuarioService.ConfirmarEmail(request.Token);
    context.Response.Headers.CacheControl = "no-store";
    return confirmado
        ? Results.Ok(new { mensagem = "E-mail confirmado com sucesso. Você já pode entrar." })
        : CriarProblem(
            context,
            StatusCodes.Status400BadRequest,
            "Token inválido.",
            "Token de confirmação inválido ou expirado.");
})
    .AllowAnonymous()
    .RequireAntiforgery();

app.MapPost("/auth/reenviar-confirmacao", async (ReenviarConfirmacaoEmailRequest request, UsuarioLocalService usuarioService, HttpContext context) =>
{
    await usuarioService.ReenviarConfirmacaoEmailAsync(request.Email);
    context.Response.Headers.CacheControl = "no-store";
    return Results.Ok(new { mensagem = "Se a conta existir e ainda não estiver confirmada, enviaremos uma nova confirmação." });
})
    .AllowAnonymous()
    .RequireAntiforgery();

app.MapPost("/auth/esqueci-senha", async (EsqueciSenhaRequest request, UsuarioLocalService usuarioService, HttpContext context) =>
{
    await usuarioService.SolicitarRedefinicaoSenhaAsync(request.Email);
    context.Response.Headers.CacheControl = "no-store";
    return Results.Ok(new { mensagem = "Se o e-mail estiver cadastrado, enviaremos instruções de recuperação." });
})
    .AllowAnonymous()
    .RequireAntiforgery();

app.MapPost("/auth/redefinir-senha", (RedefinirSenhaRequest request, UsuarioLocalService usuarioService, HttpContext context) =>
{
    bool redefinida = usuarioService.RedefinirSenha(request.Token, request.NovaSenha);
    context.Response.Headers.CacheControl = "no-store";
    return redefinida
        ? Results.Ok(new { mensagem = "Senha redefinida com sucesso. Entre com sua nova senha." })
        : CriarProblem(
            context,
            StatusCodes.Status400BadRequest,
            "Token inválido.",
            "Token inválido ou expirado, ou senha fora dos critérios.");
})
    .AllowAnonymous()
    .RequireAntiforgery();

app.MapGet("/auth/me", (ClaimsPrincipal principal, HttpContext context) =>
{
    UsuarioResponse? usuario = MapearUsuarioClaims(principal);
    context.Response.Headers.CacheControl = "no-store";
    return usuario is null
        ? CriarProblem(
            context,
            StatusCodes.Status401Unauthorized,
            "Sessão expirada ou não autenticada.",
            "Faça login para continuar.")
        : Results.Ok(usuario);
});

app.MapPost("/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Headers.CacheControl = "no-store";
    return Results.NoContent();
})
    .RequireAntiforgery();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/emails", (EmailSimuladoService emailService) =>
    {
        return Results.Ok(emailService.Listar());
    })
        .AllowAnonymous();
}

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

static ClaimsPrincipal CriarPrincipal(UsuarioResponse usuario)
{
    Claim[] claims =
    [
        new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
        new(ClaimTypes.Name, usuario.Nome),
        new(ClaimTypes.Email, usuario.Email),
        new(ClaimTypes.Role, usuario.Perfil)
    ];

    ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    return new ClaimsPrincipal(identity);
}

static AuthenticationProperties CriarPropriedadesAutenticacao()
{
    return new AuthenticationProperties
    {
        AllowRefresh = true,
        IsPersistent = false
    };
}

static UsuarioResponse? MapearUsuarioClaims(ClaimsPrincipal principal)
{
    string? id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    string? nome = principal.FindFirstValue(ClaimTypes.Name);
    string? email = principal.FindFirstValue(ClaimTypes.Email);
    string? perfil = principal.FindFirstValue(ClaimTypes.Role);

    if (!int.TryParse(id, out int usuarioId) ||
        string.IsNullOrWhiteSpace(nome) ||
        string.IsNullOrWhiteSpace(email) ||
        string.IsNullOrWhiteSpace(perfil))
    {
        return null;
    }

    return new UsuarioResponse
    {
        Id = usuarioId,
        Nome = nome,
        Email = email,
        Perfil = perfil
    };
}

static IResult CriarProblem(HttpContext context, int statusCode, string title, string detail)
{
    return Results.Problem(
        detail: detail,
        statusCode: statusCode,
        title: title,
        type: $"https://httpstatuses.com/{statusCode}",
        instance: context.Request.Path,
        extensions: new Dictionary<string, object?>
        {
            ["correlationId"] = context.TraceIdentifier
        });
}

static async Task EscreverProblemAsync(HttpContext context, int statusCode, string title, string detail)
{
    if (context.Response.HasStarted)
    {
        return;
    }

    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/problem+json";

    ProblemDetails problemDetails = new()
    {
        Title = title,
        Detail = detail,
        Status = statusCode,
        Type = $"https://httpstatuses.com/{statusCode}",
        Instance = context.Request.Path
    };
    problemDetails.Extensions["correlationId"] = context.TraceIdentifier;

    await context.Response.WriteAsJsonAsync(problemDetails);
}

static string GerarCsv(IEnumerable<string> cabecalho, IEnumerable<IEnumerable<string>> linhas)
{
    StringBuilder csv = new();
    csv.AppendLine(string.Join(";", cabecalho.Select(EscapeCsvCampo)));

    foreach (IEnumerable<string> linha in linhas)
    {
        csv.AppendLine(string.Join(";", linha.Select(EscapeCsvCampo)));
    }

    return csv.ToString();
}

static string EscapeCsvCampo(string? valor)
{
    string texto = valor ?? string.Empty;
    bool precisaEscape = texto.Contains(';') || texto.Contains('"') || texto.Contains('\n') || texto.Contains('\r');

    if (!precisaEscape)
    {
        return texto;
    }

    return $"\"{texto.Replace("\"", "\"\"")}\"";
}

static IResult CriarArquivoCsv(string nomeBase, string conteudo)
{
    byte[] bytesConteudo = Encoding.UTF8.GetPreamble()
        .Concat(Encoding.UTF8.GetBytes(conteudo))
        .ToArray();

    string nomeArquivo = $"{nomeBase}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
    return Results.File(bytesConteudo, "text/csv; charset=utf-8", nomeArquivo);
}

public partial class Program;
