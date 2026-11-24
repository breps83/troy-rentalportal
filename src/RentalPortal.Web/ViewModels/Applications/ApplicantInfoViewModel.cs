using Microsoft.AspNetCore.Mvc.ModelBinding;
using RentalPortal.Web.Domain;
using System.ComponentModel.DataAnnotations;

namespace RentalPortal.Web.ViewModels.Applications;

public class ApplicantInfoViewModel
{
    [Required, StringLength(100), Display(Name = "First Name")]
    public string? FirstName { get; set; }

    [Required, StringLength(100), Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [Required, Phone, StringLength(25)]
    public string? Phone { get; set; }

    [Required, EmailAddress, StringLength(255)]
    public string? Email { get; set; }

    [Required, StringLength(200), Display(Name = "Street address")]
    public string? Street { get; set; }

    [Required, StringLength(100)]
    public string? City { get; set; }

    [Required, StringLength(2)]
    [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "Please enter a valid state abbreviation.")]
    public string? State { get; set; }

    [Required, StringLength(10), Display(Name = "Postal code")]
    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Please enter a valid postal code.")]
    public string? PostalCode { get; set; }

    [BindNever, Display(Name = "Current address")]
    public string? CurrentAddress { get; set; }

    [BindNever]
    public bool IsEditable { get; set; }

    public static ApplicantInfoViewModel From(RentalApplication application) => new()
    {
        FirstName = application.FirstName,
        LastName = application.LastName,
        Phone = application.Phone,
        Email = application.Email,
        Street = application.CurrentStreet,
        City = application.CurrentCity,
        State = application.CurrentState,
        PostalCode = application.CurrentPostalCode,
        CurrentAddress = application.CurrentAddress
    };
}

