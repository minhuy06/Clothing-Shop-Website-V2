using Microsoft.EntityFrameworkCore;
using Clothing_Shop_Website.Models;

namespace Clothing_Shop_Website.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Khai báo các bảng sẽ được tạo trong Database
        public DbSet<User> Users {  get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductSize> ProductSizes { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

    }
}
