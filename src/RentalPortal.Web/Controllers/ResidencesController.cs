using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.Domain;
using RentalPortal.Web.Extensions;
using RentalPortal.Web.ViewModels.Shared;
using RentalPortal.Web.ViewModels.Residences;

namespace RentalPortal.Web.Controllers;

[Authorize(Roles = Roles.Applicant)]
public class ResidencesController(AppDbContext db) : Controller
{
    [HttpGet]
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
        var application = await GetApplicationAsync(applicationId);

        var rejected = RejectEditActionResult(application);

        if (rejected != null)
            return rejected;

        return PartialView("_ResidenceForm",
            new ResidenceFormViewModel { ApplicationId = applicationId });
    }

    [HttpPost]
    public async Task<IActionResult> Create(ResidenceFormViewModel model)
    {
        var application = await GetApplicationAsync(model.ApplicationId);

        var rejected = RejectEditActionResult(application);

        if (rejected != null)
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

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var application = await GetApplicationByResidenceIdAsync(id);

        if (application is null)
            return NotFound();

        var rejected = RejectEditActionResult(application);

        if (rejected != null)
            return rejected;

        var residence = application!.Residences.Single(r => r.Id == id);

        return PartialView("_ResidenceForm",
            ResidenceFormViewModel.From(application.Id, residence));

    }

    [HttpPost]
    public async Task<IActionResult> Edit(ResidenceFormViewModel model)
    {
        var application = await GetApplicationByResidenceIdAsync(model.Id);

        var rejected = RejectEditActionResult(application);

        if (rejected != null)
            return rejected;

        if (!ModelState.IsValid)
            return PartialView("_ResidenceForm", model);

        try
        {
            application!.UpdateResidence(
                model.Id,
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

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var application = await GetApplicationByResidenceIdAsync(id);

        var rejected = RejectEditActionResult(application);

        if (rejected != null)
            return rejected;

        var residence = application!.Residences.Single(r => r.Id == id);

        return PartialView("_ConfirmDelete",
            new ConfirmDeleteViewModel
            {
                Title = "Delete residence",
                Message = $"Delete {residence.Address} from history?",
                ActionUrl = Url.Action(nameof(Delete), new { id })!
            });

    }

    [HttpPost, ActionName(nameof(Delete))]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var application = await GetApplicationByResidenceIdAsync(id);

        var rejected = RejectEditActionResult(application);

        if (rejected != null)
            return rejected;

        application!.RemoveResidence(id);

        await db.SaveChangesAsync();

        return Json(new { success = true });
    }

    private Task<RentalApplication?> GetApplicationAsync(int applicationId)
    {
        return db.RentalApplications
            .Include(a => a.Residences)
            .FirstOrDefaultAsync(a => a.Id == applicationId);
    }

    private Task<RentalApplication?> GetApplicationByResidenceIdAsync(int residenceId)
    {
        return db.RentalApplications
            .Include(a => a.Residences)
            .FirstOrDefaultAsync(a =>
                a.Residences.Any(r => r.Id == residenceId));
    }

    private IActionResult? RejectEditActionResult(RentalApplication? application)
    {
        if (application is null)
            return NotFound();

        if (application.ApplicantId != User.GetUserId())
            return Forbid();

        if (!application.IsEditable)
        {
            // shown in the modal instead of the generic 409 message
            return PartialView("_ConfirmDelete", new ConfirmDeleteViewModel
            {
                Title = "Residence history",
                Message = string.Empty,
                ActionUrl = string.Empty,
                Error = $"This application is {application.Status} and can no longer be edited."
            });
        }

        return null;
    }

}