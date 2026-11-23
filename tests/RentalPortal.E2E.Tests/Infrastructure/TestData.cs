using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RentalPortal.Web.Data;
using RentalPortal.Web.Domain;
using RentalPortal.Web.Enums;
using RentalPortal.Web.Services;

namespace RentalPortal.E2E.Tests.Infrastructure;

/// <summary>
/// Seeded accounts (DbSeeder). Use these to log in; don't change their data in a way other tests read.
/// </summary>
public static class TestUsers
{
    public const string Password = DbSeeder.DefaultPassword;

    public const string Manager = "manager@rentalportal.test";

    public const string OtherManager = "manager2@rentalportal.test";

    public const string Applicant = "applicant@rentalportal.test";

    public const string OtherApplicant = "applicant2@rentalportal.test";
}

/// <summary>
/// Arranges data for a test directly through the app's services (fast, and not what's under test).
/// Every name is unique, so tests don't depend on each other or on run order. The behavior being
/// tested is then done through the UI.
/// </summary>
public class TestData(IServiceProvider services)
{
    public static string Unique(string prefix) => $"{prefix} {Guid.NewGuid().ToString("N")[..6]}";

    /// <summary>
    /// Creates a property with the given number of units (101, 102, ...), all of type Apartment.
    /// </summary>
    public async Task<Property> CreatePropertyAsync(int unitCount = 1, string namePrefix = "E2E Lofts")
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var apartment = await db.UnitTypes.SingleAsync(t => t.Name == "Apartment");

        var property = new Property
        {
            Name = Unique(namePrefix),
            Street = "100 Main St",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701"
        };

        for (var i = 1; i <= unitCount; i++)
        {
            property.Units.Add(new Unit
            {
                UnitNumber = (100 + i).ToString(),
                Bedrooms = 2,
                MonthlyRent = 1500 + i,
                UnitTypeId = apartment.Id
            });
        }

        db.Properties.Add(property);
        await db.SaveChangesAsync();

        return property;
    }

    /// <summary>
    /// Adds a unit to a property using the named unit type, even an inactive one (as existing data can).
    /// </summary>
    public async Task<Unit> CreateUnitAsync(int propertyId, string unitNumber, string unitTypeName)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var unitType = await db.UnitTypes.SingleAsync(t => t.Name == unitTypeName);

        var unit = new Unit
        {
            PropertyId = propertyId,
            UnitNumber = unitNumber,
            Bedrooms = 3,
            MonthlyRent = 2400,
            UnitTypeId = unitType.Id
        };

        db.Units.Add(unit);
        await db.SaveChangesAsync();

        return unit;
    }

    public async Task<int> GetUnitTypeIdAsync(string unitTypeName)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var unitType = await db.UnitTypes.SingleAsync(t => t.Name == unitTypeName);

        return unitType.Id;
    }

    public async Task<int> CountUnitsAsync(int propertyId)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Units.CountAsync(u => u.PropertyId == propertyId);
    }

    public async Task<ApplicationStatus> GetStatusAsync(int applicationId)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var application = await db.RentalApplications.SingleAsync(a => a.Id == applicationId);

        return application.Status;
    }

    /// <summary>
    /// Registers a new applicant with no applications, for tests that check what an applicant sees.
    /// </summary>
    public async Task<string> CreateApplicantAsync()
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var email = $"e2e-{Guid.NewGuid().ToString("N")[..8]}@rentalportal.test";

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Pat",
            LastName = "Tester"
        };

        var created = await userManager.CreateAsync(user, TestUsers.Password);

        if (!created.Succeeded)
            throw new InvalidOperationException(string.Join(", ", created.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, Roles.Applicant);

        return email;
    }

    /// <summary>
    /// Creates an application for the unit and moves it to <paramref name="status"/> through the real
    /// service and domain methods. Draft applications have both sections completed unless
    /// <paramref name="completeSections"/> is false.
    /// </summary>
    public async Task<int> CreateApplicationAsync(
        int unitId,
        string applicantEmail,
        ApplicationStatus status,
        bool completeSections = true,
        string? reviewComment = null)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<AppDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var service = provider.GetRequiredService<RentalApplicationService>();

        var applicant = await userManager.FindByEmailAsync(applicantEmail)
            ?? throw new InvalidOperationException($"No user {applicantEmail}.");

        var application = await service.StartApplicationAsync(unitId, applicant)
            ?? throw new InvalidOperationException($"Unit {unitId} is not available.");

        if (completeSections || status != ApplicationStatus.Draft)
        {
            application.UpdateApplicant(
                applicant.FirstName,
                applicant.LastName,
                "217-555-0100",
                applicantEmail,
                "12 Elm St, Springfield, IL 62701");

            application.AddResidence(
                "45 Oak Ave, Springfield, IL 62701",
                "Dana Landlord",
                "217-555-0199",
                new DateOnly(2021, 6, 1),
                new DateOnly(2024, 5, 31));

            application.SetResidenceHistoryCompleted();
            await db.SaveChangesAsync();
        }

        if (status == ApplicationStatus.Draft)
            return application.Id;

        if (status == ApplicationStatus.Withdrawn)
        {
            Check(await service.WithdrawApplicationAsync(application, applicant.Id));
            return application.Id;
        }

        Check(await service.SubmitAsync(application, applicant.Id));

        if (status == ApplicationStatus.Submitted)
            return application.Id;

        var manager = await userManager.FindByEmailAsync(TestUsers.Manager)
            ?? throw new InvalidOperationException("The seeded manager is missing.");

        var review = status switch
        {
            ApplicationStatus.Approved => ApplicationReviewStatus.Approve,
            ApplicationStatus.Returned => ApplicationReviewStatus.Return,
            ApplicationStatus.Denied => ApplicationReviewStatus.Deny,
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

        var comment = reviewComment;

        if (comment == null && review != ApplicationReviewStatus.Approve)
            comment = "Please check your details.";

        Check(await service.ReviewApplicationAsync(application, review, comment, internalNote: null, manager.Id));

        return application.Id;
    }

    /// <summary>
    /// Adds a property manager note to the application, written by <paramref name="managerEmail"/>.
    /// </summary>
    public async Task<int> CreateNoteAsync(int applicationId, string managerEmail, string text)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<AppDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        var manager = await userManager.FindByEmailAsync(managerEmail)
            ?? throw new InvalidOperationException($"No user {managerEmail}.");

        var note = ApplicationNote.Create(applicationId, manager.Id, text, DateTime.UtcNow);

        db.ApplicationNotes.Add(note);
        await db.SaveChangesAsync();

        return note.Id;
    }

    public async Task<bool> NoteExistsAsync(int noteId)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.ApplicationNotes.AnyAsync(n => n.Id == noteId);
    }

    private static void Check(Result result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(result.Error);
    }
}
