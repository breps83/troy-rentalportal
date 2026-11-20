using System.ComponentModel.DataAnnotations;
using RentalPortal.Web.Domain;

namespace RentalPortal.Web.ViewModels;

public class PropertyFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string? Name { get; set; }

    [Required, StringLength(200), Display(Name = "Street Address")]
    public string? Street { get; set; }

    [Required, StringLength(100)]
    public string? City { get; set; }

    [Required, StringLength(2), RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "Please enter a valid state abbreviation.")]
    public string? State { get; set; }

    [Required, RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Please enter a valid postal code.")]
    public string? PostalCode { get; set; }

    public void ApplyTo(Property property)
    {
        property.Name = Name!.Trim();
        property.Street = Street!.Trim();
        property.City = City!.Trim();
        property.State = State!.ToUpperInvariant().Trim();
        property.PostalCode = PostalCode!.Trim();
    }
}
