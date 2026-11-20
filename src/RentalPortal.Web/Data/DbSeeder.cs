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

        await db.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);

        await SeedDemoUsersAsync(userManager);

        await SeedPropertiesAsync(db);
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
    /// <returns>A task that represents the asynchronous seeding operation.</returns>
    private static async Task SeedDemoUsersAsync(UserManager<ApplicationUser> userManager)
    {
        var faker = new Faker(FakerCulture) { Random = new Randomizer(4021) };

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

    /// <summary>
    /// Seeds the database with property records when none exist.
    /// </summary>
    /// <remarks>Uses a fixed seed and generates eight property records.</remarks>
    /// <param name="db">The database context used to seed and persist property records.</param>
    /// <returns>A task that represents the asynchronous seed operation.</returns>
    private static async Task SeedPropertiesAsync(AppDbContext db)
    {
        if(await db.Properties.AnyAsync())
            return;

        var properties = new Faker<Property>(FakerCulture)
            .UseSeed(1187) // fixed seed, this gives the same sequence of generated data
            .RuleFor(p => p.Name, f => $"{f.Address.StreetName()} {f.PickRandom("Residences", "Apartments", "Condos")}")
            .RuleFor(p => p.Street, f => f.Address.StreetAddress())
            .RuleFor(p => p.City, f => f.Address.City())
            .RuleFor(p => p.State, f => f.Address.StateAbbr())
            .RuleFor(p => p.PostalCode, f => f.Address.ZipCode("#####"))
            .Generate(8);

        db.Properties.AddRange(properties);

        await db.SaveChangesAsync();
    }

}
