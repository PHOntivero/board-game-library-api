using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BoardGameLibrary.Api.OpenApi;

internal sealed class ApiSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        Type type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type)
            ?? context.JsonTypeInfo.Type;

        if (type.IsEnum)
        {
            schema.Type = JsonSchemaType.String;
            schema.Format = null;
            schema.Enum = Enum.GetNames(type)
                .Select(name => JsonSerializer.SerializeToNode(ToCamelCase(name))!)
                .ToList();
            schema.OneOf = null;
        }

        if (context.JsonPropertyInfo?.AttributeProvider is not null &&
            context.JsonPropertyInfo.AttributeProvider.IsDefined(typeof(RequiredAttribute), inherit: true))
        {
            DisallowNull(schema);
        }

        foreach (string requiredProperty in schema.Required ?? new HashSet<string>())
        {
            if (schema.Properties?.TryGetValue(requiredProperty, out IOpenApiSchema? propertySchema) != true)
            {
                continue;
            }

            if (propertySchema is OpenApiSchema concretePropertySchema)
            {
                DisallowNull(concretePropertySchema);
            }
        }

        return Task.CompletedTask;
    }

    private static string ToCamelCase(string value) =>
        string.IsNullOrEmpty(value)
            ? value
            : char.ToLowerInvariant(value[0]) + value[1..];

    private static void DisallowNull(OpenApiSchema schema)
    {
        schema.Type &= ~JsonSchemaType.Null;

        if (schema.OneOf is { Count: > 0 })
        {
            schema.OneOf = schema.OneOf
                .Where(candidate => candidate.Type != JsonSchemaType.Null)
                .ToList();
        }
    }
}
