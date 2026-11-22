using System.Security.Claims;
using RentalPortal.Web.Domain;

namespace RentalPortal.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool IsPropertyManager(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(Roles.PropertyManager);
    }

    public static bool IsApplicant(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(Roles.Applicant);
    }

    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The current user is not signed in.");

}
