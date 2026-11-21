using RentalPortal.Web.Domain;

namespace RentalPortal.Web.ViewModels;

public class ApplicationFormViewModel
{
    public int Id { get; set; }

    public ApplicationStatus Status { get; set; }

    public string PropertyName { get; set; } = string.Empty;

    public string UnitNumber { get; set; } = string.Empty;

    public decimal MonthlyRent { get; set; }

}
