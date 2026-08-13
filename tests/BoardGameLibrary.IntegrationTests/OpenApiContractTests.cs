using System.Net;
using System.Text.Json;
using BoardGameLibrary.IntegrationTests.Infrastructure;

namespace BoardGameLibrary.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class OpenApiContractTests(PostgreSqlFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task CategoryOperations_DocumentSuccessAndProblemResponses()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using JsonDocument document = await GetOpenApiDocumentAsync(cancellationToken);
        JsonElement paths = document.RootElement.GetProperty("paths");
        JsonElement createResponses = paths
            .GetProperty("/api/categories")
            .GetProperty("post")
            .GetProperty("responses");
        JsonElement deleteResponses = paths
            .GetProperty("/api/categories/{id}")
            .GetProperty("delete")
            .GetProperty("responses");

        AssertResponseSchema(createResponses, "201", "CreatedResourceResponse");
        AssertResponseSchema(createResponses, "400", "ValidationProblemDetails");
        AssertResponseSchema(createResponses, "409", "ProblemDetails");
        Assert.True(deleteResponses.TryGetProperty("204", out JsonElement noContentResponse));
        Assert.False(noContentResponse.TryGetProperty("content", out _));
        AssertResponseSchema(deleteResponses, "400", "ValidationProblemDetails");
        AssertResponseSchema(deleteResponses, "404", "ProblemDetails");
        AssertResponseSchema(deleteResponses, "409", "ProblemDetails");
    }

    [Fact]
    public async Task EnumSchemas_AreCamelCaseStringsAndNeverIntegers()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using JsonDocument document = await GetOpenApiDocumentAsync(cancellationToken);
        JsonElement schemas = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas");

        AssertEnumSchema(
            schemas.GetProperty("GameCopyCondition"),
            ["excellent", "good", "fair", "damaged"]);
        AssertEnumSchema(
            schemas.GetProperty("LoanStatus"),
            ["active", "overdue", "returned"]);

        JsonProperty[] enumSchemas = schemas
            .EnumerateObject()
            .Where(component => component.Value.TryGetProperty("enum", out _))
            .ToArray();

        Assert.NotEmpty(enumSchemas);

        foreach (JsonProperty enumSchema in enumSchemas)
        {
            Assert.Equal("string", enumSchema.Value.GetProperty("type").GetString());
            Assert.All(
                enumSchema.Value.GetProperty("enum").EnumerateArray(),
                value => Assert.Equal(JsonValueKind.String, value.ValueKind));
        }
    }

    [Fact]
    public async Task RequiredRequestProperties_DoNotAdvertiseNull()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using JsonDocument document = await GetOpenApiDocumentAsync(cancellationToken);
        JsonElement schemas = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas");

        AssertRequiredPropertyDoesNotAllowNull(schemas, "CreateCategoryRequest", "name");
        AssertRequiredPropertyDoesNotAllowNull(schemas, "CreateBoardGameRequest", "publicationYear");
        AssertRequiredPropertyDoesNotAllowNull(schemas, "CreateBoardGameRequest", "categoryIds");
        AssertRequiredPropertyDoesNotAllowNull(schemas, "CreateGameCopyRequest", "condition");
        AssertRequiredPropertyDoesNotAllowNull(schemas, "CreateLoanRequest", "memberId");
        AssertRequiredPropertyDoesNotAllowNull(schemas, "UpdateMemberRequest", "isActive");

        JsonElement optionalAcquiredOn = schemas
            .GetProperty("CreateGameCopyRequest")
            .GetProperty("properties")
            .GetProperty("acquiredOn");
        Assert.True(AllowsNull(optionalAcquiredOn));
    }

    [Fact]
    public async Task ListOperations_UsePublicCamelCasePaginationParameterNames()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using JsonDocument document = await GetOpenApiDocumentAsync(cancellationToken);
        JsonElement paths = document.RootElement.GetProperty("paths");
        string[] listPaths =
        [
            "/api/board-games",
            "/api/categories",
            "/api/board-games/{boardGameId}/copies",
            "/api/loans",
            "/api/members",
        ];

        foreach (string path in listPaths)
        {
            string[] parameterNames = paths
                .GetProperty(path)
                .GetProperty("get")
                .GetProperty("parameters")
                .EnumerateArray()
                .Select(parameter => parameter.GetProperty("name").GetString()!)
                .ToArray();

            Assert.Contains("page", parameterNames);
            Assert.Contains("pageSize", parameterNames);
            Assert.Contains("sortBy", parameterNames);
            Assert.Contains("sortDirection", parameterNames);
            Assert.DoesNotContain("Page", parameterNames);
            Assert.DoesNotContain("PageSize", parameterNames);
            Assert.DoesNotContain("SortBy", parameterNames);
            Assert.DoesNotContain("SortDirection", parameterNames);
        }
    }

    private async Task<JsonDocument> GetOpenApiDocumentAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await Client.GetAsync(
            "/openapi/v1.json",
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Stream content = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
    }

    private static void AssertResponseSchema(
        JsonElement responses,
        string statusCode,
        string expectedSchemaName)
    {
        Assert.True(responses.TryGetProperty(statusCode, out JsonElement response));
        string? reference = response
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();

        Assert.Equal($"#/components/schemas/{expectedSchemaName}", reference);
    }

    private static void AssertEnumSchema(JsonElement schema, string[] expectedValues)
    {
        Assert.Equal("string", schema.GetProperty("type").GetString());
        string[] actualValues = schema
            .GetProperty("enum")
            .EnumerateArray()
            .Select(value => value.GetString()!)
            .ToArray();

        Assert.Equal(expectedValues, actualValues);
    }

    private static void AssertRequiredPropertyDoesNotAllowNull(
        JsonElement schemas,
        string schemaName,
        string propertyName)
    {
        JsonElement schema = schemas.GetProperty(schemaName);
        string[] required = schema
            .GetProperty("required")
            .EnumerateArray()
            .Select(value => value.GetString()!)
            .ToArray();

        Assert.Contains(propertyName, required);
        Assert.False(AllowsNull(schema.GetProperty("properties").GetProperty(propertyName)));
    }

    private static bool AllowsNull(JsonElement schema)
    {
        if (schema.TryGetProperty("type", out JsonElement type))
        {
            if (type.ValueKind == JsonValueKind.String && type.GetString() == "null")
            {
                return true;
            }

            if (type.ValueKind == JsonValueKind.Array &&
                type.EnumerateArray().Any(value => value.GetString() == "null"))
            {
                return true;
            }
        }

        foreach (string unionKeyword in new[] { "oneOf", "anyOf" })
        {
            if (schema.TryGetProperty(unionKeyword, out JsonElement alternatives) &&
                alternatives.EnumerateArray().Any(AllowsNull))
            {
                return true;
            }
        }

        return false;
    }
}
