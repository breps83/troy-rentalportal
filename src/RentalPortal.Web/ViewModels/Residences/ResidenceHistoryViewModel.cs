using RentalPortal.Web.Domain;

namespace RentalPortal.Web.ViewModels.Residences;

public record ResidenceHistoryViewModel(
    int ApplicationId,
    bool IsEditable,
    IReadOnlyList<Residence> Residences)
{

}

