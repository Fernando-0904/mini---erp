using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Models;

namespace MiniErp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Categoria> Categorias => Set<Categoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Produto>().HasKey(produto => produto.Codigo);

        modelBuilder.Entity<Categoria>().HasKey(categoria => categoria.Id);
        modelBuilder.Entity<Categoria>()
            .Property(categoria => categoria.Id)
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<Categoria>()
            .HasIndex(categoria => categoria.Nome)
            .IsUnique();
    }
}
