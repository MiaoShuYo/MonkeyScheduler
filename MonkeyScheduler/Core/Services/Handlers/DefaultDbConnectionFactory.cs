using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace MonkeyScheduler.Core.Services.Handlers
{
    /// <summary>
    /// 默认数据库连接工厂实现
    /// 支持SQL Server连接
    /// </summary>
    public class DefaultDbConnectionFactory : IDbConnectionFactory
    {
        private readonly ILogger<DefaultDbConnectionFactory> _logger;

        public DefaultDbConnectionFactory(ILogger<DefaultDbConnectionFactory> logger)
        {
            _logger = logger;
        }

        public async Task<DbConnection> CreateConnectionAsync(string connectionString)
        {
            try
            {
                var connection = new SqlConnection(connectionString);
                _logger.LogDebug("创建数据库连接: {ConnectionString}", 
                    connectionString.Replace(GetPasswordFromConnectionString(connectionString), "***"));
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建数据库连接失败");
                throw;
            }
        }

        private string GetPasswordFromConnectionString(string connectionString)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                return builder.Password ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
