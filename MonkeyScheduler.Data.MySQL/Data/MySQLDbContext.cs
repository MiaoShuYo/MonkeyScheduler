using System.Data;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;

namespace MonkeyScheduler.Data.MySQL.Data
{
    /// <summary>
    /// MySQL数据库上下文类
    /// 负责管理数据库连接和事务，支持连接池和自动重连
    /// </summary>
    public class MySqlDbContext : IDisposable
    {
        private readonly string _connectionString;
        private readonly ILogger<MySqlDbContext>? _logger;
        private readonly object _connectionLock = new object();
        private IDbConnection? _connection;
        private IDbTransaction? _currentTransaction;
        private bool _disposed;
        private readonly int _maxRetryAttempts;
        private readonly TimeSpan _retryDelay;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <param name="logger">日志记录器（可选）</param>
        /// <param name="maxRetryAttempts">最大重试次数</param>
        /// <param name="retryDelay">重试延迟</param>
        /// <exception cref="ArgumentNullException">当connectionString为null或空时抛出</exception>
        public MySqlDbContext(string connectionString, ILogger<MySqlDbContext>? logger = null, int maxRetryAttempts = 3, TimeSpan? retryDelay = null)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            var options = new MySqlConnectionOptions
            {
                ConnectionString = connectionString,
                MaxRetryAttempts = maxRetryAttempts,
                RetryDelay = retryDelay ?? TimeSpan.FromSeconds(1)
            };

            _connectionString = EnsureConnectionPooling(options);
            _logger = logger;
            _maxRetryAttempts = options.MaxRetryAttempts;
            _retryDelay = options.RetryDelay;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options">连接配置选项</param>
        /// <param name="logger">日志记录器（可选）</param>
        /// <exception cref="ArgumentNullException">当options为null时抛出</exception>
        public MySqlDbContext(MySqlConnectionOptions options, ILogger<MySqlDbContext>? logger = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                throw new ArgumentException("连接字符串不能为空", nameof(options));
            }

            _connectionString = EnsureConnectionPooling(options);
            _logger = logger;
            _maxRetryAttempts = options.MaxRetryAttempts;
            _retryDelay = options.RetryDelay;
        }

        /// <summary>
        /// 确保连接字符串包含连接池配置
        /// </summary>
        /// <param name="options">连接配置选项</param>
        /// <returns>包含连接池配置的连接字符串</returns>
        private static string EnsureConnectionPooling(MySqlConnectionOptions options)
        {
            var builder = new MySqlConnectionStringBuilder(options.ConnectionString);
            
            // 设置连接池参数
            if (options.EnableConnectionPooling)
            {
                if (!builder.ContainsKey("Pooling"))
                    builder.Pooling = true;
                if (!builder.ContainsKey("Min Pool Size"))
                    builder["Min Pool Size"] = options.MinPoolSize;
                if (!builder.ContainsKey("Max Pool Size"))
                    builder["Max Pool Size"] = options.MaxPoolSize;
                if (!builder.ContainsKey("Connection Lifetime"))
                    builder["Connection Lifetime"] = options.ConnectionLifetime;
                if (!builder.ContainsKey("Connection Reset"))
                    builder.ConnectionReset = options.ConnectionReset;
                if (!builder.ContainsKey("Auto Enlist"))
                    builder.AutoEnlist = options.AutoEnlist;
            }
            else
            {
                builder.Pooling = false;
            }

            // 设置超时参数
            if (!builder.ContainsKey("Connection Timeout"))
                builder.ConnectionTimeout = (uint)options.ConnectionTimeout;
            if (!builder.ContainsKey("Default Command Timeout"))
                builder.DefaultCommandTimeout = (uint)options.CommandTimeout;

            return builder.ConnectionString;
        }

        /// <summary>
        /// 获取数据库连接
        /// 如果连接不存在或已关闭，将创建新连接并打开
        /// </summary>
        /// <exception cref="Exception">当创建或打开连接失败时抛出</exception>
        public virtual IDbConnection Connection
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(MySqlDbContext));
                }

                lock (_connectionLock)
                {
                    if (_connection == null || _connection.State == ConnectionState.Closed || _connection.State == ConnectionState.Broken)
                    {
                        _connection = CreateAndOpenConnection();
                    }
                    else if (_connection.State != ConnectionState.Open)
                    {
                        // 尝试重新打开连接
                        try
                        {
                            _connection.Open();
                            _logger?.LogDebug("数据库连接已重新打开");
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "重新打开连接失败，创建新连接");
                            _connection?.Dispose();
                            _connection = CreateAndOpenConnection();
                        }
                    }

                    return _connection;
                }
            }
        }

        /// <summary>
        /// 创建并打开数据库连接
        /// </summary>
        /// <returns>打开的数据库连接</returns>
        protected virtual IDbConnection CreateAndOpenConnection()
        {
            var attempts = 0;
            Exception? lastException = null;

            while (attempts < _maxRetryAttempts)
            {
                try
                {
                    var connection = new MySqlConnection(_connectionString);
                    connection.Open();
                    
                    // 验证连接是否真正可用
                    if (IsConnectionHealthy(connection))
                    {
                        _logger?.LogDebug("数据库连接已成功创建并打开");
                        return connection;
                    }
                    else
                    {
                        connection.Dispose();
                        throw new InvalidOperationException("连接创建成功但健康检查失败");
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attempts++;
                    
                    if (attempts < _maxRetryAttempts)
                    {
                        _logger?.LogWarning(ex, "创建数据库连接失败，尝试重试 ({Attempt}/{MaxAttempts})", attempts, _maxRetryAttempts);
                        Thread.Sleep(_retryDelay);
                    }
                }
            }

            _logger?.LogError(lastException, "创建数据库连接失败，已重试 {MaxAttempts} 次", _maxRetryAttempts);
            throw new Exception($"创建数据库连接失败，已重试 {_maxRetryAttempts} 次", lastException);
        }

        /// <summary>
        /// 检查连接是否健康
        /// </summary>
        /// <param name="connection">数据库连接</param>
        /// <returns>连接是否健康</returns>
        protected virtual bool IsConnectionHealthy(IDbConnection connection)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                command.CommandType = CommandType.Text;
                command.CommandTimeout = 5; // 使用默认5秒超时进行健康检查
                
                var result = command.ExecuteScalar();
                return result != null && result.ToString() == "1";
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "连接健康检查失败");
                return false;
            }
        }

        /// <summary>
        /// 开始一个新的事务
        /// </summary>
        /// <param name="isolationLevel">事务隔离级别</param>
        /// <exception cref="Exception">当创建事务失败时抛出</exception>
        public void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MySqlDbContext));
            }

            lock (_connectionLock)
            {
                if (_currentTransaction != null)
                {
                    throw new InvalidOperationException("已存在活动事务");
                }

                var connection = Connection; // 这会确保连接已打开
                _currentTransaction = connection.BeginTransaction(isolationLevel);
                _logger?.LogDebug("数据库事务已开始，隔离级别: {IsolationLevel}", isolationLevel);
            }
        }

        /// <summary>
        /// 获取当前事务
        /// </summary>
        public IDbTransaction? CurrentTransaction => _currentTransaction;

        /// <summary>
        /// 提交当前事务
        /// </summary>
        /// <exception cref="Exception">当提交事务失败时抛出</exception>
        public void CommitTransaction()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MySqlDbContext));
            }

            lock (_connectionLock)
            {
                if (_currentTransaction == null)
                {
                    throw new InvalidOperationException("没有活动的事务可以提交");
                }

                try
                {
                    _currentTransaction.Commit();
                    _logger?.LogDebug("数据库事务已提交");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "提交事务失败");
                    throw;
                }
                finally
                {
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }
            }
        }

        /// <summary>
        /// 回滚当前事务
        /// </summary>
        /// <exception cref="Exception">当回滚事务失败时抛出</exception>
        public void RollbackTransaction()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MySqlDbContext));
            }

            lock (_connectionLock)
            {
                if (_currentTransaction == null)
                {
                    throw new InvalidOperationException("没有活动的事务可以回滚");
                }

                try
                {
                    _currentTransaction.Rollback();
                    _logger?.LogDebug("数据库事务已回滚");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "回滚事务失败");
                    throw;
                }
                finally
                {
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }
            }
        }

        /// <summary>
        /// 检查连接是否可用
        /// </summary>
        /// <returns>连接是否可用</returns>
        public bool IsConnectionAvailable()
        {
            if (_disposed)
            {
                return false;
            }

            try
            {
                var connection = Connection;
                return connection.State == ConnectionState.Open && IsConnectionHealthy(connection);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的具体实现
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                lock (_connectionLock)
                {
                    // 先回滚未完成的事务
                    if (_currentTransaction != null)
                    {
                        try
                        {
                            _currentTransaction.Rollback();
                            _logger?.LogWarning("释放资源时回滚未完成的事务");
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "回滚未完成事务时发生错误");
                        }
                        finally
                        {
                            _currentTransaction.Dispose();
                            _currentTransaction = null;
                        }
                    }

                    // 关闭连接
                    if (_connection != null)
                    {
                        try
                        {
                            if (_connection.State == ConnectionState.Open)
                            {
                                _connection.Close();
                                _logger?.LogDebug("数据库连接已关闭");
                            }
                            _connection.Dispose();
                            _connection = null;
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "关闭数据库连接时发生错误");
                        }
                    }
                }
            }

            _disposed = true;
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~MySqlDbContext()
        {
            Dispose(false);
        }
    }
} 