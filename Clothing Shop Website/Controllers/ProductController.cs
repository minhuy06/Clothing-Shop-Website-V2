using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models;

namespace Clothing_Shop_Website.Controllers
{
    public class ProductController : Controller
    {
        private readonly AppDbContext _db;

        public ProductController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(
            string? search,
            int? categoryId,
            int? session,
            decimal? minPrice,
            decimal? maxPrice,
            string? sort)
        {
            // Lấy toàn bộ sản phẩm từ database (lọc tiếp theo bộ lọc form)
            var query = _db.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .AsQueryable();

            // Lọc theo tên
            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.ProductName.Contains(search));

            // Lọc theo danh mục
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryID == categoryId);

            // Lọc theo mùa (Session)
            if (session.HasValue)
                query = query.Where(p => p.Session == session);

            // Lọc theo giá
            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice);
            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice);

            // Sắp xếp
            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                _ => query.OrderByDescending(p => p.ProductID)
            };

            var products = await query.ToListAsync();

            ViewBag.TotalInDb = await _db.Products.CountAsync();
            ViewBag.Categories = await _db.Categories.ToListAsync();
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Session = session;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Sort = sort;

            return View(products);
        }

        // Chi tiết sản phẩm
        public async Task<IActionResult> Detail(int id)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null || product.Status != 1) return NotFound();

            return View(product);
        }
    }
}