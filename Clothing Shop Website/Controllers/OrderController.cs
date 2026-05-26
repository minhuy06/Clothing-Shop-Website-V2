using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models;
using Clothing_Shop_Website.Enums;
using Clothing_Shop_Website.Helper;

namespace Clothing_Shop_Website.Controllers
{
    public class OrderController : Controller
    {
        private readonly AppDbContext _db;
        public OrderController(AppDbContext db) { _db = db; }

        // ── Trang thanh toán ──
        public async Task<IActionResult> Checkout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var cartItems = await _db.CartItems
                .Include(c => c.ProductSize)     // 💡 Thay đổi: Kéo ProductSize
                    .ThenInclude(s => s.Product) // 💡 Thay đổi: Từ Size kéo ngược ra Product
                .Where(c => c.UserID == userId)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("Index", "Cart");

            var addresses = await _db.UserAddresses
                .Where(a => a.UserID == userId)
                .ToListAsync();

            ViewBag.CartItems = cartItems;
            ViewBag.Addresses = addresses;
            ViewBag.Total = cartItems.Sum(c => c.ProductSize.Product.Price * c.Quantity);
            ViewBag.SavedCoupon = HttpContext.Session.GetString("CheckoutCoupon") ?? "";
            ViewBag.SavedUsePoints = HttpContext.Session.GetInt32("CheckoutUsePoints") ?? 0;
            ViewBag.RewardPoints = RewardPointsHelper.GetPoints(HttpContext.Session);

            return View();
        }

        // ── Đặt hàng ──
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(
            string receiverName,
            string receiverPhone,
            string shippingAddress,
            string shippingProvince,
            string? discountCode,
            int usePoints = 0)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = await _db.Users
                .Include(u => u.CustomerDetail)
                .FirstOrDefaultAsync(u => u.UserID == userId.Value);
            if (user == null) return RedirectToAction("Login", "Account");

            var cartItems = await _db.CartItems
                .Include(c => c.ProductSize)     // 💡 Cập nhật Include
                    .ThenInclude(s => s.Product) // 💡 Cập nhật Include
                .Where(c => c.UserID == userId)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("Index", "Cart");

            // BUSINESS LOGIC: Kiểm tra tồn kho trước khi cho phép đặt hàng
            foreach (var item in cartItems)
            {
                if (item.ProductSize.StockQuantity < item.Quantity)
                {
                    TempData["Error"] = $"Sản phẩm {item.ProductSize.Product.ProductName} (Size {item.ProductSize.SizeName}) không đủ số lượng trong kho.";
                    return RedirectToAction("Checkout"); // Trả về trang thanh toán báo lỗi
                }
            }

            // Tính toán tiền bạc
            decimal subtotal = cartItems.Sum(c => c.ProductSize.Product.Price * c.Quantity);
            decimal shipping = subtotal >= 500000 ? 0 : 30000;
            decimal discount = 0;

            int? discountId = null;
            if (!string.IsNullOrEmpty(discountCode))
            {
                var disc = await _db.Discounts
                    .FirstOrDefaultAsync(d => d.Code == discountCode
                        && d.ExpirationDate >= DateTime.Now
                        && d.UsedCount < d.Quantity);
                if (disc != null)
                {
                    discount = disc.DiscountType == 1
                        ? subtotal * disc.DiscountValue / 100m
                        : disc.DiscountValue;
                    discountId = disc.DiscountID;
                    disc.UsedCount++;
                }
            }

            decimal pointsDiscount = 0;
            int pointsUsed = 0;
            if (usePoints > 0 && user.RewardPoints >= usePoints)
            {
                pointsDiscount = Math.Floor(usePoints / 100m) * 10000;
                decimal maxDiscount = subtotal * 0.3m;
                if (pointsDiscount > maxDiscount) pointsDiscount = maxDiscount;
                pointsUsed = usePoints;
            }

            // Giảm theo hạng thành viên (tính theo chi tiêu trong năm, reset mỗi năm)
            var year = MembershipTierHelper.CurrentYear;
            var yearStart = new DateTime(year, 1, 1);
            var nextYearStart = yearStart.AddYears(1);
            var yearlySpend = await _db.Orders.AsNoTracking()
                .Where(o => o.UserID == userId
                            && o.Status != (int)OrderStatus.Cancelled
                            && o.OrderDate >= yearStart && o.OrderDate < nextYearStart)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var tierAfterThisOrder = MembershipTierHelper.GetTierFromYearlySpend(yearlySpend + subtotal);
            var tierRate = MembershipTierHelper.GetDiscountRate(tierAfterThisOrder);
            var tierDiscount = MembershipTierHelper.ClampDiscount((subtotal + shipping) * tierRate, subtotal + shipping);

            decimal total = subtotal + shipping - discount - pointsDiscount - tierDiscount;

            // Bắt đầu Transaction để bảo toàn dữ liệu (tránh lỗi cấn trừ kho)
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    UserID = userId.Value,
                    DiscountID = discountId,
                    OrderDate = DateTime.Now,
                    ShippingAddress = shippingAddress,
                    ShippingProvince = shippingProvince,
                    ReceiverName = receiverName,
                    ReceiverPhone = receiverPhone,
                    TotalAmount = total,
                    Status = (int)OrderStatus.Pending, // 💡 Sử dụng Enum
                    RedemptionPoints = pointsUsed
                };

                _db.Orders.Add(order);
                await _db.SaveChangesAsync(); // Lưu để lấy OrderID

                // Tạo OrderDetails và TRỪ TỒN KHO
                foreach (var item in cartItems)
                {
                    _db.OrderDetails.Add(new OrderDetail
                    {
                        OrderID = order.OrderID,
                        SizeID = item.SizeID, // 💡 QUAN TRỌNG: Đã đổi thành SizeID
                        Quantity = item.Quantity,
                        UnitPrice = item.ProductSize.Product.Price // 💡 Lấy giá qua ProductSize
                    });

                    // 💡 BUSINESS LOGIC: Trừ số lượng tồn kho vật lý
                    item.ProductSize.StockQuantity -= item.Quantity;
                }

                user.RewardPoints -= pointsUsed;
                user.RewardPoints += (int)(total / 10000);

                // Cập nhật hạng thành viên theo năm hiện tại
                if (user.CustomerDetail != null)
                    user.CustomerDetail.MembershipTier = tierAfterThisOrder;

                _db.CartItems.RemoveRange(cartItems);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                RewardPointsHelper.SyncSession(HttpContext.Session, user.RewardPoints);
                HttpContext.Session.Remove("CheckoutCoupon");
                HttpContext.Session.Remove("CheckoutUsePoints");

                TempData["OrderCode"] = "NV-" + order.OrderID.ToString("D8");
                TempData["OrderTotal"] = total.ToString("N0");
                TempData["PointsEarned"] = (int)(total / 10000);

                return RedirectToAction("Success");
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Có lỗi xảy ra trong quá trình xử lý đơn hàng.";
                return RedirectToAction("Checkout");
            }
        }

        // ── Đặt hàng thành công ──
        public IActionResult Success()
        {
            if (TempData["OrderCode"] == null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        // ── Lịch sử đơn hàng ──
        public async Task<IActionResult> History()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var orders = await _db.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.ProductSize)     // 💡 Cập nhật Include
                        .ThenInclude(s => s.Product)     // 💡 Cập nhật Include
                .Where(o => o.UserID == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ── Chi tiết đơn hàng ──
        public async Task<IActionResult> Detail(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var order = await _db.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.ProductSize)     // 💡 Cập nhật Include
                        .ThenInclude(s => s.Product)     // 💡 Cập nhật Include
                .Include(o => o.Discount)
                .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == userId);

            if (order == null) return NotFound();
            return View(order);
        }

        // ── Hủy đơn hàng ──
        [HttpPost]
        public async Task<IActionResult> Cancel(int orderId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var order = await _db.Orders
                .Include(o => o.OrderDetails) // 💡 Phải Include Detail để biết đường hoàn kho
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.UserID == userId);

            // 💡 Sử dụng Enum OrderStatus.Pending thay vì số 0
            if (order != null && order.Status == (int)OrderStatus.Pending)
            {
                // 💡 Sử dụng Enum thay vì số 3 (Vì số 3 thường là Hoàn thành, 4 mới là Hủy)
                order.Status = (int)OrderStatus.Cancelled;

                // 💡 BUSINESS LOGIC: Trả lại hàng về kho khi khách tự hủy đơn
                foreach (var detail in order.OrderDetails)
                {
                    var size = await _db.ProductSizes.FindAsync(detail.SizeID);
                    if (size != null) size.StockQuantity += detail.Quantity;
                }

                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã hủy đơn hàng thành công!";
            }

            return RedirectToAction("History");
        }
    }
}