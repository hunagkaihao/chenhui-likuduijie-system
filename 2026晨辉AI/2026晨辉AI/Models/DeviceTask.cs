using System.ComponentModel.DataAnnotations;

namespace _2026晨辉AI.Models
{
    public class DeviceTask
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string DeviceId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string TaskType { get; set; }
        
        public string? TaskContent { get; set; }
        
        public string Status { get; set; } = "Pending";
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? CompletedAt { get; set; }
    }
}