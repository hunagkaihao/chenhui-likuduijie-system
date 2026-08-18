using System.ComponentModel.DataAnnotations;

namespace _2026晨辉AI.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Email { get; set; }
        
        [Required]
        [StringLength(100)]
        public string PasswordHash { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}