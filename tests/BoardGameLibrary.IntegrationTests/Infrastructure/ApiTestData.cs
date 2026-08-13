using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BoardGameLibrary.IntegrationTests.Infrastructure;

internal static class ApiTestData
{
    internal static async Task<CreatedResource> CreateAsync(
        HttpClient client,
        string postPath,
        string locationPathPrefix,
        object request,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            postPath,
            request,
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        JsonElement body = await ReadJsonAsync(response, cancellationToken);
        Guid id = body.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);

        string location = response.Headers.Location.IsAbsoluteUri
            ? response.Headers.Location.AbsolutePath
            : response.Headers.Location.OriginalString;
        string expectedLocation = $"{locationPathPrefix.TrimEnd('/')}/{id}";
        Assert.True(
            location.EndsWith(expectedLocation, StringComparison.OrdinalIgnoreCase),
            $"Expected Location to end with '{expectedLocation}', but it was '{location}'.");

        return new CreatedResource(id, response.Headers.Location);
    }

    internal static async Task<JsonElement> GetAsync(
        HttpClient client,
        string path,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await client.GetAsync(path, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadJsonAsync(response, cancellationToken);
    }

    internal static async Task<JsonElement> PutAsync(
        HttpClient client,
        string path,
        object request,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await client.PutAsJsonAsync(
            path,
            request,
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadJsonAsync(response, cancellationToken);
    }

    internal static async Task DeleteAsync(
        HttpClient client,
        string path,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await client.DeleteAsync(path, cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength ?? 0);
    }

    internal static async Task<JsonElement> AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode,
        CancellationToken cancellationToken)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        JsonElement problem = await ReadJsonAsync(response, cancellationToken);
        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());

        string? traceId = problem.GetProperty("traceId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(traceId));
        Assert.Equal(
            response.Headers.GetValues("X-Trace-Id").Single(),
            traceId);

        return problem;
    }

    private static async Task<JsonElement> ReadJsonAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        JsonElement result = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken);
        return result.Clone();
    }
}

internal sealed record CreatedResource(Guid Id, Uri Location);
