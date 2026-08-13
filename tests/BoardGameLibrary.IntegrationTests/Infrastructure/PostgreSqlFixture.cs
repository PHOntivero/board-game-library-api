using BoardGameLibrary.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace BoardGameLibrary.IntegrationTests.Infrastructure;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private const string ConnectionStringEnvironmentVariable =
        "ConnectionStrings__BoardGameLibrary";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(
            "postgres:18.4-alpine3.24")
        .WithDatabase("board_game_library_tests")
        .WithUsername("boardgame_tests")
        .WithPassword("boardgame_tests_password")
        .Build();
    private string? _previousConnectionString;

    public TestApiFactory Factory { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        string connectionString = _container.GetConnectionString();
        _previousConnectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable);
        Environment.SetEnvironmentVariable(
            ConnectionStringEnvironmentVariable,
            connectionString);

        Factory = new TestApiFactory(connectionString);

        await using AsyncServiceScope scope = Factory.Services.CreateAsyncScope();
        BoardGameLibraryDbContext dbContext = scope.ServiceProvider
            .GetRequiredService<BoardGameLibraryDbContext>();
        await dbContext.Database.MigrateAsync();

        Client = Factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost"),
            });
    }

    public async Task ResetDatabaseAsync()
    {
        await using AsyncServiceScope scope = Factory.Services.CreateAsyncScope();
        BoardGameLibraryDbContext dbContext = scope.ServiceProvider
            .GetRequiredService<BoardGameLibraryDbContext>();

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                board_game_categories,
                loans,
                game_copies,
                members,
                board_games,
                categories
            RESTART IDENTITY CASCADE;
            """);
    }

    public async Task<TResult> InDatabaseScopeAsync<TResult>(
        Func<BoardGameLibraryDbContext, Task<TResult>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        await using AsyncServiceScope scope = Factory.Services.CreateAsyncScope();
        BoardGameLibraryDbContext dbContext = scope.ServiceProvider
            .GetRequiredService<BoardGameLibraryDbContext>();

        return await action(dbContext);
    }

    public async Task InDatabaseScopeAsync(Func<BoardGameLibraryDbContext, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        await using AsyncServiceScope scope = Factory.Services.CreateAsyncScope();
        BoardGameLibraryDbContext dbContext = scope.ServiceProvider
            .GetRequiredService<BoardGameLibraryDbContext>();

        await action(dbContext);
    }

    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();
        Factory?.Dispose();
        Environment.SetEnvironmentVariable(
            ConnectionStringEnvironmentVariable,
            _previousConnectionString);
        await _container.DisposeAsync();
    }
}
