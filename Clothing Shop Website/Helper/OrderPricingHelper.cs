using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Enums;
using Clothing_Shop_Website.Models;
using Microsoft.EntityFrameworkCore;

namespace Clothing_Shop_Website.Helper
{
    public class OrderPricingResult
    {
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal CouponDiscount { get; set; }
        public decimal PointsDiscount { get; set; }
        public decimal TierDiscount { get; set; }
        public decimal Total { get; set; }
        public int? DiscountId { get; set; }
        public int PointsUsed { get; set; }
        public string MembershipTier { get; set; } = "Thường";
        public int TierDiscountPercent { get; set; }
        public string? CouponMessage { get; set; }
        public string TierAfterOrder { get; set; } = "Thường";
    }

    public static class OrderPricingHelper
    {
        public const decimal FreeShippingThreshold = 500_000m;
        public const decimal ShippingFee = 30_000m;

        public static decimal CalcSubtotal(IEnumerable<CartItem> items)
            => items.Sum(c => (c.UnitPrice ?? c.ProductSize.Product.Price) * c.Quantity);

        public static decimal CalcShipping(decimal subtotal)
            => subtotal >= FreeShippingThreshold ? 0 : ShippingFee;

        public static async Task<decimal> GetYearlySpendAsync(AppDbContext db, int userId)
        {
            var year = MembershipTierHelper.CurrentYear;
            var yearStart = new DateTime(year, 1, 1);
            var nextYearStart = yearStart.AddYears(1);
            return await db.Orders.AsNoTracking()
                .Where(o => o.UserID == userId
                            && o.Status != (int)OrderStatus.Cancelled
                            && o.OrderDate >= yearStart && o.OrderDate < nextYearStart)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
        }

        public static OrderPricingResult Calculate(
            IEnumerable<CartItem> cartItems,
            string? membershipTierFromDb,
            int availablePoints,
            string? discountCode,
            int usePoints,
            bool validateCoupon = false)
        {
            var items = cartItems.ToList();
            var subtotal = CalcSubtotal(items);
            var shipping = CalcShipping(subtotal);
            var billBase = subtotal + shipping;

            var result = new OrderPricingResult
            {
                Subtotal = subtotal,
                Shipping = shipping,
                MembershipTier = MembershipTierHelper.NormalizeTier(membershipTierFromDb),
                TierDiscountPercent = (int)(MembershipTierHelper.GetDiscountRate(membershipTierFromDb) * 100)
            };

            // Mã giảm giá
            if (!string.IsNullOrWhiteSpace(discountCode) && validateCoupon)
            {
                result.CouponMessage = "Mã sẽ được kiểm tra khi đặt hàng.";
            }

            // Điểm thưởng
            if (usePoints > 0 && availablePoints >= usePoints)
            {
                var ptsDisc = Math.Floor(usePoints / 100m) * 10000;
                var maxPts = subtotal * 0.3m;
                if (ptsDisc > maxPts)
                {
                    usePoints = (int)(Math.Floor(maxPts / 10000) * 100);
                    ptsDisc = Math.Floor(usePoints / 100m) * 10000;
                }
                result.PointsDiscount = ptsDisc;
                result.PointsUsed = usePoints;
            }

            // Giảm theo hạng (từ database)
            var tierRate = MembershipTierHelper.GetDiscountRate(result.MembershipTier);
            result.TierDiscount = MembershipTierHelper.ClampDiscount(billBase * tierRate, billBase);

            result.Total = Math.Max(0, billBase - result.CouponDiscount - result.PointsDiscount - result.TierDiscount);
            return result;
        }

        public static async Task<OrderPricingResult> CalculateForCheckoutAsync(
            AppDbContext db,
            IEnumerable<CartItem> cartItems,
            User user,
            string? discountCode,
            int usePoints)
        {
            var items = cartItems.ToList();
            var subtotal = CalcSubtotal(items);
            var shipping = CalcShipping(subtotal);
            var billBase = subtotal + shipping;

            var tier = MembershipTierHelper.NormalizeTier(user.CustomerDetail?.MembershipTier);
            var result = new OrderPricingResult
            {
                Subtotal = subtotal,
                Shipping = shipping,
                MembershipTier = tier,
                TierDiscountPercent = (int)(MembershipTierHelper.GetDiscountRate(tier) * 100)
            };

            if (!string.IsNullOrWhiteSpace(discountCode))
            {
                var code = discountCode.Trim().ToUpper();
                var disc = await db.Discounts.AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Code == code
                        && d.ExpirationDate >= DateTime.Now
                        && d.UsedCount < d.Quantity);
                if (disc != null)
                {
                    result.CouponDiscount = disc.DiscountType == 1
                        ? subtotal * disc.DiscountValue / 100m
                        : disc.DiscountValue;
                    if (result.CouponDiscount > subtotal) result.CouponDiscount = subtotal;
                    result.DiscountId = disc.DiscountID;
                    result.CouponMessage = disc.DiscountType == 1
                        ? $"Giảm {disc.DiscountValue:0.##}%"
                        : $"Giảm {disc.DiscountValue:N0}đ";
                }
                else
                    result.CouponMessage = "Mã không hợp lệ hoặc đã hết hạn.";
            }

            var pts = user.RewardPoints;
            if (usePoints > 0 && pts >= usePoints)
            {
                var ptsDisc = Math.Floor(usePoints / 100m) * 10000;
                var maxPts = subtotal * 0.3m;
                if (ptsDisc > maxPts)
                {
                    usePoints = (int)(Math.Floor(maxPts / 10000) * 100);
                    ptsDisc = Math.Floor(usePoints / 100m) * 10000;
                }
                result.PointsDiscount = ptsDisc;
                result.PointsUsed = usePoints;
            }

            var tierRate = MembershipTierHelper.GetDiscountRate(tier);
            result.TierDiscount = MembershipTierHelper.ClampDiscount(billBase * tierRate, billBase);

            var yearlySpend = await GetYearlySpendAsync(db, user.UserID);
            result.TierAfterOrder = MembershipTierHelper.GetTierFromYearlySpend(yearlySpend + subtotal);

            result.Total = Math.Max(0, billBase - result.CouponDiscount - result.PointsDiscount - result.TierDiscount);
            return result;
        }
    }
}
