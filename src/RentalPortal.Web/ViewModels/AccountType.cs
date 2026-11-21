using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace RentalPortal.Web.ViewModels;

public enum AccountType
{
    Applicant,
    [Display(Name = "Property Manager")]
    PropertyManager
}
