using Clothing_Shop_Website.Data;
using Clothing_Shop_Website.Models;
using Clothing_Shop_Website.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
            var active = await _db.Advertisements
                .Where(a => a.IsActive && a.ImageUrl != null && a.ImageUrl != "")
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            var vm = new HomeIndexViewModel
            {
                BannerAds = active.Where(a => a.Position == "banner").ToList(),
                PopupAd = active.FirstOrDefault(a => a.Position == "popup"),
                SidebarAds = active.Where(a => a.Position == "sidebar").ToList()
            };
            return View(vm);
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