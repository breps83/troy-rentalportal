using Microsoft.AspNetCore.Mvc.ModelBinding;
using RentalPortal.Web.Domain;
using System.ComponentModel.DataAnnotations;

namespace RentalPortal.Web.ViewModels.Applications;

public class ApplicantInfoViewModel
{
    [Required, StringLength(100), Display(Name = "First Name")]
    public string? FirstName { get; set; }

    [Required, StringLength(100), Display(Name ="Last Name")]
    public string? LastName { get; set; }

    [Required, Phone, StringLength(25)]
    public string? Phone { get; set; }

    [Required, EmailAddress, StringLength(255)]
    public string? Email { get; set; }

    [Required, StringLength(250), Display(Name = "Current address")]
    public string? CurrentAddress { get; set; }

    [BindNever]
    public bool IsEditable { get; set; }

    public static ApplicantInfoViewModel From(RentalApplication application) => new()
    {
        FirstName = application.FirstName,
        LastName = application.LastName,
        Phone = application.Phone,
        Email = application.Email,
        CurrentAddress = application.CurrentAddress
    };
}

