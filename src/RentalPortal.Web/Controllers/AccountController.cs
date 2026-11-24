using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RentalPortal.Web.Data;
using RentalPortal.Web.Domain;
using RentalPortal.Web.Enums;
using RentalPortal.Web.ViewModels.Account;

namespace RentalPortal.Web.Controllers;

/// <summary>
/// Handles account registration, sign-in, sign-out, and access-denied requests.
/// </summary>
/// <param name="db">Provides access to application data.</param>
/// <param name="userManager">Manages application user creation and role assignment.</param>
/// <param name="signInManager">Manages user authentication and sign-out operations.</param>
/// <param name="logger">Records account activity and authentication failures.</param>
public class AccountController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager, 
    SignInManager<ApplicationUser> signInManager,
    ILogger<AccountController> logger) : Controller
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

        await using var transaction = await db.Database.BeginTransactionAsync();

        var result = await userManager.CreateAsync(user, model.Password!);

        if (result.Succeeded)
            result = await userManager.AddToRoleAsync(user, role!);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await transaction.CommitAsync();

        logger.LogInformation("User {UserId} registered as {Role}", user.Id, role);

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
            // don't log the email; it may be personal data or a mistyped password
            logger.LogWarning("Sign-in failed: no account for the email entered");

            ModelState.AddModelError(string.Empty, invalidLoginMessage);

            return View(model);
        }

        // Attempt to sign in the user
        var result = 
            await signInManager.PasswordSignInAsync(user, model.Password!, model.RememberMe, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                logger.LogWarning("Sign-in blocked: user {UserId} is locked out", user.Id);
            else
                logger.LogWarning("Sign-in failed for user {UserId}", user.Id);

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
