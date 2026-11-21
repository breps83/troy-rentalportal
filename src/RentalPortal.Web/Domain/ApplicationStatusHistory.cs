using RentalPortal.Web.Enums;

namespace RentalPortal.Web.Domain;

public class ApplicationStatusHistory
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }

    public ApplicationStatus? FromStatus { get; set; }

    public ApplicationStatus ToStatus { get; set; }

    public string ChangedById { get; set; } = null!;

    public ApplicationUser ChangedByUser { get; set; } = null!;

    public DateTime ChangedAtUtc { get; set; }

    public string? Comment { get; set; }
}
