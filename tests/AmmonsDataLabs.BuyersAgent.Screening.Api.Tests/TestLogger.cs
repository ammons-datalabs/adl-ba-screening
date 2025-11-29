using Microsoft.Extensions.Logging;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Tests;

public class TestLogEntry
{
    public LogLevel LogLevel { get; init; }
    public EventId EventId { get; init; }
    public string Message { get; init; } = "";
    public Exception? Exception { get; init; }
}

public class TestLogger<T> : ILogger<T>
{
    private readonly List<TestLogEntry> _entries = new();

    public IReadOnlyList<TestLogEntry> Entries => _entries;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _entries.Add(new TestLogEntry
        {
            LogLevel =  logLevel,
            EventId = eventId,
            Message = formatter(state, exception),
            Exception = exception
        });
    }

    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}