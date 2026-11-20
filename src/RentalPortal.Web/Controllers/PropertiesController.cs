using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.Domain;
using RentalPortal.Web.ViewModels;

namespace RentalPortal.Web.Controllers;

[Authorize(Roles = Roles.PropertyManager)]
public class PropertiesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var properties = await db.Properties
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new PropertyRowViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Address = $"{p.Street}, {p.City}, {p.State} {p.PostalCode}"
            })
            .ToListAsync();

        return View(properties);
    }
}
