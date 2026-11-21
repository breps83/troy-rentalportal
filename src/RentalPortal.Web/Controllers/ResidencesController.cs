using Microsoft.AspNetCore.Mvc;

namespace RentalPortal.Web.Controllers;

public class ResidencesController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
