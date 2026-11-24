using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.Domain;
using RentalPortal.Web.Enums;
using RentalPortal.Web.Extensions;
using RentalPortal.Web.Services;
using RentalPortal.Web.ViewModels.Applications;
using RentalPortal.Web.ViewModels.Shared;

namespace RentalPortal.Web.Controllers;

/// <summary>
/// Handles rental application workflows for applicants and property managers.
/// </summary>
/// <param name="db">Provides access to application, property, unit, residence, and note data.</param>
/// <param name="applicationService">Coordinates application submission, withdrawal, and review operations.</param>
/// <param name="userManager">Retrieves the authenticated user's account.</param>
/// <param name="time">Provides the current UTC time for application note operations.</param>
[Authorize(Roles = $"{Roles.Applicant},{Roles.PropertyManager}")]
public class ApplicationsController(
    AppDbContext db,
    RentalApplicationService applicationService,
    UserManager<ApplicationUser> userManager,
    TimeProvider time) : Controller
{
    /// <summary>
    /// Displays rental applications, optionally filtered by status and property.
    /// </summary>
    /// <remarks>Property managers receive applications across properties; other users receive only their own
    /// applications.</remarks>
    /// <param name="status">The application status used to filter the results.</param>
    /// <param name="propertyId">The property identifier used to filter the results.</param>
    /// <returns>A view containing the matching applications and available property filters.</returns>
    public async Task<IActionResult> Index(ApplicationStatus? status, int? propertyId)
    {
        var query = db.RentalApplications.AsNoTracking();

        if (!User.IsPropertyManager())
        {
            var userId = User.GetUserId();

            query = query.Where(a => a.ApplicantId == userId);
        }

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (propertyId.HasValue)
            query = query.Where(a => a.Unit.PropertyId == propertyId.Value);

        var applications = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => new ApplicationRowViewModel
            {
                Id = a.Id,
                PropertyName = a.Unit.Property.Name,
                UnitNumber = a.Unit.UnitNumber,
                ApplicantName = a.Applicant.FirstName + " " + a.Applicant.LastName,
                Status = a.Status,
                CreatedAtUtc = a.CreatedAtUtc,
                SubmittedAtUtc = a.SubmittedAtUtc
            })
            .ToListAsync();

        var properties = await db.Properties
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem(p.Name, p.Id.ToString()))
            .ToListAsync();

        return View(new ApplicationListViewModel
        {
            Status = status,
            PropertyId = propertyId,
            Properties = properties,
            Applications = applications
        });
    }

    /// <summary>
    /// Starts a rental application for the specified unit.
    /// </summary>
    /// <param name="unitId">The identifier of the unit for which to start an application.</param>
    /// <returns>A challenge if no authenticated user is available, a redirect to the listings page when the unit is unavailable,
    /// or a redirect to the application details.</returns>
    [HttpPost, Authorize(Roles = Roles.Applicant)]
    public async Task<IActionResult> Apply(int unitId)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
            return Challenge();

        var application = await applicationService.StartApplicationAsync(unitId, user);

        if (application is null)
        {
            // show error in alert
            TempData["Error"] = "This unit is not available.";
            return RedirectToAction("Index", "Listings");
        }

        return RedirectToAction(nameof(Details), new { id = application.Id });
    }

    /// <summary>
    /// Displays the application details form.
    /// </summary>
    /// <param name="id">The application identifier.</param>
    /// <param name="section">The optional application section to display.</param>
    /// <returns>The application details view, a not-found result when the application does not exist, or a forbidden result when
    /// the application is not viewable.</returns>
    [HttpGet]
    public async Task<IActionResult> Details(int id, ApplicationSection? section)
    {
        var application = await GetApplicationAsync(id);

        if (application is null)
            return NotFound();

        if (!application.UserCanView(User))
            return Forbid();

        return View(CreateForm(application, section ?? DefaultSection(application, application.UserCanEdit(User))));
    }

    /// <summary>
    /// Processes navigation and updates for a rental application form.
    /// </summary>
    /// <remarks>Navigates backward, updates applicant information or residence history, or submits the
    /// application according to the command and current section.</remarks>
    /// <param name="id">The application identifier.</param>
    /// <param name="model">The application form data and current section.</param>
    /// <param name="command">The form action to perform.</param>
    /// <returns>An action result containing the next form section, submission result, validation response, or an error response.</returns>
    [HttpPost]
    public async Task<IActionResult> Details(int id, ApplicationFormViewModel model, string command)
    {
        var application = await GetApplicationAsync(id);

        if (application is null)
            return NotFound();

        var section = model.CurrentSection;

        IActionResult? rejected;

        switch (command)
        {
            case ApplicationFormViewModel.Back:

                if (!application.UserCanView(User))
                    return Forbid();

                return RedirectToAction(nameof(Details), new { id, section = section.PreviousSection() });

            case ApplicationFormViewModel.Continue when section == Enums.ApplicationSection.Info:

                rejected = RejectEditActionResult(application);

                if (rejected != null)
                    return rejected;

                if (!IsValid(ModelState, nameof(model.ApplicantInfo)))
                    return View(CreateForm(application, section, model.ApplicantInfo));

                var applicantInfo = model.ApplicantInfo;

                application.UpdateApplicant(
                    applicantInfo.FirstName!,
                    applicantInfo.LastName!,
                    applicantInfo.Phone!,
                    applicantInfo.Email!,
                    applicantInfo.Street!,
                    applicantInfo.City!,
                    applicantInfo.State!,
                    applicantInfo.PostalCode!);

                await db.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id, section = section.NextSection() });

            case ApplicationFormViewModel.Continue when section == ApplicationSection.ResidenceHistory:

                rejected = RejectEditActionResult(application);

                if (rejected != null)
                    return rejected;

                try
                {
                    application.SetResidenceHistoryCompleted();
                }
                catch (RuleException ex)
                {
                    TempData["Error"] = ex.Message;

                    return RedirectToAction(nameof(Details), new { id, section });
                }

                await db.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id, section = section.NextSection() });

            case ApplicationFormViewModel.Submit when section == ApplicationSection.Summary:

                rejected = RejectEditActionResult(application);

                if (rejected != null)
                    return rejected;

                var result = await applicationService.SubmitAsync(application, User.GetUserId());

                if (!result.Succeeded)
                {
                    TempData["Error"] = result.Error;

                    return RedirectToAction(nameof(Details), new { id, section });
                }

                TempData["Success"] = "Thank you. Your application has been submitted!";

                return RedirectToAction(nameof(Details), new { id });

            default:
                return BadRequest();
        }

    }

    /// <summary>
    /// Displays a confirmation for withdrawing a rental application.
    /// </summary>
    /// <remarks>Withdrawal is unavailable when the application is no longer eligible.</remarks>
    /// <param name="id">The rental application identifier.</param>
    /// <returns>A not-found result if the application does not exist, a forbidden result if the user does not own the
    /// application, or a partial view for withdrawal confirmation.</returns>
    [HttpGet, Authorize(Roles = Roles.Applicant)]
    public async Task<IActionResult> Withdraw(int id)
    {
        var application = await db.RentalApplications.FindAsync(id);

        if (application is null)
            return NotFound();

        if (application.ApplicantId != User.GetUserId())
            return Forbid();

        string? error = null;

        if (!application.CanWithdraw)
            error = $"This application is {application.Status} and can no longer be withdrawn.";

        return PartialView("_ConfirmWithdraw", WithdrawConfirmation(id, error));
    }

    /// <summary>
    /// Withdraws the applicant's rental application.
    /// </summary>
    /// <param name="id">The ID of the rental application to withdraw.</param>
    /// <returns>An action result indicating whether the application was withdrawn.</returns>
    [HttpPost, Authorize(Roles = Roles.Applicant)]
    [ActionName(nameof(Withdraw))]
    public async Task<IActionResult> WithdrawConfirmed(int id)
    {
        var application = await db.RentalApplications.FindAsync(id);

        if (application is null)
            return NotFound();

        if (application.ApplicantId != User.GetUserId())
            return Forbid();

        var result = await applicationService.WithdrawApplicationAsync(
            application, User.GetUserId());

        if (!result.Succeeded)
            return PartialView("_ConfirmWithdraw", WithdrawConfirmation(id, result.Error));

        TempData["Success"] = "Your application was withdrawn.";

        return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
    }

    /// <summary>
    /// Displays the review form for a rental application.
    /// </summary>
    /// <remarks>Only property managers may access this action.</remarks>
    /// <param name="id">The identifier of the rental application.</param>
    /// <returns>The not-found result when the application does not exist; otherwise, the partial review form.</returns>
    [HttpGet, Authorize(Roles = Roles.PropertyManager)]
    public async Task<IActionResult> Review(int id)
    {
        var application = await db.RentalApplications.FindAsync(id);

        if (application is null)
            return NotFound();

        if (!application.CanReview)
        {
            ModelState.AddModelError(string.Empty,
                $"This application is {application.Status} and can no longer be reviewed.");
        }

        return PartialView("_ReviewForm", new ReviewViewModel
        {
            ApplicationId = id,
            CanReview = application.CanReview
        });
    }

    /// <summary>
    /// Reviews a rental application and records the decision and associated notes.
    /// </summary>
    /// <param name="model">The rental application review details.</param>
    /// <returns>A not-found result when the application is missing; a partial review form with validation errors; or a success
    /// result.</returns>
    [HttpPost, Authorize(Roles = Roles.PropertyManager)]
    public async Task<IActionResult> Review(ReviewViewModel model)
    {
        var application = await db.RentalApplications
                                    .FindAsync(model.ApplicationId);
        
        if (application is null)
            return NotFound();
        
        if (!ModelState.IsValid)
            return PartialView("_ReviewForm", model);
        
        var result = await applicationService.ReviewApplicationAsync(
            application, 
            model.ReviewStatus!.Value, 
            model.Comment,
            model.InternalNote,
            User.GetUserId());
        
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);

            return PartialView("_ReviewForm", model);
        }

        return Json(new { success = true });
    }

    /// <summary>
    /// Displays the status of a rental application.
    /// </summary>
    /// <param name="id">The ID of the rental application.</param>
    /// <returns>A view component containing the application status, or a not-found or forbidden result.</returns>
    [HttpGet]
    public async Task<IActionResult> Status(int id)
    {
        var application = await db.RentalApplications.FindAsync(id);

        if (application is null)        
            return NotFound();
        
        return application.UserCanView(User) ? ViewComponent("ApplicationStatus", 
            new { applicationId = id }) : Forbid();
    }

    /// <summary>
    /// Displays the history for a rental application.
    /// </summary>
    /// <param name="id">The rental application identifier.</param>
    /// <returns>A not-found response if the application does not exist, a forbidden response if the user cannot view it, or the
    /// application history view component.</returns>
    [HttpGet]
    public async Task<IActionResult> History(int id)
    {
        var application = await db.RentalApplications.FindAsync(id);

        if (application is null)
            return NotFound();

        return application.UserCanView(User) ? ViewComponent("ApplicationHistory",
            new { applicationId = id }) : Forbid();
    }

    /// <summary>
    /// Displays the notes for a rental application.
    /// </summary>
    /// <param name="id">The ID of the rental application.</param>
    /// <returns>An action result that displays the application's notes, or a not-found result if the application does not exist.</returns>
    [HttpGet, Authorize(Roles = Roles.PropertyManager)]
    public async Task<IActionResult> Notes(int id)
    {
        if (!await db.RentalApplications.AnyAsync(a => a.Id == id))
            return NotFound();

        return ViewComponent("ApplicationNotes", new { applicationId = id });
    }

    /// <summary>
    /// Displays the form for adding a note to a rental application.
    /// </summary>
    /// <param name="id">The ID of the rental application.</param>
    /// <returns>A partial view containing the note form, or a not-found result if the application does not exist.</returns>
    [HttpGet, Authorize(Roles = Roles.PropertyManager)]
    public async Task<IActionResult> AddNote(int id)
    {
        if (!await db.RentalApplications.AnyAsync(a => a.Id == id))
            return NotFound();

        return PartialView("_NoteForm", new ApplicationNoteFormViewModel { ApplicationId = id });
    }

    /// <summary>
    /// Adds a note to the specified rental application.
    /// </summary>
    /// <param name="model">The application note form data.</param>
    /// <returns>A not-found result when the application does not exist, a partial view containing validation errors, or a JSON
    /// success result.</returns>
    [HttpPost, Authorize(Roles = Roles.PropertyManager)]
    public async Task<IActionResult> AddNote(ApplicationNoteFormViewModel model)
    {
        if (!await db.RentalApplications.AnyAsync(a => a.Id == model.ApplicationId))
            return NotFound();

        if (!ModelState.IsValid)
            return PartialView("_NoteForm", model);

        try
        {
            db.ApplicationNotes.Add(ApplicationNote.Create(
                model.ApplicationId,
                User.GetUserId(),
                model.Note!,
                time.GetUtcNow().UtcDateTime));
        }
        catch(RuleException ex)
        {
            ModelState.AddModelError(nameof(model.Note), ex.Message);

            return PartialView("_NoteForm", model);
        }

        await db.SaveChangesAsync();

        return Json(new { success = true });
    }

    /// <summary>
    /// Displays the form for editing an application note.
    /// </summary>
    /// <param name="id">The identifier of the application note.</param>
    /// <returns>The note form partial view, or a not-found or forbidden result when the note is unavailable or cannot be edited.</returns>
    [HttpGet, Authorize(Roles = Roles.PropertyManager)]
    public async Task<IActionResult> EditNote(int id)
    {
        var note = await db.ApplicationNotes.FindAsync(id);

        if (note is null)
            return NotFound();

        if (!note.IsAuthor(User.GetUserId()))
            return Forbid();

        var model = new ApplicationNoteFormViewModel
        {
            ApplicationId = note.RentalApplicationId,
            NoteId = note.Id,
            Note = note.Note
        };

        return PartialView("_NoteForm", model);
    }

    /// <summary>
    /// Edits an application note created by the current property manager.
    /// </summary>
    /// <param name="model">The note form containing the note identifier and revised text.</param>
    /// <returns>A partial note form when the note is unavailable or the submitted text is invalid; a not found or forbidden
    /// result when access requirements fail; or a success response after the note is saved.</returns>
    [HttpPost, Authorize(Roles = Roles.PropertyManager)]
    public async Task<IActionResult> EditNote(ApplicationNoteFormViewModel model)
    {
        var note = await db.ApplicationNotes.FindAsync(model.NoteId);

        if (note is null)
            return NotFound();

        if (!note.IsAuthor(User.GetUserId()))
            return Forbid();

        if (!ModelState.IsValid)
            return PartialView("_NoteForm", model);

        try
        {
            note.Edit(User.GetUserId(), model.Note!, time.GetUtcNow().UtcDateTime);
        }
        catch (RuleException ex)
        {
            ModelState.AddModelError(nameof(model.Note), ex.Message);

            return PartialView("_NoteForm", model);
        }

        await db.SaveChangesAsync();

        return Json(new { success = true });
    }

    /// <summary>
    /// Displays the confirmation view for deleting an application note.
    /// </summary>
    /// <remarks>Only property managers can access this action.</remarks>
    /// <param name="id">The identifier of the note to delete.</param>
    /// <returns>A partial confirmation view; a not-found result if the note does not exist; or a forbidden result if the current
    /// user is not the note author.</returns>
    [HttpGet, Authorize(Roles = Roles.PropertyManager)]
    public async Task<IActionResult> DeleteNote(int id)
    {
        var note = await db.ApplicationNotes.FindAsync(id);

        if (note is null)
            return NotFound();

        if (!note.IsAuthor(User.GetUserId()))
            return Forbid();

        return PartialView("_ConfirmDelete", new ConfirmDeleteViewModel
        {
            Title = "Delete note",
            Message = "Delete this note?",
            ActionUrl = Url.Action(nameof(DeleteNote), new { id })!
        });
    }

    /// <summary>
    /// Deletes an application note owned by the current property manager.
    /// </summary>
    /// <remarks>The note must exist and the current user must be its author.</remarks>
    /// <param name="id">The identifier of the application note to delete.</param>
    /// <returns>The not-found, forbidden, or successful JSON result of the delete operation.</returns>
    [HttpPost, Authorize(Roles = Roles.PropertyManager)]
    [ActionName(nameof(DeleteNote))]
    public async Task<IActionResult> DeleteNoteConfirmed(int id)
    {
        var note = await db.ApplicationNotes.FindAsync(id);

        if (note is null)
            return NotFound();

        if (!note.IsAuthor(User.GetUserId()))
            return Forbid();

        db.ApplicationNotes.Remove(note);

        await db.SaveChangesAsync();

        return Json(new { success = true });
    }

    /// <summary>
    /// Creates the confirmation view model for withdrawing an application.
    /// </summary>
    /// <param name="id">The identifier of the application to withdraw.</param>
    /// <param name="error">An optional error message to display in the confirmation view.</param>
    /// <returns>A view model containing the withdrawal confirmation text and action URL.</returns>
    private ConfirmDeleteViewModel WithdrawConfirmation(int id, string? error) =>
        new ()
        {
            Title = "Withdraw application",
            Message = "Are you sure you want to withdraw this application? This can't be undone.",
            ActionUrl = Url.Action(nameof(Withdraw), new { id })!,
            Error = error
        };

    /// <summary>
    /// Retrieves a rental application by identifier.
    /// </summary>
    /// <remarks>Includes the associated unit and property, as well as residence history.</remarks>
    /// <param name="id">The rental application identifier.</param>
    /// <returns>A task that represents the asynchronous operation; the rental application when found, or <see langword="null" />
    /// when no matching application exists.</returns>
    private Task<RentalApplication?> GetApplicationAsync(int id) =>
        db.RentalApplications
            .Include(a => a.Unit)
            .ThenInclude(u => u.Property)
            .Include(a => a.Residences)
            .FirstOrDefaultAsync(a => a.Id == id);

    /// <summary>
    /// Determines whether the current user may reject an edit to the specified rental application.
    /// </summary>
    /// <remarks>The application owner must match the current user, and the application must remain
    /// editable.</remarks>
    /// <param name="application">The rental application to validate.</param>
    /// <returns>A forbid or details redirect result when the edit is rejected; otherwise, null.</returns>
    private IActionResult? RejectEditActionResult(RentalApplication application)
    {
        if (application.ApplicantId != User.GetUserId())
            return Forbid();

        if (!application.IsEditable)
        {
            // appears in alert
            TempData["Error"] =
                $"This application is {application.Status} and can no longer be edited.";

            return RedirectToAction(nameof(Details), new { id = application.Id });
        }

        return null;
    }

    /// <summary>
    /// Creates an application form view model for the specified application and section.
    /// </summary>
    /// <param name="application">The rental application represented by the form.</param>
    /// <param name="section">The application section currently displayed in the form.</param>
    /// <param name="applicantInfo">The applicant information to use, or <see langword="null"/> to create it from the application.</param>
    /// <returns>An application form view model configured for the current user and application section.</returns>
    private ApplicationFormViewModel CreateForm(
        RentalApplication application,
        ApplicationSection section,
        ApplicantInfoViewModel? applicantInfo = null)
    {
        var isOwner = application.ApplicantId == User.GetUserId();

        var isEditable = application.UserCanEdit(User);

        var form = new ApplicationFormViewModel
        {
            Id = application.Id,
            CurrentSection = section,
            ApplicantInfo = applicantInfo ?? ApplicantInfoViewModel.From(application),
            Status = application.Status,
            PropertyName = application.Unit.Property.Name,
            UnitNumber = application.Unit.UnitNumber,
            MonthlyRent = application.Unit.MonthlyRent,
            IsOwner = isOwner,
            IsEditable = isEditable,
            CanSubmit = isEditable && application.CanSubmit,
            CanWithdraw = isOwner && application.CanWithdraw,
            ApplicantInfoCompleted = application.ApplicantInfoCompleted,
            ResidenceHistoryCompleted = application.ResidenceHistoryCompleted
        };

        form.ApplicantInfo?.IsEditable = form.IsSectionEditable(ApplicationSection.Info);

        return form;
    }

    /// <summary>
    /// Determines the default section to display for a rental application.
    /// </summary>
    /// <param name="application">The rental application being reviewed.</param>
    /// <param name="editable">Whether the application can be edited.</param>
    /// <returns>The default application section.</returns>
    private static ApplicationSection DefaultSection(RentalApplication application, bool editable) =>
      !editable ? ApplicationSection.Summary
      : !application.ApplicantInfoCompleted ? ApplicationSection.Info
      : !application.ResidenceHistoryCompleted ? ApplicationSection.ResidenceHistory
      : ApplicationSection.Summary;

    // only validate the section fields based on the section prefix
    // claude helped with this
    private static bool IsValid(ModelStateDictionary modelState, string prefix) =>
        modelState.FindKeysWithPrefix(prefix).All(entry => entry.Value.ValidationState != ModelValidationState.Invalid);

}
