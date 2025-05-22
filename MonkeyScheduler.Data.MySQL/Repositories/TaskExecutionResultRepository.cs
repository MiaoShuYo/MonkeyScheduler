using Dapper;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Data.MySQL.Data;
using MonkeyScheduler.Data.MySQL.Models;

namespace MonkeyScheduler.Data.MySQL.Repositories
{
    /// <summary>
    /// 任务执行结果仓储类
    /// 负责任务执行记录的持久化和查询操作
    /// </summary>
    public class TaskExecutionResultRepository : ITaskExecutionResult
    {
        private readonly MySqlDbContext _dbContext;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">MySQL数据库上下文</param>
        public TaskExecutionResultRepository(MySqlDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// 异步添加任务执行结果
        /// </summary>
        /// <param name="result">要添加的任务执行结果</param>
        /// <returns>新添加记录的自增ID</returns>
        public async Task<int> AddExecutionResultAsync(TaskExecutionResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result), "TaskExecutionResult cannot be null.");
            }

            const string sql = @"INSERT INTO TaskExecutionResults (
                    TaskId, 
                    StartTime, 
                    EndTime, 
                    Status, 
                    Result, 
                    ErrorMessage, 
                    StackTrace, 
                    WorkerNodeUrl, 
                    Success
                )
                VALUES (
                    @TaskId, 
                    @StartTime, 
                    @EndTime, 
                    @Status, 
                    @Result, 
                    @ErrorMessage, 
                    @StackTrace, 
                    @WorkerNodeUrl, 
                    @Success
                );
                SELECT LAST_INSERT_ID();";

            try
            {
                return await _dbContext.Connection.ExecuteScalarAsync<int>(sql, new
                {
                    TaskId = result.TaskId,
                    StartTime = result.StartTime,
                    EndTime = result.EndTime,
                    Status = result.Status,
                    Result = result.Result,
                    ErrorMessage = result.ErrorMessage,
                    StackTrace = result.StackTrace,
                    WorkerNodeUrl = result.WorkerNodeUrl ?? string.Empty,
                    Success = result.Success
                });
            }
            catch (Exception ex)
            {
                // 记录详细的错误信息
                Console.WriteLine($"Error inserting task execution result: {ex.Message}");
                Console.WriteLine($"SQL: {sql}");
                Console.WriteLine($"Parameters: TaskId={result.TaskId}, StartTime={result.StartTime}, Status={result.Status}");
                throw;
            }
        }

        /// <summary>
        /// 异步更新任务执行结果
        /// </summary>
        /// <param name="result">要更新的任务执行结果</param>
        public async Task UpdateExecutionResultAsync(TaskExecutionResult result)
        {
            const string sql = @"
                UPDATE TaskExecutionResults 
                SET EndTime = @EndTime,
                    Status = @Status,
                    Result = @Result,
                    ErrorMessage = @ErrorMessage,
                    StackTrace = @StackTrace
                WHERE Id = @Id";

            await _dbContext.Connection.ExecuteAsync(sql, result);
        }

        /// <summary>
        /// 异步获取指定任务在指定时间范围内的执行结果
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>符合条件的执行结果集合，按开始时间降序排列</returns>
        public async Task<IEnumerable<TaskExecutionResult>> GetTaskExecutionResultsAsync(int taskId, DateTime startTime,
            DateTime endTime)
        {
            const string sql = @"
                SELECT * FROM TaskExecutionResults 
                WHERE TaskId = @TaskId 
                AND StartTime BETWEEN @StartTime AND @EndTime
                ORDER BY StartTime DESC";

            return await _dbContext.Connection.QueryAsync<TaskExecutionResult>(
                sql,
                new { TaskId = taskId, StartTime = startTime, EndTime = endTime });
        }

        /// <summary>
        /// 异步获取指定任务的最后一次执行结果
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>最后一次执行结果，如果不存在则返回null</returns>
        public async Task<TaskExecutionResult> GetLastExecutionResultAsync(int taskId)
        {
            const string sql = @"
                SELECT * FROM TaskExecutionResults 
                WHERE TaskId = @TaskId 
                ORDER BY StartTime DESC 
                LIMIT 1";

            return await _dbContext.Connection.QueryFirstOrDefaultAsync<TaskExecutionResult>(
                sql,
                new { TaskId = taskId });
        }
    }
}
