using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MiniErp.Api.Data;

namespace MiniErp.Api.Tests;

public sealed class MiniErpApiFactory : WebApplicationFactory<Program>
{
    private readonly string environmentName;
    private SqliteConnection? databaseConnection;

    public MiniErpApiFactory(string environmentName = "Development")
    {
        this.environmentName = environmentName;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environmentName);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));

            databaseConnection = new SqliteConnection("Data Source=:memory:;Cache=Shared");
            databaseConnection.Open();

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(databaseConnection));
        });
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

        if (disposing)
        {
            if (databaseConnection is not null)
            {
                databaseConnection.Dispose();
                databaseConnection = null;
            }
        }
    }
}
