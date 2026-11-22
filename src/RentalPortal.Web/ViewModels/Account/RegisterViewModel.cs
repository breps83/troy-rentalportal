using RentalPortal.Web.Enums;
using System.ComponentModel.DataAnnotations;

namespace RentalPortal.Web.ViewModels.Account;

public class RegisterViewModel
{
    [Required, StringLength(100), Display(Name = "First Name")]
    public string? FirstName { get; set; }

    [Required, StringLength(100), Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [Required, EmailAddress, StringLength(256)]
    public string? Email { get; set; }

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    public string? Password { get; set; }

    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "The passwords don't match."), Display(Name = "Confirm password")]
    public string? ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Choose an account type."), Display(Name = "Account Type")]
    public AccountType? AccountType { get; set; }

}
