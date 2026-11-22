using Microsoft.AspNetCore.Mvc.Rendering;
using RentalPortal.Web.Enums;

namespace RentalPortal.Web.ViewModels.Applications;

public class ApplicationListViewModel
{
    public ApplicationStatus? Status { get; init; }

    public int? PropertyId { get; init; }

    public IReadOnlyList<SelectListItem> Properties { get; init; }
        = new List<SelectListItem>();

    public IReadOnlyList<ApplicationRowViewModel> Applications { get; init; }
        = new List<ApplicationRowViewModel>();
}
