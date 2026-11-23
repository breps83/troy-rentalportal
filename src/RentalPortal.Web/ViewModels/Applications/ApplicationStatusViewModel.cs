using RentalPortal.Web.Enums;

namespace RentalPortal.Web.ViewModels.Applications;

public class ApplicationStatusViewModel
{
    public int ApplicationId { get; init; }

    public ApplicationStatus Status { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? SubmittedAtUtc { get; init; }

    public DateOnly? LeaseStart { get; init; }

    public DateOnly? LeaseEnd { get; init; }

    public string? ReviewComment { get; init; }

    public bool CanReview { get; init; }

}
