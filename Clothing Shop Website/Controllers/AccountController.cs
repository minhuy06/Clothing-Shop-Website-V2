using Microsoft.AspNetCore.Mvc;

namespace Shop_Thoi_Trang.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login() => View();
        public IActionResult Profile() => View();
    }
}
