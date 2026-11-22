using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RentalPortal.Web.Domain;
using RentalPortal.Web.Enums;
using RentalPortal.Web.ViewModels.Account;

namespace RentalPortal.Web.Controllers;

public class AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : Controller
{
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        // Map each account type to its role explicitly; anything else (e.g. a forged numeric value) is rejected.
        string? role = model.AccountType switch
        {
            AccountType.Applicant => Roles.Applicant,
            AccountType.PropertyManager => Roles.PropertyManager,
            _ => null
        };

        if (role is null)
            ModelState.AddModelError(nameof(model.AccountType), "Choose a valid account type.");

        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName!.Trim(),
            LastName = model.LastName!.Trim()
        };

        var result = await userManager.CreateAsync(user, model.Password!);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await userManager.AddToRoleAsync(user, role!);

        await signInManager.SignInAsync(user, isPersistent: false);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Define a generic error message for invalid login attempts
        // use the same message for both cases to avoid revealing whether the email exists or not
        string invalidLoginMessage = "Invalid login attempt.";

        // Check if the user exists
        var user = await userManager.FindByEmailAsync(model.Email!);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, invalidLoginMessage);

            return View(model);
        }

        // Attempt to sign in the user
        var result = await signInManager.PasswordSignInAsync(user, model.Password!, model.RememberMe, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.IsLockedOut
                ? "This account is locked. Try again in a few minutes."
                : invalidLoginMessage);

            return View(model);
        }

        return Url.IsLocalUrl(model.ReturnUrl)
             ? LocalRedirect(model.ReturnUrl)
             : RedirectToAction("Index", "Home");
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
