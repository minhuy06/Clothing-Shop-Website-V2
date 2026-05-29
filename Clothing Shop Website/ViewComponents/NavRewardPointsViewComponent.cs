using Clothing_Shop_Website.Helper;
using Microsoft.AspNetCore.Mvc;

namespace Clothing_Shop_Website.ViewComponents
{
    public class NavRewardPointsViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var points = RewardPointsHelper.GetPoints(HttpContext.Session);
            return View(points);
        }
    }
}
