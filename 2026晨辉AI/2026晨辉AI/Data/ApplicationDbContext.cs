using _2026晨辉AI.Models;
using Microsoft.EntityFrameworkCore;

namespace _2026晨辉AI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        
        public DbSet<User> Users { get; set; }
        public DbSet<DeviceTask> DeviceTasks { get; set; }
        public DbSet<AgvTask> AgvTasks { get; set; }
        public DbSet<Cell> Cells { get; set; }
        public DbSet<BindRecord> BindRecords { get; set; }
    }
}