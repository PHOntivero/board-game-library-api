using BoardGameLibrary.Application.BoardGames;
using BoardGameLibrary.Application.Categories;
using BoardGameLibrary.Application.Common.Persistence;
using BoardGameLibrary.Application.GameCopies;
using BoardGameLibrary.Application.Loans;
using BoardGameLibrary.Application.Members;
using BoardGameLibrary.Infrastructure.Persistence;
using BoardGameLibrary.Infrastructure.Persistence.Repositories;
using BoardGameLibrary.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BoardGameLibrary.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "BoardGameLibrary";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        bool enableDemoSeed = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<BoardGameLibraryDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history"));

            if (enableDemoSeed)
            {
                options.UseSeeding((context, _) =>
                    DemoDataSeeder.Seed(context, TimeProvider.System));
                options.UseAsyncSeeding((context, _, cancellationToken) =>
                    DemoDataSeeder.SeedAsync(context, TimeProvider.System, cancellationToken));
            }
        });

        services.AddScoped<IBoardGameRepository, BoardGameRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IGameCopyRepository, GameCopyRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
