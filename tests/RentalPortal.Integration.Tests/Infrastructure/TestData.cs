using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RentalPortal.Web.Data;
using RentalPortal.Web.Domain;
using RentalPortal.Web.Enums;
using RentalPortal.Web.Extensions;
using RentalPortal.Web.Services;

namespace RentalPortal.Integration.Tests.Infrastructure;

/// <summary>
/// Seeded accounts (DbSeeder).
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
/// Sets up and reads data directly through the app's services. Names are unique so tests don't depend
/// on each other.
/// </summary>
public class TestData(IServiceProvider services)
{
    public static string Unique(string prefix) => $"{prefix} {Guid.NewGuid().ToString("N")[..6]}";

    public DateOnly Today => services.GetRequiredService<TimeProvider>().GetToday();

    /// <summary>
    /// Creates a property with one unit (101) and returns the unit, with its property loaded.
    /// </summary>
    public async Task<Unit> CreateUnitAsync(string unitTypeName = "Apartment")
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var unitType = await db.UnitTypes.SingleAsync(t => t.Name == unitTypeName);

        var property = new Property
        {
            Name = Unique("Integration Lofts"),
            Street = "100 Main St",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701"
        };

        var unit = new Unit
        {
            UnitNumber = "101",
            Bedrooms = 2,
            MonthlyRent = 1500,
            UnitTypeId = unitType.Id,
            Property = property
        };

        db.Units.Add(unit);
        await db.SaveChangesAsync();

        return unit;
    }

    /// <summary>
    /// Creates a completed application and moves it to <paramref name="status"/> (Draft, Submitted, or
    /// Withdrawn) through the real service. Uses a new unit unless <paramref name="unitId"/> is given.
    /// </summary>
    public async Task<int> CreateApplicationAsync(string applicantEmail, ApplicationStatus status, int? unitId = null)
    {
        if (!unitId.HasValue)
            unitId = (await CreateUnitAsync()).Id;

        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<AppDbContext>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var service = provider.GetRequiredService<RentalApplicationService>();

        var applicant = await userManager.FindByEmailAsync(applicantEmail)
            ?? throw new InvalidOperationException($"No user {applicantEmail}.");

        var application = await service.StartApplicationAsync(unitId.Value, applicant)
            ?? throw new InvalidOperationException($"Unit {unitId} is not available.");

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

        if (status == ApplicationStatus.Submitted)
            Check(await service.SubmitAsync(application, applicant.Id));
        else if (status == ApplicationStatus.Withdrawn)
            Check(await service.WithdrawApplicationAsync(application, applicant.Id));
        else if (status != ApplicationStatus.Draft)
            throw new ArgumentOutOfRangeException(nameof(status));

        return application.Id;
    }

    /// <summary>
    /// Adds a lease to the unit, backed by a new submitted application.
    /// </summary>
    public async Task AddLeaseAsync(int unitId, DateOnly startDate, DateOnly endDate)
    {
        var applicationId = await CreateApplicationAsync(TestUsers.OtherApplicant, ApplicationStatus.Submitted, unitId);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Leases.Add(new Lease
        {
            UnitId = unitId,
            RentalApplicationId = applicationId,
            StartDate = startDate,
            EndDate = endDate
        });

        await db.SaveChangesAsync();
    }

    public async Task<List<UnitType>> GetUnitTypesAsync()
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.UnitTypes.AsNoTracking().ToListAsync();
    }

    public async Task<bool> UnitExistsAsync(int unitId)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Units.AnyAsync(u => u.Id == unitId);
    }

    public async Task<bool> PropertyExistsAsync(int propertyId)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Properties.AnyAsync(p => p.Id == propertyId);
    }

    public async Task<RentalApplication> GetApplicationAsync(int applicationId)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.RentalApplications.AsNoTracking().SingleAsync(a => a.Id == applicationId);
    }

    public async Task<int> CountApplicationsAsync(int unitId)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.RentalApplications.CountAsync(a => a.UnitId == unitId);
    }

    public async Task<bool> PropertyExistsAsync(string name)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Properties.AnyAsync(p => p.Name == name);
    }

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

    public async Task<ApplicationNote?> GetNoteAsync(int noteId)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.ApplicationNotes.AsNoTracking().SingleOrDefaultAsync(n => n.Id == noteId);
    }

    /// <summary>
    /// Creates an applicant with <see cref="TestUsers.Password"/> and returns the email. For tests that
    /// change the account, such as locking it out, so the seeded users stay usable.
    /// </summary>
    public async Task<string> CreateApplicantAsync()
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var email = $"{Guid.NewGuid():N}@rentalportal.test";
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Jordan",
            LastName = "Tester"
        };

        var result = await userManager.CreateAsync(user, TestUsers.Password);
        if (result.Succeeded)
            result = await userManager.AddToRoleAsync(user, Roles.Applicant);

        if (!result.Succeeded)
            throw new InvalidOperationException(result.Errors.First().Description);

        return email;
    }

    /// <summary>
    /// The user's roles, or null when there is no user with that email.
    /// </summary>
    public async Task<IList<string>?> GetRolesAsync(string email)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            return null;

        return await userManager.GetRolesAsync(user);
    }

    private static void Check(Result result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(result.Error);
    }
}
