using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MonkeyScheduler.WorkerService.Services;

/// <summary>
/// Worker节点健康检查服务
/// </summary>
public class WorkerHealthCheck : IHealthCheck
{
    /// <summary>
    /// 检查Worker节点的健康状态
    /// </summary>
    /// <param name="context">健康检查上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康检查结果</returns>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // 这里可以添加更多的健康检查逻辑
        // 例如：检查内存使用情况、CPU负载、网络连接等
        
        return Task.FromResult(HealthCheckResult.Healthy("Worker节点运行正常"));
    }
} 