using Microsoft.EntityFrameworkCore;
using Clothing_Shop_Website.Models;

namespace Clothing_Shop_Website.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Khai báo các bảng sẽ được tạo trong Database
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<CustomerDetail> CustomerDetails { get; set; } = null!;
        public DbSet<StaffDetail> StaffDetails { get; set; } = null!;
        public DbSet<StaffShift> StaffShifts { get; set; } = null!;
        public DbSet<UserAddress> UserAddresses { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<ProductSize> ProductSizes { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<Discount> Discounts { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<InventoryReceipt> InventoryReceipts { get; set; } = null!;
        public DbSet<InventoryReceiptDetail> InventoryReceiptDetails { get; set; } = null!;
        public DbSet<Advertisement> Advertisements { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CartItem>()
                .HasOne(c => c.Advertisement)
                .WithMany(a => a.CartItems)
                .HasForeignKey(c => c.AdID)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
