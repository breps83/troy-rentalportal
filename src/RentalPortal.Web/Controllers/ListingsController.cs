using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.ViewModels;

namespace RentalPortal.Web.Controllers;

[Authorize]
public class ListingsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var units = await db.Units
            .AsNoTracking()
            .OrderBy(u => u.Property.Name)
            .ThenBy(u => u.UnitNumber)
            .Select(u => new AvailableUnitViewModel
            {
                Id = u.Id,
                PropertyName = u.Property.Name,
                Address = u.Property.Street + ", " + u.Property.City + ", " + u.Property.State,
                UnitNumber = u.UnitNumber,
                UnitType = u.UnitType.Name,
                Bedrooms = u.Bedrooms,
                MonthlyRent = u.MonthlyRent
            })
            .ToListAsync();

        return View(units);
    }
}
