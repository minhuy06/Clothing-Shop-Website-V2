using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models;

namespace Clothing_Shop_Website.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _db;
        public CartController(AppDbContext db) { _db = db; }

        // ── Xem giỏ hàng ──
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var items = await _db.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserID == userId)
                .ToListAsync();

            return View(items);
        }

        // ── Thêm vào giỏ ──
        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            // Kiểm tra sản phẩm tồn tại
            var product = await _db.Products.FindAsync(productId);
            if (product == null)
                return Json(new { success = false, message = "Sản phẩm không tồn tại!" });

            // Đã có trong giỏ → tăng số lượng
            var existing = await _db.CartItems
                .FirstOrDefaultAsync(c => c.UserID == userId && c.ProductID == productId);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                _db.CartItems.Add(new CartItem
                {
                    UserID = userId.Value,
                    ProductID = productId,
                    Quantity = quantity
                });
            }

            await _db.SaveChangesAsync();

            // Đếm tổng số sản phẩm trong giỏ
            var cartCount = await _db.CartItems
                .Where(c => c.UserID == userId)
                .SumAsync(c => c.Quantity);

            return Json(new { success = true, message = "Đã thêm vào giỏ hàng!", cartCount });
        }

        // ── Cập nhật số lượng ──
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartId, int quantity)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var item = await _db.CartItems
                .FirstOrDefaultAsync(c => c.CartID == cartId && c.UserID == userId);

            if (item != null)
            {
                if (quantity <= 0)
                    _db.CartItems.Remove(item);
                else
                    item.Quantity = quantity;

                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // ── Xóa khỏi giỏ ──
        [HttpPost]
        public async Task<IActionResult> Remove(int cartId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var item = await _db.CartItems
                .FirstOrDefaultAsync(c => c.CartID == cartId && c.UserID == userId);

            if (item != null)
            {
                _db.CartItems.Remove(item);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // ── Xóa tất cả ──
        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var items = await _db.CartItems.Where(c => c.UserID == userId).ToListAsync();
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // ── Đếm giỏ hàng (dùng cho badge) ──
        public async Task<IActionResult> Count()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(0);

            var count = await _db.CartItems
                .Where(c => c.UserID == userId)
                .SumAsync(c => c.Quantity);

            return Json(count);
        }
    }
}