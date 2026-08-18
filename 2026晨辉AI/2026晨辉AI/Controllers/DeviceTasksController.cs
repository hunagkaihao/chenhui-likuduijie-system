using _2026晨辉AI.Data;
using _2026晨辉AI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2026晨辉AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceTasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public DeviceTasksController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // GET: api/DeviceTasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DeviceTask>>> GetDeviceTasks()
        {
            return await _context.DeviceTasks.ToListAsync();
        }
        
        // GET: api/DeviceTasks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DeviceTask>> GetDeviceTask(int id)
        {
            var deviceTask = await _context.DeviceTasks.FindAsync(id);
            
            if (deviceTask == null)
            {
                return NotFound();
            }
            
            return deviceTask;
        }
        
        // POST: api/DeviceTasks - 下发任务
        [HttpPost]
        public async Task<ActionResult<DeviceTask>> PostDeviceTask(DeviceTask deviceTask)
        {
            // 设置默认状态为Pending
            deviceTask.Status = "Pending";
            deviceTask.CreatedAt = DateTime.Now;
            
            _context.DeviceTasks.Add(deviceTask);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("GetDeviceTask", new { id = deviceTask.Id }, deviceTask);
        }
        
        // PUT: api/DeviceTasks/5 - 更新任务状态
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDeviceTask(int id, DeviceTask deviceTask)
        {
            if (id != deviceTask.Id)
            {
                return BadRequest();
            }
            
            // 如果状态更新为Completed，设置完成时间
            if (deviceTask.Status == "Completed" && deviceTask.CompletedAt == null)
            {
                deviceTask.CompletedAt = DateTime.Now;
            }
            
            _context.Entry(deviceTask).State = EntityState.Modified;
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DeviceTaskExists(id))
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
        
        // DELETE: api/DeviceTasks/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeviceTask(int id)
        {
            var deviceTask = await _context.DeviceTasks.FindAsync(id);
            if (deviceTask == null)
            {
                return NotFound();
            }
            
            _context.DeviceTasks.Remove(deviceTask);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
        
        // GET: api/DeviceTasks/device/{deviceId} - 获取指定设备的任务
        [HttpGet("device/{deviceId}")]
        public async Task<ActionResult<IEnumerable<DeviceTask>>> GetDeviceTasksByDeviceId(string deviceId)
        {
            var tasks = await _context.DeviceTasks.Where(t => t.DeviceId == deviceId).ToListAsync();
            return tasks;
        }
        
        private bool DeviceTaskExists(int id)
        {
            return _context.DeviceTasks.Any(e => e.Id == id);
        }
    }
}