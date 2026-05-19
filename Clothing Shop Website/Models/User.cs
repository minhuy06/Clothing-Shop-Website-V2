using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clothing_Shop_Website.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(50, ErrorMessage = "Họ tên không được vượt quá 50 ký tự")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(15)]
        [Column(TypeName = "varchar(15)")]
        public string Phone { get; set; } = null!;

        [Required]
        public int Role { get; set; } // 0: Admin, 1: Staff, 2: Customer

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 100 ký tự")]
        [Column(TypeName = "varchar(100)")]
        public string Password { get; set; } = null!;

        

        /// <summary>0 = chưa cập nhật, 1 = nữ, 2 = nam, 3 = khác</summary>
        [Range(0, 3)]
        public int Gender { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        public int Status { get; set; } // 1: Hoạt động, 0: Khóa

        // Navigation properties
        public virtual CustomerDetail? CustomerDetail { get; set; }
        public virtual StaffDetail? StaffDetail { get; set; }

        [NotMapped]
        public int RewardPoints
        {
            get => CustomerDetail?.RewardPoints ?? 0;
            set
            {
                if (CustomerDetail == null)
                {
                    CustomerDetail = new CustomerDetail();
                }
                CustomerDetail.RewardPoints = value;
            }
        }

        public virtual ICollection<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}