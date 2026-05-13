using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clothing_Shop_Website.Models
{
    public class InventoryReceiptDetail
    {
        [Key]
        public int DetailID { get; set; }

        [Required]
        public int ReceiptID { get; set; }

        [Required]
        public int SizeID { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ImportPrice { get; set; }

        // Khóa ngoại
        [ForeignKey("ReceiptID")]
        public virtual InventoryReceipt InventoryReceipt { get; set; } = null!;

        [ForeignKey("SizeID")]
        public virtual ProductSize ProductSize { get; set; } = null!;
    }
}