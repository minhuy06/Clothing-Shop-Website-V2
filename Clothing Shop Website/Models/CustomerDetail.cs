using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clothing_Shop_Website.Models
{
    public class CustomerDetail
    {
        // Khóa chính kiêm Khóa ngoại trỏ về bảng Users
        [Key]
        [ForeignKey("User")]
        public int UserID { get; set; }

        // Chuyển RewardPoints từ Users sang đây vì chỉ Khách hàng mới có điểm thưởng
        [Range(0, int.MaxValue, ErrorMessage = "Điểm thưởng không được âm")]
        public int RewardPoints { get; set; } = 0;

        // Có thể thêm Hạng thành viên (Ví dụ: Đồng, Bạc, Vàng...)
        [MaxLength(50)]
        public string? MembershipTier { get; set; }

        // Điều hướng trỏ ngược về User
        public virtual User User { get; set; } = null!;
    }
}