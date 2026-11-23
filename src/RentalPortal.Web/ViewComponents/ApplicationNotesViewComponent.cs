using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.Extensions;
using RentalPortal.Web.ViewModels.Applications;

namespace RentalPortal.Web.ViewComponents;

public class ApplicationNotesViewComponent(AppDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int applicationId)
    {
        if (!UserClaimsPrincipal.IsPropertyManager())
            return Content(string.Empty);

        var userId = UserClaimsPrincipal.GetUserId();

        var notes = await db.ApplicationNotes
            .AsNoTracking()
            .Where(n => n.RentalApplicationId == applicationId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ThenByDescending(n => n.Id)
            .Select(n => new ApplicationNoteViewModel(
                n.Id,
                n.RentalApplicationId,
                n.Note,
                n.CreatedByUser.FirstName + " " + n.CreatedByUser.LastName,
                n.CreatedAtUtc,
                n.UpdatedByUser == null
                    ? null
                    : n.UpdatedByUser.FirstName + " " + n.UpdatedByUser.LastName,
                n.UpdatedAtUtc,
                n.CreatedById == userId))
            .ToListAsync();

        ViewData["ApplicationId"] = applicationId;

        return View(notes);
    }
}
