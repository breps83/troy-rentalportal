using RentalPortal.Web.Domain;
using System.Security.Claims;

namespace RentalPortal.Web.Extensions;

public static class RentalApplicationExtensions
{
    public static bool UserCanView(this RentalApplication application, ClaimsPrincipal user) =>
        user.IsPropertyManager() || application.ApplicantId == user.GetUserId();

    public static bool UserCanEdit(this RentalApplication application, ClaimsPrincipal user) =>
        application.IsEditable && application.ApplicantId == user.GetUserId();
}
