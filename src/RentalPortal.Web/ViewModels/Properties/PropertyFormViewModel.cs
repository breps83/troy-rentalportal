using System.ComponentModel.DataAnnotations;
using RentalPortal.Web.Domain;

namespace RentalPortal.Web.ViewModels.Properties;

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

    /// <summary>
    /// Creates a property form view model from a property.
    /// </summary>
    /// <param name="property">The property used to populate the view model.</param>
    /// <returns>A property form view model containing the property details.</returns>
    public static PropertyFormViewModel From(Property property) => new()
    {
        Id = property.Id,
        Name = property.Name,
        Street = property.Street,
        City = property.City,
        State = property.State,
        PostalCode = property.PostalCode
    };

    /// <summary>
    /// Applies the form values to the specified property.
    /// </summary>
    /// <remarks>Whitespace is removed from text values, and the state code is converted to uppercase
    /// invariant before it is trimmed.</remarks>
    /// <param name="property">The property to update.</param>
    public void ApplyTo(Property property)
    {
        property.Name = Name!.Trim();
        property.Street = Street!.Trim();
        property.City = City!.Trim();
        property.State = State!.ToUpperInvariant().Trim();
        property.PostalCode = PostalCode!.Trim();
    }
}
