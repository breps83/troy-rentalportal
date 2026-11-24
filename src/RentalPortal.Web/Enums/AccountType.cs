using System.ComponentModel.DataAnnotations;

namespace RentalPortal.Web.Enums;

/// <summary>
/// Specifies the role of an account in the rental portal.
/// </summary>
public enum AccountType
{
    Applicant,
    [Display(Name = "Property Manager")]
    PropertyManager
}
