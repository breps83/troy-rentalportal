using RentalPortal.Web.Domain;
using System.ComponentModel.DataAnnotations;

namespace RentalPortal.Web.ViewModels.Residences;

public class ResidenceFormViewModel : IValidatableObject
{

    public int Id { get; set; }

    public int ApplicationId { get; set; }

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

    [Required, StringLength(100), Display(Name = "Landlord name")]
    public string? LandlordName { get; set; }

    [Required, Phone, StringLength(25), Display(Name = "Landlord phone")]
    public string? LandlordPhone { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Move In Date")]
    public DateOnly? MoveInDate { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Move Out Date")]
    public DateOnly? MoveOutDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MoveInDate > MoveOutDate)
        {
            yield return new ValidationResult("Move Out date can't be before Move In Date", [nameof(MoveOutDate)]);
        }
    }

    public static ResidenceFormViewModel From(int applicationId, Residence residence) => new ResidenceFormViewModel()
    {
        Id = residence.Id,
        ApplicationId = applicationId,
        Street = residence.Street,
        City = residence.City,
        State = residence.State,
        PostalCode = residence.PostalCode,
        LandlordName = residence.LandlordName,
        LandlordPhone = residence.LandlordPhone,
        MoveInDate = residence.MoveInDate,
        MoveOutDate = residence.MoveOutDate
    };
}
