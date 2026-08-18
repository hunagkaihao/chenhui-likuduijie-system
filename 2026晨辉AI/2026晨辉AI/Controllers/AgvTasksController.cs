using _2026晨辉AI.Data;
using _2026晨辉AI.Models;
using _2026晨辉AI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace _2026晨辉AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgvTasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly RcsApiManager _rcsApiManager;
        private readonly ModbusService _modbusService;
        private readonly ILogger<AgvTasksController> _logger;
        
        public AgvTasksController(ApplicationDbContext context, RcsApiManager rcsApiManager, ModbusService modbusService, ILogger<AgvTasksController> logger)
        {
            _context = context;
            _rcsApiManager = rcsApiManager;
            _modbusService = modbusService;
            _logger = logger;
        }
        
        // GET: api/AgvTasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AgvTask>>> GetAgvTasks()
        {
            return await _context.AgvTasks.ToListAsync();
        }
        
        // GET: api/AgvTasks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AgvTask>> GetAgvTask(int id)
        {
            var agvTask = await _context.AgvTasks.FindAsync(id);
            
            if (agvTask == null)
            {
                return NotFound();
            }
            
            return agvTask;
        }
        
        // POST: api/AgvTasks
        [HttpPost]
        public async Task<ActionResult<object>> PostAgvTask(AgvTask agvTask)
        {
            _logger.LogInformation("=== 收到创建AGV任务请求 ===");
            _logger.LogInformation("请求参数: FromLocation={FromLocation}, ToLocation={ToLocation}, AgvCode={AgvCode}", 
                agvTask.FromLocation, agvTask.ToLocation, agvTask.AgvCode);

            // 生成任务编码
            agvTask.TaskCode = "AGV" + DateTime.Now.ToString("yyyyMMddHHmmss");
            agvTask.Status = "Pending";
            agvTask.CreatedAt = DateTime.Now;
            
            // 任务类型：默认Y1105，起点以2开头时改为Y1102
            agvTask.TaskType = "Y1105";
            if (!string.IsNullOrEmpty(agvTask.FromLocation) && agvTask.FromLocation.StartsWith("A"))
            {
                agvTask.TaskType = "Y001";
            }
            if (!string.IsNullOrEmpty(agvTask.FromLocation) && agvTask.FromLocation.StartsWith("CP"))
            {
                agvTask.TaskType = "Y02";
            }
            _logger.LogInformation("生成任务编码: TaskCode={TaskCode}, TaskType={TaskType}", agvTask.TaskCode, agvTask.TaskType);

            // 同一起点不能同时存在多个未完成的任务
            if (!string.IsNullOrEmpty(agvTask.FromLocation))
            {
                var activeStatuses = new[] { "Pending", "InProgress", "Waiting" };
                var existing = await _context.AgvTasks
                    .Where(t => t.FromLocation == agvTask.FromLocation
                                && activeStatuses.Contains(t.Status))
                    .FirstOrDefaultAsync();
                if (existing != null)
                {
                    _logger.LogWarning("起点 {FromLocation} 已存在未完成任务（{TaskCode}），拒绝下发", agvTask.FromLocation, existing.TaskCode);
                    return BadRequest(new
                    {
                        success = false,
                        message = $"起点 {agvTask.FromLocation} 已存在未完成任务（任务编码：{existing.TaskCode}），请等待完成后再下发",
                        agvTask = agvTask
                    });
                }
            }

            try
            {
                // 准备RCS任务参数
                string reqCode = "REQ" + DateTime.Now.ToString("yyyyMMddHHmmss");
                string taskTyp = agvTask.TaskType;
                string ctnrTyp = "1"; // 用户指定的容器类型
                string[] userCallCodePath = new string[] { agvTask.FromLocation, agvTask.ToLocation };
                string taskCode = agvTask.TaskCode;
                string boxCode = "BOX" + DateTime.Now.ToString("yyyyMMddHHmmss");
                string agvCode = agvTask.AgvCode;

                _logger.LogInformation("调用RCS下发任务: ReqCode={ReqCode}, TaskCode={TaskCode}, 路径={From}->{To}", 
                    reqCode, taskCode, agvTask.FromLocation, agvTask.ToLocation);

                // 先调用RCS API下发任务，成功后才入库
                var result = await _rcsApiManager.CreateAgvTaskAsync(reqCode, taskTyp, ctnrTyp, userCallCodePath, taskCode, boxCode, agvCode);

                _logger.LogInformation("RCS返回结果: code={Code}, message={Message}, taskCode={RcsTaskCode}", 
                    result.code, result.message, result.taskCode);

                if (result.code == "0")
                {
                    // RCS成功，入库并设置状态为InProgress
                    agvTask.Status = "InProgress";
                    agvTask.StartedAt = DateTime.Now;
                    _context.AgvTasks.Add(agvTask);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("任务创建成功并已入库: TaskCode={TaskCode}", taskCode);

                    return new { 
                        success = true,
                        message = result.message,
                        taskCode = result.taskCode,
                        agvTask = agvTask
                    };
                }
                else
                {
                    // RCS失败，不入库
                    _logger.LogWarning("RCS创建任务失败，不入库: TaskCode={TaskCode}, 原因={Message}", taskCode, result.message);
                    return new { 
                        success = false,
                        message = result.message,
                        taskCode = result.taskCode,
                        agvTask = agvTask
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建AGV任务异常: TaskCode={TaskCode}", agvTask.TaskCode);
                return BadRequest(new { success = false, message = ex.Message, agvTask = agvTask });
            }
        }
        
        // PUT: api/AgvTasks/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAgvTask(int id, AgvTask agvTask)
        {
            if (id != agvTask.Id)
            {
                return BadRequest();
            }
            
            // 更新状态和时间
            if (agvTask.Status == "InProgress" && agvTask.StartedAt == null)
            {
                agvTask.StartedAt = DateTime.Now;
            }
            else if (agvTask.Status == "Completed" && agvTask.CompletedAt == null)
            {
                agvTask.CompletedAt = DateTime.Now;
            }
            
            _context.Entry(agvTask).State = EntityState.Modified;
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AgvTaskExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            
            return NoContent();
        }
        
        // DELETE: api/AgvTasks/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAgvTask(int id)
        {
            var agvTask = await _context.AgvTasks.FindAsync(id);
            if (agvTask == null)
            {
                return NotFound();
            }
            
            _context.AgvTasks.Remove(agvTask);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
        
        // POST: api/AgvTasks/setAsExecuting
        [HttpPost("setAsExecuting/{id}")]
        public async Task<IActionResult> SetAsExecuting(int id)
        {
            var agvTask = await _context.AgvTasks.FindAsync(id);
            if (agvTask == null)
            {
                return NotFound();
            }
            
            agvTask.Status = "InProgress";
            agvTask.StartedAt = DateTime.Now;
            
            _context.Entry(agvTask).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
        
        // POST: api/AgvTasks/setAsCompleted
        [HttpPost("setAsCompleted/{id}")]
        public async Task<IActionResult> SetAsCompleted(int id)
        {
            var agvTask = await _context.AgvTasks.FindAsync(id);
            if (agvTask == null)
            {
                return NotFound();
            }
            
            agvTask.Status = "Completed";
            agvTask.CompletedAt = DateTime.Now;
            
            _context.Entry(agvTask).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
        
        // POST: api/AgvTasks/setAsCancel
        [HttpPost("setAsCancel/{id}")]
        public async Task<IActionResult> SetAsCancel(int id)
        {
            _logger.LogInformation("=== 收到取消任务请求: Id={Id} ===", id);

            var agvTask = await _context.AgvTasks.FindAsync(id);
            if (agvTask == null)
            {
                _logger.LogWarning("取消任务失败：未找到任务 Id={Id}", id);
                return NotFound();
            }

            // 通知RCS取消任务
            if (!string.IsNullOrEmpty(agvTask.TaskCode))
            {
                try
                {
                    var reqCode = Guid.NewGuid().ToString("N")[..20];
                    _logger.LogInformation("调用RCS取消任务: TaskCode={TaskCode}, ReqCode={ReqCode}", agvTask.TaskCode, reqCode);
                    var cancelResult = await _rcsApiManager.CancelTaskAsync(reqCode, agvTask.TaskCode);
                    if (cancelResult?.code != "0")
                    {
                        _logger.LogWarning("RCS取消任务失败: {Message}", cancelResult?.message);
                    }
                    else
                    {
                        _logger.LogInformation("RCS取消任务成功: TaskCode={TaskCode}", agvTask.TaskCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "调用RCS取消接口异常: TaskCode={TaskCode}", agvTask.TaskCode);
                }
            }
            
            agvTask.Status = "Canceled";
            
            _context.Entry(agvTask).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            _logger.LogInformation("任务已取消: Id={Id}, TaskCode={TaskCode}", id, agvTask.TaskCode);
            
            return NoContent();
        }

        // POST: api/AgvTasks/sendToRcs/{id}
        [HttpPost("sendToRcs/{id}")]
        public async Task<ActionResult<object>> SendToRcs(int id)
        {
            var agvTask = await _context.AgvTasks.FindAsync(id);
            if (agvTask == null)
            {
                return NotFound();
            }

            try
            {
                // 准备RCS任务参数
                string reqCode = "REQ" + DateTime.Now.ToString("yyyyMMddHHmmss");
                string taskTyp = agvTask.TaskType;
                string ctnrTyp = "1"; // 用户指定的容器类型
                string[] userCallCodePath = new string[] { agvTask.FromLocation, agvTask.ToLocation };
                string taskCode = agvTask.TaskCode;
                string boxCode = "BOX" + DateTime.Now.ToString("yyyyMMddHHmmss");
                string agvCode = agvTask.AgvCode;

                // 调用RCS API下发任务
                var result = await _rcsApiManager.CreateAgvTaskAsync(reqCode, taskTyp, ctnrTyp, userCallCodePath, taskCode, boxCode, agvCode);

                // 更新任务状态
                if (result.code == "0")
                {
                    agvTask.Status = "InProgress";
                    agvTask.StartedAt = DateTime.Now;
                    _context.Entry(agvTask).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    agvTask.Status = "Cancelled";
                    agvTask.CompletedAt = DateTime.Now;
                    _context.Entry(agvTask).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }

                return new { 
                    success = result.code == "0",
                    message = result.message,
                    taskCode = result.taskCode
                };
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        
        private bool AgvTaskExists(int id)
        {
            return _context.AgvTasks.Any(e => e.Id == id);
        }

        // POST: api/AgvTasks/ctuCallback
        [HttpPost("ctuCallback")]
        public async Task<ActionResult<ResultAgvTaskDto>> CtuCallbackAsync(AgvCallBackRequest input)
        {
            _logger.LogInformation("收到CTU回调: Method={Method}, TaskCode={TaskCode}, ReqCode={ReqCode}", 
                input.Method, input.TaskCode, input.ReqCode);
            try
            {
                if (input.Method == "taskStart")
                {
                    var agvTask = await _context.AgvTasks.FirstOrDefaultAsync(t => t.TaskCode == input.TaskCode);
                    if (agvTask != null)
                    {
                        agvTask.Status = "InProgress";
                        agvTask.StartedAt = DateTime.Now;
                        _context.Entry(agvTask).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("CTU回调-任务开始: TaskCode={TaskCode}", input.TaskCode);
                    }
                    return new ResultAgvTaskDto("0", "成功", input.ReqCode, "");
                }
                else if (input.Method == "cellOut")
                {
                    var agvTask = await _context.AgvTasks.FirstOrDefaultAsync(t => t.TaskCode == input.TaskCode);
                    if (agvTask != null)
                    {
                        // 这里可以添加库位解绑逻辑
                        _context.Entry(agvTask).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("CTU回调-出库: TaskCode={TaskCode}", input.TaskCode);
                    }
                    return new ResultAgvTaskDto("0", "成功", input.ReqCode, "");
                }
                else if (input.Method == "waitingContinue")
                {
                    // AGV任务等待时更新状态为等待，由后台服务持续检查并发送继续指令
                    var agvTask = await _context.AgvTasks.FirstOrDefaultAsync(t => t.TaskCode == input.TaskCode);
                    if (agvTask != null)
                    {
                        agvTask.Status = "Waiting";
                        _context.Entry(agvTask).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                    _logger.LogInformation("CTU回调-任务等待: TaskCode={TaskCode}", input.TaskCode);
                    return new ResultAgvTaskDto("1", "等待判断输送线信号", input.ReqCode, "任务已设置为等待状态，后台服务将持续监控并在条件满足时继续");
                }
                else if (input.Method == "taskFinish")
                {
                    var agvTask = await _context.AgvTasks.FirstOrDefaultAsync(t => t.TaskCode == input.TaskCode);
                    if (agvTask != null)
                    {
                        agvTask.Status = "Completed";
                        agvTask.CompletedAt = DateTime.Now;
                        _context.Entry(agvTask).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("CTU回调-任务完成: TaskCode={TaskCode}", input.TaskCode);
                        
                        // 任务完成时发送闭合继电器命令
                        try
                        {
                            // 发送闭合继电器命令：01 06 00 00 00 01 48 0A
                            byte[] closeCommand = new byte[] { 0x01, 0x06, 0x00, 0x00, 0x00, 0x01, 0x48, 0x0A };
                            await _modbusService.SendRawDataAsync("192.168.98.15", 2005, closeCommand);
                            
                            // 3秒后发送断开继电器命令
                            _ = Task.Run(async () =>
                            {
                                await Task.Delay(3000);
                                try
                                {
                                    // 发送断开继电器命令：01 05 00 00 00 00 CD CA
                                    byte[] openCommand = new byte[] { 0x01, 0x05, 0x00, 0x00, 0x00, 0x00, 0xCD, 0xCA };
                                    await _modbusService.SendRawDataAsync("192.168.98.15", 2005, openCommand);
                                }
                                catch (Exception ex)
                                {
                                    // 记录发送断开命令失败的日志
                                    _logger.LogError(ex, "发送断开继电器命令失败");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            // 记录发送命令失败的日志
                            _logger.LogError(ex, "发送继电器命令失败");
                        }
                    }
                    return new ResultAgvTaskDto("0", "成功", input.ReqCode, "");
                }
                else if (input.Method == "taskCancel")
                {
                    var agvTask = await _context.AgvTasks.FirstOrDefaultAsync(t => t.TaskCode == input.TaskCode);
                    if (agvTask != null)
                    {
                        agvTask.Status = "Canceled";
                        _context.Entry(agvTask).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("CTU回调-任务取消: TaskCode={TaskCode}", input.TaskCode);
                    }
                    return new ResultAgvTaskDto("0", "成功", input.ReqCode, "");
                }
                else
                {
                    return new ResultAgvTaskDto("0", "成功", input.ReqCode, "");
                }
            }
            catch (Exception ex)
            {
                return new ResultAgvTaskDto("1", ex.Message, input.ReqCode, "");
            }
        }

        // POST: api/AgvTasks/agvCallback
        [HttpPost("agvCallback")]
        public async Task<ActionResult<ResultAgvTaskDto>> AgvCallbackAsync(AgvCallBackRequest input)
        {
            _logger.LogInformation("收到AGV回调: Method={Method}, TaskCode={TaskCode}, ReqCode={ReqCode}", 
                input.Method, input.TaskCode, input.ReqCode);
            try
            {
                if (input.Method == "taskStart")
                {
                    var agvTask = await _context.AgvTasks.FirstOrDefaultAsync(t => t.TaskCode == input.TaskCode);
                    if (agvTask != null)
                    {
                        agvTask.Status = "InProgress";
                        agvTask.StartedAt = DateTime.Now;
                        _context.Entry(agvTask).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("AGV回调-任务开始: TaskCode={TaskCode}", input.TaskCode);
                    }
                    return new ResultAgvTaskDto("0", "成功", input.ReqCode, "");
                }
                else if (input.Method == "cellOut")
                {
                    // 这里可以添加库位解绑逻辑
                    return new ResultAgvTaskDto("0", "成功", input.ReqCode, "");
                }
                else if (input.Method == "waitingContinue")
                {
                    // AGV任务等待时检查第二个开关状态，然后发送继续任务指令
                    try
                    {
                        // 读取状态：01 04 00 00 00 04 F1 C9
                        byte[] readStatusCommand = new byte[] { 0x01, 0x04, 0x00, 0x00, 0x00, 0x04, 0xF1, 0xC9 };
                        var response = await _modbusService.SendRawDataAsync("192.168.98.32", 1883, readStatusCommand);
                        
                        // 检查返回值，当第五位是01时不通知RCS继续任务
                        if (response.Success && response.ResponseData != null && response.ResponseData.Length >= 5)
                        {
                            byte fifthByte = response.ResponseData[4];
                            _logger.LogInformation("读取到的第五位值: {FifthByte:X2}", fifthByte);
                            
                            if (fifthByte != 0x01)
                            {
                                // 发送继续任务指令给RCS
                                string continueReqCode = "CONT" + DateTime.Now.ToString("yyyyMMddHHmmss");
                                await _rcsApiManager.ContinueTaskAsync(continueReqCode, input.TaskCode);
                                _logger.LogInformation("发送继续任务指令给RCS: TaskCode={TaskCode}", input.TaskCode);
                                return new ResultAgvTaskDto("0", "成功", input.ReqCode, "");
                            }
                            else
                            {
                                _logger.LogInformation("第五位是01，不发送继续任务指令: TaskCode={TaskCode}", input.TaskCode);
                                return new ResultAgvTaskDto("1", "失败", input.ReqCode, "第五位是01，不发送继续任务指令");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // 记录读取状态失败的日志
                        _logger.LogError(ex, "处理等待继续任务失败");
                    }
                    return new ResultAgvTaskDto("1", "失败", input.ReqCode, "处理等待继续任务失败");
                }
                else if (input.Method == "taskFinish")
                {
                    var agvTask = await _context.AgvTasks.FirstOrDefaultAsync(t => t.TaskCode == input.TaskCode);
                    if (agvTask != null)
                    {
                        agvTask.Status = "Completed";
                        agvTask.CompletedAt = DateTime.Now;
                        _context.Entry(agvTask).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("AGV回调-任务完成: TaskCode={TaskCode}", input.TaskCode);
                        
                        // 任务完成时发送闭合继电器命令
                        try
                        {
                            // 发送闭合继电器命令：01 06 00 00 00 01 48 0A
                            byte[] closeCommand = new byte[] { 0x01, 0x06, 0x00, 0x00, 0x00, 0x01, 0x48, 0x0A };
                            await _modbusService.SendRawDataAsync("192.168.98.32", 1883, closeCommand);
                            
                            // 5秒后发送断开继电器命令
                            _ = Task.Run(async () =>
                            {
                                await Task.Delay(5000);
                                try
                                {
                                    // 发送断开继电器命令：01 05 00 00 00 00 CD CA
                                    byte[] openCommand = new byte[] { 0x01, 0x05, 0x00, 0x00, 0x00, 0x00, 0xCD, 0xCA };
                                    await _modbusService.SendRawDataAsync("192.168.98.32", 1883, openCommand);
                                }
                                catch (Exception ex)
                                {
                                    // 记录发送断开命令失败的日志
                                    _logger.LogError(ex, "发送断开继电器命令失败");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            // 记录发送命令失败的日志
                            _logger.LogError(ex, "发送继电器命令失败");
                        }
                    }
                    return new ResultAgvTaskDto("0", "成功", input.ReqCode, "");
                }
                else
                {
                    return new ResultAgvTaskDto("0", "成功", input.ReqCode, "");
                }
            }
            catch (Exception ex)
            {
                return new ResultAgvTaskDto("1", ex.Message, input.ReqCode, "");
            }
        }

        // POST: api/AgvTasks/warnCallback
        [HttpPost("warnCallback")]
        public async Task<ActionResult<ResultAgvTaskDto>> WarnCallbackAsync(WarnCallDto input)
        {
            try
            {
                // 这里可以添加告警处理逻辑
                return new ResultAgvTaskDto("0", "成功", input.ReqCode, "");
            }
            catch (Exception ex)
            {
                return new ResultAgvTaskDto("1", ex.Message, input.ReqCode, "");
            }
        }
    }

    // AGV回调请求类
    public class AgvCallBackRequest
    {
        public string ReqCode { get; set; }
        public string TaskCode { get; set; }
        public string Method { get; set; }
        public string CurrentPositionCode { get; set; }
        public string CallCode { get; set; }
        public string PodCode { get; set; }
        public string CtnrCode { get; set; }
    }

    // 告警回调请求类
    public class WarnCallDto
    {
        public string ReqCode { get; set; }
        public List<WarnData> Data { get; set; }
    }

    public class WarnData
    {
        public string TaskCode { get; set; }
        public string RobotCode { get; set; }
        public string WarnContent { get; set; }
    }

    // 响应类
    public class ResultAgvTaskDto
    {
        public string code { get; set; }
        public string message { get; set; }
        public string reqCode { get; set; }
        public string taskCode { get; set; }

        public ResultAgvTaskDto(string code, string message, string reqCode, string taskCode)
        {
            this.code = code;
            this.message = message;
            this.reqCode = reqCode;
            this.taskCode = taskCode;
        }
    }
}