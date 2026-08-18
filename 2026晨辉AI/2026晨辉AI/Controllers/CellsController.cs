using _2026晨辉AI.Data;
using _2026晨辉AI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2026晨辉AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CellsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public CellsController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // GET: api/Cells
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cell>>> GetCells()
        {
            return await _context.Cells.ToListAsync();
        }
        
        // GET: api/Cells/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Cell>> GetCell(int id)
        {
            var cell = await _context.Cells.FindAsync(id);
            
            if (cell == null)
            {
                return NotFound();
            }
            
            return cell;
        }
        
        // GET: api/Cells/code/{code}
        [HttpGet("code/{code}")]
        public async Task<ActionResult<Cell>> GetCellByCode(string code)
        {
            var cell = await _context.Cells.FirstOrDefaultAsync(c => c.CellCode == code);
            
            if (cell == null)
            {
                return NotFound();
            }
            
            return cell;
        }
        
        // POST: api/Cells
        [HttpPost]
        public async Task<ActionResult<Cell>> PostCell(Cell cell)
        {
            cell.Status = "Available";
            cell.CreatedAt = DateTime.Now;
            
            _context.Cells.Add(cell);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("GetCell", new { id = cell.Id }, cell);
        }
        
        // PUT: api/Cells/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCell(int id, Cell cell)
        {
            if (id != cell.Id)
            {
                return BadRequest();
            }
            
            cell.LastUpdatedAt = DateTime.Now;
            
            _context.Entry(cell).State = EntityState.Modified;
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CellExists(id))
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
        
        // DELETE: api/Cells/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCell(int id)
        {
            var cell = await _context.Cells.FindAsync(id);
            if (cell == null)
            {
                return NotFound();
            }
            
            _context.Cells.Remove(cell);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
        
        // POST: api/Cells/bind
        [HttpPost("bind")]
        public async Task<ActionResult<bool>> BindCell([FromBody] CellBindDto input)
        {
            var cell = await _context.Cells.FirstOrDefaultAsync(c => c.CellCode == input.CellCode);
            if (cell == null)
            {
                return NotFound();
            }
            
            cell.BindMaterialCode = input.MaterialCode;
            cell.BindQuantity = input.Quantity;
            cell.Status = "Occupied";
            cell.LastUpdatedAt = DateTime.Now;
            
            _context.Entry(cell).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            return true;
        }
        
        // POST: api/Cells/unbind
        [HttpPost("unbind")]
        public async Task<ActionResult<bool>> UnbindCell([FromBody] CellUnbindDto input)
        {
            var cell = await _context.Cells.FirstOrDefaultAsync(c => c.CellCode == input.CellCode);
            if (cell == null)
            {
                return NotFound();
            }
            
            cell.BindMaterialCode = null;
            cell.BindQuantity = null;
            cell.Status = "Available";
            cell.LastUpdatedAt = DateTime.Now;
            
            _context.Entry(cell).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            return true;
        }
        
        // GET: api/Cells/zone/{zone}
        [HttpGet("zone/{zone}")]
        public async Task<ActionResult<IEnumerable<Cell>>> GetCellsByZone(string zone)
        {
            var cells = await _context.Cells.Where(c => c.Zone == zone).ToListAsync();
            return cells;
        }
        
        private bool CellExists(int id)
        {
            return _context.Cells.Any(e => e.Id == id);
        }
    }
    
    // DTOs for cell operations
    public class CellBindDto
    {
        public string CellCode { get; set; }
        public string MaterialCode { get; set; }
        public int Quantity { get; set; }
    }
    
    public class CellUnbindDto
    {
        public string CellCode { get; set; }
    }
}