using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models;
using Microsoft.EntityFrameworkCore;

namespace Clothing_Shop_Website.Helper
{
    public static class AdvertisementHelper
    {
        private static readonly HashSet<string> SampleImagePaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/images/ads/summer-sale.jpg",
            "/images/ads/office-collection.jpg",
            "/images/ads/evening-dress.jpg"
        };

        public static bool IsSampleAdvertisement(Advertisement ad)
        {
            if (!string.IsNullOrEmpty(ad.ImageUrl))
            {
                if (ad.ImageUrl.StartsWith("/images/ads/", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (SampleImagePaths.Contains(ad.ImageUrl))
                    return true;
            }
            return false;
        }

        public static async Task<int> RemoveSampleAdvertisementsAsync(AppDbContext db)
        {
            var all = await db.Advertisements.ToListAsync();
            var samples = all.Where(IsSampleAdvertisement).ToList();
            if (samples.Count == 0) return 0;
            db.Advertisements.RemoveRange(samples);
            await db.SaveChangesAsync();
            return samples.Count;
        }
    }
}
