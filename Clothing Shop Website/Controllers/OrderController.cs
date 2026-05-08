using Microsoft.AspNetCore.Mvc;

namespace Shop_Thoi_Trang.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult Checkout() => View();
        public IActionResult History() => View();
    }
}
