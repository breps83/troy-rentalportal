using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.Extensions;
using RentalPortal.Web.ViewModels.Listings;

namespace RentalPortal.Web.Controllers;

[Authorize]
public class ListingsController(AppDbContext db, TimeProvider time) : Controller
{
    public async Task<IActionResult> Index()
    {
        var today = time.GetToday();

        var units = await db.Units
            .AsNoTracking()
            .Where(u => !u.Leases.Any(l => l.StartDate <= today && today <= l.EndDate))
            .OrderBy(u => u.Property.Name)
            .ThenBy(u => u.UnitNumber)
            .Select(u => new AvailableUnitViewModel
            {
                Id = u.Id,
                PropertyName = u.Property.Name,
                Address = u.Property.Street + ", " + u.Property.City + ", " + u.Property.State,
                UnitNumber = u.UnitNumber,
                UnitType = u.UnitType.Name,
                UnitTypeIsActive = u.UnitType.IsActive,
                Bedrooms = u.Bedrooms,
                MonthlyRent = u.MonthlyRent
            })
            .ToListAsync();

        return View(units);
    }
}
