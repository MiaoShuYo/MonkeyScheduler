using System;
using System.Collections.Generic;
using System.Linq;
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
    }
} 