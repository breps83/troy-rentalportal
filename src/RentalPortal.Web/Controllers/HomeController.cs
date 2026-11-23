using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RentalPortal.Web.ViewModels.Shared;

namespace RentalPortal.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? id)
    {
        return View(new ErrorViewModel { 
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = id
        });
    }
}
