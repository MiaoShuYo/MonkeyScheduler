using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;

namespace MonkeyScheduler.WorkerService.Services
{
    /// <summary>
    /// 默认任务执行器配置
    /// </summary>
    public class DefaultTaskExecutorOptions
    {
        /// <summary>
        /// 调度器服务地址
        /// </summary>
        public string SchedulerUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// 默认任务执行器，负责执行任务并上报结果
    /// </summary>
    public class DefaultTaskExecutor : ITaskExecutor
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DefaultTaskExecutorOptions _options;

        /// <summary>
        /// 初始化默认任务执行器
        /// </summary>
        /// <param name="httpClientFactory">HTTP客户端工厂</param>
        /// <param name="options">配置选项</param>
        public DefaultTaskExecutor(IHttpClientFactory httpClientFactory, IOptions<DefaultTaskExecutorOptions> options)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }

        /// <summary>
        /// 执行任务
        /// </summary>
        /// <param name="task">要执行的任务</param>
        /// <param name="statusCallback">任务完成时的回调函数</param>
        /// <returns>异步任务</returns>
        public async Task ExecuteAsync(ScheduledTask task, Func<TaskExecutionResult, Task>? statusCallback = null)
        {
            var startTime = DateTime.UtcNow;
            var client = _httpClientFactory.CreateClient();

            try
            {
                // 发送任务执行请求
                var response = await client.PostAsJsonAsync($"{_options.SchedulerUrl}/api/task/execute", task);
                response.EnsureSuccessStatusCode();

                // 调用完成回调
                if (statusCallback != null)
                {
                    var result = new TaskExecutionResult
                    {
                        TaskId = task.Id,
                        Status = ExecutionStatus.Completed,
                        StartTime = startTime,
                        EndTime = DateTime.UtcNow,
                        Success = true
                    };
                    await statusCallback(result);
                }
            }
            catch (Exception ex)
            {
                // 如果执行失败，调用回调报告失败状态
                if (statusCallback != null)
                {
                    var result = new TaskExecutionResult
                    {
                        TaskId = task.Id,
                        Status = ExecutionStatus.Failed,
                        StartTime = startTime,
                        EndTime = DateTime.UtcNow,
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                    await statusCallback(result);
                }
                throw new Exception($"任务执行失败: {ex.Message}", ex);
            }
        }
    }
} 