namespace RentalPortal.Web.Domain;

public class UnitType
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // This method determines if the unit type is selectable based on its active status or if it matches the current unit type ID.
    // In-memory use only: EF Core can't translate it to SQL, so don't call it inside a LINQ query
    // against the database. UnitsController.GetUnitTypeOptionsAsync repeats the condition inline for that reason.
    public bool IsSelectableFor(int? currentUnitTypeId) => IsActive || currentUnitTypeId == Id;
}
