using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPortal.Web.ViewModels.Shared;

namespace RentalPortal.Web.Controllers;

/// <summary>
/// Handles requests for the portal's home page and error responses.
/// </summary>
[Authorize]
public class HomeController : Controller
{
    /// <summary>
    /// Returns the home page view.
    /// </summary>
    /// <returns>The view result for the home page.</returns>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Displays the error page.
    /// </summary>
    /// <param name="id">The HTTP status code associated with the error.</param>
    /// <returns>An action result that renders the error view.</returns>
    [AllowAnonymous]
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
