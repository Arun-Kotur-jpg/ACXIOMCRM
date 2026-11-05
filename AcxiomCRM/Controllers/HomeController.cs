using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

public class HomeController : BaseController
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return RedirectToAction("Login", "Account");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewBag.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return View();
    }
}

