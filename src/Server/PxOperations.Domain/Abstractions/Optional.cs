namespace PxOperations.Domain.Abstractions;

public readonly struct Optional<T> : IEquatable<Optional<T>>
{
    private readonly T? _value;
    private readonly bool _hasValue;

    private Optional(T? value)
    {
        _value = value;
        _hasValue = true;
    }

    public bool HasValue => _hasValue;

    public T? Value => _value;

    public static Optional<T> Undefined => default;

    public static Optional<T> Of(T? value) => new(value);

    public T? Resolve(T? fallback) => _hasValue ? _value : fallback;

    public Optional<TResult> Map<TResult>(Func<T, TResult> mapper)
        => _hasValue && _value is not null
            ? Optional<TResult>.Of(mapper(_value))
            : _hasValue
                ? Optional<TResult>.Of(default)
                : Optional<TResult>.Undefined;

    public bool Equals(Optional<T> other)
        => _hasValue == other._hasValue && EqualityComparer<T>.Default.Equals(_value, other._value);

    public override bool Equals(object? obj)
        => obj is Optional<T> other && Equals(other);

    public override int GetHashCode()
        => _hasValue ? HashCode.Combine(true, _value) : HashCode.Combine(false);

    public static bool operator ==(Optional<T> left, Optional<T> right) => left.Equals(right);
    public static bool operator !=(Optional<T> left, Optional<T> right) => !left.Equals(right);
}
