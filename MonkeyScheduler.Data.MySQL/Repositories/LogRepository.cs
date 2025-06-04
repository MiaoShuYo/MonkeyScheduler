using MonkeyScheduler.Data.MySQL.Data;
using MonkeyScheduler.Data.MySQL.Models;

namespace MonkeyScheduler.Data.MySQL.Repositories
{
    /// <summary>
    /// 日志仓储类
    /// 负责日志数据的持久化和查询操作
    /// </summary>
    public class LogRepository
    {
        private readonly MySqlDbContext _dbContext;
        private readonly IDapperWrapper _dapperWrapper;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">MySQL数据库上下文</param>
        /// <param name="dapperWrapper">Dapper包装器</param>
        public LogRepository(MySqlDbContext dbContext, IDapperWrapper dapperWrapper)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _dapperWrapper = dapperWrapper ?? throw new ArgumentNullException(nameof(dapperWrapper));
        }

        /// <summary>
        /// 异步添加日志记录
        /// </summary>
        /// <param name="logEntry">要添加的日志条目</param>
        /// <returns>新添加日志记录的自增ID</returns>
        public async Task<int> AddLogAsync(LogEntry logEntry)
        {
            if (logEntry == null)
                throw new ArgumentNullException(nameof(logEntry));

            const string sql = @"
                INSERT INTO Logs (
                    Level, 
                    Message, 
                    Exception, 
                    Timestamp, 
                    Source,
                    Category,
                    EventId
                )
                VALUES (
                    @Level, 
                    @Message, 
                    @Exception, 
                    @Timestamp, 
                    @Source,
                    @Category,
                    @EventId
                );
                SELECT LAST_INSERT_ID();";

            try
            {
                // 确保连接是关闭的
                if (_dbContext.Connection.State == System.Data.ConnectionState.Open)
                {
                    _dbContext.Connection.Close();
                }

                // ��开连接
                _dbContext.Connection.Open();

                var parameters = new
                {
                    Level = logEntry.Level ?? string.Empty,
                    Message = logEntry.Message ?? string.Empty,
                    Exception = logEntry.Exception,
                    Timestamp = logEntry.Timestamp,
                    Source = logEntry.Source ?? string.Empty,
                    Category = logEntry.Category ?? string.Empty,
                    EventId = logEntry.EventId ?? string.Empty
                };

                // 调用 DapperWrapper 执行插入操作
                var result = await _dapperWrapper.ExecuteScalarAsync<int>(_dbContext.Connection, sql, parameters);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                // 确保在操作完成后关闭连接
                if (_dbContext.Connection.State == System.Data.ConnectionState.Open)
                {
                    _dbContext.Connection.Close();
                }
            }
        }

        /// <summary>
        /// 异步获取指定时间范围内的日志记录
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="level">日志级别（可选）</param>
        /// <returns>符合条件的日志记录集合，按时间戳降序排列</returns>
        public async Task<IEnumerable<LogEntry>> GetLogsAsync(DateTime startTime, DateTime endTime, string? level = null)
        {
            if (endTime <= startTime)
            {
                throw new ArgumentException("End time must be greater than start time.");
            }

            try
            {
                // 确保连接是关闭的
                if (_dbContext.Connection.State == System.Data.ConnectionState.Open)
                {
                    _dbContext.Connection.Close();
                }

                // 打开连接
                _dbContext.Connection.Open();

                var sql = @"
                    SELECT * FROM Logs 
                    WHERE Timestamp BETWEEN @StartTime AND @EndTime";

                if (!string.IsNullOrEmpty(level))
                {
                    sql += " AND Level = @Level";
                }

                sql += " ORDER BY Timestamp DESC";

                return await _dapperWrapper.QueryAsync<LogEntry>(
                    _dbContext.Connection,
                    sql,
                    new { StartTime = startTime, EndTime = endTime, Level = level });
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                // 确保在操作完成后关闭连接
                if (_dbContext.Connection.State == System.Data.ConnectionState.Open)
                {
                    _dbContext.Connection.Close();
                }
            }
        }
    }
}
