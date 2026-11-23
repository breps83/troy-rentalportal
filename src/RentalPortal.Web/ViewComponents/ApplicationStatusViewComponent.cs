using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.Extensions;
using RentalPortal.Web.ViewModels.Applications;

namespace RentalPortal.Web.ViewComponents;

public class ApplicationStatusViewComponent(AppDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int applicationId)
    {
        var isPropertyManager = UserClaimsPrincipal.IsPropertyManager();

        var model = await db.RentalApplications
            .AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => new ApplicationStatusViewModel
            {
                ApplicationId = a.Id,
                Status = a.Status,
                CreatedAtUtc = a.CreatedAtUtc,
                SubmittedAtUtc = a.SubmittedAtUtc,
                LeaseStart = a.Lease != null ? a.Lease.StartDate : null,
                LeaseEnd = a.Lease != null ? a.Lease.EndDate : null,
                ReviewComment = a.StatusHistory
                                    .Where(i => i.ToStatus == a.Status)
                                    .OrderByDescending(c => c.ChangedAtUtc)
                                    .Select(c => c.Comment)
                                    .FirstOrDefault(),
                CanReview = isPropertyManager && a.Status == Enums.ApplicationStatus.Submitted
            })
            .SingleAsync();
        
        return View(model);
    }
}