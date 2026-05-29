using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clothing_Shop_Website.Models
{
    public class CartItem
    {
        [Key]
        public int CartID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int SizeID { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        /// <summary>Giá bán áp dụng khi thêm vào giỏ (sau giảm QC nếu có).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitPrice { get; set; }

        /// <summary>Quảng cáo áp dụng giảm giá (nullable).</summary>
        public int? AdID { get; set; }

        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("SizeID")]
        public virtual ProductSize ProductSize { get; set; } = null!;

        [ForeignKey("AdID")]
        public virtual Advertisement? Advertisement { get; set; }
    }
}
