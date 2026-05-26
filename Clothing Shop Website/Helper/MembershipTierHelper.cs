using System;

namespace Clothing_Shop_Website.Helper
{
    public static class MembershipTierHelper
    {
        // Quy đổi hạng theo tổng chi tiêu trong NĂM (reset mỗi năm)
        public const decimal BronzeThreshold = 500_000m;   // Đồng
        public const decimal SilverThreshold = 2_000_000m; // Bạc
        public const decimal GoldThreshold = 10_000_000m;  // Vàng

        public static string GetTierFromYearlySpend(decimal yearlySpend)
        {
            if (yearlySpend >= GoldThreshold) return "Vàng";
            if (yearlySpend >= SilverThreshold) return "Bạc";
            if (yearlySpend >= BronzeThreshold) return "Đồng";
            return "Thường";
        }

        public static decimal GetDiscountRate(string? tier)
        {
            return tier switch
            {
                "Đồng" => 0.05m,
                "Bạc" => 0.10m,
                "Vàng" => 0.15m,
                _ => 0m
            };
        }

        public static decimal ClampDiscount(decimal discount, decimal baseAmount)
        {
            if (discount < 0) return 0;
            if (discount > baseAmount) return baseAmount;
            return discount;
        }

        public static int CurrentYear => DateTime.Now.Year;
    }
}

