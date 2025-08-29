using MonkeyScheduler.Core.Models;
using MonkeyScheduler.SchedulerService.Services;
using MonkeyScheduler.SchedulerService.Services.Strategies;

namespace SchedulingServer;

/// <summary>
/// 自定义负载均衡器实现
/// 使用增强的轮询策略，支持请求计数限制
/// </summary>
public class CustomLoadBalancer : LoadBalancer
{
    private readonly Dictionary<string, int> _nodeRequestCounts = new();
    private int _currentNodeIndex = 0;
    private readonly object _lockObject = new object();

    public CustomLoadBalancer(INodeRegistry nodeRegistry)
        : base(nodeRegistry, new RoundRobinStrategy())
    {
    }
    
    /// <summary>
    /// 选择最适合执行任务的节点
    /// 使用增强的轮询策略，限制每个节点的最大请求数
    /// </summary>
    /// <param name="task">要执行的任务</param>
    /// <returns>选中的节点URL</returns>
    public override string SelectNode(ScheduledTask task)
    {
        lock (_lockObject)
        {
            var nodes = GetAvailableNodes();
            if (!nodes.Any())
        {
            throw new InvalidOperationException("没有可用的节点");
        }

            // 查找未达到请求限制的节点
            var eligibleNodes = nodes.Where(node => 
                !_nodeRequestCounts.ContainsKey(node) || 
                _nodeRequestCounts[node] < 2).ToList();

            if (!eligibleNodes.Any())
        {
                // 如果所有节点都达到限制，重置计数
                foreach (var node in nodes)
                {
                    _nodeRequestCounts[node] = 0;
                }
                eligibleNodes = nodes;
            }

            // 使用轮询选择节点
            var selectedNode = eligibleNodes[_currentNodeIndex % eligibleNodes.Count];
            _currentNodeIndex = (_currentNodeIndex + 1) % eligibleNodes.Count;

            // 增加选中节点的请求计数
            _nodeRequestCounts[selectedNode] = _nodeRequestCounts.GetValueOrDefault(selectedNode, 0) + 1;

            return selectedNode;
        }
    }

    /// <summary>
    /// 减少指定节点的负载计数
    /// </summary>
    /// <param name="nodeUrl">节点URL</param>
    public override void DecreaseLoad(string nodeUrl)
    {
        lock (_lockObject)
        {
            base.DecreaseLoad(nodeUrl);
            
        if (_nodeRequestCounts.ContainsKey(nodeUrl))
        {
            _nodeRequestCounts[nodeUrl] = Math.Max(0, _nodeRequestCounts[nodeUrl] - 1);
            }
        }
    }
    
    /// <summary>
    /// 从负载均衡器中移除节点
    /// </summary>
    /// <param name="nodeUrl">节点URL</param>
    public override void RemoveNode(string nodeUrl)
    {
        lock (_lockObject)
        {
            base.RemoveNode(nodeUrl);
        _nodeRequestCounts.Remove(nodeUrl);
        
        // 如果移除的是当前节点，需要调整索引
            var nodes = GetAvailableNodes();
            if (_currentNodeIndex >= nodes.Count)
        {
            _currentNodeIndex = 0;
            }
        }
    }

    /// <summary>
    /// 添加新节点到负载均衡器
    /// </summary>
    /// <param name="nodeUrl">节点URL</param>
    public override void AddNode(string nodeUrl)
    {
        lock (_lockObject)
        {
            base.AddNode(nodeUrl);
            if (!_nodeRequestCounts.ContainsKey(nodeUrl))
            {
            _nodeRequestCounts[nodeUrl] = 0;
            }
        }
    }

    /// <summary>
    /// 获取可用节点列表
    /// </summary>
    /// <returns>可用节点列表</returns>
    private List<string> GetAvailableNodes()
    {
        var allNodes = GetNodeLoads();
        return allNodes.Keys.ToList();
    }

    /// <summary>
    /// 获取节点请求计数信息
    /// </summary>
    /// <returns>节点请求计数字典</returns>
    public Dictionary<string, int> GetNodeRequestCounts()
    {
        lock (_lockObject)
        {
            return new Dictionary<string, int>(_nodeRequestCounts);
        }
    }

    /// <summary>
    /// 重置所有节点的请求计数
    /// </summary>
    public void ResetRequestCounts()
    {
        lock (_lockObject)
        {
            _nodeRequestCounts.Clear();
        }
    }
}