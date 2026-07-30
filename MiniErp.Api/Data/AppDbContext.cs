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
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TokenUsuario> TokensUsuario => Set<TokenUsuario>();

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

        modelBuilder.Entity<Usuario>().HasKey(usuario => usuario.Id);
        modelBuilder.Entity<Usuario>()
            .Property(usuario => usuario.Id)
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<Usuario>()
            .Property(usuario => usuario.Nome)
            .HasMaxLength(80)
            .IsRequired();
        modelBuilder.Entity<Usuario>()
            .Property(usuario => usuario.Email)
            .HasMaxLength(254)
            .UseCollation("NOCASE")
            .IsRequired();
        modelBuilder.Entity<Usuario>()
            .Property(usuario => usuario.Perfil)
            .HasMaxLength(30)
            .IsRequired();
        modelBuilder.Entity<Usuario>()
            .Property(usuario => usuario.SenhaHash)
            .IsRequired();
        modelBuilder.Entity<Usuario>()
            .Property(usuario => usuario.SenhaSalt)
            .IsRequired();
        modelBuilder.Entity<Usuario>()
            .Property(usuario => usuario.EmailConfirmado)
            .HasDefaultValue(false);
        modelBuilder.Entity<Usuario>()
            .HasIndex(usuario => usuario.Email)
            .IsUnique();

        modelBuilder.Entity<TokenUsuario>().HasKey(token => token.Id);
        modelBuilder.Entity<TokenUsuario>()
            .Property(token => token.Id)
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<TokenUsuario>()
            .Property(token => token.Tipo)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        modelBuilder.Entity<TokenUsuario>()
            .Property(token => token.TokenHash)
            .IsRequired();
        modelBuilder.Entity<TokenUsuario>()
            .HasOne(token => token.Usuario)
            .WithMany()
            .HasForeignKey(token => token.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TokenUsuario>()
            .HasIndex(token => new { token.UsuarioId, token.Tipo });
        modelBuilder.Entity<TokenUsuario>()
            .HasIndex(token => token.ExpiraEmUtc);

        modelBuilder.Entity<Usuario>().HasData(new Usuario
        {
            Id = 1,
            Nome = "Administrador",
            Email = "admin@mini-erp.com",
            Perfil = "Admin",
            SenhaSalt = Convert.FromBase64String("noExopFskEdytn5nkRiWDA=="),
            SenhaHash = Convert.FromBase64String("rViskkWPpo95fXV2hgw3bEKVUbvr065wNyCrRoprTTY="),
            EmailConfirmado = true,
            EmailConfirmadoEmUtc = new DateTime(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc),
            CriadoEmUtc = new DateTime(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
