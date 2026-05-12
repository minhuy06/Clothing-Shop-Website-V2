using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clothing_Shop_Website.Models
{
    public class Discount
    {
        [Key]
        public int DiscountID { get; set; }

        [Required(ErrorMessage = "Mã giảm giá không được để trống")]
        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string Code { get; set; } = null!;

        [Required]
        public int DiscountType { get; set; } // Quy ước: 0 = Giảm theo tiền mặt (VNĐ), 1 = Giảm theo phần trăm (%)

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Giá trị giảm không hợp lệ")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng mã không được âm")]
        public int Quantity { get; set; }

        /// <summary>Số lần mã đã được áp dụng (đồng bộ khi đặt hàng thành công).</summary>
        [Range(0, int.MaxValue)]
        public int UsedCount { get; set; }

        [Required(ErrorMessage = "Ngày hết hạn không được để trống")]
        public DateTime ExpirationDate { get; set; }

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
