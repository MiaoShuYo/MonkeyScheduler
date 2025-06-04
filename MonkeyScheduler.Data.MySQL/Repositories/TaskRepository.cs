using Dapper;
using MonkeyScheduler.Data.MySQL.Data;
using MonkeyScheduler.Data.MySQL.Models;

namespace MonkeyScheduler.Data.MySQL.Repositories
{
    /// <summary>
    /// 任务仓储类
    /// 负责任务信息的持久化和查询操作
    /// </summary>
    public class TaskRepository
    {
        private readonly MySqlDbContext _dbContext;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">MySQL数据库上下文</param>
        public TaskRepository(MySqlDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// 异步添加新任务
        /// </summary>
        /// <param name="task">要添加的任务对象</param>
        /// <returns>新添加任务的自增ID</returns>
        public async Task<int> AddTaskAsync(ScheduledTask task)
        {
            const string sql = @"
                INSERT INTO ScheduledTasks (Name, Description, CronExpression, IsEnabled, CreatedAt, TaskType, TaskParameters)
                VALUES (@Name, @Description, @CronExpression, @IsEnabled, @CreatedAt, @TaskType, @TaskParameters);
                SELECT LAST_INSERT_ID();";

            return await _dbContext.Connection.ExecuteScalarAsync<int>(sql, task);
        }

        /// <summary>
        /// 异步更新任务信息
        /// </summary>
        /// <param name="task">要更新的任务对象</param>
        public async Task UpdateTaskAsync(ScheduledTask task)
        {
            const string sql = @"
                UPDATE ScheduledTasks 
                SET Name = @Name,
                    Description = @Description,
                    CronExpression = @CronExpression,
                    IsEnabled = @IsEnabled,
                    LastModifiedAt = @LastModifiedAt,
                    TaskType = @TaskType,
                    TaskParameters = @TaskParameters
                WHERE Id = @Id";

            await _dbContext.Connection.ExecuteAsync(sql, task);
        }

        /// <summary>
        /// 异步根据ID获取任务
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <returns>任务对象，如果不存在则返回null</returns>
        public async Task<ScheduledTask> GetTaskByIdAsync(int id)
        {
            const string sql = "SELECT * FROM ScheduledTasks WHERE Id = @Id";
            return await _dbContext.Connection.QueryFirstOrDefaultAsync<ScheduledTask>(sql, new { Id = id });
        }

        /// <summary>
        /// 异步获取所有任务
        /// </summary>
        /// <returns>所有任务集合，按创建时间降序排列</returns>
        public async Task<IEnumerable<ScheduledTask>> GetAllTasksAsync()
        {
            const string sql = "SELECT * FROM ScheduledTasks ORDER BY CreatedAt DESC";
            return await _dbContext.Connection.QueryAsync<ScheduledTask>(sql);
        }

        /// <summary>
        /// 异步获取所有启用的任务
        /// </summary>
        /// <returns>所有启用状态的任务集合，按创建时间降序排列</returns>
        public async Task<IEnumerable<ScheduledTask>> GetEnabledTasksAsync()
        {
            const string sql = "SELECT * FROM ScheduledTasks WHERE IsEnabled = true ORDER BY CreatedAt DESC";
            return await _dbContext.Connection.QueryAsync<ScheduledTask>(sql);
        }
    }
} 