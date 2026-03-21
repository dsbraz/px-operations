using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using PxOperations.Domain.Abstractions;

namespace PxOperations.Api.Serialization;

public sealed class OptionalSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Optional<>))
        {
            var innerType = type.GetGenericArguments()[0];
            var innerNullable = !innerType.IsValueType || Nullable.GetUnderlyingType(innerType) is not null;

            schema.Properties?.Clear();
            var jsonType = GetJsonSchemaType(innerType);
            schema.Type = innerNullable ? jsonType | JsonSchemaType.Null : jsonType;
        }

        return Task.CompletedTask;
    }

    private static JsonSchemaType GetJsonSchemaType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string)) return JsonSchemaType.String;
        if (underlying == typeof(int) || underlying == typeof(long)) return JsonSchemaType.Integer;
        if (underlying == typeof(bool)) return JsonSchemaType.Boolean;
        if (underlying == typeof(double) || underlying == typeof(float) || underlying == typeof(decimal)) return JsonSchemaType.Number;

        return JsonSchemaType.String;
    }
}
