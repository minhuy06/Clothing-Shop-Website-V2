using System.Collections.Generic;
using Clothing_Shop_Website.Models;

namespace Clothing_Shop_Website.Models.ViewModels
{
    public class HomeIndexViewModel
    {
        public List<Advertisement> BannerAds { get; set; } = new();
        public List<Advertisement> PopupAds { get; set; } = new();
    }
}
