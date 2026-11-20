using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RentalPortal.Web.Domain;
using RentalPortal.Web.ViewModels;

namespace RentalPortal.Web.Controllers;

public class AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : Controller
{
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName!.Trim(),
            LastName = model.LastName!.Trim()
        };
        var result = await userManager.CreateAsync(user, model.Password!);

    
        if(!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
     
        var role = model.AccountType == AccountType.Applicant ? Roles.Applicant : Roles.PropertyManager;
        await userManager.AddToRoleAsync(user, role);
        await signInManager.SignInAsync(user, isPersistent: false);

        return RedirectToAction("Index", "Home");

    }

}
