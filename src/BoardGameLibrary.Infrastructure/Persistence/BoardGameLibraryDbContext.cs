using BoardGameLibrary.Domain.BoardGames;
using BoardGameLibrary.Domain.Categories;
using BoardGameLibrary.Domain.GameCopies;
using BoardGameLibrary.Domain.Loans;
using BoardGameLibrary.Domain.Members;
using Microsoft.EntityFrameworkCore;

namespace BoardGameLibrary.Infrastructure.Persistence;

public sealed class BoardGameLibraryDbContext(DbContextOptions<BoardGameLibraryDbContext> options)
    : DbContext(options)
{
    public DbSet<BoardGame> BoardGames => Set<BoardGame>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<GameCopy> GameCopies => Set<GameCopy>();

    public DbSet<Member> Members => Set<Member>();

    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BoardGameLibraryDbContext).Assembly);
    }
}
