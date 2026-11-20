using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Domain;

namespace RentalPortal.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Property> Properties => Set<Property>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(user =>
        {
            user.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            user.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        });

        builder.Entity<Property>(property =>
        {
            property.Property(p => p.Name).HasMaxLength(100);
            property.Property(p => p.Street).HasMaxLength(200);
            property.Property(p => p.City).HasMaxLength(100);
            property.Property(p => p.State).HasMaxLength(2);
            property.Property(p => p.PostalCode).HasMaxLength(10);
        });
    }
}
