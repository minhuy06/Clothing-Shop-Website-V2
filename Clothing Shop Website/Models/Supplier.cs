using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clothing_Shop_Website.Models
{
    public class Supplier
    {
        [Key]
        public int SupplierID { get; set; }

        [Required]
        [MaxLength(50)]
        public string SupplierName { get; set; } = null!;

        [MaxLength(15)]
        [Column(TypeName = "varchar(15)")]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(50)]
        public string? Country { get; set; }

        [MaxLength(50)]
        public string? ContactInfo { get; set; }

        // Quan hệ 1-N: 1 Nhà cung cấp có nhiều Phiếu nhập
        public virtual ICollection<InventoryReceipt> InventoryReceipts { get; set; } = new List<InventoryReceipt>();
    }
}