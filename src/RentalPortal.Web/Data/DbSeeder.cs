using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Domain;

namespace RentalPortal.Web.Data;

public class DbSeeder
{
    // Default password for seeded users
    // In a production application, you would not want to use a default password like this.
    // This is just for demonstration purposes.
    public const string DefaultPassword = "Password1**";
    public const string FakerCulture = "en_US";

    public static readonly (string Name, bool IsActive)[] UnitTypes = 
        [
            ("Studio", true),
            ("Apartment", true),
            ("Condo", true),
            ("Townhouse", true)
        ];
    

    /// <summary>
    /// Migrates the database and seeds default roles and demo users.
    /// </summary>
    /// <param name="services">The application's service provider.</param>
    /// <returns>A task that represents the asynchronous seed operation.</returns>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<AppDbContext>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var faker = new Faker(FakerCulture) { Random = new Randomizer(4021) };

        await db.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);

        await SeedDemoUsersAsync(userManager, faker);

        await SeedUnitTypesAsync(db);
        
        await SeedPropertiesAsync(db, faker);

       
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
    /// Seeds demo property managers and applicants in the identity store.
    /// </summary>
    /// <remarks>Uses deterministic generated applicant names and fixed test credentials.</remarks>
    /// <param name="userManager">The user manager used to create and configure demo users.</param>
    /// <param name="faker">The faker instance used to generate deterministic test data.</param>
    /// <returns>A task that represents the asynchronous seeding operation.</returns>
    private static async Task SeedDemoUsersAsync(UserManager<ApplicationUser> userManager, Faker faker)
    {
        

        var propertyManagers = new List<ApplicationUser>()
        {
            await EnsureUserAsync(userManager, "manager@rentalportal.test", "Bernie", "Lomax", Roles.PropertyManager),
            await EnsureUserAsync(userManager, "manager2@rentalportal.test", "Bill", "Lumbergh", Roles.PropertyManager)
        };

        var applicants = new List<ApplicationUser>()
        {
            await EnsureUserAsync(userManager, "applicant@rentalportal.test", "Arnold", "Schwarzenegger", Roles.Applicant),
            await EnsureUserAsync(userManager, "applicant2@rentalportal.test", "Sylvester", "Stallone", Roles.Applicant),
            await EnsureUserAsync(userManager, "applicant3@rentalportal.test", "Bruce", "Willis", Roles.Applicant),
            await EnsureUserAsync(userManager, "applicant4@rentalportal.test", "Chuck", "Norris", Roles.Applicant)
        };

        for (var i = 4; i <= 8; i++)
        {
            applicants.Add(await EnsureUserAsync(userManager, $"applicant{i}@rentalportal.test", faker.Name.FirstName(), faker.Name.LastName(), Roles.Applicant));
        }

    }


    /// <summary>
    /// Seeds the database with property records when none exist.
    /// </summary>
    /// <remarks>Uses a fixed seed and generates eight property records.</remarks>
    /// <param name="db">The database context used to seed and persist property records.</param>
    /// <returns>A task that represents the asynchronous seed operation.</returns>
    private static async Task SeedPropertiesAsync(AppDbContext db, Faker faker)
    {
        if(await db.Properties.AnyAsync())
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
            var unitCount = faker.Random.Int(5, 8);

            for (var i = 0; i < unitCount; i++)
            {
                var bedrooms = faker.Random.Int(0, 3);
                var type = bedrooms switch
                {
                    0 => types["Studio"],
                    3 => types["Apartment"],
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

    private static async Task SeedUnitTypesAsync(AppDbContext db)
    {
        var existingUnitTypes = await db.UnitTypes.Select(t => t.Name).ToListAsync();

        foreach (var (name, isActive) in UnitTypes.Where(t => !existingUnitTypes.Contains(t.Name)))
        {
            db.UnitTypes.Add(new UnitType { Name = name, IsActive = isActive });
        }

        await db.SaveChangesAsync();
    }

    


    /// <summary>
    /// Ensures that a user with the specified email address exists and assigns the specified role.
    /// </summary>
    /// <param name="userManager">The user manager used to find, create, and manage user roles.</param>
    /// <param name="email">The email address used to identify the user.</param>
    /// <param name="firstName">The user's first name.</param>
    /// <param name="lastName">The user's last name.</param>
    /// <param name="role">The role to assign to a newly created user.</param>
    /// <returns>The existing or newly created application user.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the user cannot be created.</exception>
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

}
