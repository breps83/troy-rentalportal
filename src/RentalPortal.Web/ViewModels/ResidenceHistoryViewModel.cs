using RentalPortal.Web.Domain;

namespace RentalPortal.Web.ViewModels;

public record ResidenceHistoryViewModel(
    int ApplicationId, 
    bool IsEditable, 
    IReadOnlyList<Residence> Residences)
{

}

