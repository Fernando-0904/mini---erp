using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using MiniErp.Api.Data;
using MiniErp.Api.Endpoints;
using MiniErp.Api.Security;
using MiniErp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

const string PerfilAdministrador = "Administrador";
const string PerfilAdminLegado = "Admin";
const string PerfilOperador = "Operador";
const string PerfilUsuarioLegado = "Usuário";
const string PoliticaOperar = "PodeOperar";
const string PoliticaAdministrar = "PodeAdministrar";
const decimal LimiteAprovacaoOperadorPadrao = 1000m;

decimal limiteAprovacaoOperador = builder.Configuration.GetValue<decimal?>("Compras:LimiteAprovacaoOperador")
    ?? LimiteAprovacaoOperadorPadrao;

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
    ConfigurarCorsMiniErp(options, allowedOrigins);
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

RegistrarServicosAplicacao(builder.Services, databaseConnectionString);

var app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    ILogger<Program> logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        AplicarMigrationsComProtecao(dbContext, logger);
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Falha ao aplicar migrations automáticas no startup da API.");
        throw;
    }
}

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

app.MapProdutoEndpoints(PoliticaOperar, PoliticaAdministrar);
app.MapCategoriaEndpoints(PoliticaOperar, PoliticaAdministrar);
app.MapFornecedorEndpoints(PoliticaOperar, PoliticaAdministrar);
app.MapCompraEndpoints(PoliticaOperar, PoliticaAdministrar, limiteAprovacaoOperador);
app.MapRelatorioEndpoints(PoliticaAdministrar);
app.MapAuthEndpoints();
app.MapDevEndpoints();

app.Run();

static async Task EscreverProblemAsync(HttpContext context, int statusCode, string title, string detail)
{
    await ApiHttpHelpers.EscreverProblemAsync(
        context,
        statusCode,
        title,
        detail);
}

static void AplicarMigrationsComProtecao(AppDbContext dbContext, ILogger logger)
{
    if (!TentarObterCaminhosBackupSqlite(dbContext, out string databasePath, out string backupPath))
    {
        dbContext.Database.Migrate();
        return;
    }

    File.Copy(databasePath, backupPath, overwrite: true);

    try
    {
        dbContext.Database.Migrate();
        RemoverArquivoSeExistir(backupPath);
    }
    catch
    {
        RestaurarBancoSqliteDoBackupSeDisponivel(databasePath, backupPath, logger);

        throw;
    }
}

static bool TentarObterCaminhosBackupSqlite(AppDbContext dbContext, out string databasePath, out string backupPath)
{
    databasePath = string.Empty;
    backupPath = string.Empty;

    if (!dbContext.Database.IsSqlite())
    {
        return false;
    }

    string? connectionString = dbContext.Database.GetConnectionString();

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return false;
    }

    string dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource ?? string.Empty;

    if (string.IsNullOrWhiteSpace(dataSource) || dataSource == ":memory:")
    {
        return false;
    }

    databasePath = Path.GetFullPath(dataSource, Directory.GetCurrentDirectory());

    if (!File.Exists(databasePath))
    {
        return false;
    }

    backupPath = $"{databasePath}.pre-migrate.bak";
    return true;
}

static void RestaurarBancoSqliteDoBackupSeDisponivel(string databasePath, string backupPath, ILogger logger)
{
    if (!File.Exists(backupPath))
    {
        return;
    }

    File.Copy(backupPath, databasePath, overwrite: true);
    RemoverArquivoSeExistir(backupPath);
    logger.LogWarning("Falha na migration. Banco SQLite restaurado a partir do backup de segurança.");
}

static void RemoverArquivoSeExistir(string filePath)
{
    if (File.Exists(filePath))
    {
        File.Delete(filePath);
    }
}

static void AplicarPoliticaCorsMiniErp(CorsPolicyBuilder policy, string[] allowedOrigins)
{
    policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
}

static void ConfigurarCorsMiniErp(CorsOptions options, string[] allowedOrigins)
{
    options.AddPolicy("MiniErpCors", policy =>
    {
        AplicarPoliticaCorsMiniErp(policy, allowedOrigins);
    });
}

static void RegistrarServicosAplicacao(IServiceCollection services, string databaseConnectionString)
{
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(databaseConnectionString));
    services.AddScoped<ProdutoService>();
    services.AddScoped<CategoriaService>();
    services.AddScoped<FornecedorService>();
    services.AddScoped<MovimentacaoEstoqueService>();
    services.AddScoped<PedidoCompraService>();
    services.AddScoped<RelatorioService>();
    services.AddScoped<AuditoriaService>();
    services.AddScoped<UsuarioLocalService>();
    services.AddSingleton<LoginAttemptGuardService>();
    services.AddSingleton<EmailSimuladoService>();
    services.AddSingleton<IEmailService>(serviceProvider => serviceProvider.GetRequiredService<EmailSimuladoService>());
    services.AddProblemDetails();
}

public partial class Program;
