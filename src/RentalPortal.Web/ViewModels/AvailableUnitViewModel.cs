namespace RentalPortal.Web.ViewModels;

public class AvailableUnitViewModel
{
    public int Id { get; set; }

    public string PropertyName { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public string UnitNumber { get; init; } = string.Empty;

    public string UnitType { get; init; } = string.Empty;

    public int Bedrooms { get; init; }

    public decimal MonthlyRent { get; init; }
}
