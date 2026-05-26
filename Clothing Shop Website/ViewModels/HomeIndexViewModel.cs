using System.Collections.Generic;
using Clothing_Shop_Website.Models;

namespace Clothing_Shop_Website.ViewModels
{
    public class HomeIndexViewModel
    {
        public List<Advertisement> BannerAds { get; set; } = new();
        public Advertisement? PopupAd { get; set; }
        public List<Advertisement> SidebarAds { get; set; } = new();
    }
}
