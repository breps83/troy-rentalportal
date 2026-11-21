using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.Domain;
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
            return RedirectToAction("Index", "Listings");
        
        return RedirectToAction("Details", new { id = application.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var application = await GetApplication(id);

        if (application is null)
            return NotFound();

        if (!application.CanView(User))
            return Forbid();

        return View(CreateForm(application));
    }

    private Task<RentalApplication?> GetApplication(int id) =>
        db.RentalApplications
            .Include(a => a.Unit)
            .ThenInclude(u => u.Property)
            .FirstOrDefaultAsync(a => a.Id == id);

    private ApplicationFormViewModel CreateForm(RentalApplication application)
    {
        return new ApplicationFormViewModel
        {
            Id = application.Id,
            Status = application.Status,
            PropertyName = application.Unit.Property.Name,
            UnitNumber = application.Unit.UnitNumber,
            MonthlyRent = application.Unit.MonthlyRent
        };
    }
    
    

    
}
