using System;
using System.Linq;
using System.Threading.Tasks;
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
                .Include(c => c.ProductSize)
                    .ThenInclude(s => s.Product)
                        .ThenInclude(p => p.Category)
                .Where(c => c.UserID == userId)
                .ToListAsync();

            return View(items);
        }

        // ── Thêm vào giỏ ──
        [HttpPost]
        // 💡 THAY ĐỔI QUAN TRỌNG: Nhận sizeId thay vì productId
        public async Task<IActionResult> Add(int sizeId, int quantity = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            // Kiểm tra Kích cỡ (Size) và Sản phẩm có tồn tại không
            var size = await _db.ProductSizes
                .Include(s => s.Product)
                .FirstOrDefaultAsync(s => s.SizeID == sizeId);

            if (size == null)
                return Json(new { success = false, message = "Sản phẩm hoặc kích cỡ không tồn tại!" });

            // 💡 BUSINESS LOGIC: Chặn luôn nếu kho đã hết hàng
            if (size.StockQuantity < quantity)
                return Json(new { success = false, message = "Số lượng tồn kho không đủ!" });

            // Kiểm tra xem giỏ hàng đã có cái Size này chưa
            var existing = await _db.CartItems
                .FirstOrDefaultAsync(c => c.UserID == userId && c.SizeID == sizeId); // 💡 Dùng SizeID

            if (existing != null)
            {
                // Kiểm tra xem cộng dồn vào có vượt quá tồn kho không
                if (existing.Quantity + quantity > size.StockQuantity)
                    return Json(new { success = false, message = "Vượt quá số lượng tồn kho cho phép!" });

                existing.Quantity += quantity;
            }
            else
            {
                _db.CartItems.Add(new CartItem
                {
                    UserID = userId.Value,
                    SizeID = sizeId, // 💡 Lưu SizeID vào Database
                    Quantity = quantity
                });
            }

            await _db.SaveChangesAsync();

            // Đếm tổng số sản phẩm trong giỏ để cập nhật icon số lượng trên Header
            var cartCount = await _db.CartItems
                .Where(c => c.UserID == userId)
                .SumAsync(c => c.Quantity);

            return Json(new { success = true, message = "Đã thêm vào giỏ hàng!", cartCount });
        }

        // ── Cập nhật số lượng (ở trang Xem giỏ hàng) ──
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartId, int quantity)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var item = await _db.CartItems
                .Include(c => c.ProductSize) // 💡 Kéo theo Size để kiểm tra tồn kho
                .FirstOrDefaultAsync(c => c.CartID == cartId && c.UserID == userId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    _db.CartItems.Remove(item);
                }
                else
                {
                    // 💡 BUSINESS LOGIC: Không cho khách tăng số lượng lố số hàng đang có
                    if (quantity > item.ProductSize.StockQuantity)
                    {
                        TempData["Error"] = $"Chỉ còn {item.ProductSize.StockQuantity} sản phẩm trong kho.";
                    }
                    else
                    {
                        item.Quantity = quantity;
                    }
                }

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

        // ── Đếm giỏ hàng (dùng cho badge trên Header) ──
        [HttpGet]
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