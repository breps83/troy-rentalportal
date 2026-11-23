using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.Domain;
using RentalPortal.Web.Enums;
using RentalPortal.Web.Services;

namespace RentalPortal.Tests;

/// <summary>
/// Tests for <see cref="RentalApplicationService.SubmitAsync"/>: only the owner can submit, and only
/// once both sections are saved (FR 3.b.iii, Challenge a).
/// </summary>
/// <remarks>
/// Only the rejected cases are covered. They return before anything is saved, so the database is
/// never opened. A successful submit saves to the database, which needs a test database provider
/// (not added yet). The submit rule itself is covered in <see cref="RentalApplicationTests"/>.
/// </remarks>
public class RentalApplicationServiceTests
{
    private const string OwnerId = "applicant-1";
    private const string OtherApplicantId = "applicant-2";

    private static readonly DateTime _createdUtc = new(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// A service whose database is never opened. The connection string isn't used because the
    /// rejected cases return before saving.
    /// </summary>
    private static RentalApplicationService NewService()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused")
            .Options;

        return new RentalApplicationService(new AppDbContext(options), TimeProvider.System);
    }

    private static RentalApplication NewDraft() =>
        RentalApplication.NewApplication(
            unitId: 7,
            new ApplicationUser
            {
                Id = OwnerId,
                FirstName = "Bruce",
                LastName = "Willis",
                Email = "a@rentalportal.test"
            },
            _createdUtc);

    private static RentalApplication NewCompleteDraft()
    {
        var application = NewDraft();
        application.UpdateApplicant("Bruce", "Willis", "555-0100", "a@rentalportal.test", "1 Main St, Springfield, IL");
        application.AddResidence("10 Oak Ave, Springfield, IL", "Pat Landlord", "555-0199",
            new DateOnly(2022, 1, 1), new DateOnly(2024, 6, 30));
        application.SetResidenceHistoryCompleted();
        return application;
    }

    [Fact]
    public async Task SubmitAsync_ByAnotherUser_IsRejected_AndStaysDraft()
    {
        // Arrange
        var service = NewService();
        var application = NewCompleteDraft();

        // Act
        var result = await service.SubmitAsync(application, OtherApplicantId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Failed. This may not be your application.", result.Error);
        Assert.Equal(ApplicationStatus.Draft, application.Status);
        Assert.Null(application.SubmittedAtUtc);
    }

    [Fact]
    public async Task SubmitAsync_BeforeBothSectionsSaved_IsRejected_AndStaysDraft()
    {
        // Arrange
        var service = NewService();
        var application = NewDraft();

        // Act
        var result = await service.SubmitAsync(application, OwnerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Please complete both sections before submitting.", result.Error);
        Assert.Equal(ApplicationStatus.Draft, application.Status);
        Assert.Null(application.SubmittedAtUtc);
    }

    /// <summary>A second submit, for example from a double-click, is rejected.</summary>
    [Fact]
    public async Task SubmitAsync_WhenAlreadySubmitted_IsRejected()
    {
        // Arrange
        var service = NewService();
        var application = NewCompleteDraft();
        application.Submit(
            OwnerId, new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 11, 2), []);

        // Act
        var result = await service.SubmitAsync(application, OwnerId);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Equal(2, application.StatusHistory.Count);
    }

}
