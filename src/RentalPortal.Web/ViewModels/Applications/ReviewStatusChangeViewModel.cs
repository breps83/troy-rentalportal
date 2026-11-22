using RentalPortal.Web.Enums;

namespace RentalPortal.Web.ViewModels.Applications;

public record ReviewStatusChangeViewModel(
    ApplicationStatus? FromStatus,
    ApplicationStatus ToStatus,
    string ChangedBy,
    DateTime ChangedAtUtc,
    string? Comment)
{

}
