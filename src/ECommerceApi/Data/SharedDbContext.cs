using ECommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.Data;

/// <summary>
/// Contexto apontando para o banco compartilhado (todo_db).
/// Acessa apenas a tabela de usuários — fonte única de verdade para Auth e Gold.
/// </summary>
public class SharedDbContext : DbContext
{
    public SharedDbContext(DbContextOptions<SharedDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(u => u.GoldBalance)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        // Ignora as navigation properties que existem no User mas não neste contexto
        modelBuilder.Entity<User>().Ignore(u => u.Cart);
        modelBuilder.Entity<User>().Ignore(u => u.Orders);
    }
}
