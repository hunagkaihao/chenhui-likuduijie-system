using _2026晨辉AI.Data;
using _2026晨辉AI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace _2026晨辉AI.Services
{
    public class WaitingTaskMonitorService : BackgroundService
    {
        private readonly ILogger<WaitingTaskMonitorService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public WaitingTaskMonitorService(ILogger<WaitingTaskMonitorService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WaitingTaskMonitorService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckWaitingTasksAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking waiting tasks");
                }

                // 每5秒检查一次
                await Task.Delay(5000, stoppingToken);
            }

            _logger.LogInformation("WaitingTaskMonitorService is stopping.");
        }

        private async Task CheckWaitingTasksAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var modbusService = scope.ServiceProvider.GetRequiredService<ModbusService>();
                var rcsApiManager = scope.ServiceProvider.GetRequiredService<RcsApiManager>();

                // 查找所有等待状态的任务
                var waitingTasks = await dbContext.AgvTasks.Where(t => t.Status == "Waiting").ToListAsync();
                //_logger.LogInformation("检测到等待继续任务数量："+waitingTasks.Count);
                foreach (var task in waitingTasks)
                {
                    try
                    {
                        // 读取状态：01 04 00 00 00 04 F1 C9
                        byte[] readStatusCommand = new byte[] { 0x01, 0x04, 0x00, 0x00, 0x00, 0x04, 0xF1, 0xC9 };
                        var response = await modbusService.SendRawDataAsync("192.168.98.15", 2005, readStatusCommand);

                        // 检查返回值，当第五位不是01时通知RCS继续任务
                        if (response.Success && response.ResponseData != null && response.ResponseData.Length >= 5)
                        {
                            byte fifthByte = response.ResponseData[4];
                            _logger.LogInformation($"Checking task {task.TaskCode}, fifth byte: {fifthByte:X2}");

                            if (fifthByte != 0x01)
                            {
                                // 发送继续任务指令给RCS
                                string continueReqCode = "CONT" + DateTime.Now.ToString("yyyyMMddHHmmss");
                                await rcsApiManager.ContinueTaskAsync(continueReqCode, task.TaskCode);
                                
                                // 更新任务状态
                                task.Status = "InProgress";
                                dbContext.Entry(task).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                await dbContext.SaveChangesAsync();
                                
                                _logger.LogInformation($"Sent continue command for task {task.TaskCode}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing waiting task {task.TaskCode}");
                    }
                }
            }
        }
    }
}
