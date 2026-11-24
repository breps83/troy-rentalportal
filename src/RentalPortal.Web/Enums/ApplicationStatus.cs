namespace RentalPortal.Web.Enums;

/// <summary>
/// Represents the current state of a rental application.
/// </summary>
public enum ApplicationStatus
{
    Draft,
    Submitted,
    Returned,
    Approved,
    Denied,
    Withdrawn
}
