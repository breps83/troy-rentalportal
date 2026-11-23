using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.Extensions;
using RentalPortal.Web.ViewModels.Applications;

namespace RentalPortal.Web.ViewComponents;

public class ApplicationHistoryViewComponent(AppDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int applicationId)
    {
        if (!UserClaimsPrincipal.IsPropertyManager())
            return Content(string.Empty);

        var model = await db.ApplicationStatusHistory
            .AsNoTracking()
            .Where(c => c.RentalApplicationId == applicationId)
            .OrderBy(c => c.ChangedAtUtc).ThenBy(c => c.Id)
            .Select(c => new ReviewStatusChangeViewModel(
                c.FromStatus,
                c.ToStatus,
                $"{c.ChangedByUser.FirstName} {c.ChangedByUser.LastName}",
                c.ChangedAtUtc,
                c.Comment))
            .ToListAsync();

        return View(model);

    }
}