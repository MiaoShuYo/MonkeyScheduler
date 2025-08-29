using Dapper;
using MonkeyScheduler.Data.MySQL.Data;
using MonkeyScheduler.Storage;
using MySQLScheduledTask = MonkeyScheduler.Core.Models.ScheduledTask;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;

namespace MonkeyScheduler.Data.MySQL.Repositories
{
    /// <summary>
    /// MySQL任务仓储实现类
    /// 负责将任务数据持久化到MySQL数据库中
    /// </summary>
    public class MySQLTaskRepository : ITaskRepository
    {
        private readonly MySqlDbContext _dbContext;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">MySQL数据库上下文</param>
        /// <exception cref="ArgumentNullException">当dbContext为null时抛出</exception>
        public MySQLTaskRepository(MySqlDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// 添加新任务
        /// </summary>
        /// <param name="task">要添加的任务对象</param>
        /// <exception cref="ArgumentNullException">当task为null时抛出</exception>
        public void AddTask(MySQLScheduledTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            // 将领域模型转换为数据库模型
            var dbTask = new Models.ScheduledTask
            {
                Id = task.Id.ToString(),  // 使用 Guid 的字符串表示
                Name = task.Name,
                Description = string.Empty,  // 默认空描述
                CronExpression = task.CronExpression,
                IsEnabled = task.Enabled,
                CreatedAt = DateTime.UtcNow,  // 使用UTC时间
                TaskType = "Default",  // 默认任务类型
                TaskParameters = string.Empty  // 默认空参数
            };

            // 插入新任务（Id 由外部传入的 Guid 字符串，不使用自增）
            const string sql = @"
                INSERT INTO ScheduledTasks (Id, Name, Description, CronExpression, IsEnabled, CreatedAt, TaskType, TaskParameters)
                VALUES (@Id, @Name, @Description, @CronExpression, @IsEnabled, @CreatedAt, @TaskType, @TaskParameters);";

            _dbContext.Connection.Execute(sql, dbTask);
            // 不修改传入的 task.Id
        }

        /// <summary>
        /// 异步添加新任务
        /// </summary>
        /// <param name="task">要添加的任务对象</param>
        /// <exception cref="ArgumentNullException">当task为null时抛出</exception>
        public async Task AddTaskAsync(MySQLScheduledTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            // 将领域模型转换为数据库模型
            var dbTask = new Models.ScheduledTask
            {
                Id = task.Id.ToString(),
                Name = task.Name,
                Description = string.Empty,
                CronExpression = task.CronExpression,
                IsEnabled = task.Enabled,
                CreatedAt = DateTime.UtcNow,
                TaskType = "Default",
                TaskParameters = string.Empty
            };

            // 异步插入新任务（Id 为 Guid 字符串）
            const string sql = @"
                INSERT INTO ScheduledTasks (Id, Name, Description, CronExpression, IsEnabled, CreatedAt, TaskType, TaskParameters)
                VALUES (@Id, @Name, @Description, @CronExpression, @IsEnabled, @CreatedAt, @TaskType, @TaskParameters);";

            await _dbContext.Connection.ExecuteAsync(sql, dbTask);
        }

        /// <summary>
        /// 更新现有任务
        /// </summary>
        /// <param name="task">要更新的任务对象</param>
        /// <exception cref="ArgumentNullException">当task为null时抛出</exception>
        public void UpdateTask(MySQLScheduledTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            // 将领域模型转换为数据库模型
            var dbTask = new Models.ScheduledTask
            {
                 Id = task.Id.ToString(),  // 使用 Guid 的字符串表示
                Name = task.Name,
                Description = string.Empty,
                CronExpression = task.CronExpression,
                IsEnabled = task.Enabled,
                LastModifiedAt = DateTime.UtcNow,  // 更新修改时间
                TaskType = "Default",
                TaskParameters = string.Empty
            };

            // 更新任务信息
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

            _dbContext.Connection.Execute(sql, dbTask);
        }

        /// <summary>
        /// 异步更新现有任务
        /// </summary>
        /// <param name="task">要更新的任务对象</param>
        /// <exception cref="ArgumentNullException">当task为null时抛出</exception>
        public async Task UpdateTaskAsync(MySQLScheduledTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            // 将领域模型转换为数据库模型
            var dbTask = new Models.ScheduledTask
            {
                Id = task.Id.ToString(),
                Name = task.Name,
                Description = string.Empty,
                CronExpression = task.CronExpression,
                IsEnabled = task.Enabled,
                LastModifiedAt = DateTime.UtcNow,
                TaskType = "Default",
                TaskParameters = string.Empty
            };

            // 异步更新任务信息
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

            await _dbContext.Connection.ExecuteAsync(sql, dbTask);
        }

        /// <summary>
        /// 删除指定任务
        /// </summary>
        /// <param name="taskId">要删除的任务ID</param>
        public void DeleteTask(Guid taskId)
        {
            const string sql = "DELETE FROM ScheduledTasks WHERE Id = @Id";
            _dbContext.Connection.Execute(sql, new { Id = taskId.ToString() });
        }

        /// <summary>
        /// 异步删除指定任务
        /// </summary>
        /// <param name="taskId">要删除的任务ID</param>
        public async Task DeleteTaskAsync(Guid taskId)
        {
            const string sql = "DELETE FROM ScheduledTasks WHERE Id = @Id";
            await _dbContext.Connection.ExecuteAsync(sql, new { Id = taskId.ToString() });
        }

        /// <summary>
        /// 获取单个任务
        /// </summary>
        /// <param name="taskId">要获取的任务ID</param>
        /// <returns>如果找到则返回任务对象，否则返回null</returns>
        public MySQLScheduledTask? GetTask(Guid taskId)
        {
            const string sql = "SELECT * FROM ScheduledTasks WHERE Id = @Id";
            var dbTask = _dbContext.Connection.QueryFirstOrDefault<Models.ScheduledTask>(
                sql,
                new { Id = taskId.ToString() });

            if (dbTask == null)
                return null;

            // 将数据库模型转换为领域模型
            return new MySQLScheduledTask
            {
                Id = Guid.Parse(dbTask.Id),  // 将字符串转换回 Guid
                Name = dbTask.Name,
                CronExpression = dbTask.CronExpression,
                Enabled = dbTask.IsEnabled
            };
        }

        /// <summary>
        /// 异步获取单个任务
        /// </summary>
        /// <param name="taskId">要获取的任务ID</param>
        /// <returns>如果找到则返回任务对象，否则返回null</returns>
        public async Task<MySQLScheduledTask?> GetTaskAsync(Guid taskId)
        {
            const string sql = "SELECT * FROM ScheduledTasks WHERE Id = @Id";
            var dbTask = await _dbContext.Connection.QueryFirstOrDefaultAsync<Models.ScheduledTask>(
                sql,
                new { Id = taskId.ToString() });

            if (dbTask == null)
                return null;

            // 将数据库模型转换为领域模型
            return new MySQLScheduledTask
            {
                Id = Guid.Parse(dbTask.Id),
                Name = dbTask.Name,
                CronExpression = dbTask.CronExpression,
                Enabled = dbTask.IsEnabled
            };
        }

        /// <summary>
        /// 获取所有任务
        /// </summary>
        /// <returns>按创建时间降序排列的任务集合</returns>
        public IEnumerable<MySQLScheduledTask> GetAllTasks()
        {
            const string sql = "SELECT * FROM ScheduledTasks ORDER BY CreatedAt DESC";
            var dbTasks = _dbContext.Connection.Query<Models.ScheduledTask>(sql).ToList();
            return dbTasks.Select(dbTask => new MySQLScheduledTask
            {
                Id = Guid.Parse(dbTask.Id),  // 将字符串转换回 Guid
                Name = dbTask.Name,
                CronExpression = dbTask.CronExpression,
                Enabled = dbTask.IsEnabled
            });
        }

        /// <summary>
        /// 异步获取所有任务
        /// </summary>
        /// <returns>按创建时间降序排列的任务集合</returns>
        public async Task<IEnumerable<MySQLScheduledTask>> GetAllTasksAsync()
        {
            const string sql = "SELECT * FROM ScheduledTasks ORDER BY CreatedAt DESC";
            var dbTasks = await _dbContext.Connection.QueryAsync<Models.ScheduledTask>(sql);
            return dbTasks.Select(dbTask => new MySQLScheduledTask
            {
                Id = Guid.Parse(dbTask.Id),
                Name = dbTask.Name,
                CronExpression = dbTask.CronExpression,
                Enabled = dbTask.IsEnabled
            });
        }
    }
}
