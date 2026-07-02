using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Models;

namespace MiniErp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Produto> Produtos => Set<Produto>();
}
