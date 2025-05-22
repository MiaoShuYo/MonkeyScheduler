using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.WorkerService.Services
{
    /// <summary>
    /// 状态上报服务接口
    /// </summary>
    public interface IStatusReporterService
    {
        /// <summary>
        /// 向调度器上报任务执行结果
        /// </summary>
        /// <param name="result">任务执行结果</param>
        /// <returns>异步任务</returns>
        Task ReportStatusAsync(TaskExecutionResult result);
    }
} 