using MonkeyScheduler.Core;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.SchedulerService;
using MonkeyScheduler.SchedulerService.Services;
using MonkeyScheduler.Storage;

// 创建Web应用构建器
var builder = WebApplication.CreateBuilder(args);

// 添加基础服务到容器
builder.Services.AddControllers();                    // 添加控制器支持
builder.Services.AddEndpointsApiExplorer();           // 添加API文档支持
builder.Services.AddSwaggerGen();                     // 添加Swagger支持

// 添加自定义服务到容器
builder.Services.AddSingleton<NodeRegistry>();        // 节点注册表
builder.Services.AddSingleton<LoadBalancer>();        // 负载均衡器
builder.Services.AddSingleton<TaskRetryManager>();    // 任务重试管理器
builder.Services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();  // 任务存储
builder.Services.AddSingleton<ITaskExecutor, TaskDispatcher>();  // 任务执行器
builder.Services.AddSingleton<Scheduler>();           // 调度器
builder.Services.AddHttpClient();                     // HTTP客户端工厂

// 构建Web应用
var app = builder.Build();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();     // 启用Swagger
    app.UseSwaggerUI();   // 启用Swagger UI
}

app.UseHttpsRedirection();    // 启用HTTPS重定向
app.UseAuthorization();       // 启用授权
app.MapControllers();         // 映射控制器路由

// 启动调度器
var scheduler = app.Services.GetRequiredService<Scheduler>();
scheduler.Start();

// 运行应用
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
