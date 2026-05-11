using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clothing_Shop_Website.Models
{
    public class Order
    {
        [Key]
        public int OrderID { get; set; }

        [Required]
        public int UserID { get; set; }

        public int? DiscountID { get; set; } // Cho phép null vì không phải đơn nào cũng có mã giảm giá

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Địa chỉ giao hàng không được để trống")]
        [StringLength(100)]
        public string ShippingAddress { get; set; } = null!;

        [Required(ErrorMessage = "Tỉnh/Thành phố giao hàng không được để trống")]
        [StringLength(50)]
        public string ShippingProvince { get; set; } = null!;

        [Required(ErrorMessage = "Tên người nhận không được để trống")]
        [StringLength(50)]
        public string ReceiverName { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại người nhận không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(15)]
        [Column(TypeName = "varchar(15)")]
        public string ReceiverPhone { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public int Status { get; set; } // Ví dụ: 0: Chờ duyệt, 1: Đang giao, 2: Hoàn thành, 3: Đã hủy

        public int RedemptionPoints { get; set; } = 0;

        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("DiscountID")]
        public virtual Discount? Discount { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
