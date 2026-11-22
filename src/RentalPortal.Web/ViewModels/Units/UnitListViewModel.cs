namespace RentalPortal.Web.ViewModels.Units;

public record UnitListViewModel(int PropertyId, IReadOnlyList<UnitRowViewModel> Units);

