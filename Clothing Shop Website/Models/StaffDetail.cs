using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace Clothing_Shop_Website.Models
{
    public class StaffDetail
    {
        // Khóa chính kiêm Khóa ngoại trỏ về bảng Users
        [Key]
        [ForeignKey("User")]
        public int UserID { get; set; }

        [Column(TypeName = "date")]
        public DateTime HireDate { get; set; } // Ngày vào làm

        [Column(TypeName = "decimal(18,2)")]
        public decimal Salary { get; set; } // Mức lương

        // Điều hướng trỏ ngược về User
        public virtual User User { get; set; } = null!;

        // Quan hệ 1-N với bảng StaffShifts
        public virtual ICollection<StaffShift> StaffShifts { get; set; } = new List<StaffShift>();
    }
}