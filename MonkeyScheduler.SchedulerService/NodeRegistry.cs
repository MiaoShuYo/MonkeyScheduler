using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MonkeyScheduler.SchedulerService
{
    public class NodeRegistry
    {
        private readonly ConcurrentDictionary<string, DateTime> _nodes = new();

        public void Register(string nodeUrl)
        {
            _nodes[nodeUrl] = DateTime.UtcNow;
        }

        public void Heartbeat(string nodeUrl)
        {
            _nodes[nodeUrl] = DateTime.UtcNow;
        }

        public List<string> GetAliveNodes(TimeSpan timeout)
        {
            var now = DateTime.UtcNow;
            return _nodes
                .Where(n => now - n.Value <= timeout)
                .Select(n => n.Key)
                .ToList();
        }

        public void RemoveNode(string nodeUrl)
        {
            _nodes.TryRemove(nodeUrl, out _);
        }
    }
} 