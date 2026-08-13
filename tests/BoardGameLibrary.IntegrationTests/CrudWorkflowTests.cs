using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BoardGameLibrary.IntegrationTests.Infrastructure;

namespace BoardGameLibrary.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class CrudWorkflowTests(PostgreSqlFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task CatalogAndMembershipWorkflow_UsesRealHttpPipelineAndPostgreSql()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        using (HttpResponseMessage invalidResponse = await Client.PostAsJsonAsync(
                   "/api/categories",
                   new { },
                   cancellationToken))
        {
            JsonElement validationProblem = await ApiTestData.AssertProblemAsync(
                invalidResponse,
                HttpStatusCode.BadRequest,
                "validation_failed",
                cancellationToken);
            Assert.NotEmpty(validationProblem.GetProperty("errors").EnumerateObject());
        }

        CreatedResource category = await ApiTestData.CreateAsync(
            Client,
            "/api/categories",
            "/api/categories",
            new { name = "Strategy" },
            cancellationToken);
        JsonElement categoryDetails = await ApiTestData.GetAsync(
            Client,
            $"/api/categories/{category.Id}",
            cancellationToken);
        Assert.Equal("Strategy", categoryDetails.GetProperty("name").GetString());
        Assert.True(categoryDetails.GetProperty("isActive").GetBoolean());

        JsonElement updatedCategory = await ApiTestData.PutAsync(
            Client,
            $"/api/categories/{category.Id}",
            new { name = "Modern Strategy", isActive = true },
            cancellationToken);
        Assert.Equal("Modern Strategy", updatedCategory.GetProperty("name").GetString());

        CreatedResource boardGame = await ApiTestData.CreateAsync(
            Client,
            "/api/board-games",
            "/api/board-games",
            new
            {
                title = "Brass: Birmingham",
                publisher = "Roxley",
                description = "Economic strategy game",
                publicationYear = 2018,
                minPlayers = 2,
                maxPlayers = 4,
                playingTimeMinutes = 120,
                categoryIds = new[] { category.Id },
            },
            cancellationToken);
        JsonElement boardGameDetails = await ApiTestData.GetAsync(
            Client,
            $"/api/board-games/{boardGame.Id}",
            cancellationToken);
        Assert.Equal("Brass: Birmingham", boardGameDetails.GetProperty("title").GetString());
        Assert.Equal(0, boardGameDetails.GetProperty("totalCopies").GetInt32());
        Assert.False(boardGameDetails.GetProperty("isAvailable").GetBoolean());
        Assert.Equal(category.Id, boardGameDetails.GetProperty("categories")[0].GetProperty("id").GetGuid());

        JsonElement updatedBoardGame = await ApiTestData.PutAsync(
            Client,
            $"/api/board-games/{boardGame.Id}",
            new
            {
                title = "Brass Birmingham Deluxe",
                publisher = "Roxley Games",
                description = "Updated edition",
                publicationYear = 2019,
                minPlayers = 2,
                maxPlayers = 4,
                playingTimeMinutes = 110,
                categoryIds = new[] { category.Id },
                isActive = true,
            },
            cancellationToken);
        Assert.Equal("Brass Birmingham Deluxe", updatedBoardGame.GetProperty("title").GetString());
        Assert.Equal(110, updatedBoardGame.GetProperty("playingTimeMinutes").GetInt32());

        CreatedResource copy = await ApiTestData.CreateAsync(
            Client,
            $"/api/board-games/{boardGame.Id}/copies",
            "/api/game-copies",
            new
            {
                inventoryCode = "brass-001",
                condition = "good",
                acquiredOn = "2025-01-15",
            },
            cancellationToken);
        JsonElement copyDetails = await ApiTestData.GetAsync(
            Client,
            $"/api/game-copies/{copy.Id}",
            cancellationToken);
        Assert.Equal("BRASS-001", copyDetails.GetProperty("inventoryCode").GetString());
        Assert.Equal("good", copyDetails.GetProperty("condition").GetString());
        Assert.Equal("2025-01-15", copyDetails.GetProperty("acquiredOn").GetString());
        Assert.True(copyDetails.GetProperty("isAvailable").GetBoolean());

        JsonElement updatedCopy = await ApiTestData.PutAsync(
            Client,
            $"/api/game-copies/{copy.Id}",
            new
            {
                inventoryCode = "brass-002",
                condition = "damaged",
                acquiredOn = "2025-02-16",
                isActive = true,
            },
            cancellationToken);
        Assert.Equal("BRASS-002", updatedCopy.GetProperty("inventoryCode").GetString());
        Assert.Equal("damaged", updatedCopy.GetProperty("condition").GetString());
        Assert.Equal("2025-02-16", updatedCopy.GetProperty("acquiredOn").GetString());
        Assert.False(updatedCopy.GetProperty("isAvailable").GetBoolean());

        CreatedResource member = await ApiTestData.CreateAsync(
            Client,
            "/api/members",
            "/api/members",
            new
            {
                memberNumber = "mem-001",
                fullName = "Ada Lovelace",
                email = "ada@example.com",
                phoneNumber = "+55 11 99999-0000",
                joinedOn = "2025-03-20",
            },
            cancellationToken);
        JsonElement memberDetails = await ApiTestData.GetAsync(
            Client,
            $"/api/members/{member.Id}",
            cancellationToken);
        Assert.Equal("MEM-001", memberDetails.GetProperty("memberNumber").GetString());
        Assert.Equal("2025-03-20", memberDetails.GetProperty("joinedOn").GetString());

        JsonElement updatedMember = await ApiTestData.PutAsync(
            Client,
            $"/api/members/{member.Id}",
            new
            {
                memberNumber = "mem-002",
                fullName = "Grace Hopper",
                email = "grace@example.com",
                phoneNumber = (string?)null,
                joinedOn = "2025-04-21",
                isActive = true,
            },
            cancellationToken);
        Assert.Equal("MEM-002", updatedMember.GetProperty("memberNumber").GetString());
        Assert.Equal("Grace Hopper", updatedMember.GetProperty("fullName").GetString());
        Assert.Equal("2025-04-21", updatedMember.GetProperty("joinedOn").GetString());

        JsonElement boardGameList = await ApiTestData.GetAsync(
            Client,
            $"/api/board-games?categoryId={category.Id}&players=3&isAvailable=false",
            cancellationToken);
        Assert.Equal(boardGame.Id, boardGameList.GetProperty("items")[0].GetProperty("id").GetGuid());

        JsonElement copyList = await ApiTestData.GetAsync(
            Client,
            $"/api/board-games/{boardGame.Id}/copies?condition=damaged&isAvailable=false",
            cancellationToken);
        Assert.Equal(copy.Id, copyList.GetProperty("items")[0].GetProperty("id").GetGuid());
        Assert.Equal("damaged", copyList.GetProperty("items")[0].GetProperty("condition").GetString());

        JsonElement memberList = await ApiTestData.GetAsync(
            Client,
            "/api/members?search=Grace",
            cancellationToken);
        Assert.Equal(member.Id, memberList.GetProperty("items")[0].GetProperty("id").GetGuid());

        using (HttpResponseMessage protectedBoardGame = await Client.DeleteAsync(
                   $"/api/board-games/{boardGame.Id}",
                   cancellationToken))
        {
            await ApiTestData.AssertProblemAsync(
                protectedBoardGame,
                HttpStatusCode.Conflict,
                "board_game_has_copies",
                cancellationToken);
        }

        using (HttpResponseMessage protectedCategory = await Client.DeleteAsync(
                   $"/api/categories/{category.Id}",
                   cancellationToken))
        {
            await ApiTestData.AssertProblemAsync(
                protectedCategory,
                HttpStatusCode.Conflict,
                "category_has_board_games",
                cancellationToken);
        }

        await ApiTestData.DeleteAsync(
            Client,
            $"/api/game-copies/{copy.Id}",
            cancellationToken);
        await ApiTestData.DeleteAsync(
            Client,
            $"/api/board-games/{boardGame.Id}",
            cancellationToken);
        await ApiTestData.DeleteAsync(
            Client,
            $"/api/categories/{category.Id}",
            cancellationToken);
        await ApiTestData.DeleteAsync(
            Client,
            $"/api/members/{member.Id}",
            cancellationToken);

        using HttpResponseMessage missingCategory = await Client.GetAsync(
            $"/api/categories/{category.Id}",
            cancellationToken);
        await ApiTestData.AssertProblemAsync(
            missingCategory,
            HttpStatusCode.NotFound,
            "category_not_found",
            cancellationToken);
    }
}
