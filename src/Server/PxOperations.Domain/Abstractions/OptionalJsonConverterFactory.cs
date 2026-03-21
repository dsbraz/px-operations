using System.Text.Json;
using System.Text.Json.Serialization;

namespace PxOperations.Domain.Abstractions;

public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var innerType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OptionalJsonConverter<>).MakeGenericType(innerType);
        return (JsonConverter)Activator.CreateInstance(converterType, options)!;
    }

    private sealed class OptionalJsonConverter<T>(JsonSerializerOptions options) : JsonConverter<Optional<T>>
    {
        private readonly JsonConverter<T> _innerConverter =
            (JsonConverter<T>)options.GetConverter(typeof(T));

        public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return Optional<T>.Of(default);
            }

            var value = _innerConverter.Read(ref reader, typeof(T), options);
            return Optional<T>.Of(value);
        }

        public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
        {
            if (!value.HasValue || value.Value is null)
            {
                writer.WriteNullValue();
                return;
            }

            _innerConverter.Write(writer, value.Value, options);
        }
    }
}
