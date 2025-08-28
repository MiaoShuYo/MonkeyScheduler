using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.Core.Services.Handlers
{
    /// <summary>
    /// SQL任务处理器
    /// 支持执行SQL脚本的任务类型
    /// </summary>
    public class SqlTaskHandler : ITaskHandler
    {
        private readonly ILogger<SqlTaskHandler> _logger;
        private readonly IDbConnectionFactory _connectionFactory;

        public string TaskType => "sql";
        public string Description => "SQL脚本任务处理器，支持执行数据库查询和更新操作";

        public SqlTaskHandler(ILogger<SqlTaskHandler> logger, IDbConnectionFactory connectionFactory)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
        }

        public async Task<TaskExecutionResult> HandleAsync(ScheduledTask task, object? parameters = null)
        {
            var startTime = DateTime.UtcNow;
            var result = new TaskExecutionResult
            {
                TaskId = task.Id,
                StartTime = startTime,
                Status = ExecutionStatus.Running
            };

            try
            {
                var sqlParams = ParseSqlParameters(parameters);
                
                _logger.LogInformation("执行SQL任务: {TaskName}, 数据库: {Database}", 
                    task.Name, sqlParams.Database);

                using var connection = await _connectionFactory.CreateConnectionAsync(sqlParams.ConnectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = sqlParams.SqlScript;
                command.CommandType = CommandType.Text;
                command.CommandTimeout = sqlParams.Timeout;

                // 添加参数
                if (sqlParams.Parameters != null)
                {
                    foreach (var param in sqlParams.Parameters)
                    {
                        var dbParam = command.CreateParameter();
                        dbParam.ParameterName = param.Key;
                        dbParam.Value = param.Value;
                        command.Parameters.Add(dbParam);
                    }
                }

                var dataTable = new DataTable();
                using var reader = await command.ExecuteReaderAsync();
                dataTable.Load(reader);

                result.Status = ExecutionStatus.Completed;
                result.EndTime = DateTime.UtcNow;
                result.Success = true;
                result.Result = $"执行成功，影响行数: {dataTable.Rows.Count}";

                _logger.LogInformation("SQL任务执行完成: {TaskName}, 影响行数: {RowCount}", 
                    task.Name, dataTable.Rows.Count);
            }
            catch (Exception ex)
            {
                result.Status = ExecutionStatus.Failed;
                result.EndTime = DateTime.UtcNow;
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.StackTrace = ex.StackTrace;

                _logger.LogError(ex, "SQL任务执行失败: {TaskName}", task.Name);
            }

            return result;
        }

        public async Task<bool> ValidateParametersAsync(object? parameters)
        {
            try
            {
                var sqlParams = ParseSqlParameters(parameters);
                return !string.IsNullOrEmpty(sqlParams.SqlScript) && !string.IsNullOrEmpty(sqlParams.ConnectionString);
            }
            catch
            {
                return false;
            }
        }

        public TaskHandlerConfiguration GetConfiguration()
        {
            return new TaskHandlerConfiguration
            {
                TaskType = TaskType,
                Description = Description,
                SupportsRetry = true,
                SupportsTimeout = true,
                DefaultTimeoutSeconds = 60,
                DefaultParameters = new Dictionary<string, object>
                {
                    ["timeout"] = 60,
                    ["parameters"] = new Dictionary<string, object>()
                }
            };
        }

        private SqlTaskParameters ParseSqlParameters(object? parameters)
        {
            if (parameters is SqlTaskParameters sqlParams)
                return sqlParams;

            if (parameters is string jsonString)
            {
                return JsonSerializer.Deserialize<SqlTaskParameters>(jsonString) 
                    ?? throw new ArgumentException("无效的SQL任务参数");
            }

            if (parameters is JsonElement jsonElement)
            {
                return JsonSerializer.Deserialize<SqlTaskParameters>(jsonElement.GetRawText()) 
                    ?? throw new ArgumentException("无效的SQL任务参数");
            }

            throw new ArgumentException("无效的任务参数类型");
        }
    }

    /// <summary>
    /// SQL任务参数
    /// </summary>
    public class SqlTaskParameters
    {
        public string SqlScript { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public Dictionary<string, object>? Parameters { get; set; }
        public int Timeout { get; set; } = 60;
    }

    /// <summary>
    /// 数据库连接工厂接口
    /// </summary>
    public interface IDbConnectionFactory
    {
        Task<DbConnection> CreateConnectionAsync(string connectionString);
    }
}
