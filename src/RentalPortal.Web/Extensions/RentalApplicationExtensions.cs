using RentalPortal.Web.Domain;
using System.Security.Claims;

namespace RentalPortal.Web.Extensions;

public static class RentalApplicationExtensions
{
    public static bool CanView(this RentalApplication application, ClaimsPrincipal user) =>
        user.IsPropertyManager() || application.ApplicantId == user.GetUserId();

}
