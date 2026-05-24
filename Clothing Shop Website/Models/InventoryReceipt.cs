using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clothing_Shop_Website.Models
{
    public class InventoryReceipt
    {
        [Key]
        public int ReceiptID { get; set; }

        [Required]
        public int SupplierID { get; set; }

        [Required]
        public DateTime ImportDate { get; set; }

        // Cột Trạng thái (0: Chờ duyệt, 1: Đã duyệt, 2: Từ chối)
        [Required]
        public int Status { get; set; } = 0;

        [Required]
        public int CreatedBy { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; } = null!;

        // Khóa ngoại trỏ về bảng Suppliers
        [ForeignKey("SupplierID")]
        public virtual Supplier Supplier { get; set; } = null!;

        // Quan hệ 1-N: 1 Phiếu nhập có nhiều Chi tiết phiếu nhập
        public virtual ICollection<InventoryReceiptDetail> InventoryReceiptDetails { get; set; } = new List<InventoryReceiptDetail>();
    }
}