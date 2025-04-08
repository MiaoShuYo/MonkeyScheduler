using System.Threading.Tasks;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.Core.Services
{
    public interface ITaskExecutor
    {
        Task ExecuteAsync(ScheduledTask task);
    }
} 