using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.WorkerService.Services;
using MonkeyScheduler.WorkerService.Options;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// 注册 Controller
builder.Services.AddControllers();

// 注册 Swagger 生成器
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MonkeyScheduler Worker API", Version = "v1" });
});

// 注册配置选项
builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection("MonkeyScheduler:Worker"));

// 注册服务
builder.Services.AddHttpClient();
builder.Services.AddScoped<ITaskExecutor, DefaultTaskExecutor>();
builder.Services.AddScoped<IStatusReporterService, StatusReporterService>();
// 将心跳服务作为后台服务运行
builder.Services.AddHostedService<NodeHeartbeatService>();

// 注册健康检查
builder.Services.AddHealthChecks();

var app = builder.Build();

// 开发环境启用 Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MonkeyScheduler Worker API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
