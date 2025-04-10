using MonkeyScheduler.Core;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.WorkerService.Services;
using MonkeyScheduler.Storage;

var builder = WebApplication.CreateBuilder(args);

// 添加服务到容器
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加自定义服务
builder.Services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();
builder.Services.AddSingleton<ITaskExecutor, SampleTaskExecutor>();
builder.Services.AddHttpClient();

// 配置Worker节点
var workerUrl = builder.Configuration["Worker:Url"] ?? "http://localhost:5001";
var schedulerUrl = builder.Configuration["Scheduler:Url"] ?? "http://localhost:5000";

// 添加状态上报服务
builder.Services.AddSingleton(provider => 
    new StatusReporterService(
        provider.GetRequiredService<IHttpClientFactory>(),
        schedulerUrl,
        workerUrl
    )
);

// 添加心跳服务
builder.Services.AddHostedService(provider => 
    new NodeHeartbeatService(
        provider.GetRequiredService<IHttpClientFactory>(),
        schedulerUrl,
        workerUrl
    )
);

var app = builder.Build();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
