using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MonkeyScheduler.SchedulerService.Services;

/// <summary>
/// 调度器健康检查服务，用于监控任务节点的健康状态
/// </summary>
public class SchedulerHealthCheck : IHealthCheck
{
    private readonly NodeRegistry _nodeRegistry;
    /// <summary>
    /// 节点心跳超时时间，默认为 30 秒
    /// </summary>
    private readonly TimeSpan _heartbeatTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 初始化调度器健康检查服务
    /// </summary>
    /// <param name="nodeRegistry">节点注册表服务</param>
    /// <exception cref="ArgumentNullException">当 nodeRegistry 为 null 时抛出</exception>
    public SchedulerHealthCheck(NodeRegistry nodeRegistry)
    {
        _nodeRegistry = nodeRegistry ?? throw new ArgumentNullException(nameof(nodeRegistry));
    }

    /// <summary>
    /// 执行健康检查，检查所有注册节点的健康状态
    /// </summary>
    /// <param name="context">健康检查上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康检查结果，包含节点状态信息</returns>
    /// <remarks>
    /// 健康检查结果分为三种状态：
    /// 1. 健康：所有节点心跳正常
    /// 2. 降级：部分节点心跳超时
    /// 3. 不健康：没有注册的节点
    /// </remarks>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var nodes = _nodeRegistry.GetAllNodes();
        if (!nodes.Any())
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("没有注册的任务节点"));
        }

        var unhealthyNodes = nodes
            .Where(n => DateTime.UtcNow - n.Value > _heartbeatTimeout)
            .ToList();

        if (unhealthyNodes.Any())
        {
            var unhealthyNodeUrl = string.Join(", ", unhealthyNodes.Select(n => n.Key));
            return Task.FromResult(HealthCheckResult.Degraded(
                $"以下节点心跳超时: {unhealthyNodeUrl}",
                data: new Dictionary<string, object>
                {
                    ["unhealthy_nodes"] = unhealthyNodeUrl,
                    ["total_nodes"] = nodes.Count,
                    ["healthy_nodes"] = nodes.Count - unhealthyNodes.Count
                }));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "所有节点心跳正常",
            data: new Dictionary<string, object>
            {
                ["total_nodes"] = nodes.Count,
                ["healthy_nodes"] = nodes.Count
            }));
    }
} 