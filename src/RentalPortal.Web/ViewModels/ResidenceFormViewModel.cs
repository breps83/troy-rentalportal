using System.ComponentModel.DataAnnotations;

namespace RentalPortal.Web.ViewModels;

public class ResidenceFormViewModel
{

    public int Id { get; set; }

    public int ApplicationId { get; set; }

    [Required, StringLength(250)]
    public string? Address { get; set; }

    [Required, Phone, StringLength(25), Display(Name = "Landlord phone")]
    public string? LandlordPhone { get; set; }

    [Required, DataType(DataType.Date), Display(Name ="Move In Date")]
    public DateOnly? MoveInDate { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Move Out Date")]
    public DateOnly? MoveOutDate { get; set; }




}
