namespace RentalPortal.Web.ViewModels.Units;

public class UnitRowViewModel
{
    public int Id { get; init; }

    public string UnitNumber { get; init; } = string.Empty;

    public int Bedrooms { get; init; }

    public decimal MonthlyRent { get; init; }

    public string UnitType { get; init; } = string.Empty;

    public bool UnitTypeIsActive { get; init; }
}
