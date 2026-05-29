using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models;

class Program
{
    static void Main()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=ClothingShopWebsiteDB;Trusted_Connection=True;MultipleActiveResultSets=true");

        using (var db = new AppDbContext(optionsBuilder.Options))
        {
            try
            {
                var product = new Product
                {
                    ProductName = "Test Product",
                    CategoryID = 1,
                    Session = 1,
                    Price = 100000,
                    ImageUrl = null,
                    Description = null,
                    Color = null,
                    Style = null,
                    Material = null,
                    Status = 0
                };
                db.Products.Add(product);
                db.SaveChanges();
                Console.WriteLine("Product saved successfully with ID: " + product.ProductID);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.ToString());
                if (ex.InnerException != null)
                {
                    Console.WriteLine("INNER ERROR: " + ex.InnerException.ToString());
                }
            }
        }
    }
}
