using RentalPortal.Web.Enums;

namespace RentalPortal.Web.ViewModels.Applications;

public class ApplicationStatusViewModel
{
    public int ApplicationId { get; init; }

    public ApplicationStatus Status { get; init; }

    public string ApplicantName { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? SubmittedAtUtc { get; init; }

}
