using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.ViewModels.Units;

namespace RentalPortal.Web.ViewComponents;

public class UnitListViewComponent(AppDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int propertyId)
    {
        var units = await db.Units
            .AsNoTracking()
            .Where(u => u.PropertyId == propertyId)
            .OrderBy(u => u.UnitNumber)
            .Select(u => new UnitRowViewModel
            {
                Id = u.Id,
                UnitNumber = u.UnitNumber,
                Bedrooms = u.Bedrooms,
                MonthlyRent = u.MonthlyRent,
                UnitType = u.UnitType.Name,
                UnitTypeIsActive = u.UnitType.IsActive
            })
            .ToListAsync();

        return View(new UnitListViewModel(propertyId, units));

    }
}
