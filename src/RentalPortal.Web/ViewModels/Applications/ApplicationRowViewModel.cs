using RentalPortal.Web.Enums;

namespace RentalPortal.Web.ViewModels.Applications;

public class ApplicationRowViewModel
{
    public int Id { get; init; }

    public string PropertyName { get; init; } = string.Empty;

    public string UnitNumber { get; init; } = string.Empty;

    public string ApplicantName { get; init; } = string.Empty;

    public ApplicationStatus Status { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? SubmittedAtUtc { get; init; }

}