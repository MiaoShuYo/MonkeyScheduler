using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Core.Configuration;
using MonkeyScheduler.Data.MySQL;
using MonkeyScheduler.WorkerService.Services;
using WorkerService.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();                    // 添加控制器支持
builder.Services.AddEndpointsApiExplorer();           // 添加API文档支持
builder.Services.AddSwaggerGen();                     // 添加Swagger支持

// 添加任务处理器服务
builder.Services.AddTaskHandlers();

// 配置任务处理器
builder.Services.ConfigureTaskHandlers(factory =>
{
    // 可以在这里注册自定义的任务处理器
    // factory.RegisterHandler<CustomTaskHandler>("custom");
});

// 添加Worker服务
builder.Services.AddWorkerService(
    builder.Configuration["MonkeyScheduler:WorkerService:Url"] ?? "http://localhost:5001"
);

// 注册插件化任务执行器（替换原有的CustomTaskExecutor）
builder.Services.AddSingleton<ITaskExecutor, PluginTaskExecutor>();
builder.Services.AddMySqlDataAccess(); // 添加MySQL数据访问服务
var app = builder.Build();
app.UseAuthorization();       // 启用授权
app.MapControllers();         // 映射控制器路由
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();     // 启用Swagger
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
// 添加健康检查端点
app.UseWorkerService();
app.UseMySqlLogging();

app.Run();