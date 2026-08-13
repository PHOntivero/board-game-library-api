using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BoardGameLibrary.IntegrationTests.Infrastructure;

namespace BoardGameLibrary.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class HealthAndOpenApiTests(PostgreSqlFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task LiveReadyAndOpenApi_AreAvailableInTesting()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using HttpResponseMessage liveResponse = await Client.GetAsync(
            "/health/live",
            cancellationToken);
        using HttpResponseMessage readyResponse = await Client.GetAsync(
            "/health/ready",
            cancellationToken);
        using HttpResponseMessage openApiResponse = await Client.GetAsync(
            "/openapi/v1.json",
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, openApiResponse.StatusCode);

        JsonElement live = await liveResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        JsonElement ready = await readyResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        Assert.Equal("healthy", live.GetProperty("status").GetString());
        Assert.Equal("self", live.GetProperty("checks")[0].GetProperty("name").GetString());
        Assert.Equal("database", ready.GetProperty("checks")[0].GetProperty("name").GetString());
        Assert.True(openApiResponse.Headers.Contains("X-Trace-Id"));
    }

    [Fact]
    public async Task LiveDoesNotDependOnDatabase_AndOpenApiIsHiddenInProduction()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        const string variableName = "ConnectionStrings__BoardGameLibrary";
        string? previousValue = Environment.GetEnvironmentVariable(variableName);

        try
        {
            const string unavailableConnection =
                "Host=127.0.0.1;Port=1;Database=unavailable;Username=test;Password=test;Timeout=1;Command Timeout=1";
            Environment.SetEnvironmentVariable(variableName, unavailableConnection);

            using (var unavailableFactory = new TestApiFactory(unavailableConnection))
            using (HttpClient unavailableClient = unavailableFactory.CreateClient(
                       new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
                       {
                           AllowAutoRedirect = false,
                           BaseAddress = new Uri("https://localhost"),
                       }))
            using (HttpResponseMessage liveResponse = await unavailableClient.GetAsync(
                       "/health/live",
                       cancellationToken))
            using (HttpResponseMessage readyResponse = await unavailableClient.GetAsync(
                       "/health/ready",
                       cancellationToken))
            {
                Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
                Assert.Equal(HttpStatusCode.ServiceUnavailable, readyResponse.StatusCode);
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, previousValue);
        }

        using var productionFactory = new TestApiFactory(
            Fixture.ConnectionString,
            "Production");
        using HttpClient productionClient = productionFactory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost"),
            });
        using HttpResponseMessage openApiResponse = await productionClient.GetAsync(
            "/openapi/v1.json",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, openApiResponse.StatusCode);
    }
}
