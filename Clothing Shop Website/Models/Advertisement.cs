using System;
using System.ComponentModel.DataAnnotations;

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

        [MaxLength(500)]
        public string? LinkUrl { get; set; }

        /// <summary>"banner" | "popup" | "sidebar"</summary>
        [MaxLength(20)]
        public string Position { get; set; } = "banner";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Khóa ngoại trỏ tới bảng Products (Có thể null nếu quảng cáo không thuộc sản phẩm nào)
        public int? ProductID { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("ProductID")]
        public virtual Product? Product { get; set; }
    }
}
