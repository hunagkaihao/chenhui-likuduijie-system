using System.ComponentModel.DataAnnotations;

namespace _2026晨辉AI.Models
{
    public class BindRecord
    {
        [Key]
        public int Id { get; set; }

        [StringLength(200)]
        public string Biaoshima { get; set; }

        [StringLength(200)]
        public string Tuopanma { get; set; }

        [StringLength(200)]
        public string? Picima { get; set; }

        [StringLength(200)]
        public string? Tepi { get; set; }

        [StringLength(100)]
        public string WorkMan { get; set; }

        [StringLength(100)]
        public string Company { get; set; }

        public int Shuliang { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Success";

        [StringLength(500)]
        public string? Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
