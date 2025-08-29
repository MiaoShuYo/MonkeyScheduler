using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.Storage
{
    public class InMemoryTaskRepository : ITaskRepository
    {
        private readonly List<ScheduledTask> _tasks = new();

        public void AddTask(ScheduledTask task)
        {
            _tasks.Add(task);
        }

        public void UpdateTask(ScheduledTask task)
        {
            var index = _tasks.FindIndex(t => t.Id == task.Id);
            if (index != -1)
            {
                _tasks[index] = task;
            }
        }

        public void DeleteTask(Guid taskId)
        {
            _tasks.RemoveAll(t => t.Id == taskId);
        }

        public ScheduledTask? GetTask(Guid taskId)
        {
            return _tasks.FirstOrDefault(t => t.Id == taskId);
        }

        public IEnumerable<ScheduledTask> GetAllTasks()
        {
            return _tasks;
        }

        public Task AddTaskAsync(ScheduledTask task)
        {
            AddTask(task);
            return Task.CompletedTask;
        }

        public Task UpdateTaskAsync(ScheduledTask task)
        {
            UpdateTask(task);
            return Task.CompletedTask;
        }

        public Task DeleteTaskAsync(Guid taskId)
        {
            DeleteTask(taskId);
            return Task.CompletedTask;
        }

        public Task<ScheduledTask?> GetTaskAsync(Guid taskId)
        {
            return Task.FromResult(GetTask(taskId));
        }

        public Task<IEnumerable<ScheduledTask>> GetAllTasksAsync()
        {
            return Task.FromResult(GetAllTasks());
        }
    }
} 