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
using Clothing_Shop_Website.ViewModels;

namespace Clothing_Shop_Website.Controllers
{
    public class OrderController : Controller
    {
        private readonly AppDbContext _db;
        public OrderController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Checkout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = await _db.Users
                .Include(u => u.CustomerDetail)
                .FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return RedirectToAction("Login", "Account");

            var cartItems = await _db.CartItems
                .Include(c => c.ProductSize)
                    .ThenInclude(s => s.Product)
                        .ThenInclude(p => p.Category)
                .Where(c => c.UserID == userId)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("Index", "Cart");

            var savedCoupon = HttpContext.Session.GetString("CheckoutCoupon") ?? "";
            var savedPoints = HttpContext.Session.GetInt32("CheckoutUsePoints") ?? 0;

            var pricing = await OrderPricingHelper.CalculateForCheckoutAsync(
                _db, cartItems, user, savedCoupon, savedPoints);

            RewardPointsHelper.SyncSession(HttpContext.Session, user.RewardPoints);

            var model = new CheckoutViewModel
            {
                CartItems = cartItems,
                Addresses = await _db.UserAddresses.Where(a => a.UserID == userId).ToListAsync(),
                Subtotal = pricing.Subtotal,
                Shipping = pricing.Shipping,
                CouponDiscount = pricing.CouponDiscount,
                PointsDiscount = pricing.PointsDiscount,
                TierDiscount = pricing.TierDiscount,
                Total = pricing.Total,
                MembershipTier = pricing.MembershipTier,
                TierCssClass = MembershipTierHelper.GetTierCssClass(pricing.MembershipTier),
                TierDiscountPercent = pricing.TierDiscountPercent,
                RewardPoints = user.RewardPoints,
                UsePoints = pricing.PointsUsed,
                CouponCode = savedCoupon,
                CouponMessage = pricing.CouponMessage
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

            if (string.IsNullOrWhiteSpace(receiverName) || string.IsNullOrWhiteSpace(receiverPhone)
                || string.IsNullOrWhiteSpace(shippingAddress) || string.IsNullOrWhiteSpace(shippingProvince))
            {
                TempData["Error"] = "Vui lòng điền đầy đủ thông tin giao hàng.";
                return RedirectToAction("Checkout");
            }

            var user = await _db.Users
                .Include(u => u.CustomerDetail)
                .FirstOrDefaultAsync(u => u.UserID == userId.Value);
            if (user == null) return RedirectToAction("Login", "Account");

            if (user.CustomerDetail == null)
            {
                user.CustomerDetail = new CustomerDetail
                {
                    UserID = user.UserID,
                    RewardPoints = 0,
                    MembershipTier = "Thường"
                };
                _db.CustomerDetails.Add(user.CustomerDetail);
            }

            var cartItems = await _db.CartItems
                .Include(c => c.ProductSize)
                    .ThenInclude(s => s.Product)
                .Where(c => c.UserID == userId)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("Index", "Cart");

            foreach (var item in cartItems)
            {
                if (item.ProductSize.StockQuantity < item.Quantity)
                {
                    TempData["Error"] = $"Sản phẩm {item.ProductSize.Product.ProductName} (Size {item.ProductSize.SizeName}) không đủ tồn kho.";
                    return RedirectToAction("Checkout");
                }
            }

            var pricing = await OrderPricingHelper.CalculateForCheckoutAsync(
                _db, cartItems, user, discountCode, usePoints);

            if (usePoints > 0 && pricing.PointsUsed == 0)
            {
                TempData["Error"] = "Số điểm không hợp lệ hoặc vượt quá giới hạn (tối đa 30% đơn hàng).";
                return RedirectToAction("Checkout");
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                if (pricing.DiscountId.HasValue)
                {
                    var disc = await _db.Discounts.FindAsync(pricing.DiscountId.Value);
                    if (disc != null && disc.UsedCount < disc.Quantity)
                        disc.UsedCount++;
                }

                var order = new Order
                {
                    UserID = userId.Value,
                    DiscountID = pricing.DiscountId,
                    OrderDate = DateTime.Now,
                    ShippingAddress = shippingAddress.Trim(),
                    ShippingProvince = shippingProvince.Trim(),
                    Country = "Việt Nam",
                    ReceiverName = receiverName.Trim(),
                    ReceiverPhone = receiverPhone.Trim(),
                    TotalAmount = pricing.Total,
                    Status = (int)OrderStatus.Pending,
                    RedemptionPoints = pricing.PointsUsed
                };

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                foreach (var item in cartItems)
                {
                    _db.OrderDetails.Add(new OrderDetail
                    {
                        OrderID = order.OrderID,
                        SizeID = item.SizeID,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice ?? item.ProductSize.Product.Price
                    });
                    item.ProductSize.StockQuantity -= item.Quantity;
                }

                user.CustomerDetail.RewardPoints -= pricing.PointsUsed;
                user.CustomerDetail.RewardPoints += (int)(pricing.Total / 10000);
                user.CustomerDetail.MembershipTier = pricing.TierAfterOrder;

                _db.CartItems.RemoveRange(cartItems);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                RewardPointsHelper.SyncSession(HttpContext.Session, user.RewardPoints);
                HttpContext.Session.Remove("CheckoutCoupon");
                HttpContext.Session.Remove("CheckoutUsePoints");

                TempData["Success"] = $"Đặt hàng thành công! Mã đơn: NV-{order.OrderID:D8} · Tổng: {pricing.Total:N0}đ · Tích thêm {(int)(pricing.Total / 10000)} điểm.";

                return RedirectToAction("History");
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Có lỗi xảy ra khi đặt hàng. Vui lòng thử lại.";
                return RedirectToAction("Checkout");
            }
        }

        public async Task<IActionResult> History()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var orders = await _db.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.ProductSize)
                        .ThenInclude(s => s.Product)
                .Where(o => o.UserID == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var order = await _db.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.ProductSize)
                        .ThenInclude(s => s.Product)
                .Include(o => o.Discount)
                .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == userId);

            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int orderId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var order = await _db.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.UserID == userId);

            if (order != null && order.Status == (int)OrderStatus.Pending)
            {
                order.Status = (int)OrderStatus.Cancelled;

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
