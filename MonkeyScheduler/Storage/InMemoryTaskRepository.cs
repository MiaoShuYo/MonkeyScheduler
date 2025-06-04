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
    }
} 