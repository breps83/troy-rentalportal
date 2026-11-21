namespace RentalPortal.Web.ViewModels;

public record UnitListViewModel(int PropertyId, IReadOnlyList<UnitRowViewModel> Units);

