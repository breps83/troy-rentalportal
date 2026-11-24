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

    /// <summary>
    /// Creates a lease from an approved rental application.
    /// </summary>
    /// <remarks>The lease end date is calculated from the lease start date and the default term.</remarks>
    /// <param name="application">The rental application used to create the lease.</param>
    /// <param name="startDate">The first day of the lease term.</param>
    /// <returns>A lease associated with the application and unit.</returns>
    public static Lease CreateFromApplication(
        RentalApplication application,
        DateOnly startDate) =>
            new()
            {
                UnitId = application.UnitId,
                RentalApplication = application,
                StartDate = startDate,
                EndDate = startDate.AddMonths(DefaultTermMonths).AddDays(-1)
            };

    /// <summary>
    /// Determines whether the lease is active on the specified date.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns><see langword="true"/> when the date falls between the start and end dates, inclusive; otherwise, <see
    /// langword="false"/>.</returns>
    public bool IsActive(DateOnly date) =>
        (StartDate <= date && date <= EndDate);

    /// <summary>
    /// Determines whether the specified date range overlaps this lease.
    /// </summary>
    /// <param name="startDate">The inclusive start date of the date range.</param>
    /// <param name="endDate">The inclusive end date of the date range.</param>
    /// <returns><see langword="true"/> when the date ranges overlap; otherwise, <see langword="false"/>.</returns>
    public bool Overlaps(DateOnly startDate, DateOnly endDate) =>
        (StartDate <= endDate && startDate <= EndDate);

}
