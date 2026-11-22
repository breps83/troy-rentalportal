using RentalPortal.Web.Domain;
using System.ComponentModel.DataAnnotations;

namespace RentalPortal.Web.ViewModels;

public class ResidenceFormViewModel : IValidatableObject
{

    public int Id { get; set; }

    public int ApplicationId { get; set; }

    [Required, StringLength(250)]
    public string? Address { get; set; }

    [Required, StringLength(100), Display(Name = "Landlord name")]
    public string? LandlordName { get; set; }

    [Required, Phone, StringLength(25), Display(Name = "Landlord phone")]
    public string? LandlordPhone { get; set; }

    [Required, DataType(DataType.Date), Display(Name ="Move In Date")]
    public DateOnly? MoveInDate { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Move Out Date")]
    public DateOnly? MoveOutDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if(MoveInDate > MoveOutDate)
        {
            yield return new ValidationResult("Move Out date can't be before Move In Date", [nameof(MoveOutDate)]);
        }
    }

    public static ResidenceFormViewModel From(int applicationId, Residence residence) => new ResidenceFormViewModel()
    {
        Id = residence.Id,
        ApplicationId = applicationId,
        Address = residence.Address,
        LandlordName = residence.LandlordName,
        LandlordPhone = residence.LandlordPhone,
        MoveInDate = residence.MoveInDate,
        MoveOutDate = residence.MoveOutDate
    };
}
