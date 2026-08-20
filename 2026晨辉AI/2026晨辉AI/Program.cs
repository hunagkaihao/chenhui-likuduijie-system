using _2026晨辉AI.Data;
using _2026晨辉AI.Services;
using _2026晨辉AI.Services.Tests;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 配置生产环境端口
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://*:5000");
}

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information() // 最小级别：Information
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning) // 过滤 EF Core 的详细日志
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning) // 过滤 ASP.NET Core 的详细日志
    .MinimumLevel.Override("System.Net.Http", Serilog.Events.LogEventLevel.Warning) // 过滤 HTTP 客户端日志
    .MinimumLevel.Override("Microsoft.AspNetCore.HttpLogging", Serilog.Events.LogEventLevel.Warning) // 过滤 HTTP 请求日志
    .WriteTo.Console()          // 输出到控制台
    .WriteTo.File(             // 输出到本地文件
        Path.Combine(AppContext.BaseDirectory, "Log", "log-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();
builder.Host.UseSerilog();

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
    
    // 手动补充 Cells 表新增列（EnsureCreated 不会在已存在的表上新增列）
    try
    {
        using (var connection = new MySqlConnector.MySqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var existingColumns = new HashSet<string>();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Cells'";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        existingColumns.Add(reader.GetString(0));
                    }
                }
            }
            foreach (var column in new[] {
                ("PalletCode", "varchar(200) NULL"),
                ("Picima", "varchar(200) NULL"),
                ("Tepi", "varchar(200) NULL")
            })
            {
                if (!existingColumns.Contains(column.Item1))
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = $"ALTER TABLE Cells ADD COLUMN {column.Item1} {column.Item2}";
                        await cmd.ExecuteNonQueryAsync();
                    }
                    Console.WriteLine($"Cells 表新增列 {column.Item1} 完成");
                }
            }
        }
        Console.WriteLine("Cells 表结构已就绪");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"补充 Cells 表列失败: {ex.Message}");
    }
    
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
