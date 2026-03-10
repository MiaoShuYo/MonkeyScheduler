using System.ComponentModel.DataAnnotations;

namespace MonkeyScheduler.Core.Configuration
{
    /// <summary>
    /// 配置验证器
    /// </summary>
    public static class ConfigurationValidator
    {
        /// <summary>
        /// 验证重试配置
        /// </summary>
        /// <param name="config">重试配置</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateRetryConfiguration(RetryConfiguration config)
        {
            if (config.DefaultMaxRetryCount <= 0)
            {
                return new ValidationResult("DefaultMaxRetryCount 必须大于 0");
            }

            if (config.DefaultRetryIntervalSeconds <= 0)
            {
                return new ValidationResult("DefaultRetryIntervalSeconds 必须大于 0");
            }

            if (config.DefaultTimeoutSeconds <= 0)
            {
                return new ValidationResult("DefaultTimeoutSeconds 必须大于 0");
            }

            if (config.MaxRetryIntervalSeconds <= 0)
            {
                return new ValidationResult("MaxRetryIntervalSeconds 必须大于 0");
            }

            if (config.RetryCooldownSeconds <= 0)
            {
                return new ValidationResult("RetryCooldownSeconds 必须大于 0");
            }

            if (config.MaxRetryIntervalSeconds < config.DefaultRetryIntervalSeconds)
            {
                return new ValidationResult("MaxRetryIntervalSeconds 不能小于 DefaultRetryIntervalSeconds");
            }

            return ValidationResult.Success;
        }

        /// <summary>
        /// 验证数据库配置
        /// </summary>
        /// <param name="config">数据库配置</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateDatabaseConfiguration(DatabaseConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.MySQL))
            {
                return new ValidationResult("MySQL 连接字符串不能为空");
            }

            if (config.ConnectionTimeoutSeconds <= 0)
            {
                return new ValidationResult("ConnectionTimeoutSeconds 必须大于 0");
            }

            if (config.CommandTimeoutSeconds <= 0)
            {
                return new ValidationResult("CommandTimeoutSeconds 必须大于 0");
            }

            if (config.MaxPoolSize <= 0)
            {
                return new ValidationResult("MaxPoolSize 必须大于 0");
            }

            if (config.MinPoolSize < 0)
            {
                return new ValidationResult("MinPoolSize 不能小于 0");
            }

            if (config.MaxPoolSize < config.MinPoolSize)
            {
                return new ValidationResult("MaxPoolSize 不能小于 MinPoolSize");
            }

            return ValidationResult.Success;
        }

        /// <summary>
        /// 验证调度器配置
        /// </summary>
        /// <param name="config">调度器配置</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateSchedulerConfiguration(SchedulerConfiguration config)
        {
            if (config.CheckIntervalMilliseconds <= 0)
            {
                return new ValidationResult("CheckIntervalMilliseconds 必须大于 0");
            }

            if (config.MaxConcurrentTasks <= 0)
            {
                return new ValidationResult("MaxConcurrentTasks 必须大于 0");
            }

            if (config.TaskExecutionTimeoutSeconds <= 0)
            {
                return new ValidationResult("TaskExecutionTimeoutSeconds 必须大于 0");
            }

            if (config.StatisticsCollectionIntervalSeconds <= 0)
            {
                return new ValidationResult("StatisticsCollectionIntervalSeconds 必须大于 0");
            }

            if (config.HealthCheckIntervalSeconds <= 0)
            {
                return new ValidationResult("HealthCheckIntervalSeconds 必须大于 0");
            }

            return ValidationResult.Success;
        }

        /// <summary>
        /// 验证Worker配置
        /// </summary>
        /// <param name="config">Worker配置</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateWorkerConfiguration(WorkerConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.WorkerUrl))
            {
                return new ValidationResult("WorkerUrl 不能为空");
            }

            if (string.IsNullOrWhiteSpace(config.SchedulerUrl))
            {
                return new ValidationResult("SchedulerUrl 不能为空");
            }

            if (config.HeartbeatIntervalSeconds <= 0)
            {
                return new ValidationResult("HeartbeatIntervalSeconds 必须大于 0");
            }

            if (config.StatusReportIntervalSeconds <= 0)
            {
                return new ValidationResult("StatusReportIntervalSeconds 必须大于 0");
            }

            if (config.TaskExecutionTimeoutSeconds <= 0)
            {
                return new ValidationResult("TaskExecutionTimeoutSeconds 必须大于 0");
            }

            if (config.MaxConcurrentTasks <= 0)
            {
                return new ValidationResult("MaxConcurrentTasks 必须大于 0");
            }

            if (config.HealthCheckIntervalSeconds <= 0)
            {
                return new ValidationResult("HealthCheckIntervalSeconds 必须大于 0");
            }

            if (config.RegistrationRetryIntervalSeconds <= 0)
            {
                return new ValidationResult("RegistrationRetryIntervalSeconds 必须大于 0");
            }

            if (config.MaxRegistrationRetryCount <= 0)
            {
                return new ValidationResult("MaxRegistrationRetryCount 必须大于 0");
            }

            return ValidationResult.Success;
        }

        /// <summary>
        /// 验证负载均衡器配置
        /// </summary>
        /// <param name="config">负载均衡器配置</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateLoadBalancerConfiguration(LoadBalancerConfiguration config)
        {
            if (config.HealthCheckIntervalSeconds <= 0)
            {
                return new ValidationResult("HealthCheckIntervalSeconds 必须大于 0");
            }

            if (config.NodeTimeoutSeconds <= 0)
            {
                return new ValidationResult("NodeTimeoutSeconds 必须大于 0");
            }

            if (config.MaxFailureCount <= 0)
            {
                return new ValidationResult("MaxFailureCount 必须大于 0");
            }

            if (config.NodeRecoveryTimeSeconds <= 0)
            {
                return new ValidationResult("NodeRecoveryTimeSeconds 必须大于 0");
            }

            if (config.DefaultNodeWeight <= 0)
            {
                return new ValidationResult("DefaultNodeWeight 必须大于 0");
            }

            if (config.SessionAffinityTimeoutSeconds <= 0)
            {
                return new ValidationResult("SessionAffinityTimeoutSeconds 必须大于 0");
            }

            return ValidationResult.Success;
        }

        /// <summary>
        /// 验证日志配置
        /// </summary>
        /// <param name="config">日志配置</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateLoggingConfiguration(LoggingConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.LogLevel))
            {
                return new ValidationResult("LogLevel 不能为空");
            }

            if (config.MaxLogFileSizeMB <= 0)
            {
                return new ValidationResult("MaxLogFileSizeMB 必须大于 0");
            }

            if (config.RetainedLogFileCount <= 0)
            {
                return new ValidationResult("RetainedLogFileCount 必须大于 0");
            }

            return ValidationResult.Success;
        }

        /// <summary>
        /// 验证安全配置
        /// </summary>
        /// <param name="config">安全配置</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateSecurityConfiguration(SecurityConfiguration config)
        {
            if (config.EnableAuthentication && string.IsNullOrWhiteSpace(config.JwtSecret))
            {
                return new ValidationResult("启用身份验证时，JwtSecret 不能为空");
            }

            if (config.JwtExpirationHours <= 0)
            {
                return new ValidationResult("JwtExpirationHours 必须大于 0");
            }

            return ValidationResult.Success;
        }

        /// <summary>
        /// 验证完整的MonkeyScheduler配置
        /// </summary>
        /// <param name="config">MonkeyScheduler配置</param>
        /// <returns>验证结果列表</returns>
        public static List<ValidationResult> ValidateMonkeySchedulerConfiguration(MonkeySchedulerConfiguration config)
        {
            var results = new List<ValidationResult>();

            // 验证各个配置部分
            var retryResult = ValidateRetryConfiguration(config.Retry);
            if (retryResult != ValidationResult.Success)
            {
                results.Add(retryResult);
            }

            var databaseResult = ValidateDatabaseConfiguration(config.Database);
            if (databaseResult != ValidationResult.Success)
            {
                results.Add(databaseResult);
            }

            var schedulerResult = ValidateSchedulerConfiguration(config.Scheduler);
            if (schedulerResult != ValidationResult.Success)
            {
                results.Add(schedulerResult);
            }

            var workerResult = ValidateWorkerConfiguration(config.Worker);
            if (workerResult != ValidationResult.Success)
            {
                results.Add(workerResult);
            }

            var loadBalancerResult = ValidateLoadBalancerConfiguration(config.LoadBalancer);
            if (loadBalancerResult != ValidationResult.Success)
            {
                results.Add(loadBalancerResult);
            }

            var loggingResult = ValidateLoggingConfiguration(config.Logging);
            if (loggingResult != ValidationResult.Success)
            {
                results.Add(loggingResult);
            }

            var securityResult = ValidateSecurityConfiguration(config.Security);
            if (securityResult != ValidationResult.Success)
            {
                results.Add(securityResult);
            }

            return results;
        }
    }
} 