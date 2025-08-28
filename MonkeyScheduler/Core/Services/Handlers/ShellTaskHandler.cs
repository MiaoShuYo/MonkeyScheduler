using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.Core.Services.Handlers
{
    /// <summary>
    /// Shell任务处理器
    /// 支持执行Shell命令的任务类型
    /// </summary>
    public class ShellTaskHandler : ITaskHandler
    {
        private readonly ILogger<ShellTaskHandler> _logger;

        public string TaskType => "shell";
        public string Description => "Shell命令任务处理器，支持执行系统命令和脚本";

        public ShellTaskHandler(ILogger<ShellTaskHandler> logger)
        {
            _logger = logger;
        }

        public async Task<TaskExecutionResult> HandleAsync(ScheduledTask task, object? parameters = null)
        {
            var startTime = DateTime.UtcNow;
            var result = new TaskExecutionResult
            {
                TaskId = task.Id,
                StartTime = startTime,
                Status = ExecutionStatus.Running
            };

            try
            {
                var shellParams = ParseShellParameters(parameters);
                
                _logger.LogInformation("执行Shell任务: {TaskName}, 命令: {Command}", 
                    task.Name, shellParams.Command);

                var process = new Process();
                var startInfo = new ProcessStartInfo();

                // 根据操作系统设置不同的Shell
                if (OperatingSystem.IsWindows())
                {
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = $"/c {shellParams.Command}";
                }
                else
                {
                    startInfo.FileName = "/bin/bash";
                    startInfo.Arguments = $"-c \"{shellParams.Command}\"";
                }

                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.CreateNoWindow = true;
                startInfo.WorkingDirectory = shellParams.WorkingDirectory ?? Environment.CurrentDirectory;

                process.StartInfo = startInfo;

                var output = new StringBuilder();
                var error = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        output.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        error.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // 等待进程完成，支持超时
                var completed = await Task.Run(() => process.WaitForExit(shellParams.Timeout * 1000));

                if (!completed)
                {
                    process.Kill();
                    throw new TimeoutException($"命令执行超时，已强制终止进程");
                }

                result.Status = ExecutionStatus.Completed;
                result.EndTime = DateTime.UtcNow;
                result.Success = process.ExitCode == 0;
                result.Result = output.ToString();
                result.ErrorMessage = error.Length > 0 ? error.ToString() : null;

                if (process.ExitCode != 0)
                {
                    result.Status = ExecutionStatus.Failed;
                }

                _logger.LogInformation("Shell任务执行完成: {TaskName}, 退出码: {ExitCode}", 
                    task.Name, process.ExitCode);
            }
            catch (Exception ex)
            {
                result.Status = ExecutionStatus.Failed;
                result.EndTime = DateTime.UtcNow;
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.StackTrace = ex.StackTrace;

                _logger.LogError(ex, "Shell任务执行失败: {TaskName}", task.Name);
            }

            return result;
        }

        public async Task<bool> ValidateParametersAsync(object? parameters)
        {
            try
            {
                var shellParams = ParseShellParameters(parameters);
                return !string.IsNullOrEmpty(shellParams.Command);
            }
            catch
            {
                return false;
            }
        }

        public TaskHandlerConfiguration GetConfiguration()
        {
            return new TaskHandlerConfiguration
            {
                TaskType = TaskType,
                Description = Description,
                SupportsRetry = true,
                SupportsTimeout = true,
                DefaultTimeoutSeconds = 300,
                DefaultParameters = new Dictionary<string, object>
                {
                    ["timeout"] = 300,
                    ["workingDirectory"] = Environment.CurrentDirectory
                }
            };
        }

        private ShellTaskParameters ParseShellParameters(object? parameters)
        {
            if (parameters is ShellTaskParameters shellParams)
                return shellParams;

            if (parameters is string jsonString)
            {
                return JsonSerializer.Deserialize<ShellTaskParameters>(jsonString) 
                    ?? throw new ArgumentException("无效的Shell任务参数");
            }

            if (parameters is JsonElement jsonElement)
            {
                return JsonSerializer.Deserialize<ShellTaskParameters>(jsonElement.GetRawText()) 
                    ?? throw new ArgumentException("无效的Shell任务参数");
            }

            throw new ArgumentException("无效的任务参数类型");
        }
    }

    /// <summary>
    /// Shell任务参数
    /// </summary>
    public class ShellTaskParameters
    {
        public string Command { get; set; } = string.Empty;
        public string? WorkingDirectory { get; set; }
        public int Timeout { get; set; } = 300;
        public Dictionary<string, string>? EnvironmentVariables { get; set; }
    }
}
