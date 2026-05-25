using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clothing_Shop_Website.Models
{
    public class ProductSize
    {
        [Key]
        public int SizeID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Tên kích cỡ không được để trống")]
        [StringLength(5)]
        [Column(TypeName = "varchar(5)")]
        public string SizeName { get; set; } = null!;

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho không được âm")]
        public int StockQuantity { get; set; } = 0;
        public int MinimumStock { get; set; }

        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; } = null!;

        // Quan hệ 1-N: 1 Size có thể xuất hiện trong nhiều Chi tiết phiếu nhậps
        public virtual ICollection<InventoryReceiptDetail> InventoryReceiptDetails { get; set; } = new List<InventoryReceiptDetail>();

        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
