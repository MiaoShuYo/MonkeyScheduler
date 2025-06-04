using MonkeyScheduler.Core.Models;
using MonkeyScheduler.SchedulerService.Services;

namespace SchedulingServer;

/// <summary>
/// 自定义负载均衡器实现
/// </summary>
public class CustomLoadBalancer:ILoadBalancer
{
    private List<string> _nodes = new();
    private Dictionary<string, int> _nodeRequestCounts = new();
    private int _currentNodeIndex = 0;
    private readonly INodeRegistry _nodeRegistry;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

    public CustomLoadBalancer(INodeRegistry nodeRegistry)
    {
        _nodeRegistry = nodeRegistry ?? throw new ArgumentNullException(nameof(nodeRegistry));
    }
    
    /// <summary>
    /// 选择最适合执行任务的节点
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    public string SelectNode(ScheduledTask task)
    {
        _nodes = _nodeRegistry.GetAliveNodes(_timeout);
        foreach (var node in _nodes)
        {
            if (!_nodeRequestCounts.ContainsKey(node))
            {
                _nodeRequestCounts.Add(node,0);
            }
            
        }
        if (_nodes.Count == 0)
        {
            throw new InvalidOperationException("没有可用的节点");
        }

        var currentNode = _nodes[_currentNodeIndex];
        var requestCount = _nodeRequestCounts.GetValueOrDefault(currentNode, 0);

        if (requestCount >= 2)
        {
            // 重置当前节点的请求计数
            _nodeRequestCounts[currentNode] = 0;
            // 移动到下一个节点
            _currentNodeIndex = (_currentNodeIndex + 1) % _nodes.Count;
            currentNode = _nodes[_currentNodeIndex];
        }

        // 增加当前节点的请求计数
        _nodeRequestCounts[currentNode] = requestCount + 1;
        return currentNode;
    }

    /// <summary>
    /// 减少指定节点的负载计数
    /// </summary>
    /// <param name="nodeUrl"></param>
    public void DecreaseLoad(string nodeUrl)
    {
        if (_nodeRequestCounts.ContainsKey(nodeUrl))
        {
            _nodeRequestCounts[nodeUrl] = Math.Max(0, _nodeRequestCounts[nodeUrl] - 1);
        }
    }
    
    /// <summary>
    /// 从负载均衡器中移除节点
    /// </summary>
    /// <param name="nodeUrl"></param>
    public void RemoveNode(string nodeUrl)
    {
        _nodes.Remove(nodeUrl);
        _nodeRequestCounts.Remove(nodeUrl);
        
        // 如果移除的是当前节点，需要调整索引
        if (_currentNodeIndex >= _nodes.Count)
        {
            _currentNodeIndex = 0;
        }
    }

    /// <summary>
    /// 添加新节点到负载均衡器
    /// </summary>
    /// <param name="nodeUrl"></param>
    public void AddNode(string nodeUrl)
    {
        if (!_nodes.Contains(nodeUrl))
        {
            _nodes.Add(nodeUrl);
            _nodeRequestCounts[nodeUrl] = 0;
        }
    }
}