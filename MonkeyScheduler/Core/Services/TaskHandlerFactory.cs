using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MonkeyScheduler.Core.Services
{
    /// <summary>
    /// 任务处理器工厂
    /// 负责管理和创建不同类型的任务处理器
    /// </summary>
    public class TaskHandlerFactory : ITaskHandlerFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TaskHandlerFactory> _logger;
        private readonly Dictionary<string, Type> _handlerTypes;
        private readonly Dictionary<string, ITaskHandler> _handlerInstances;

        public TaskHandlerFactory(IServiceProvider serviceProvider, ILogger<TaskHandlerFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _handlerTypes = new Dictionary<string, Type>();
            _handlerInstances = new Dictionary<string, ITaskHandler>();
        }

        /// <summary>
        /// 注册任务处理器
        /// </summary>
        /// <typeparam name="THandler">处理器类型</typeparam>
        /// <param name="taskType">任务类型</param>
        public void RegisterHandler<THandler>(string taskType) where THandler : class, ITaskHandler
        {
            _handlerTypes[taskType] = typeof(THandler);
            _logger.LogInformation("注册任务处理器: {TaskType} -> {HandlerType}", taskType, typeof(THandler).Name);
        }

        /// <summary>
        /// 获取任务处理器
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <returns>任务处理器</returns>
        public ITaskHandler GetHandler(string taskType)
        {
            if (_handlerInstances.TryGetValue(taskType, out var cachedInstance))
            {
                return cachedInstance;
            }

            if (!_handlerTypes.TryGetValue(taskType, out var handlerType))
            {
                throw new InvalidOperationException($"未找到任务类型 '{taskType}' 的处理器");
            }

            try
            {
                var handler = (ITaskHandler)ActivatorUtilities.CreateInstance(_serviceProvider, handlerType);
                _handlerInstances[taskType] = handler;
                return handler;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建任务处理器失败: {TaskType}", taskType);
                throw;
            }
        }

        /// <summary>
        /// 获取所有支持的任务类型
        /// </summary>
        /// <returns>任务类型列表</returns>
        public IEnumerable<string> GetSupportedTaskTypes()
        {
            return _handlerTypes.Keys;
        }

        /// <summary>
        /// 验证任务类型是否支持
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <returns>是否支持</returns>
        public bool IsTaskTypeSupported(string taskType)
        {
            return _handlerTypes.ContainsKey(taskType);
        }
    }

    /// <summary>
    /// 任务处理器工厂接口
    /// </summary>
    public interface ITaskHandlerFactory
    {
        void RegisterHandler<THandler>(string taskType) where THandler : class, ITaskHandler;
        ITaskHandler GetHandler(string taskType);
        IEnumerable<string> GetSupportedTaskTypes();
        bool IsTaskTypeSupported(string taskType);
    }
}
