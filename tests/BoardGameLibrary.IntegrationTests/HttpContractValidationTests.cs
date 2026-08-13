using System.Net;
using System.Net.Http.Json;
using BoardGameLibrary.IntegrationTests.Infrastructure;

namespace BoardGameLibrary.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class HttpContractValidationTests(PostgreSqlFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task PutRequiresCompleteBody_AndNonNullActiveState()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        CreatedResource category = await ApiTestData.CreateAsync(
            Client,
            "/api/categories",
            "/api/categories",
            new { name = "Strategy" },
            cancellationToken);

        using HttpResponseMessage missingActive = await Client.PutAsJsonAsync(
            $"/api/categories/{category.Id}",
            new { name = "Updated Strategy" },
            cancellationToken);
        await AssertValidationProblemAsync(missingActive, cancellationToken);

        using HttpResponseMessage nullActive = await Client.PutAsJsonAsync(
            $"/api/categories/{category.Id}",
            new { name = "Updated Strategy", isActive = (bool?)null },
            cancellationToken);
        await AssertValidationProblemAsync(nullActive, cancellationToken);
    }

    [Fact]
    public async Task InvalidIdentifiersInPathsAndQueries_ReturnValidationProblems()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        const string emptyIdentifier = "00000000-0000-0000-0000-000000000000";

        using HttpResponseMessage malformedPath = await Client.GetAsync(
            "/api/categories/not-a-guid",
            cancellationToken);
        await AssertValidationProblemAsync(malformedPath, cancellationToken);

        using HttpResponseMessage emptyPath = await Client.GetAsync(
            $"/api/categories/{emptyIdentifier}",
            cancellationToken);
        await AssertValidationProblemAsync(emptyPath, cancellationToken);

        using HttpResponseMessage emptyParentPath = await Client.GetAsync(
            $"/api/board-games/{emptyIdentifier}/copies",
            cancellationToken);
        await AssertValidationProblemAsync(emptyParentPath, cancellationToken);

        using HttpResponseMessage emptyQuery = await Client.GetAsync(
            $"/api/board-games?categoryId={emptyIdentifier}",
            cancellationToken);
        await AssertValidationProblemAsync(emptyQuery, cancellationToken);
    }

    [Fact]
    public async Task EnumAndLoanDateFilters_RejectInvalidPublicRepresentations()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid boardGameId = Guid.CreateVersion7();

        using HttpResponseMessage numericBodyEnum = await Client.PostAsJsonAsync(
            $"/api/board-games/{boardGameId}/copies",
            new
            {
                inventoryCode = "COPY-001",
                condition = 1,
                acquiredOn = (string?)null,
            },
            cancellationToken);
        await AssertValidationProblemAsync(numericBodyEnum, cancellationToken);

        using HttpResponseMessage unknownQueryEnum = await Client.GetAsync(
            $"/api/board-games/{boardGameId}/copies?condition=unknown",
            cancellationToken);
        await AssertValidationProblemAsync(unknownQueryEnum, cancellationToken);

        using HttpResponseMessage nonUtcFilter = await Client.GetAsync(
            "/api/loans?loanedFrom=2026-08-13T10%3A00%3A00-03%3A00",
            cancellationToken);
        await AssertValidationProblemAsync(nonUtcFilter, cancellationToken);

        using HttpResponseMessage invertedRange = await Client.GetAsync(
            "/api/loans?loanedFrom=2026-08-14T00%3A00%3A00Z&loanedTo=2026-08-13T00%3A00%3A00Z",
            cancellationToken);
        await AssertValidationProblemAsync(invertedRange, cancellationToken);
    }

    [Fact]
    public async Task BoardGameContract_RejectsCrossFieldAndCategoryCollectionViolations()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid categoryId = Guid.CreateVersion7();

        using HttpResponseMessage invertedPlayerRange = await Client.PostAsJsonAsync(
            "/api/board-games",
            CreateBoardGameBody([categoryId], minPlayers: 5, maxPlayers: 2),
            cancellationToken);
        await AssertValidationProblemAsync(invertedPlayerRange, cancellationToken);

        using HttpResponseMessage emptyCategories = await Client.PostAsJsonAsync(
            "/api/board-games",
            CreateBoardGameBody([]),
            cancellationToken);
        await AssertValidationProblemAsync(emptyCategories, cancellationToken);

        using HttpResponseMessage duplicateCategories = await Client.PostAsJsonAsync(
            "/api/board-games",
            CreateBoardGameBody([categoryId, categoryId]),
            cancellationToken);
        await AssertValidationProblemAsync(duplicateCategories, cancellationToken);
    }

    private static object CreateBoardGameBody(
        Guid[] categoryIds,
        int minPlayers = 2,
        int maxPlayers = 4) =>
        new
        {
            title = "Validation Test Game",
            publisher = "Test Publisher",
            description = (string?)null,
            publicationYear = 2026,
            minPlayers,
            maxPlayers,
            playingTimeMinutes = 60,
            categoryIds,
        };

    private static async Task AssertValidationProblemAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken) =>
        _ = await ApiTestData.AssertProblemAsync(
            response,
            HttpStatusCode.BadRequest,
            "validation_failed",
            cancellationToken);
}
