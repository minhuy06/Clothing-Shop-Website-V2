using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clothing_Shop_Website.Models
{
    public class UserAddress
    {
        [Key]
        public int AddressID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required(ErrorMessage = "Tỉnh/Thành phố không được để trống")]
        [StringLength(50)]
        public string Province_City { get; set; } = null!;

        [Required(ErrorMessage = "Địa chỉ chi tiết không được để trống")]
        [StringLength(100)]
        public string DetailedAddress { get; set; } = null!;

        [Required(ErrorMessage = "Tên người nhận không được để trống")]
        [StringLength(50)]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(15)]
        [Column(TypeName = "varchar(15)")]
        public string Phone { get; set; } = null!;

        public bool IsDefault { get; set; }

        [ForeignKey("UserID")]
        public virtual User user { get; set; } = null!;
    }
}
