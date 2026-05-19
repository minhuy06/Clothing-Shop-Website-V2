using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clothing_Shop_Website.Models
{
    public class StaffShift
    {
        [Key]
        public int ShiftID { get; set; }

        [Required]
        public int UserID { get; set; }

        // Quy ước: 1 = Ca sáng, 2 = Ca chiều, 3 = Ca tối
        [Required]
        public int ShiftType { get; set; }

        [MaxLength(20)]
        public string? DayOfWeek { get; set; }

        [ForeignKey("UserID")]
        public virtual StaffDetail StaffDetail { get; set; } = null!;
    }
}