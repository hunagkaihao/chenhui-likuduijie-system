using System.ComponentModel.DataAnnotations;

namespace _2026晨辉AI.Models
{
    public class Cell
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string CellCode { get; set; }
        
        [Required]
        [StringLength(50)]
        public string CellType { get; set; }
        
        [StringLength(50)]
        public string Status { get; set; } = "Available";
        
        [StringLength(200)]
        public string Location { get; set; }
        
        [StringLength(100)]
        public string Zone { get; set; }
        
        [StringLength(100)]
        public string ShelfCode { get; set; }
        
        [StringLength(100)]
        public string? BindMaterialCode { get; set; }
        
        public int? BindQuantity { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? LastUpdatedAt { get; set; }
        
        public string Description { get; set; }
    }
}