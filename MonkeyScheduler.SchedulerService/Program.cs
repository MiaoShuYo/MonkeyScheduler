using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MonkeyScheduler.SchedulerService.Services;

var builder = WebApplication.CreateBuilder(args);

// 注册 Controller
builder.Services.AddControllers();
// 注册 Swagger 生成器
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MonkeyScheduler API", Version = "v1" });
});

// 接入调度服务（默认内存实现）。具体数据库由宿主应用选择性注入相应的包与扩展方法。
builder.Services.AddSchedulerService(builder.Configuration);

var app = builder.Build();

// 开发环境启用 Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MonkeyScheduler API V1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
// 启动调度器与健康检查端点
app.UseSchedulerService();
app.Run();
