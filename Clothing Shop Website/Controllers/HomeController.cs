using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models;
using Clothing_Shop_Website.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Clothing_Shop_Website.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _db;

        public HomeController(ILogger<HomeController> logger, AppDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;
            var popupAds = await _db.Advertisements
                .Include(a => a.Product)
                .Where(a => a.IsActive && a.Position == "popup"
                    && a.ImageUrl != null && a.ImageUrl != ""
                    && (!a.StartDate.HasValue || a.StartDate <= now)
                    && (!a.EndDate.HasValue || a.EndDate >= now))
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            return View(new HomeIndexViewModel { PopupAds = popupAds });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
