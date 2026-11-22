using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.ViewModels.Residences;

namespace RentalPortal.Web.ViewComponents;

public class ResidenceHistoryViewComponent(AppDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int applicationId, bool editable)
    {
        var residences = await db.Residences
            .AsNoTracking()
            .Where(r => r.ApplicationId == applicationId)
            .OrderByDescending(r => r.MoveInDate)
            .ToListAsync();

        return View(new ResidenceHistoryViewModel(applicationId, editable, residences));
    }
}
