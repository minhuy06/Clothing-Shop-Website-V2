using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models;
using Clothing_Shop_Website.Helper;

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
            string? sort,
            int? highlight)
        {
            // Lấy toàn bộ sản phẩm từ database (lọc tiếp theo bộ lọc form)
            var query = _db.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .Where(p => p.Status == 1)
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

            var products = await query.ToListAsync();

            // Sắp xếp (in-memory) để ưu tiên highlight lên đầu nhưng vẫn giữ sort cho phần còn lại
            products = sort switch
            {
                "price_asc" => products.OrderBy(p => p.Price).ToList(),
                "price_desc" => products.OrderByDescending(p => p.Price).ToList(),
                _ => products.OrderByDescending(p => p.ProductID).ToList()
            };

            if (highlight.HasValue)
            {
                products = products
                    .OrderByDescending(p => p.ProductID == highlight.Value)
                    .ToList();
            }

            // Quảng cáo giảm giá theo sản phẩm (để hiển thị giá sale + gạch giá gốc)
            var now = DateTime.Now;
            var productIds = products.Select(p => p.ProductID).ToList();
            var activeAds = await _db.Advertisements
                .AsNoTracking()
                .Where(a => a.IsActive
                    && a.ProductID.HasValue
                    && productIds.Contains(a.ProductID.Value)
                    && a.DiscountValue > 0
                    && (!a.StartDate.HasValue || a.StartDate <= now)
                    && (!a.EndDate.HasValue || a.EndDate >= now))
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            // 1 SP chỉ lấy 1 QC mới nhất
            var adMap = activeAds
                .GroupBy(a => a.ProductID!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            ViewBag.Categories = await _db.Categories.ToListAsync();
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Session = session;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Sort = sort;
            ViewBag.Highlight = highlight;
            ViewBag.ActiveAdMap = adMap;

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

        /// <summary>Trả về danh sách size + tồn kho từ bảng ProductSizes (cho FE).</summary>
        [HttpGet]
        public async Task<IActionResult> GetSizes(int productId)
        {
            var sizes = await _db.ProductSizes
                .AsNoTracking()
                .Where(s => s.ProductID == productId)
                .OrderBy(s => s.SizeName)
                .Select(s => new
                {
                    id = s.SizeID,
                    name = s.SizeName,
                    stock = s.StockQuantity,
                    inStock = s.StockQuantity > 0
                })
                .ToListAsync();

            return Json(sizes);
        }
    }
}