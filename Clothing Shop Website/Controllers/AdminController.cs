using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models;

namespace Clothing_Shop_Website.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        public AdminController(AppDbContext db) { _db = db; }

        // ── Kiểm tra quyền Admin ──
        private bool IsAdmin() => HttpContext.Session.GetInt32("Role") == 1;

        // ═══════════════════════════════
        //   DASHBOARD
        // ═══════════════════════════════
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            ViewBag.TotalProducts = await _db.Products.CountAsync();
            ViewBag.TotalOrders = await _db.Orders.CountAsync();
            ViewBag.TotalCustomers = await _db.Users.CountAsync(u => u.Role == 0);
            ViewBag.TotalRevenue = await _db.Orders
                .Where(o => o.Status == 2)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.RecentOrders = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            ViewBag.TopProducts = await _db.OrderDetails
                .Include(d => d.Product)
                .GroupBy(d => d.ProductID)
                .Select(g => new {
                    Product = g.First().Product,
                    TotalSold = g.Sum(d => d.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToListAsync();

            return View();
        }

        // ═══════════════════════════════
        //   SẢN PHẨM
        // ═══════════════════════════════
        public async Task<IActionResult> Products(string? search, int? categoryId, string? season)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var query = _db.Products.Include(p => p.Category).AsQueryable();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.ProductName.Contains(search));
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryID == categoryId);
            if (!string.IsNullOrEmpty(season) && int.TryParse(season, out int s))
                query = query.Where(p => p.Session == s);

            ViewBag.Categories = await _db.Categories.ToListAsync();
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Season = season;

            return View(await query.OrderByDescending(p => p.ProductID).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(
            string productName, int categoryID, int session,
            decimal price, decimal originalPrice,
            int stock, string? imageUrl, string? description)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            _db.Products.Add(new Product
            {
                ProductName = productName,
                CategoryID = categoryID,
                Session = session,
                Price = price,
                OriginalPrice = originalPrice,
                ImageUrl = imageUrl,
                Description = description
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm sản phẩm!";
            return RedirectToAction("Products");
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct(
            int productID, string productName, int categoryID, int session,
            decimal price, decimal originalPrice,
            string? imageUrl, string? description)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var p = await _db.Products.FindAsync(productID);
            if (p != null)
            {
                p.ProductName = productName;
                p.CategoryID = categoryID;
                p.Session = session;
                p.Price = price;
                p.OriginalPrice = originalPrice;
                p.ImageUrl = imageUrl;
                p.Description = description;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật sản phẩm!";
            }
            return RedirectToAction("Products");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int productID)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var p = await _db.Products.FindAsync(productID);
            if (p != null)
            {
                _db.Products.Remove(p);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã xóa sản phẩm!";
            }
            return RedirectToAction("Products");
        }

        // ═══════════════════════════════
        //   ĐƠN HÀNG
        // ═══════════════════════════════
        public async Task<IActionResult> Orders(string? status, string? search)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var query = _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && int.TryParse(status, out int s))
                query = query.Where(o => o.Status == s);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(o =>
                    o.ReceiverName.Contains(search) ||
                    o.ReceiverPhone.Contains(search) ||
                    o.OrderID.ToString().Contains(search));

            ViewBag.Status = status;
            ViewBag.Search = search;

            return View(await query.OrderByDescending(o => o.OrderDate).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, int status)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var order = await _db.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật trạng thái!";
            }
            return RedirectToAction("Orders");
        }

        // ═══════════════════════════════
        //   KHÁCH HÀNG
        // ═══════════════════════════════
        public async Task<IActionResult> Customers(string? search)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var query = _db.Users.Where(u => u.Role == 0).AsQueryable();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Phone.Contains(search));

            ViewBag.Search = search;
            return View(await query.OrderByDescending(u => u.UserID).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLockUser(int userId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.Status = user.Status == 1 ? 0 : 1;
                await _db.SaveChangesAsync();
                TempData["Success"] = user.Status == 0 ? "Đã khóa tài khoản!" : "Đã mở khóa tài khoản!";
            }
            return RedirectToAction("Customers");
        }

        // ═══════════════════════════════
        //   MÃ GIẢM GIÁ
        // ═══════════════════════════════
        public async Task<IActionResult> Discounts()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View(await _db.Discounts.OrderByDescending(d => d.DiscountID).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> AddDiscount(
            string code, decimal discountValue, string discountType,
            int quantity, DateTime expirationDate)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (await _db.Discounts.AnyAsync(d => d.Code == code.ToUpper()))
            {
                TempData["Error"] = "Mã này đã tồn tại!";
                return RedirectToAction("Products");
            }

            _db.Discounts.Add(new Discount
            {
                Code = code.ToUpper(),
                DiscountValue = discountValue,
                DiscountType = discountType,
                Quantity = quantity,
                ExpirationDate = expirationDate
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm mã giảm giá!";
            return RedirectToAction("Products");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDiscount(int discountId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var d = await _db.Discounts.FindAsync(discountId);
            if (d != null) { _db.Discounts.Remove(d); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Đã xóa mã!";
            return RedirectToAction("Products");
        }
    }
}