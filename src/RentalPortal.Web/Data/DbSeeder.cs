using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Domain;
using RentalPortal.Web.Enums;

namespace RentalPortal.Web.Data;

public class DbSeeder
{
    // Default password for seeded users
    // In a production application, you would not want to use a default password like this.
    // This is just for demonstration purposes.
    public const string DefaultPassword = "Password1**";
    public const string FakerCulture = "en_US";

    public const int TotalApplicants = 8;
    public static readonly (string Name, bool IsActive)[] UnitTypes =
        [
            ("Studio", true),
            ("Apartment", true),
            ("Condo", true),
            ("Townhouse", true),
            // Inactive (FR 2.b): no longer offered for new units, but still shown on the
            // seeded units that already use it, so the "inactive" rule can be demoed.
            ("Penthouse", false)
        ];

    public static readonly string[] NoteSamples =
        [
            "This is a test note from the seeder.",
            "Another note from seeder.",
            "Yet anothre note from seeder.",
            "Demo note from seeder.",
            "5th note from seeder.",
            "Note from seeder.",
            "This is a demo note from seeder."            
        ];

    /// <summary>
    /// Seeds the database with roles and demo application data.
    /// </summary>
    /// <remarks>Applies pending migrations and rolls back all seeded changes if an exception
    /// occurs.</remarks>
    /// <param name="services">The service provider used to resolve database and identity services.</param>
    /// <returns>A task that represents the asynchronous seeding operation.</returns>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var provider = scope.ServiceProvider;

        var environment = provider.GetRequiredService<IHostEnvironment>();

        var db = provider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        var faker = new Faker(FakerCulture) { Random = new Randomizer(4021) };

        var now = provider.GetRequiredService<TimeProvider>()
            .GetUtcNow()
            .UtcDateTime;

        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            await SeedRolesAsync(roleManager);

            await SeedUnitTypesAsync(db);

            // only seed demo content in development
            if (environment.IsDevelopment())
            {
                // seed demo users if none exist
                var (seedDatabase, propertyManagers, applicants) = await SeedDemoUsersAsync(userManager, faker);

                if (seedDatabase)
                {
                    await SeedPropertiesAsync(db, faker);

                    await SeedApplicationsAsync(db,
                        faker,
                        (propertyManagers, applicants),
                        now);
                }
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();

            throw;
        }

    }

    /// <summary>
    /// Creates the applicant and property manager roles when they do not already exist.
    /// </summary>
    /// <param name="roleManager">The role manager used to check for and create roles.</param>
    /// <returns>A task that represents the asynchronous seeding operation.</returns>
    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { Roles.Applicant, Roles.PropertyManager })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    /// <summary>
    /// Creates demo property managers and applicants in the user store.
    /// </summary>
    /// <param name="userManager">The user manager used to ensure demo accounts exist.</param>
    /// <param name="faker">The faker used to generate names for additional applicant accounts.</param>
    /// <returns>The demo property managers and applicants.</returns>
    private static async Task<
        (bool seedDatabase, List<ApplicationUser> propertyManagers, List<ApplicationUser> applicants)>
        SeedDemoUsersAsync(UserManager<ApplicationUser> userManager, Faker faker)
    {
        // don't seed if users already exist
        if (await userManager.Users.AnyAsync())
            return (false, [], []);

        var propertyManagers = new List<ApplicationUser>()
        {
            await EnsureUserAsync(
                userManager,
                "manager@rentalportal.test",
                "Bernie",
                "Lomax",
                Roles.PropertyManager),

            await EnsureUserAsync(
                userManager,
                "manager2@rentalportal.test",
                "Bill",
                "Lumbergh",
                Roles.PropertyManager)
        };

        // 4 controlled applicants, const TotalApplicants = 8
        var applicants = new List<ApplicationUser>()
        {
            await EnsureUserAsync(
                userManager,
                "applicant@rentalportal.test",
                "Arnold",
                "Schwarzenegger",
                Roles.Applicant),

            await EnsureUserAsync(
                userManager,
                "applicant2@rentalportal.test",
                "Sylvester",
                "Stallone",
                Roles.Applicant),

            await EnsureUserAsync(
                userManager,
                "applicant3@rentalportal.test",
                "Bruce",
                "Willis",
                Roles.Applicant),

            await EnsureUserAsync(
                userManager,
                "applicant4@rentalportal.test",
                "Chuck",
                "Norris",
                Roles.Applicant)
        };

        // remaining applicants (random)
        for (var i = applicants.Count + 1; i <= TotalApplicants; i++)
        {
            applicants.Add(await EnsureUserAsync(
                userManager,
                $"applicant{i}@rentalportal.test",
                faker.Name.FirstName(),
                faker.Name.LastName(),
                Roles.Applicant));
        }

        return (true, propertyManagers, applicants);

    }

    /// <summary>
    /// Seeds the database with property records when none exist.
    /// </summary>
    /// <remarks>Uses a fixed seed and generates eight property records.</remarks>
    /// <param name="db">The database context used to seed and persist property records.</param>
    /// <param name="faker"></param>
    /// <returns>A task that represents the asynchronous seed operation.</returns>
    private static async Task SeedPropertiesAsync(AppDbContext db, Faker faker)
    {
        if (await db.Properties.AnyAsync())
            return;

        var types = await db.UnitTypes.ToDictionaryAsync(t => t.Name);

        var properties = new Faker<Property>(FakerCulture)
            .UseSeed(1187) // fixed seed, this gives the same sequence of generated data
            .RuleFor(p => p.Name, f => $"{f.Address.StreetName()} {f.PickRandom("Residences", "Apartments", "Condos", "Townhouses")}")
            .RuleFor(p => p.Street, f => f.Address.StreetAddress())
            .RuleFor(p => p.City, f => f.Address.City())
            .RuleFor(p => p.State, f => f.Address.StateAbbr())
            .RuleFor(p => p.PostalCode, f => f.Address.ZipCode("#####"))
            .Generate(8);

        foreach (var property in properties)
        {
            // between 5 & 8 units
            var unitCount = faker.Random.Int(5, 8);

            for (var i = 0; i < unitCount; i++)
            {
                var bedrooms = faker.Random.Int(0, 3);
                // Pick a unit type that fits the bedroom count. 
                // The faker has a fixed seed, so the same units get the same types on every reseed.
                var type = bedrooms switch
                {
                    // Studios have no separate bedroom.
                    0 => types["Studio"],
                    // 3 bedrooms: 10% Penthouse, 90% Apartment. Penthouse is inactive, so these units
                    // show it with an "inactive" badge but no other unit can be given it 
                    3 => faker.Random.Bool(0.1f) ? types["Penthouse"] : types["Apartment"],
                    // 1 or 2 bedrooms: 20% Condo, 80% Townhouse.
                    _ => faker.Random.Bool(0.2f) ? types["Condo"] : types["Townhouse"]
                };

                property.Units.Add(new Unit
                {
                    // 101, 102, 103, 104, 201, 202, 203, 204, etc.
                    UnitNumber = $"{i / 4 + 1}{i % 4 + 1:00}",
                    Bedrooms = bedrooms,
                    // Rent is 950, plus 375 per bedroom, plus a random 0–200 in steps of 10.
                    // $950 for studio, $1325 for 1BR, $1700 for 2BR, $2075 for 3BR, plus a random amount up to $200
                    MonthlyRent = 950 + bedrooms * 375 + faker.Random.Int(0, 20) * 10,
                    UnitTypeId = type.Id
                });
            }
        }

        db.Properties.AddRange(properties);

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds unit types that are not already present in the database.
    /// </summary>
    /// <param name="db">The database context used to query and persist unit types.</param>
    /// <returns>A task that represents the asynchronous seeding operation.</returns>
    private static async Task SeedUnitTypesAsync(AppDbContext db)
    {
        var existingUnitTypes = await db.UnitTypes.Select(t => t.Name).ToListAsync();

        foreach (var (name, isActive) in UnitTypes.Where(t => !existingUnitTypes.Contains(t.Name)))
        {
            db.UnitTypes.Add(new UnitType { Name = name, IsActive = isActive });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedApplicationsAsync(
        AppDbContext db,
        Faker faker,
        (List<ApplicationUser> propertyManagers, List<ApplicationUser> applicants) users,
        DateTime now)
    {
        if (await db.RentalApplications.AnyAsync())
            return;

        var units = await db.Units.OrderBy(u => u.Id).ToListAsync();

        var plan = new (ApplicationStatus Status, int Applicant, int Unit)[]
        {
            (ApplicationStatus.Draft, 0, 0),
            (ApplicationStatus.Draft, 1, 1),
            (ApplicationStatus.Submitted, 0, 2),
            (ApplicationStatus.Submitted, 2, 2),
            (ApplicationStatus.Submitted, 3, 3),
            (ApplicationStatus.Returned, 0, 4),
            (ApplicationStatus.Approved, 1, 5),
            (ApplicationStatus.Approved, 4, 6),
            (ApplicationStatus.Denied, 2, 7),
            (ApplicationStatus.Withdrawn, 3, 8)
        };

        var reviewed = new List<(RentalApplication Application, DateTime SubmittedAt)>();

        for (var i = 0; i < plan.Length; i++)
        {
            var (status, applicantIndex, unitIndex) = plan[i];

            var applicant = users.applicants[applicantIndex];

            var manager = users.propertyManagers[i % users.propertyManagers.Count];

            var createdAt = now.AddDays(-1 * faker.Random.Int(14, 60));

            var application = RentalApplication.NewApplication(
                units[unitIndex].Id,
                applicant,
                createdAt);

            // leave 1 blank application (i != 1)
            if (i != 1)
            {
                SeedApplication(
                    application,
                    applicant,
                    faker,
                    createdAt);
            }

            if (status != ApplicationStatus.Draft)
            {
                var submittedDate = createdAt.AddDays(1);

                var reviewedDate = createdAt.AddDays(3);

                var reviewedToday = DateOnly.FromDateTime(reviewedDate);

                application.Submit(
                    applicant.Id,
                    submittedDate,
                    DateOnly.FromDateTime(submittedDate),
                    []);

                switch (status)
                {
                    case ApplicationStatus.Returned:
                        application.Return(
                            manager.Id,
                            "Return Lease Test Comment from Seeder.",
                            reviewedDate);
                        break;
                    case ApplicationStatus.Approved:
                        application.ApproveApplicationForLease(
                            manager.Id,
                            "Approved Lease Test Comment from Seeder.",
                            reviewedDate,
                            reviewedToday,
                            reviewedToday,
                            []);
                        break;
                    case ApplicationStatus.Denied:
                        application.Deny(
                            manager.Id,
                            "Denied Lease Test Comment from Seeder.",
                            reviewedDate);
                        break;
                    case ApplicationStatus.Withdrawn:
                        application.Withdraw(
                            applicant.Id, reviewedDate);
                        break;

                }

                reviewed.Add((application, submittedDate));
            }

            db.RentalApplications.Add(application);

        }

        await db.SaveChangesAsync();

        SeedNotes(db, faker, users.propertyManagers, reviewed);

        await db.SaveChangesAsync();
    }

    private static void SeedNotes(
        AppDbContext db,
        Faker faker,
        List<ApplicationUser> propertyManagers,
        List<(RentalApplication Application, DateTime SubmittedAt)> reviewed)
    {
        foreach (var (application, submittedAt) in reviewed)
        {
            var noteCount = faker.Random.Int(0, 3);

            for (var n = 0; n < noteCount; n++)
            {
                var manager = faker.PickRandom(propertyManagers);

                var createdAt = submittedAt.AddHours(faker.Random.Int(1, 48));

                var note = ApplicationNote.Create(
                    application.Id,
                    manager.Id,
                    faker.PickRandom(NoteSamples),
                    createdAt);

                if (faker.Random.Bool(0.25f))
                {
                    note.Edit(
                        manager.Id,
                        faker.PickRandom(NoteSamples),
                        createdAt.AddHours(faker.Random.Int(1, 4)));
                }

                db.ApplicationNotes.Add(note);
            }
        }
    }

    /// <summary>
    /// Ensures a user exists and belongs to the specified role.
    /// </summary>
    /// <remarks>Role membership is checked on every invocation so missing role assignments are
    /// repaired.</remarks>
    /// <param name="userManager">The user manager used to find, create, and assign the user.</param>
    /// <param name="email">The user's email address and username.</param>
    /// <param name="firstName">The user's first name.</param>
    /// <param name="lastName">The user's last name.</param>
    /// <param name="role">The role to assign to the user.</param>
    /// <returns>The existing or newly created application user.</returns>
    /// <exception cref="InvalidOperationException">The user could not be created or assigned to the specified role.</exception>
    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string firstName, string lastName, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName
            };
            var result = await userManager.CreateAsync(user, DefaultPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not seed {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        // Checked on every run, not just when the user is created, so a user left without
        // their role (e.g. by an earlier failed run) is repaired.
        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not add {email} to {role}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }
        }

        return user;
    }

    private static void SeedApplication(
        RentalApplication application,
        ApplicationUser applicant,
        Faker faker,
        DateTime createdAtUtc)
    {
        application.UpdateApplicant(
            applicant.FirstName,
            applicant.LastName,
            faker.Phone.PhoneNumber(),
            applicant.Email!,
            GetAddress(faker));

        var moveOut = DateOnly.FromDateTime(createdAtUtc)
                        .AddDays(-1 * faker.Random.Int(0, 30));

        var residenceCount = faker.Random.Int(1, 3);

        for (var r = 0; r < residenceCount; r++)
        {
            var moveIn = moveOut.AddMonths(-1 * faker.Random.Int(12, 36));

            application.AddResidence(
                GetAddress(faker),
                faker.Name.FullName(),
                faker.Phone.PhoneNumber(),
                moveIn,
                moveOut);

            moveOut = moveIn.AddDays(-1);

        }

        application.SetResidenceHistoryCompleted();
    }

    private static string GetAddress(Faker faker) =>
        $"{faker.Address.StreetAddress()}, {faker.Address.City()}, {faker.Address.StateAbbr()} {faker.Address.ZipCode("#####")}";

}
