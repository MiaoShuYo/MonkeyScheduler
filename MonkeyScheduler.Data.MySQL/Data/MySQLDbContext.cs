using System.Data;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;

namespace MonkeyScheduler.Data.MySQL.Data
{
    /// <summary>
    /// MySQL数据库上下文类
    /// 负责管理数据库连接和事务
    /// </summary>
    public class MySqlDbContext : IDisposable
    {
        private readonly string _connectionString;
        private readonly ILogger<MySqlDbContext>? _logger;
        private IDbConnection? _connection;
        private bool _disposed;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <param name="logger">日志记录器（可选）</param>
        /// <exception cref="ArgumentNullException">当connectionString为null或空时抛出</exception>
        public MySqlDbContext(string connectionString, ILogger<MySqlDbContext>? logger=null)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            _connectionString = connectionString;
            _logger = logger;
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
                if (_connection == null)
                {
                    try
                    {
                        _connection = new MySqlConnection(_connectionString);
                        _connection.Open();
                        _logger?.LogDebug("数据库连接已打开");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "创建数据库连接失败");
                        throw new Exception("创建数据库连接失败", ex);
                    }
                }
                else if (_connection.State != ConnectionState.Open)
                {
                    try
                    {
                        _connection.Open();
                        _logger?.LogDebug("数据库连接已重新打开");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "重新打开数据库连接失败");
                        throw new Exception("重新打开数据库连接失败", ex);
                    }
                }

                return _connection;
            }
        }

        /// <summary>
        /// 开始一个新的事务
        /// </summary>
        /// <exception cref="Exception">当创建事务失败时抛出</exception>
        public void BeginTransaction()
        {
            if (_connection == null)
            {
                _connection = new MySqlConnection(_connectionString);
            }

            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            _connection.BeginTransaction();
        }

        /// <summary>
        /// 提交当前事务
        /// </summary>
        /// <exception cref="Exception">当提交事务失败时抛出</exception>
        public void CommitTransaction()
        {
            if (_connection?.State == ConnectionState.Open)
            {
                var transaction = _connection.BeginTransaction();
                try
                {
                    transaction.Commit();
                    _logger?.LogDebug("事务已提交");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "提交事务失败");
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// 回滚当前事务
        /// </summary>
        /// <exception cref="Exception">当回滚事务失败时抛出</exception>
        public void RollbackTransaction()
        {
            if (_connection?.State == ConnectionState.Open)
            {
                var transaction = _connection.BeginTransaction();
                try
                {
                    transaction.Rollback();
                    _logger?.LogDebug("事务已回滚");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "回滚事务失败");
                    throw;
                }
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