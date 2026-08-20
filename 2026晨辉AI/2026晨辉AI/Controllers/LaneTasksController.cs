using _2026晨辉AI.Data;
using _2026晨辉AI.Models;
using _2026晨辉AI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2026晨辉AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LaneTasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly RcsApiManager _rcsApiManager;
        private readonly ILogger<LaneTasksController> _logger;

        public LaneTasksController(ApplicationDbContext context, RcsApiManager rcsApiManager, ILogger<LaneTasksController> logger)
        {
            _context = context;
            _rcsApiManager = rcsApiManager;
            _logger = logger;
        }

        // POST: api/LaneTasks/bind
        [HttpPost("bind")]
        public async Task<ActionResult<object>> Bind([FromBody] LaneBindDto input)
        {
            if (string.IsNullOrWhiteSpace(input.FromLocation) ||
                string.IsNullOrWhiteSpace(input.MaterialId) ||
                string.IsNullOrWhiteSpace(input.PalletId))
            {
                return BadRequest(new { success = false, message = "物料条码、托盘条码、起始位置为必填项" });
            }

            var cell = await _context.Cells.FirstOrDefaultAsync(c => c.Location == input.FromLocation);
            if (cell == null)
            {
                cell = new Cell
                {
                    CellCode = input.FromLocation,
                    CellType = "巷道",
                    Location = input.FromLocation,
                    Status = "Occupied",
                    CreatedAt = DateTime.Now
                };
                _context.Cells.Add(cell);
            }

            cell.BindMaterialCode = input.MaterialId;
            cell.PalletCode = input.PalletId;
            cell.Picima = input.Picima;
            cell.Tepi = input.Tepi;
            cell.Status = "Occupied";
            cell.LastUpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("巷道任务绑定成功: 起始位置={FromLocation}, 物料={MaterialId}, 托盘={PalletId}",
                input.FromLocation, input.MaterialId, input.PalletId);
            return new { success = true, message = "绑定成功" };
        }

        // POST: api/LaneTasks/dispatch
        [HttpPost("dispatch")]
        public async Task<ActionResult<object>> Dispatch([FromBody] LaneDispatchDto input)
        {
            if (string.IsNullOrWhiteSpace(input.FromLocation))
            {
                return BadRequest(new { success = false, message = "起始位置为必填项" });
            }

            var cell = await _context.Cells.FirstOrDefaultAsync(c => c.Location == input.FromLocation);
            if (cell == null)
            {
                return new { success = false, message = "起始位置不存在，请先绑定物料" };
            }

            if (cell.Status != "Occupied")
            {
                return new { success = false, message = $"起始位置 {input.FromLocation} 无货物，请先绑定物料" };
            }

            // 查找巷道外侧：同 CellCode 中 Location 最小者
            var laneCells = await _context.Cells
                .Where(c => c.CellCode == cell.CellCode)
                .OrderBy(c => c.Location)
                .ToListAsync();
            var outerCell = laneCells.FirstOrDefault();
            if (outerCell != null && outerCell.Status == "Occupied")
            {
                _logger.LogInformation("巷道外侧有货物，暂不下发: 巷道={Lane}, 外侧点位={Location}",
                    cell.CellCode, outerCell.Location);
                return new { success = false, blocked = true, message = "巷道外侧有货物，请等待外侧货物清空后再下发" };
            }

            // 同一起点不允许存在多个未完成任务
            var activeStatuses = new[] { "Pending", "InProgress", "Waiting" };
            var existing = await _context.AgvTasks
                .FirstOrDefaultAsync(t => t.FromLocation == input.FromLocation && activeStatuses.Contains(t.Status));
            if (existing != null)
            {
                return new { success = false, message = $"起点 {input.FromLocation} 已存在未完成任务（任务编号：{existing.TaskCode}），请等待完成后在下发" };
            }

            var taskCode = "AGV" + DateTime.Now.ToString("yyyyMMddHHmmss");
            string taskTyp = "Y1105";
            if (input.FromLocation.StartsWith("A"))
            {
                taskTyp = "Y01";
            }
            if (input.FromLocation.StartsWith("CP"))
            {
                taskTyp = "Y02";
            }

            try
            {
                string reqCode = "REQ" + DateTime.Now.ToString("yyyyMMddHHmmss");
                string ctnrTyp = "1";
                string[] userCallCodePath = new string[] { input.FromLocation, "LK02" };
                string boxCode = "BOX" + DateTime.Now.ToString("yyyyMMddHHmmss");

                var result = await _rcsApiManager.CreateAgvTaskAsync(reqCode, taskTyp, ctnrTyp, userCallCodePath, taskCode, boxCode, null);

                if (result.code == "0")
                {
                    var agvTask = new AgvTask
                    {
                        TaskCode = taskCode,
                        TaskType = taskTyp,
                        Status = "InProgress",
                        FromLocation = input.FromLocation,
                        ToLocation = "LK02",
                        StartedAt = DateTime.Now,
                        CreatedAt = DateTime.Now
                    };
                    _context.AgvTasks.Add(agvTask);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("巷道任务下发成功: TaskCode={TaskCode}, 路径={From}->LK02",
                        taskCode, input.FromLocation);
                    return new { success = true, message = result.message, taskCode = result.taskCode };
                }
                else
                {
                    _logger.LogWarning("巷道任务下发失败: TaskCode={TaskCode}, 原因={Message}", taskCode, result.message);
                    return new { success = false, message = result.message };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "巷道任务下发异常: TaskCode={TaskCode}", taskCode);
                return new { success = false, message = ex.Message };
            }
        }
    }

    public class LaneBindDto
    {
        public string FromLocation { get; set; }
        public string MaterialId { get; set; }
        public string PalletId { get; set; }
        public string? Picima { get; set; }
        public string? Tepi { get; set; }
    }

    public class LaneDispatchDto
    {
        public string FromLocation { get; set; }
    }
}
