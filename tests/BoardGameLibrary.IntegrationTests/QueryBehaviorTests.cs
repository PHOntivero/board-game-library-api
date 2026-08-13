using System.Text.Json;
using BoardGameLibrary.IntegrationTests.Infrastructure;

namespace BoardGameLibrary.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class QueryBehaviorTests(PostgreSqlFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task CategoryQueries_ApplyActiveDefaultPaginationSortAndLiteralSearch()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var categories = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (string name in new[] { "Zulu", "Mike", "Literal %_\\ Marker", "Inactive" })
        {
            CreatedResource created = await ApiTestData.CreateAsync(
                Client,
                "/api/categories",
                "/api/categories",
                new { name },
                cancellationToken);
            categories[name] = created.Id;
        }

        JsonElement inactive = await ApiTestData.PutAsync(
            Client,
            $"/api/categories/{categories["Inactive"]}",
            new { name = "Inactive", isActive = false },
            cancellationToken);
        Assert.False(inactive.GetProperty("isActive").GetBoolean());

        JsonElement firstPage = await ApiTestData.GetAsync(
            Client,
            "/api/categories?page=1&pageSize=2&sortBy=name&sortDirection=desc",
            cancellationToken);
        Assert.Equal(1, firstPage.GetProperty("page").GetInt32());
        Assert.Equal(2, firstPage.GetProperty("pageSize").GetInt32());
        Assert.Equal(3, firstPage.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, firstPage.GetProperty("totalPages").GetInt32());
        Assert.Equal("Zulu", firstPage.GetProperty("items")[0].GetProperty("name").GetString());
        Assert.Equal("Mike", firstPage.GetProperty("items")[1].GetProperty("name").GetString());
        Assert.All(
            firstPage.GetProperty("items").EnumerateArray(),
            item => Assert.True(item.GetProperty("isActive").GetBoolean()));

        JsonElement inactiveOnly = await ApiTestData.GetAsync(
            Client,
            "/api/categories?isActive=false",
            cancellationToken);
        Assert.Equal(1, inactiveOnly.GetProperty("totalCount").GetInt32());
        Assert.Equal("Inactive", inactiveOnly.GetProperty("items")[0].GetProperty("name").GetString());

        string literalSearch = Uri.EscapeDataString("%_\\");
        JsonElement literalMatch = await ApiTestData.GetAsync(
            Client,
            $"/api/categories?search={literalSearch}",
            cancellationToken);
        Assert.Equal(1, literalMatch.GetProperty("totalCount").GetInt32());
        Assert.Equal(
            "Literal %_\\ Marker",
            literalMatch.GetProperty("items")[0].GetProperty("name").GetString());
    }
}
