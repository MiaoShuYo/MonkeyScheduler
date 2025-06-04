using System.Data.SQLite;
using Microsoft.Extensions.Logging;

namespace MonkeyScheduler.Logging
{
    /// <summary>
    /// 自定义日志记录器
    /// 实现ILogger接口，将日志记录到SQLite数据库
    /// </summary>
    public class Logger : ILogger
    {
        private readonly string _dbPath;
        private readonly string _connectionString;
        private readonly int _maxLogCount;
        private readonly TimeSpan _maxLogAge;
        private readonly ILogFormatter _formatter;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbPath">数据库文件路径</param>
        /// <param name="maxLogCount">最大日志数量</param>
        /// <param name="maxLogAge">日志最大保留时间</param>
        /// <param name="formatter">日志格式化器</param>
        public Logger(string dbPath = "logs.db", int maxLogCount = 10000, TimeSpan? maxLogAge = null, ILogFormatter? formatter = null)
        {
            _dbPath = dbPath;
            _connectionString = $"Data Source={_dbPath};Version=3;";
            _maxLogCount = maxLogCount;
            _maxLogAge = maxLogAge ?? TimeSpan.FromDays(30);
            _formatter = formatter ?? new DefaultLogFormatter();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            // 检查数据库文件是否存在
            if (!File.Exists(_dbPath))
            {
                SQLiteConnection.CreateFile(_dbPath);
                
                // 只在创建新数据库时创建表
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(connection))
                    {
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Logs (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Timestamp TEXT NOT NULL,
                                Level TEXT NOT NULL,
                                Message TEXT NOT NULL,
                                Exception TEXT
                            )";
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        /// <param name="logLevel">日志级别</param>
        /// <param name="eventId">事件ID</param>
        /// <param name="state">状态</param>
        /// <param name="exception">异常</param>
        /// <param name="formatter">格式化器</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);
            var level = logLevel.ToString().ToUpper();
            LogAsync(level, message, exception).Wait();
        }

        /// <summary>
        /// 检查是否启用指定级别的日志
        /// </summary>
        /// <param name="logLevel">日志级别</param>
        /// <returns>是否启用</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            // None 级别的日志不应该被记录
            if (logLevel == LogLevel.None)
            {
                return false;
            }
            return true; // 其他级别默认启用
        }

        /// <summary>
        /// 开始日志范围
        /// </summary>
        /// <typeparam name="TState">状态类型</typeparam>
        /// <param name="state">状态</param>
        /// <returns>日志范围</returns>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null; // 不支持日志范围
        }

        /// <summary>
        /// 异步记录日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常</param>
        public async Task LogAsync(string level, string message, Exception? exception = null)
        {
            var formattedMessage = _formatter.Format(level, message, exception);
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                        INSERT INTO Logs (Timestamp, Level, Message, Exception)
                        VALUES (@Timestamp, @Level, @Message, @Exception)";
                    
                    command.Parameters.AddWithValue("@Timestamp", timestamp);
                    command.Parameters.AddWithValue("@Level", level);
                    command.Parameters.AddWithValue("@Message", message);
                    command.Parameters.AddWithValue("@Exception", exception?.ToString());
                    
                    await command.ExecuteNonQueryAsync();
                }
            }

            // 清理旧日志
            await CleanupLogsAsync();
        }

        private async Task CleanupLogsAsync()
        {
            await CleanupByAgeAsync();
            await CleanupByCountAsync();
        }

        private async Task CleanupByAgeAsync()
        {
            var cutoffDate = DateTime.UtcNow.Subtract(_maxLogAge).ToString("yyyy-MM-dd HH:mm:ss.fff");

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                        DELETE FROM Logs 
                        WHERE Timestamp < @cutoffDate";
                    
                    command.Parameters.AddWithValue("@cutoffDate", cutoffDate);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task CleanupByCountAsync()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                        DELETE FROM Logs 
                        WHERE Id NOT IN (
                            SELECT Id 
                            FROM Logs 
                            ORDER BY Timestamp DESC 
                            LIMIT @maxCount
                        )";
                    
                    command.Parameters.AddWithValue("@maxCount", _maxLogCount);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// 获取日志数量
        /// </summary>
        /// <returns>日志数量</returns>
        public async Task<int> GetLogCountAsync()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT COUNT(*) FROM Logs";
                    return Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
        }

        public async Task LogInfoAsync(string message)
        {
            await LogAsync("INFO", message);
        }

        public async Task LogWarningAsync(string message)
        {
            await LogAsync("WARNING", message);
        }

        public async Task LogErrorAsync(string message, Exception? exception = null)
        {
            await LogAsync("ERROR", message, exception);
        }

        public async Task<DateTime?> GetOldestLogDateAsync()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT MIN(Timestamp) FROM Logs";
                    var result = await command.ExecuteScalarAsync();
                    if (result == DBNull.Value || result == null)
                    {
                        return null;
                    }
                    return DateTime.ParseExact(result.ToString()!, 
                                            "yyyy-MM-dd HH:mm:ss.fff", 
                                            System.Globalization.CultureInfo.InvariantCulture);
                }
            }
        }
    }
} 