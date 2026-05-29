using System.Collections.Generic;
using Clothing_Shop_Website.Models;

namespace Clothing_Shop_Website.ViewModels
{
    public class CheckoutViewModel
    {
        public List<CartItem> CartItems { get; set; } = new();
        public List<UserAddress> Addresses { get; set; } = new();

        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal CouponDiscount { get; set; }
        public decimal PointsDiscount { get; set; }
        public decimal TierDiscount { get; set; }
        public decimal Total { get; set; }

        public string MembershipTier { get; set; } = "Thường";
        public string TierCssClass { get; set; } = "tier-thuong";
        public int TierDiscountPercent { get; set; }

        public int RewardPoints { get; set; }
        public int UsePoints { get; set; }
        public string CouponCode { get; set; } = "";
        public string? CouponMessage { get; set; }
    }
}
