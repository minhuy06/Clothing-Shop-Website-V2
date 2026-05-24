using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clothing_Shop_Website.Models
{
    public class Product
    {
        [Key]
        public int ProductID { get; set; }

        [Required]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(100)]
        public string ProductName { get; set; } = null!;

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string? ImageUrl { get; set; }

        [StringLength(100)]
        public string? Description { get; set; }

        public int Session { get; set; }

        [MaxLength(50)]
        public string? Color { get; set; }

        [MaxLength(50)]
        public string? Style { get; set; }

        [MaxLength(50)]
        public string? Material { get; set; }

        /// <summary>0 = chưa hiển thị giao diện khách, 1 = đã cập nhật lên giao diện.</summary>
        public int Status { get; set; } = 0;

        [ForeignKey("CategoryID")]
        public virtual Category Category { get; set; } = null!;

        public virtual ICollection<ProductSize> ProductSizes { get; set; } = new List<ProductSize>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
