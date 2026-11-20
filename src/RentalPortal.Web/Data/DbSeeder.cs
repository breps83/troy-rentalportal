using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Domain;

namespace RentalPortal.Web.Data;

public class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<AppDbContext>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
    }


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

}
