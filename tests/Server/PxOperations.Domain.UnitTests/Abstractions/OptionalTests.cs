using PxOperations.Domain.Abstractions;

namespace PxOperations.Domain.UnitTests.Abstractions;

public sealed class OptionalTests
{
    [Fact]
    public void Undefined_should_not_have_value()
    {
        var optional = Optional<string>.Undefined;

        Assert.False(optional.HasValue);
    }

    [Fact]
    public void Undefined_should_return_default_when_accessing_value()
    {
        var optional = Optional<string>.Undefined;

        Assert.Null(optional.Value);
    }

    [Fact]
    public void Of_with_value_should_have_value()
    {
        var optional = Optional<string>.Of("hello");

        Assert.True(optional.HasValue);
        Assert.Equal("hello", optional.Value);
    }

    [Fact]
    public void Of_with_null_should_have_value_as_null()
    {
        var optional = Optional<string>.Of(null);

        Assert.True(optional.HasValue);
        Assert.Null(optional.Value);
    }

    [Fact]
    public void Resolve_should_return_fallback_when_undefined()
    {
        var optional = Optional<string>.Undefined;

        Assert.Equal("fallback", optional.Resolve("fallback"));
    }

    [Fact]
    public void Resolve_should_return_value_when_defined()
    {
        var optional = Optional<string>.Of("hello");

        Assert.Equal("hello", optional.Resolve("fallback"));
    }

    [Fact]
    public void Resolve_should_return_null_when_defined_with_null()
    {
        var optional = Optional<string>.Of(null);

        Assert.Null(optional.Resolve("fallback"));
    }

    [Fact]
    public void Map_should_transform_value_when_defined()
    {
        var optional = Optional<string>.Of("hello");

        var mapped = optional.Map(s => s.Length);

        Assert.True(mapped.HasValue);
        Assert.Equal(5, mapped.Value);
    }

    [Fact]
    public void Map_should_propagate_undefined()
    {
        var optional = Optional<string>.Undefined;

        var mapped = optional.Map(s => s.Length);

        Assert.False(mapped.HasValue);
    }

    [Fact]
    public void Map_should_produce_defined_default_when_defined_with_null()
    {
        var optional = Optional<string>.Of(null);

        var mapped = optional.Map(s => s.Length);

        Assert.True(mapped.HasValue);
        Assert.Equal(default, mapped.Value);
    }

    [Fact]
    public void Map_should_produce_defined_null_when_defined_with_null_reference_result()
    {
        var optional = Optional<string>.Of(null);

        var mapped = optional.Map(s => s.ToUpper());

        Assert.True(mapped.HasValue);
        Assert.Null(mapped.Value);
    }

    [Fact]
    public void Equality_two_undefined_should_be_equal()
    {
        var a = Optional<string>.Undefined;
        var b = Optional<string>.Undefined;

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equality_same_value_should_be_equal()
    {
        var a = Optional<string>.Of("hello");
        var b = Optional<string>.Of("hello");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_different_values_should_not_be_equal()
    {
        var a = Optional<string>.Of("hello");
        var b = Optional<string>.Of("world");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equality_defined_null_and_undefined_should_not_be_equal()
    {
        var a = Optional<string>.Of(null);
        var b = Optional<string>.Undefined;

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Default_struct_should_be_undefined()
    {
        Optional<int> optional = default;

        Assert.False(optional.HasValue);
    }

    [Fact]
    public void Resolve_should_work_with_value_types()
    {
        var optional = Optional<int>.Of(42);

        Assert.Equal(42, optional.Resolve(0));
    }

    [Fact]
    public void Resolve_should_return_fallback_for_undefined_value_type()
    {
        var optional = Optional<int>.Undefined;

        Assert.Equal(99, optional.Resolve(99));
    }
}
