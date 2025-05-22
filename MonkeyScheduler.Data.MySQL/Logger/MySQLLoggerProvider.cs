using Microsoft.Extensions.Logging;
using MonkeyScheduler.Data.MySQL.Repositories;

namespace MonkeyScheduler.Data.MySQL.Logger;

public class MySQLLoggerProvider : ILoggerProvider
{
    private readonly LogRepository _logRepository;

    public MySQLLoggerProvider(LogRepository logRepository)
    {
        _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
    }

    public ILogger CreateLogger(string categoryName)
        => new MySQLLogger(categoryName, _logRepository);

    public void Dispose()
    {
    }
}