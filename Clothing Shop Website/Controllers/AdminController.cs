using Microsoft.AspNetCore.Mvc;

public class AdminController : Controller
{
    public IActionResult Dashboard()
    {
        return View();
    }

    public IActionResult Products()
    {
        return View();
    }

    public IActionResult Orders()   
    {
        return View();
    }
}