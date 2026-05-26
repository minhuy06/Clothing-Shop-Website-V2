using System.Linq;
using System.Threading.Tasks;
using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clothing_Shop_Website.ViewComponents
{
    public class NavRewardPointsViewComponent : ViewComponent
    {
        private readonly AppDbContext _db;

        public NavRewardPointsViewComponent(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.Session.GetUserId();
            if (userId == null)
                return View(0);

            var points = await _db.Users.AsNoTracking()
                .Where(u => u.UserID == userId)
                .Select(u => u.RewardPoints)
                .FirstOrDefaultAsync();

            HttpContext.Session.SetInt32("Points", points);
            return View(points);
        }
    }
}
