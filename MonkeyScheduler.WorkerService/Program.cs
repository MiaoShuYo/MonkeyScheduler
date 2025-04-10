using MonkeyScheduler.Core;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.WorkerService.Services;
using MonkeyScheduler.Storage;

/// <summary>
/// Worker Service的入口程序
/// 负责配置和启动Worker节点服务
/// </summary>
var builder = WebApplication.CreateBuilder(args);

// 添加基础服务到依赖注入容器
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 注册核心服务
// 使用内存存储作为任务仓库
builder.Services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();
// 注册默认任务执行器
builder.Services.AddSingleton<ITaskExecutor, DefaultTaskExecutor>();
// 注册HTTP客户端工厂
builder.Services.AddHttpClient();

// 从配置文件读取服务地址配置
// 如果未配置则使用默认值
var workerUrl = builder.Configuration["Worker:Url"] ?? "http://localhost:5001";
var schedulerUrl = builder.Configuration["Scheduler:Url"] ?? "http://localhost:5000";

// 注册状态上报服务
// 用于向调度器报告Worker节点的状态
builder.Services.AddSingleton(provider => 
    new StatusReporterService(
        provider.GetRequiredService<IHttpClientFactory>(),
        schedulerUrl,
        workerUrl
    )
);

// 注册心跳服务
// 定期向调度器发送心跳包以保持节点活跃状态
builder.Services.AddHostedService(provider => 
    new NodeHeartbeatService(
        provider.GetRequiredService<IHttpClientFactory>(),
        schedulerUrl,
        workerUrl
    )
);

var app = builder.Build();

// 配置HTTP请求处理管道
if (app.Environment.IsDevelopment())
{
    // 在开发环境中启用Swagger
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 启用HTTPS重定向
app.UseHttpsRedirection();
// 启用授权
app.UseAuthorization();
// 映射控制器路由
app.MapControllers();

// 启动应用程序
app.Run();

/// <summary>
/// 示例天气预报记录类型
/// 用于演示API功能
/// </summary>
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    /// <summary>
    /// 将摄氏度转换为华氏度
    /// </summary>
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
