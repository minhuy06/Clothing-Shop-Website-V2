using System.Collections.Generic;
using Clothing_Shop_Website.Models;

namespace Clothing_Shop_Website.ViewModels
{
    public class HomeIndexViewModel
    {
        public IList<Advertisement> PopupAds { get; set; } = new List<Advertisement>();
    }
}
