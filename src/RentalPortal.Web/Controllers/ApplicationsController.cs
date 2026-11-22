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
using RentalPortal.Web.ViewModels.Applications;

namespace RentalPortal.Web.Controllers;

[Authorize(Roles = $"{Roles.Applicant},{Roles.PropertyManager}")]
public class ApplicationsController(
    AppDbContext db,
    RentalApplicationService applicationService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var query = db.RentalApplications.AsNoTracking();

        if (!User.IsPropertyManager())
        {
            var userId = User.GetUserId();

            query = query.Where(a => a.ApplicantId == userId);
        }

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

        return View(applications);
    }

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
        var application = await GetApplicationAsync(id);

        if (application is null)
            return NotFound();

        if (!application.UserCanView(User))
            return Forbid();

        return View(CreateForm(application, section ?? DefaultSection(application, application.UserCanEdit(User))));
    }

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

                return RedirectToAction("Details", new { id, section = section.PreviousSection() });

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
                    applicantInfo.CurrentAddress!);

                await db.SaveChangesAsync();

                return RedirectToAction("Details", new { id, section = section.NextSection() });

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

    private Task<RentalApplication?> GetApplicationAsync(int id) =>
        db.RentalApplications
            .Include(a => a.Unit)
            .ThenInclude(u => u.Property)
            .Include(a => a.Residences)
            .FirstOrDefaultAsync(a => a.Id == id);

    private IActionResult? RejectEditActionResult(RentalApplication application)
    {
        if (application.ApplicantId != User.GetUserId())
            return Forbid();

        if (!application.IsEditable)
        {
            // appears in alert
            TempData["Error"] = $"This application is {application.Status} and can no longer be edited.";
            
            return RedirectToAction("Details", new { id = application.Id });
        }

        return null;
    }

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
            CanSubmit = application.UserCanEdit(User) && application.CanSubmit,
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
