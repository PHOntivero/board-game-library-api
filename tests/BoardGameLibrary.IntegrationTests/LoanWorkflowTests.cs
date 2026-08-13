using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BoardGameLibrary.Domain.Loans;
using BoardGameLibrary.IntegrationTests.Infrastructure;

namespace BoardGameLibrary.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class LoanWorkflowTests(PostgreSqlFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task SameCopyAndDoubleReturn_ProduceOneSuccessAndOneConflict()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LibraryGraph graph = await CreateGraphAsync("race", copyCount: 1, cancellationToken);
        Guid secondMemberId = await CreateMemberAsync("RACE-002", "race2@example.test", cancellationToken);

        Task<HttpResponseMessage>[] requests =
        [
            CreateLoanResponseAsync(graph.MemberId, graph.CopyIds[0], cancellationToken),
            CreateLoanResponseAsync(secondMemberId, graph.CopyIds[0], cancellationToken),
        ];

        HttpResponseMessage[] responses = await Task.WhenAll(requests);

        try
        {
            HttpResponseMessage created = Assert.Single(
                responses,
                response => response.StatusCode == HttpStatusCode.Created);
            HttpResponseMessage conflict = Assert.Single(
                responses,
                response => response.StatusCode == HttpStatusCode.Conflict);
            Guid loanId = await ReadIdAsync(created, cancellationToken);

            await AssertProblemCodeAsync(
                conflict,
                "game_copy_has_open_loan",
                cancellationToken);

            JsonElement unavailableGame = await GetJsonAsync(
                $"/api/board-games/{graph.BoardGameId}",
                cancellationToken);
            Assert.False(unavailableGame.GetProperty("isAvailable").GetBoolean());
            Assert.Equal(0, unavailableGame.GetProperty("availableCopies").GetInt32());

            Task<HttpResponseMessage>[] returns =
            [
                Client.PostAsync($"/api/loans/{loanId}/return", null, cancellationToken),
                Client.PostAsync($"/api/loans/{loanId}/return", null, cancellationToken),
            ];

            HttpResponseMessage[] returnResponses = await Task.WhenAll(returns);

            try
            {
                Assert.Single(
                    returnResponses,
                    response => response.StatusCode == HttpStatusCode.NoContent);
                HttpResponseMessage returnConflict = Assert.Single(
                    returnResponses,
                    response => response.StatusCode == HttpStatusCode.Conflict);
                await AssertProblemCodeAsync(
                    returnConflict,
                    "loan_already_returned",
                    cancellationToken);
            }
            finally
            {
                foreach (HttpResponseMessage response in returnResponses)
                {
                    response.Dispose();
                }
            }

            JsonElement availableGame = await GetJsonAsync(
                $"/api/board-games/{graph.BoardGameId}",
                cancellationToken);
            Assert.True(availableGame.GetProperty("isAvailable").GetBoolean());
            Assert.Equal(1, availableGame.GetProperty("availableCopies").GetInt32());
        }
        finally
        {
            foreach (HttpResponseMessage response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task ConcurrentRequests_CannotExceedThreeOpenLoansForMember()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LibraryGraph graph = await CreateGraphAsync("limit", copyCount: 4, cancellationToken);

        using HttpResponseMessage first = await CreateLoanResponseAsync(
            graph.MemberId,
            graph.CopyIds[0],
            cancellationToken);
        using HttpResponseMessage second = await CreateLoanResponseAsync(
            graph.MemberId,
            graph.CopyIds[1],
            cancellationToken);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        Task<HttpResponseMessage>[] requests =
        [
            CreateLoanResponseAsync(graph.MemberId, graph.CopyIds[2], cancellationToken),
            CreateLoanResponseAsync(graph.MemberId, graph.CopyIds[3], cancellationToken),
        ];

        HttpResponseMessage[] responses = await Task.WhenAll(requests);

        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
            HttpResponseMessage conflict = Assert.Single(
                responses,
                response => response.StatusCode == HttpStatusCode.Conflict);
            await AssertProblemCodeAsync(conflict, "loan_limit_reached", cancellationToken);

            JsonElement loans = await GetJsonAsync(
                $"/api/loans?memberId={graph.MemberId}",
                cancellationToken);
            Assert.Equal(3, loans.GetProperty("totalCount").GetInt32());
        }
        finally
        {
            foreach (HttpResponseMessage response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task MemberWithOverdueLoan_CannotCreateAnotherLoan()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LibraryGraph graph = await CreateGraphAsync("overdue", copyCount: 2, cancellationToken);

        await Fixture.InDatabaseScopeAsync(async dbContext =>
        {
            Loan overdueLoan = Loan.Create(
                graph.CopyIds[0],
                graph.MemberId,
                DateTimeOffset.UtcNow.AddDays(-20));
            dbContext.Loans.Add(overdueLoan);
            await dbContext.SaveChangesAsync(cancellationToken);
        });

        using HttpResponseMessage response = await CreateLoanResponseAsync(
            graph.MemberId,
            graph.CopyIds[1],
            cancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertProblemCodeAsync(response, "member_has_overdue_loan", cancellationToken);

        JsonElement overdueLoans = await GetJsonAsync(
            "/api/loans?status=overdue",
            cancellationToken);
        Assert.Equal(1, overdueLoans.GetProperty("totalCount").GetInt32());
        Assert.Equal(
            "overdue",
            overdueLoans.GetProperty("items")[0].GetProperty("status").GetString());
    }

    private async Task<LibraryGraph> CreateGraphAsync(
        string suffix,
        int copyCount,
        CancellationToken cancellationToken)
    {
        Guid categoryId = await PostAndReadIdAsync(
            "/api/categories",
            new { name = $"Category {suffix}" },
            cancellationToken);
        Guid boardGameId = await PostAndReadIdAsync(
            "/api/board-games",
            new
            {
                title = $"Board Game {suffix}",
                publisher = "Integration Publisher",
                description = "A focused loan concurrency scenario.",
                publicationYear = 2020,
                minPlayers = 1,
                maxPlayers = 4,
                playingTimeMinutes = 60,
                categoryIds = new[] { categoryId },
            },
            cancellationToken);
        Guid memberId = await CreateMemberAsync(
            $"MEM-{suffix.ToUpperInvariant()}",
            $"{suffix}@example.test",
            cancellationToken);
        var copyIds = new List<Guid>(copyCount);

        for (int index = 1; index <= copyCount; index++)
        {
            Guid copyId = await PostAndReadIdAsync(
                $"/api/board-games/{boardGameId}/copies",
                new
                {
                    inventoryCode = $"{suffix.ToUpperInvariant()}-{index:000}",
                    condition = "good",
                    acquiredOn = new DateOnly(2024, 1, index),
                },
                cancellationToken);
            copyIds.Add(copyId);
        }

        return new LibraryGraph(boardGameId, memberId, copyIds);
    }

    private Task<Guid> CreateMemberAsync(
        string memberNumber,
        string email,
        CancellationToken cancellationToken) =>
        PostAndReadIdAsync(
            "/api/members",
            new
            {
                memberNumber,
                fullName = $"Member {memberNumber}",
                email,
                phoneNumber = (string?)null,
                joinedOn = new DateOnly(2024, 1, 1),
            },
            cancellationToken);

    private async Task<Guid> PostAndReadIdAsync<TRequest>(
        string path,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            path,
            request,
            cancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        return await ReadIdAsync(response, cancellationToken);
    }

    private Task<HttpResponseMessage> CreateLoanResponseAsync(
        Guid memberId,
        Guid gameCopyId,
        CancellationToken cancellationToken) =>
        Client.PostAsJsonAsync(
            "/api/loans",
            new { memberId, gameCopyId },
            cancellationToken);

    private static async Task<Guid> ReadIdAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return body.GetProperty("id").GetGuid();
    }

    private async Task<JsonElement> GetJsonAsync(
        string path,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await Client.GetAsync(path, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        string expectedCode,
        CancellationToken cancellationToken)
    {
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }

    private sealed record LibraryGraph(
        Guid BoardGameId,
        Guid MemberId,
        IReadOnlyList<Guid> CopyIds);
}
