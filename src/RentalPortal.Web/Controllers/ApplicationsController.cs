using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.Domain;
using RentalPortal.Web.Enums;
using RentalPortal.Web.Extensions;
using RentalPortal.Web.Services;
using RentalPortal.Web.ViewModels;

namespace RentalPortal.Web.Controllers;

[Authorize]
public class ApplicationsController(
    AppDbContext db,
    RentalApplicationService applicationService,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpPost, Authorize(Roles = Roles.Applicant)]
    public async Task<IActionResult> Apply(int unitId)
    {
        var user = await userManager.GetUserAsync(User);

        if(user is null)
            return Challenge();

        var application = await applicationService.StartApplicationAsync(unitId, user);

        if(application is null)
        {
            // show error in alert
            TempData["Error"] = "This unit is not available.";
            return RedirectToAction("Index", "Listings");
        }
        
        return RedirectToAction("Details", new { id = application.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, ApplicationSection? section)
    {
        var application = await GetApplication(id);

        if (application is null)
            return NotFound();

        if (!application.UserCanView(User))
            return Forbid();

        return View(CreateForm(application, section ?? DefaultSection(application, application.UserCanEdit(User))));
    }

    [HttpPost]
    public async Task<IActionResult> Details(int id, ApplicationFormViewModel model, string command)
    {
        var application = await GetApplication(id);

        if (application is null)
            return NotFound();

        
        if (command == ApplicationFormViewModel.Back)
        {
            if (!application.UserCanView(User))
                return Forbid();

            return RedirectToAction("Details", new { id, section = model.CurrentSection.PreviousSection() });
        }

        if (application.ApplicantId != User.GetUserId())
            return Forbid();

        
        if (!application.IsEditable)
        {
            // appears in alert
            TempData["Error"] = $"This application is {application.Status} and can no longer be edited.";
            return RedirectToAction("Details", new { id });
        }

        var section = model.CurrentSection;

        switch (command)
        {
            case ApplicationFormViewModel.Continue when section == Enums.ApplicationSection.Info:
                if (!IsValid(ModelState, nameof(model.ApplicantInfo)))
                    return View(CreateForm(application, section, model.ApplicantInfo));

                var applicantInfo = model.ApplicantInfo;

                application.UpdateApplicant(
                    applicantInfo.FirstName!,
                    applicantInfo.LastName!,
                    applicantInfo.Phone!,
                    applicantInfo.Email!,
                    applicantInfo.CurrentAddress!);

                await db.SaveChangesAsync();

                return RedirectToAction("Details", new { id, section = section.NextSection() });

            default:
                return BadRequest();
        }
        
    }

    private Task<RentalApplication?> GetApplication(int id) =>
        db.RentalApplications
            .Include(a => a.Unit)
            .ThenInclude(u => u.Property)
            .FirstOrDefaultAsync(a => a.Id == id);

    private ApplicationFormViewModel CreateForm(
        RentalApplication application, 
        ApplicationSection section, 
        ApplicantInfoViewModel? applicantInfo = null) 
    {

        var form = new ApplicationFormViewModel
        {
            Id = application.Id,
            CurrentSection = section,
            ApplicantInfo = applicantInfo ?? ApplicantInfoViewModel.From(application),
            Status = application.Status,
            PropertyName = application.Unit.Property.Name,
            UnitNumber = application.Unit.UnitNumber,
            MonthlyRent = application.Unit.MonthlyRent,
            IsEditable = application.UserCanEdit(User),
            ApplicantInfoCompleted = application.ApplicantInfoCompleted,
            ResidenceHistoryCompleted = application.ResidenceHistoryCompleted
        };

        form.ApplicantInfo?.IsEditable = form.IsSectionEditable(ApplicationSection.Info);

        return form;
    }

    private static ApplicationSection DefaultSection(RentalApplication application, bool editable) =>
      !editable ? ApplicationSection.Summary
      : !application.ApplicantInfoCompleted ? ApplicationSection.Info
      : !application.ResidenceHistoryCompleted ? ApplicationSection.ResidenceHistory
      : ApplicationSection.Summary;

    // only validate the section fields
    // claude helped with this
    private static bool IsValid(ModelStateDictionary modelState, string prefix) =>
        modelState.FindKeysWithPrefix(prefix).All(entry => entry.Value.ValidationState != ModelValidationState.Invalid);

    
    

    
}
