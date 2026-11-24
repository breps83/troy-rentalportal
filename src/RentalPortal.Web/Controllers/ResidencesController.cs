using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.Domain;
using RentalPortal.Web.Extensions;
using RentalPortal.Web.ViewModels.Shared;
using RentalPortal.Web.ViewModels.Residences;

namespace RentalPortal.Web.Controllers;

/// <summary>
/// Manages residence history for rental applications.
/// </summary>
/// <param name="db">The application database context.</param>
[Authorize(Roles = Roles.Applicant)]
public class ResidencesController(AppDbContext db) : Controller
{
    /// <summary>
    /// Displays the residence history for a rental application.
    /// </summary>
    /// <param name="applicationId">The identifier of the rental application.</param>
    /// <returns>The residence history view component, or a not-found or forbidden result.</returns>
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

    /// <summary>
    /// Displays the form for creating a residence associated with an application.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <returns>An action result containing the residence form or a result rejecting the edit.</returns>
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

    /// <summary>
    /// Adds a residence to the application.
    /// </summary>
    /// <param name="model">The residence details to add.</param>
    /// <returns>A partial view when the model or residence details are invalid; otherwise, a JSON response indicating success.</returns>
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
                model.Street!,
                model.City!,
                model.State!,
                model.PostalCode!,
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

    /// <summary>
    /// Retrieves a residence for editing.
    /// </summary>
    /// <param name="id">The residence identifier.</param>
    /// <returns>A partial residence form, or a not found or rejection result.</returns>
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

    /// <summary>
    /// Updates an existing residence and its landlord and occupancy details.
    /// </summary>
    /// <param name="model">The residence values to apply to the rental application.</param>
    /// <returns>A partial view with validation errors when the update is rejected or invalid; otherwise, JSON indicating
    /// success.</returns>
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
                model.Street!,
                model.City!,
                model.State!,
                model.PostalCode!,
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

    /// <summary>
    /// Displays a confirmation for deleting a residence.
    /// </summary>
    /// <param name="id">The residence identifier.</param>
    /// <returns>A partial view containing the delete confirmation, or an action result when the residence cannot be edited.</returns>
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

    /// <summary>
    /// Deletes a residence from an application.
    /// </summary>
    /// <param name="id">The identifier of the residence to delete.</param>
    /// <returns>A JSON success result, or an action result that rejects the deletion.</returns>
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

    /// <summary>
    /// Retrieves a rental application and its residences.
    /// </summary>
    /// <remarks>Includes the application's residences in the returned result.</remarks>
    /// <param name="applicationId">The identifier of the rental application.</param>
    /// <returns>The matching rental application, or <see langword="null"/> when no application has the specified identifier.</returns>
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

    /// <summary>
    /// Determines whether an edit action should be rejected.
    /// </summary>
    /// <remarks>Returns a not-found result when the application does not exist, a forbidden result when the
    /// current user does not own it, or a confirmation partial view when the application is no longer
    /// editable.</remarks>
    /// <param name="application">The rental application to validate.</param>
    /// <returns>An action result when editing is rejected; otherwise, <see langword="null" />.</returns>
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