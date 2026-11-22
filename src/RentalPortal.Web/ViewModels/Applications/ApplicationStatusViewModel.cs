using RentalPortal.Web.Enums;

namespace RentalPortal.Web.ViewModels.Applications;

public class ApplicationStatusViewModel
{
    public int ApplicationId { get; init; }

    public ApplicationStatus Status { get; init; }

    public string ApplicantName { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? SubmittedAtUtc { get; init; }

    public DateOnly? LeaseStart { get; init; }

    public DateOnly? LeaseEnd { get; init; }

    public string? ReviewComment { get; init; }

    public bool CanReview { get; init; }

    public bool ShowHistory { get; init; }

    public IReadOnlyList<ReviewStatusChangeViewModel> ReviewStatusHistory { get; set; } =
        new List<ReviewStatusChangeViewModel>();

}
