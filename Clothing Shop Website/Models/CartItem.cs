using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clothing_Shop_Website.Models
{
    public class CartItem
    {
        [Key]
        public int CartID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int SizeID { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("SizeID")]
        public virtual ProductSize ProductSize { get; set; } = null!;
    }
}
