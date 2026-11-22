using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.Domain;
using RentalPortal.Web.Extensions;
using RentalPortal.Web.ViewModels;
using static System.Net.Mime.MediaTypeNames;

namespace RentalPortal.Web.Controllers;

[Authorize(Roles = Roles.Applicant)]
public class ResidencesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> List(int applicationId)
    {
        var application = await db.RentalApplications.FindAsync(applicationId);

        if (application is null)
            return NotFound();

        if (application.ApplicantId != User.GetUserId())
            return Forbid();

        return ViewComponent("ResidenceHistory",
            new { applicationId, editable = application.IsEditable });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int applicationId)
    {
        var application = await GetApplication(applicationId);

        if (RejectEdit(application) is { } rejection)
            return rejection;

        return PartialView("_ResidenceForm",
            new ResidenceFormViewModel { ApplicationId = applicationId });
    }

    [HttpPost]
    public async Task<IActionResult> Create(ResidenceFormViewModel model)
    {
        var application = await GetApplication(model.ApplicationId);

        var rejected = RejectEdit(application);

        if(rejected != null)
            return rejected;

        if (!ModelState.IsValid)
            return PartialView("_ResidenceForm", model);

        try
        {
            application!.AddResidence(
                model.Address!,
                model.LandlordName!,
                model.LandlordPhone!,
                model.MoveInDate!.Value,
                model.MoveOutDate!.Value);
        }
        catch (RuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return PartialView("_ResidenceForm", model);
        }

        await db.SaveChangesAsync();

        return Json(new { success = true });
    }

    private Task<RentalApplication?> GetApplication(int applicationId)
    {
        return db.RentalApplications
            .Include(a => a.Residences)
            .FirstOrDefaultAsync(a => a.Id == applicationId);
    }

    private IActionResult? RejectEdit(RentalApplication? application)
    {
        if (application is null)
            return NotFound();

        if (application.ApplicantId != User.GetUserId())
            return Forbid();

        if (!application.IsEditable)
            return Conflict();

        return null;
    }

}