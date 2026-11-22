using RentalPortal.Web.Domain;

namespace RentalPortal.Tests;

/// <summary>
/// Tests for the <see cref="Lease"/> rules: the default 12-month term created on approval and the
/// active-on-a-date check used by FR 3.e.
/// </summary>
public class LeaseTests
{
    private static Lease NewLease(DateOnly startDate)
    {
        var applicant = new ApplicationUser { Id = "applicant-1", FirstName = "Bruce", LastName = "Willis" };
        var application = RentalApplication.NewApplication(
            unitId: 7,
            applicant,
            new DateTime(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc));

        return Lease.CreateFromApplication(application, startDate);
    }

    [Fact]
    public void CreateFromApplication_UsesTheApplicationsUnit_AndLinksTheApplication()
    {
        // Act
        var lease = NewLease(new DateOnly(2026, 12, 1));

        // Assert
        Assert.Equal(7, lease.UnitId);
        Assert.NotNull(lease.RentalApplication);
        Assert.Equal(7, lease.RentalApplication.UnitId);
    }

    /// <summary>The default term is 12 months, ending the day before the anniversary of the start.</summary>
    [Theory]
    [InlineData("2026-12-01", "2027-11-30")]
    [InlineData("2027-01-01", "2027-12-31")]
    [InlineData("2027-03-01", "2028-02-29")]
    [InlineData("2028-02-29", "2029-02-27")]
    public void CreateFromApplication_EndsTheDayBeforeTwelveMonthsLater(string start, string expectedEnd)
    {
        // Act
        var lease = NewLease(DateOnly.Parse(start));

        // Assert
        Assert.Equal(DateOnly.Parse(start), lease.StartDate);
        Assert.Equal(DateOnly.Parse(expectedEnd), lease.EndDate);
    }

    /// <summary>A lease is active from its start date through its end date, inclusive.</summary>
    [Theory]
    [InlineData("2026-11-30", false)]
    [InlineData("2026-12-01", true)]
    [InlineData("2027-06-15", true)]
    [InlineData("2027-11-30", true)]
    [InlineData("2027-12-01", false)]
    public void IsActive_IsTrueFromStartThroughEndDate(string date, bool expected)
    {
        // Arrange
        var lease = NewLease(new DateOnly(2026, 12, 1));

        // Act & Assert
        Assert.Equal(expected, lease.IsActive(DateOnly.Parse(date)));
    }

    /// <summary>
    /// Two periods overlap when they share at least one day; touching end and start dates count as
    /// an overlap because both days are inclusive.
    /// </summary>
    [Theory]
    [InlineData("2026-01-01", "2026-11-30", false)]
    [InlineData("2026-01-01", "2026-12-01", true)]
    [InlineData("2027-01-01", "2027-03-31", true)]
    [InlineData("2026-01-01", "2028-12-31", true)]
    [InlineData("2027-11-30", "2028-11-29", true)]
    [InlineData("2027-12-01", "2028-11-30", false)]
    public void Overlaps_IsTrueWhenThePeriodsShareADay(string start, string end, bool expected)
    {
        // Arrange (Dec 1, 2026 through Nov 30, 2027)
        var lease = NewLease(new DateOnly(2026, 12, 1));

        // Act & Assert
        Assert.Equal(expected, lease.Overlaps(DateOnly.Parse(start), DateOnly.Parse(end)));
    }
}
