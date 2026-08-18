using System.ComponentModel.DataAnnotations;

namespace _2026晨辉AI.Models
{
    public class AgvTask
    {
        [Key]
        public int Id { get; set; }
        
        [StringLength(100)]
        public string? TaskCode { get; set; }
        
        [StringLength(50)]
        public string? TaskType { get; set; }
        
        [StringLength(50)]
        public string Status { get; set; } = "Pending";
        
        [StringLength(200)]
        public string? FromLocation { get; set; }
        
        [StringLength(200)]
        public string? ToLocation { get; set; }
        
        [StringLength(100)]
        public string? AgvCode { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? StartedAt { get; set; }
        
        public DateTime? CompletedAt { get; set; }
        
        public string? Description { get; set; }
    }
}