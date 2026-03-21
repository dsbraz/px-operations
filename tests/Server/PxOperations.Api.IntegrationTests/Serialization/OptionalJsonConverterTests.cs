using System.Text.Json;
using PxOperations.Domain.Abstractions;

namespace PxOperations.Api.IntegrationTests.Serialization;

public sealed class OptionalJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new OptionalJsonConverterFactory() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private sealed record TestDto(
        Optional<string> Name = default,
        Optional<string?> NullableName = default,
        Optional<int> Count = default);

    [Fact]
    public void Deserialize_missing_field_should_be_undefined()
    {
        var json = "{}";
        var dto = JsonSerializer.Deserialize<TestDto>(json, Options)!;

        Assert.False(dto.Name.HasValue);
        Assert.False(dto.NullableName.HasValue);
        Assert.False(dto.Count.HasValue);
    }

    [Fact]
    public void Deserialize_explicit_null_should_be_defined_with_null()
    {
        var json = """{"name": null}""";
        var dto = JsonSerializer.Deserialize<TestDto>(json, Options)!;

        Assert.True(dto.Name.HasValue);
        Assert.Null(dto.Name.Value);
    }

    [Fact]
    public void Deserialize_value_should_be_defined_with_value()
    {
        var json = """{"name": "hello"}""";
        var dto = JsonSerializer.Deserialize<TestDto>(json, Options)!;

        Assert.True(dto.Name.HasValue);
        Assert.Equal("hello", dto.Name.Value);
    }

    [Fact]
    public void Deserialize_int_value_should_be_defined()
    {
        var json = """{"count": 42}""";
        var dto = JsonSerializer.Deserialize<TestDto>(json, Options)!;

        Assert.True(dto.Count.HasValue);
        Assert.Equal(42, dto.Count.Value);
    }

    [Fact]
    public void Serialize_undefined_should_write_null()
    {
        var dto = new TestDto();
        var json = JsonSerializer.Serialize(dto, Options);
        var doc = JsonDocument.Parse(json);

        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("name").ValueKind);
    }

    [Fact]
    public void Serialize_defined_null_should_write_null()
    {
        var dto = new TestDto(Name: Optional<string>.Of(null));
        var json = JsonSerializer.Serialize(dto, Options);
        var doc = JsonDocument.Parse(json);

        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("name").ValueKind);
    }

    [Fact]
    public void Serialize_defined_value_should_write_value()
    {
        var dto = new TestDto(Name: Optional<string>.Of("hello"));
        var json = JsonSerializer.Serialize(dto, Options);
        var doc = JsonDocument.Parse(json);

        Assert.Equal("hello", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public void Roundtrip_missing_field_should_resolve_to_fallback()
    {
        var json = """{"name": "hello"}""";
        var dto = JsonSerializer.Deserialize<TestDto>(json, Options)!;

        Assert.Equal("hello", dto.Name.Resolve("fallback"));
        Assert.Equal("default-nullable", dto.NullableName.Resolve("default-nullable"));
    }

    [Fact]
    public void Roundtrip_explicit_null_should_resolve_to_null()
    {
        var json = """{"nullableName": null}""";
        var dto = JsonSerializer.Deserialize<TestDto>(json, Options)!;

        Assert.Null(dto.NullableName.Resolve("fallback"));
    }
}
