using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace RentalPortal.Web.Enums;

public enum AccountType
{
    Applicant,
    [Display(Name = "Property Manager")]
    PropertyManager
}
