using Microsoft.Extensions.Logging;

namespace PxOperations.BlazorWasm.Tests.Helpers;

/// <summary>
/// Guarda o que a tela registrou, para provar que uma falha engolida pela
/// mensagem amigável ainda deixa a exceção no log.
/// </summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Entries.Add((logLevel, exception));
}
