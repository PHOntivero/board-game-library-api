using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BoardGameLibrary.Api.OpenApi;

internal sealed class RequiredPropertiesDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (document.Components?.Schemas is null)
        {
            return Task.CompletedTask;
        }

        foreach (IOpenApiSchema schema in document.Components.Schemas.Values)
        {
            foreach (string requiredProperty in schema.Required ?? new HashSet<string>())
            {
                if (schema.Properties?.TryGetValue(requiredProperty, out IOpenApiSchema? propertySchema) == true &&
                    propertySchema is OpenApiSchema concretePropertySchema)
                {
                    concretePropertySchema.Type &= ~JsonSchemaType.Null;

                    if (concretePropertySchema.OneOf is { Count: > 0 })
                    {
                        concretePropertySchema.OneOf = concretePropertySchema.OneOf
                            .Where(candidate => candidate.Type != JsonSchemaType.Null)
                            .ToList();
                    }
                }
            }
        }

        return Task.CompletedTask;
    }
}
