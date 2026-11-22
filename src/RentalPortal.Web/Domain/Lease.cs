namespace RentalPortal.Web.Domain;

public class Lease
{
    public const int DefaultTermMonths = 12;

    public int Id { get; set; }

    public int UnitId { get; set; }

    public Unit Unit { get; set; } = null!;

    public int RentalApplicationId { get; set; }

    public RentalApplication RentalApplication { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public static Lease CreateFromApplication(
        RentalApplication application,
        DateOnly startDate) =>
            new Lease()
            {
                UnitId = application.UnitId,
                RentalApplication = application,
                StartDate = startDate,
                EndDate = startDate.AddMonths(DefaultTermMonths).AddDays(-1)
            };

    public bool IsActive(DateOnly date) =>
        (StartDate <= date && date <= EndDate);

    public bool Overlaps(DateOnly startDate, DateOnly endDate) =>
        (StartDate <= endDate && startDate <= EndDate);

}
