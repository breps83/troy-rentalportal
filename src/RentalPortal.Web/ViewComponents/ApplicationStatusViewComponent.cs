using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.ViewModels;
using RentalPortal.Web.ViewModels.Applications;

namespace RentalPortal.Web.ViewComponents;

public class ApplicationStatusViewComponent(AppDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int applicationId)
    {
        var model = await db.RentalApplications
            .AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => new ApplicationStatusViewModel
            {
                ApplicationId = a.Id,
                Status = a.Status,
                ApplicantName = a.Applicant.FirstName + " " + a.Applicant.LastName,
                CreatedAtUtc = a.CreatedAtUtc,
                SubmittedAtUtc = a.SubmittedAtUtc
            })
            .SingleAsync();

        return View(model);
    }
}