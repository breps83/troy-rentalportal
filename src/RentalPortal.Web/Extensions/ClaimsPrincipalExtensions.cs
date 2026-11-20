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
}
