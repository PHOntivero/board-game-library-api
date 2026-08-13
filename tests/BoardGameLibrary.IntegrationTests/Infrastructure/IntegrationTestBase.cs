namespace BoardGameLibrary.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase(PostgreSqlFixture fixture) : IAsyncLifetime
{
    protected PostgreSqlFixture Fixture { get; } = fixture;

    protected HttpClient Client => Fixture.Client;

    public async ValueTask InitializeAsync() => await Fixture.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
