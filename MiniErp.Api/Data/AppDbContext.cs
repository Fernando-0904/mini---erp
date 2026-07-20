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
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Produto>().HasKey(produto => produto.Codigo);
        
        modelBuilder.Entity<Produto>()
            .HasOne(produto => produto.Categoria)
            .WithMany()
            .HasForeignKey(produto => produto.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Produto>()
            .HasOne(produto => produto.Fornecedor)
            .WithMany()
            .HasForeignKey(produto => produto.FornecedorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Categoria>().HasKey(categoria => categoria.Id);
        modelBuilder.Entity<Categoria>()
            .Property(categoria => categoria.Id)
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<Categoria>()
            .HasIndex(categoria => categoria.Nome)
            .IsUnique();

        modelBuilder.Entity<Fornecedor>().HasKey(fornecedor => fornecedor.Id);
        modelBuilder.Entity<Fornecedor>()
            .Property(fornecedor => fornecedor.Id)
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<Fornecedor>()
            .HasIndex(fornecedor => fornecedor.Codigo)
            .IsUnique();
        modelBuilder.Entity<Fornecedor>()
            .HasIndex(fornecedor => fornecedor.Documento)
            .IsUnique();

        modelBuilder.Entity<MovimentacaoEstoque>().HasKey(movimentacao => movimentacao.Id);
        modelBuilder.Entity<MovimentacaoEstoque>()
            .Property(movimentacao => movimentacao.Id)
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<MovimentacaoEstoque>()
            .Property(movimentacao => movimentacao.Tipo)
            .HasConversion<string>();
        modelBuilder.Entity<MovimentacaoEstoque>()
            .HasOne(movimentacao => movimentacao.Produto)
            .WithMany()
            .HasForeignKey(movimentacao => movimentacao.ProdutoCodigo)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MovimentacaoEstoque>()
            .HasIndex(movimentacao => movimentacao.ProdutoCodigo);
        modelBuilder.Entity<MovimentacaoEstoque>()
            .HasIndex(movimentacao => movimentacao.DataMovimentacaoUtc);
    }
}
