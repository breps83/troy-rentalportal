using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Domain;

namespace RentalPortal.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Property> Properties => Set<Property>();

    public DbSet<Unit> Units => Set<Unit>();

    public DbSet<UnitType> UnitTypes => Set<UnitType>();

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

        builder.Entity<UnitType>(unitType =>
        {
            unitType.Property(t => t.Name).HasMaxLength(50);
            unitType.HasIndex(t => t.Name).IsUnique();
        });

        builder.Entity<Unit>(unit =>
        {
            unit.Property(u => u.UnitNumber).HasMaxLength(20);
            unit.Property(u => u.MonthlyRent).HasPrecision(10, 2);
            unit.HasIndex(u => new { u.PropertyId, u.UnitNumber }).IsUnique();

            unit.HasOne(u => u.Property).WithMany(p => p.Units).OnDelete(DeleteBehavior.Cascade);
            unit.HasOne(u => u.UnitType).WithMany().OnDelete(DeleteBehavior.Restrict);

        });
    }
}
