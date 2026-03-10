using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.Storage
{
    public interface ITaskRepository
    {
        void AddTask(ScheduledTask task);
        void UpdateTask(ScheduledTask task);
        void DeleteTask(Guid taskId);
        ScheduledTask? GetTask(Guid taskId);
        IEnumerable<ScheduledTask> GetAllTasks();
        
        Task AddTaskAsync(ScheduledTask task);
        Task UpdateTaskAsync(ScheduledTask task);
        Task DeleteTaskAsync(Guid taskId);
        Task<ScheduledTask?> GetTaskAsync(Guid taskId);
        Task<IEnumerable<ScheduledTask>> GetAllTasksAsync();
    }
} 