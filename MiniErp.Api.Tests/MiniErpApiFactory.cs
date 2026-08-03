using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniErp.Api.Data;

namespace MiniErp.Api.Tests;

public sealed class MiniErpApiFactory : WebApplicationFactory<Program>
{
    private readonly string environmentName;
    private readonly string databasePath;

    public MiniErpApiFactory(string environmentName = "Development")
    {
        this.environmentName = environmentName;

        string workspaceRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string databaseDirectory = Path.Combine(workspaceRoot, ".tmp-tests");
        Directory.CreateDirectory(databaseDirectory);

        databasePath = Path.Combine(databaseDirectory, $"mini-erp-tests-{Guid.NewGuid():N}.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environmentName);
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            $"Data Source={databasePath};Pooling=False");
    }

    public HttpClient CriarCliente()
    {
        HttpClient client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });

        using IServiceScope scope = Services.CreateScope();
        AppDbContext contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        contexto.Database.Migrate();

        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();

        if (disposing && File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }
}
