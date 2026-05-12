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

        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; } = null!;
    }
}
