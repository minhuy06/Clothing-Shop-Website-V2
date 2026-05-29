using System;
using System.Linq;
using System.Threading.Tasks;
using Clothing_Shop_Website.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clothing_Shop_Website.ViewComponents
{
    public class NavSidebarAdsViewComponent : ViewComponent
    {
        private readonly AppDbContext _db;

        public NavSidebarAdsViewComponent(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var now = DateTime.Now;
            var ads = await _db.Advertisements
                .Include(a => a.Product)
                    .ThenInclude(p => p!.Category)
                .Where(a => a.IsActive && a.Position == "sidebar"
                    && a.ImageUrl != null && a.ImageUrl != ""
                    && a.DiscountValue > 0
                    && (!a.StartDate.HasValue || a.StartDate <= now)
                    && (!a.EndDate.HasValue || a.EndDate >= now))
                .OrderByDescending(a => a.CreatedDate)
                .Take(5)
                .ToListAsync();

            return View(ads);
        }
    }
}
