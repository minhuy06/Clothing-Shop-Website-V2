using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clothing_Shop_Website.Models
{
    public class Advertisement
    {
        [Key]
        public int AdID { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        /// <summary>"popup" | "sidebar"</summary>
        [MaxLength(20)]
        public string Position { get; set; } = "popup";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>Sản phẩm được quảng cáo (nullable nếu QC không gắn SP).</summary>
        public int? ProductID { get; set; }

        /// <summary>0 = giảm theo tiền (VNĐ), 1 = giảm theo % (giống bảng Discounts).</summary>
        public int DiscountType { get; set; }

        /// <summary>Giá trị giảm: VNĐ hoặc % tùy DiscountType. 0 = không giảm.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [ForeignKey("ProductID")]
        public virtual Product? Product { get; set; }

        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
