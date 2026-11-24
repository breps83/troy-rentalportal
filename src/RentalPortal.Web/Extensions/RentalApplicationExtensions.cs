using RentalPortal.Web.Domain;
using System.Security.Claims;

namespace RentalPortal.Web.Extensions;

public static class RentalApplicationExtensions
{
    /// <summary>
    /// Determines whether the specified user can view a rental application.
    /// </summary>
    /// <param name="application">The rental application to view.</param>
    /// <param name="user">The user requesting access.</param>
    /// <returns><see langword="true"/> if the user is a property manager or owns the application; otherwise, <see
    /// langword="false"/>.</returns>
    public static bool UserCanView(this RentalApplication application, ClaimsPrincipal user) =>
        user.IsPropertyManager() || application.ApplicantId == user.GetUserId();

    /// <summary>
    /// Determines whether the specified user can edit the rental application.
    /// </summary>
    /// <param name="application">The rental application to evaluate.</param>
    /// <param name="user">The principal requesting edit access.</param>
    /// <returns><see langword="true"/> if the application is editable and belongs to the specified user; otherwise, <see
    /// langword="false"/>.</returns>
    public static bool UserCanEdit(this RentalApplication application, ClaimsPrincipal user) =>
        application.IsEditable && application.ApplicantId == user.GetUserId();
}
