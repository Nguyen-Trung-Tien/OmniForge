using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace OmniForge.Services.Logging;

/// <summary>
/// Thread-safe structured JSON-Lines file logger provider for production observability and AI Agent self-diagnostics.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logFilePath;
    private readonly BlockingCollection<string> _messageQueue = new(1024);
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processQueueTask;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    public FileLoggerProvider()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OmniForge",
            "logs",
            $"omniforge-{DateTimeOffset.UtcNow:yyyyMMdd}.jsonl"))
    {
    }

    public FileLoggerProvider(string logFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);
        _logFilePath = logFilePath;
        _processQueueTask = Task.Run(ProcessLogQueueAsync);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, this));
    }

    internal void EnqueueLogEntry(string jsonEntry)
    {
        if (!_messageQueue.IsAddingCompleted)
        {
            try
            {
                _messageQueue.TryAdd(jsonEntry);
            }
            catch (InvalidOperationException)
            {
                // Ignore if collection is already marked complete during shutdown
            }
        }
    }

    private async Task ProcessLogQueueAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            foreach (var entry in _messageQueue.GetConsumingEnumerable(_cts.Token))
            {
                try
                {
                    await using var stream = new FileStream(
                        _logFilePath,
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.ReadWrite,
                        bufferSize: 4096,
                        useAsync: true);
                    await using var writer = new StreamWriter(stream);
                    await writer.WriteLineAsync(entry).ConfigureAwait(false);
                }
                catch (IOException)
                {
                    // Avoid crashing background logger on transient file lock
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on disposal
        }
    }

    public void Dispose()
    {
        _messageQueue.CompleteAdding();
        _cts.Cancel();
        try
        {
            _processQueueTask.Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
            // Ignore shutdown cancellation exceptions
        }
        _cts.Dispose();
        _messageQueue.Dispose();
    }

    private sealed class FileLogger(string categoryName, FileLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            ArgumentNullException.ThrowIfNull(formatter);

            var payload = new
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = logLevel.ToString(),
                Category = categoryName,
                EventId = eventId.Id,
                Message = formatter(state, exception),
                Exception = exception?.ToString()
            };

            var json = JsonSerializer.Serialize(payload);
            provider.EnqueueLogEntry(json);
        }
    }
}

