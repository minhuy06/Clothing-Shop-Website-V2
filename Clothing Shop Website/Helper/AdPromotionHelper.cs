using System;
using Clothing_Shop_Website.Models;

namespace Clothing_Shop_Website.Helper
{
    public static class AdPromotionHelper
    {
        public static bool IsPromotionActive(Advertisement? ad, DateTime? at = null)
        {
            if (ad == null || !ad.IsActive || ad.DiscountValue <= 0 || !ad.ProductID.HasValue)
                return false;

            var now = at ?? DateTime.Now;
            if (ad.StartDate.HasValue && now < ad.StartDate.Value)
                return false;
            if (ad.EndDate.HasValue && now > ad.EndDate.Value)
                return false;

            return true;
        }

        public static decimal GetSalePrice(decimal originalPrice, Advertisement? ad, DateTime? at = null)
        {
            if (!IsPromotionActive(ad, at))
                return originalPrice;

            decimal discount = ad!.DiscountType == 1
                ? originalPrice * ad.DiscountValue / 100m
                : ad.DiscountValue;

            if (discount < 0) discount = 0;
            if (discount > originalPrice) discount = originalPrice;

            return originalPrice - discount;
        }
    }
}
