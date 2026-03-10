using Microsoft.Extensions.Logging;
using MonkeyScheduler.Data.MySQL.Models;
using MonkeyScheduler.Data.MySQL.Repositories;

namespace MonkeyScheduler.Data.MySQL.Logger;

public class MySQLLogger : ILogger
{
    private readonly string _categoryName;
    private readonly LogRepository _logRepository;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public MySQLLogger(string categoryName, LogRepository logRepository)
    {
        _categoryName = categoryName;
        _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
    }

    public IDisposable BeginScope<TState>(TState state)
        => null;

    public bool IsEnabled(LogLevel logLevel)
        => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state,
        Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        var logEntry = new LogEntry
        {
            Level = logLevel.ToString(),
            Message = message,
            Exception = exception?.ToString(),
            Timestamp = DateTime.UtcNow,
            Source = _categoryName
        };

        // 异步写库且不阻塞调用线程，使用信号量控制并发
        _ = Task.Run(async () =>
        {
            try
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);
                await _logRepository.AddLogAsync(logEntry).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出异常，因为这是日志记录器
                System.Diagnostics.Debug.WriteLine($"Error logging to database: {ex}");
            }
            finally
            {
                _semaphore.Release();
            }
        });
    }

    // 实现 IDisposable 接口以释放资源
    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}