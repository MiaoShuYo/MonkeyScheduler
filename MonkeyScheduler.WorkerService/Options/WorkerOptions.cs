namespace MonkeyScheduler.WorkerService.Options
{
    public class WorkerOptions
    {
        public string WorkerUrl { get; set; } = string.Empty;
        public string SchedulerUrl { get; set; } = string.Empty;

        public static WorkerOptions Create(string workerUrl)
        {
            return new WorkerOptions
            {
                WorkerUrl = workerUrl
            };
        }
    }
} 