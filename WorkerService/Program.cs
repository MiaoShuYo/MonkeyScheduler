using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Data.MySQL;
using MonkeyScheduler.WorkerService.Services;
using WorkerService.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();                    // 添加控制器支持
builder.Services.AddEndpointsApiExplorer();           // 添加API文档支持
builder.Services.AddSwaggerGen();                     // 添加Swagger支持
// 添加Worker服务
builder.Services.AddWorkerService(
    builder.Configuration["MonkeyScheduler:WorkerService:Url"] ?? "http://localhost:5001"
);


// 注册自定义任务执行器
builder.Services.AddSingleton<ITaskExecutor, CustomTaskExecutor>();
builder.Services.AddMySqlDataAccess(); // 添加MySQL数据访问服务
var app = builder.Build();
app.UseAuthorization();       // 启用授权
app.MapControllers();         // 映射控制器路由
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();     // 启用Swagger
    app.UseSwaggerUI();   // 启用Swagger UI
}

app.UseHttpsRedirection();
// 添加健康检查端点
app.UseWorkerService();

app.Run();