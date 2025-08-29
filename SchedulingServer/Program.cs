using MonkeyScheduler.Core.Services;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging;
using MonkeyScheduler.Data.MySQL;
using MonkeyScheduler.SchedulerService;
using MonkeyScheduler.SchedulerService.Controllers;
using MonkeyScheduler.SchedulerService.Services;
using SchedulingServer;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();                    // 添加控制器支持
builder.Services.AddEndpointsApiExplorer();           // 添加API文档支持
builder.Services.AddSwaggerGen(c =>                   // 添加Swagger支持
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SchedulingServer API", Version = "v1" });
});
// 添加控制器并注册类库的控制器
builder.Services.AddControllers()
    .AddApplicationPart(typeof(WorkerApiController).Assembly);

// Add services to the container.
// 添加调度服务
builder.Services.AddSchedulerService(builder.Configuration);

// 注册自定义负载均衡器（使用增强的轮询策略）
builder.Services.AddSingleton<CustomLoadBalancer>();
builder.Services.AddSingleton<ILoadBalancer>(sp => 
    sp.GetRequiredService<CustomLoadBalancer>());

// 注册NodeRegistry服务
builder.Services.AddSingleton<NodeRegistry>();
builder.Services.AddSingleton<INodeRegistry>(sp => 
    sp.GetRequiredService<NodeRegistry>());
builder.Services.AddMySqlDataAccess();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();     // 启用Swagger
    app.UseSwaggerUI();
}

//app.UseAuthorization();       // 启用授权
app.MapControllers();         // 映射控制器路由
app.UseSchedulerService();

// 启用 MySQL 日志记录（在应用启动后添加，避免构建期间阻塞）
app.UseMySqlLogging();

// 根路径重定向到 swagger，便于直接验证服务可达
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();