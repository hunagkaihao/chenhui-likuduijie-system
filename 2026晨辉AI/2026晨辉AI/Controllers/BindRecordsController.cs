using _2026晨辉AI.Data;
using _2026晨辉AI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2026晨辉AI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BindRecordsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BindRecordsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/BindRecords
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BindRecord>>> GetBindRecords()
        {
            return await _context.BindRecords.OrderByDescending(b => b.CreatedAt).ToListAsync();
        }

        // POST: api/BindRecords
        [HttpPost]
        public async Task<ActionResult<BindRecord>> PostBindRecord(BindRecord bindRecord)
        {
            bindRecord.CreatedAt = DateTime.Now;
            _context.BindRecords.Add(bindRecord);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBindRecords), new { id = bindRecord.Id }, bindRecord);
        }

        // GET: api/BindRecords/recent?count=10
        [HttpGet("recent")]
        public async Task<ActionResult<IEnumerable<BindRecord>>> GetRecent([FromQuery] int count = 50)
        {
            return await _context.BindRecords.OrderByDescending(b => b.CreatedAt).Take(count).ToListAsync();
        }
    }
}
