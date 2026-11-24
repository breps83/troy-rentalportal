using System.Security.Claims;
using RentalPortal.Web.Domain;

namespace RentalPortal.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Determines whether the principal belongs to the property manager role.
    /// </summary>
    /// <param name="principal">The claims principal to evaluate.</param>
    /// <returns><see langword="true"/> when the principal is in the property manager role; otherwise, <see langword="false"/>.</returns>
    public static bool IsPropertyManager(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(Roles.PropertyManager);
    }

    /// <summary>
    /// Determines whether the principal is in the applicant role.
    /// </summary>
    /// <param name="principal">The claims principal to evaluate.</param>
    /// <returns><see langword="true"/> when the principal is an applicant; otherwise, <see langword="false"/>.</returns>
    public static bool IsApplicant(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(Roles.Applicant);
    }

    /// <summary>
    /// Gets the user identifier from the current claims principal.
    /// </summary>
    /// <param name="user">The claims principal representing the signed-in user.</param>
    /// <returns>The value of the name identifier claim.</returns>
    /// <exception cref="InvalidOperationException">The current user is not signed in or has no name identifier claim.</exception>
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The current user is not signed in.");

}
