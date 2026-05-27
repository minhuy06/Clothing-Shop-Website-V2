using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models;
using Clothing_Shop_Website.Helper;

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

            // Backfill UnitPrice cho dữ liệu cũ (để giỏ hàng luôn dùng giá sau giảm nếu có QC active)
            if (items.Any(i => !i.UnitPrice.HasValue || i.UnitPrice <= 0))
            {
                var now = DateTime.Now;
                var productIds = items.Select(i => i.ProductSize.ProductID).Distinct().ToList();
                var ads = await _db.Advertisements
                    .AsNoTracking()
                    .Where(a => a.IsActive
                        && a.ProductID.HasValue
                        && productIds.Contains(a.ProductID.Value)
                        && a.DiscountValue > 0
                        && (!a.StartDate.HasValue || a.StartDate <= now)
                        && (!a.EndDate.HasValue || a.EndDate >= now))
                    .OrderByDescending(a => a.CreatedDate)
                    .ToListAsync();

                var adMap = ads
                    .GroupBy(a => a.ProductID!.Value)
                    .ToDictionary(g => g.Key, g => g.First());

                var changed = false;
                foreach (var it in items)
                {
                    if (it.UnitPrice.HasValue && it.UnitPrice > 0) continue;
                    if (adMap.TryGetValue(it.ProductSize.ProductID, out var ad))
                    {
                        it.UnitPrice = AdPromotionHelper.GetSalePrice(it.ProductSize.Product.Price, ad);
                        it.AdID = ad.AdID;
                        changed = true;
                    }
                    else
                    {
                        it.UnitPrice = it.ProductSize.Product.Price;
                        it.AdID = null;
                        changed = true;
                    }
                }

                if (changed) await _db.SaveChangesAsync();
            }

            ViewBag.RewardPoints = RewardPointsHelper.GetPoints(HttpContext.Session);
            ViewBag.SavedCoupon = HttpContext.Session.GetString("CheckoutCoupon") ?? "";
            ViewBag.SavedUsePoints = HttpContext.Session.GetInt32("CheckoutUsePoints") ?? 0;

            return View(items);
        }

        /// <summary>Kiểm tra mã giảm giá (AJAX từ trang giỏ hàng).</summary>
        [HttpGet]
        public async Task<IActionResult> ValidateCoupon(string code)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            if (string.IsNullOrWhiteSpace(code))
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá." });

            var subtotal = await _db.CartItems
                .Where(c => c.UserID == userId)
                .Include(c => c.ProductSize)
                    .ThenInclude(s => s.Product)
                .SumAsync(c => (c.UnitPrice ?? c.ProductSize.Product.Price) * c.Quantity);

            var disc = await _db.Discounts.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Code == code.Trim().ToUpper()
                    && d.ExpirationDate >= DateTime.Now
                    && d.UsedCount < d.Quantity);

            if (disc == null)
                return Json(new { success = false, message = "Mã không hợp lệ hoặc đã hết hạn." });

            decimal amount = disc.DiscountType == 1
                ? subtotal * disc.DiscountValue / 100m
                : disc.DiscountValue;

            if (amount > subtotal) amount = subtotal;

            string label = disc.DiscountType == 1
                ? $"Giảm {disc.DiscountValue:0.##}%"
                : $"Giảm {disc.DiscountValue:N0}đ";

            return Json(new { success = true, discount = amount, code = disc.Code, label });
        }

        /// <summary>Lưu mã & điểm đã chọn trước khi sang trang thanh toán.</summary>
        [HttpPost]
        public IActionResult SetCheckoutPromo(string? coupon, int usePoints = 0)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            HttpContext.Session.SetString("CheckoutCoupon", (coupon ?? "").Trim().ToUpper());
            HttpContext.Session.SetInt32("CheckoutUsePoints", Math.Max(0, usePoints));
            return Json(new { success = true });
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

            // Tính giá sau giảm (nếu có quảng cáo theo sản phẩm đang active)
            var now = DateTime.Now;
            var ad = await _db.Advertisements
                .AsNoTracking()
                .Where(a => a.IsActive
                    && a.ProductID == size.ProductID
                    && a.DiscountValue > 0
                    && (!a.StartDate.HasValue || a.StartDate <= now)
                    && (!a.EndDate.HasValue || a.EndDate >= now))
                .OrderByDescending(a => a.CreatedDate)
                .FirstOrDefaultAsync();

            var unitPrice = AdPromotionHelper.GetSalePrice(size.Product.Price, ad);

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

                // Nếu item cũ chưa có UnitPrice thì set theo giá hiện tại (giữ snapshot về sau)
                if (!existing.UnitPrice.HasValue || existing.UnitPrice <= 0)
                {
                    existing.UnitPrice = unitPrice;
                    existing.AdID = ad?.AdID;
                }
            }
            else
            {
                _db.CartItems.Add(new CartItem
                {
                    UserID = userId.Value,
                    SizeID = sizeId, // 💡 Lưu SizeID vào Database
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    AdID = ad?.AdID
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