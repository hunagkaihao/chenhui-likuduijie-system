using System.ComponentModel.DataAnnotations;

namespace _2026晨辉AI.Models
{
    public class Cell
    {
        /// <summary>
        /// Id编号
        /// </summary>
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// 库位码
        /// </summary>
        [Required]
        [StringLength(100)]
        public string CellCode { get; set; }
        /// <summary>
        /// 库位类型
        /// </summary>
        [Required]
        [StringLength(50)]
        public string CellType { get; set; }
        /// <summary>
        /// 物料状态 Available:可用;  Occupied:占用
        /// </summary>
        [StringLength(50)]
        public string Status { get; set; } = "Available";
        /// <summary>
        /// 库位码
        /// </summary>
        [StringLength(200)]
        public string Location { get; set; }
        /// <summary>
        /// 区域
        /// </summary>
        [StringLength(100)]
        public string? Zone { get; set; }
        /// <summary>
        /// 货架码
        /// </summary>
        [StringLength(100)]
        public string? ShelfCode { get; set; }
        /// <summary>
        /// 绑定物料码
        /// </summary>
        [StringLength(100)]
        public string? BindMaterialCode { get; set; }
        /// <summary>
        /// 绑定物料数量
        /// </summary>
        public int? BindQuantity { get; set; }
        /// <summary>
        /// 托盘码
        /// </summary>
        [StringLength(200)]
        public string? PalletCode { get; set; }
        /// <summary>
        /// 批次码
        /// </summary>
        [StringLength(200)]
        public string? Picima { get; set; }
        /// <summary>
        /// 检验码
        /// </summary>
        [StringLength(200)]
        public string? Tepi { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        /// <summary>
        /// 最新更新时间
        /// </summary>
        public DateTime? LastUpdatedAt { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }
    }
}