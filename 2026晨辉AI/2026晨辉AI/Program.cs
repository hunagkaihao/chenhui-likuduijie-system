using _2026晨辉AI.Data;
using _2026晨辉AI.Services;
using _2026晨辉AI.Services.Tests;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 配置生产环境端口
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://*:5000");
}

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<RcsApiManager>();
builder.Services.AddSingleton<ModbusService>(sp => 
    new ModbusService(sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<ILogger<ModbusService>>())
);
builder.Services.AddSingleton<Rs485Service>();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"=== ACTUAL CONNECTION STRING: {connectionString} ===");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 25)) // 请根据您的实际 MySQL 版本修改
    )
);
// Configure database connection
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseMySql(
//         builder.Configuration.GetConnectionString("DefaultConnection"),
//         ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
//     )
// );

// Register background services
builder.Services.AddHostedService<WaitingTaskMonitorService>();

var app = builder.Build();

// Ensure database and tables are created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
    
    // 手动创建 BindRecords 表（EnsureCreated 不会在已有数据库上新增表）
    try
    {
        dbContext.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS BindRecords (
                Id int NOT NULL AUTO_INCREMENT,
                Biaoshima varchar(200) NOT NULL,
                Tuopanma varchar(200) NOT NULL,
                Picima varchar(200) NULL,
                Tepi varchar(200) NULL,
                WorkMan varchar(100) NOT NULL,
                Company varchar(100) NOT NULL,
                Shuliang int NOT NULL,
                Status varchar(50) NOT NULL DEFAULT 'Success',
                Message varchar(500) NULL,
                CreatedAt datetime NOT NULL,
                PRIMARY KEY (Id)
            )");
        Console.WriteLine("BindRecords 表已就绪");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"创建 BindRecords 表失败: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
// 启用Swagger（生产环境也可访问）
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// 运行CRC校验功能测试
if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("\n=== 运行CRC校验功能测试 ===");
    var rs485Tests = new Rs485ServiceTests();
    rs485Tests.RunAllTests();
    Console.WriteLine("\n测试完成，继续启动应用...\n");
}

app.Run();
