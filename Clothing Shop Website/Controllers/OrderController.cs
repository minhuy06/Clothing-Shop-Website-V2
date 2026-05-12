using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models;

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
                .Include(c => c.Product)
                .Where(c => c.UserID == userId)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("Index", "Cart");

            // Lấy địa chỉ mặc định
            var addresses = await _db.UserAddresses
                .Where(a => a.UserID == userId)
                .ToListAsync();

            ViewBag.CartItems = cartItems;
            ViewBag.Addresses = addresses;
            ViewBag.Total = cartItems.Sum(c => c.Product.Price * c.Quantity);

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

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login", "Account");

            var cartItems = await _db.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserID == userId)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("Index", "Cart");

            // Tính tổng tiền
            decimal subtotal = cartItems.Sum(c => c.Product.Price * c.Quantity);
            decimal shipping = subtotal >= 500000 ? 0 : 30000;
            decimal discount = 0;

            // Kiểm tra mã giảm giá
            int? discountId = null;
            if (!string.IsNullOrEmpty(discountCode))
            {
                var disc = await _db.Discounts
                    .FirstOrDefaultAsync(d => d.Code == discountCode
                        && d.ExpirationDate >= DateTime.Now
                        && d.UsedCount < d.Quantity);
                if (disc != null)
                {
                    // DiscountType: 0 = VNĐ, 1 = phần trăm (theo model)
                    discount = disc.DiscountType == 1
                        ? subtotal * disc.DiscountValue / 100m
                        : disc.DiscountValue;
                    discountId = disc.DiscountID;
                    disc.UsedCount++;
                }
            }

            // Điểm thưởng
            decimal pointsDiscount = 0;
            int pointsUsed = 0;
            if (usePoints > 0 && user.RewardPoints >= usePoints)
            {
                pointsDiscount = Math.Floor(usePoints / 100m) * 10000;
                decimal maxDiscount = subtotal * 0.3m;
                if (pointsDiscount > maxDiscount) pointsDiscount = maxDiscount;
                pointsUsed = usePoints;
            }

            decimal total = subtotal + shipping - discount - pointsDiscount;

            // Tạo đơn hàng
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
                Status = 0,  // Chờ duyệt
                RedemptionPoints = pointsUsed
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // Tạo OrderDetails
            foreach (var item in cartItems)
            {
                _db.OrderDetails.Add(new OrderDetail
                {
                    OrderID = order.OrderID,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                });
            }

            // Trừ điểm + cộng điểm mới (10.000đ = 1 điểm)
            user.RewardPoints -= pointsUsed;
            user.RewardPoints += (int)(total / 10000);

            // Xóa giỏ hàng
            _db.CartItems.RemoveRange(cartItems);
            await _db.SaveChangesAsync();

            TempData["OrderCode"] = "NV-" + order.OrderID.ToString("D8");
            TempData["OrderTotal"] = total.ToString("N0");
            TempData["PointsEarned"] = (int)(total / 10000);

            return RedirectToAction("Success");
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
                    .ThenInclude(d => d.Product)
                .Include(o => o.Discount)
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
                    .ThenInclude(d => d.Product)
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
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.UserID == userId);

            if (order != null && order.Status == 0) // Chỉ hủy khi Chờ duyệt
            {
                order.Status = 3; // Đã hủy
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã hủy đơn hàng!";
            }

            return RedirectToAction("History");
        }
    }
}