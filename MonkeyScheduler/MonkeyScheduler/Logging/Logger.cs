using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

namespace MonkeyScheduler.Logging
{
    public class Logger
    {
        private readonly string _dbPath;
        private readonly string _connectionString;
        private readonly int _maxLogCount;
        private readonly TimeSpan _maxLogAge;
        private readonly ILogFormatter _formatter;

        public Logger(string dbPath = "logs.db", int maxLogCount = 10000, TimeSpan? maxLogAge = null, ILogFormatter formatter = null)
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
            if (!File.Exists(_dbPath))
            {
                SQLiteConnection.CreateFile(_dbPath);
            }

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Logs (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Timestamp DATETIME NOT NULL,
                            Level TEXT NOT NULL,
                            Message TEXT NOT NULL,
                            Exception TEXT
                        )";
                    command.ExecuteNonQuery();
                }
            }
        }

        public async Task LogAsync(string level, string message, Exception exception = null)
        {
            var formattedMessage = _formatter.Format(level, message, exception);

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                        INSERT INTO Logs (Timestamp, Level, Message, Exception)
                        VALUES (@timestamp, @level, @message, @exception)";
                    
                    command.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@level", level);
                    command.Parameters.AddWithValue("@message", formattedMessage);
                    command.Parameters.AddWithValue("@exception", exception?.ToString() ?? (object)DBNull.Value);
                    
                    await command.ExecuteNonQueryAsync();
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

        public async Task LogErrorAsync(string message, Exception exception = null)
        {
            await LogAsync("ERROR", message, exception);
        }

        public async Task CleanupLogsAsync()
        {
            await CleanupByAgeAsync();
            await CleanupByCountAsync();
        }

        private async Task CleanupByAgeAsync()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                        DELETE FROM Logs 
                        WHERE Timestamp < @cutoffDate";
                    
                    command.Parameters.AddWithValue("@cutoffDate", DateTime.UtcNow - _maxLogAge);
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

        public async Task<DateTime?> GetOldestLogDateAsync()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT MIN(Timestamp) FROM Logs";
                    var result = await command.ExecuteScalarAsync();
                    return result == DBNull.Value ? null : (DateTime?)result;
                }
            }
        }
    }
} 