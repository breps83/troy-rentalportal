using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Domain;
using System.Security.Policy;

namespace RentalPortal.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Property> Properties => Set<Property>();

    public DbSet<Unit> Units => Set<Unit>();

    public DbSet<UnitType> UnitTypes => Set<UnitType>();

    public DbSet<RentalApplication> RentalApplications => Set<RentalApplication>();

    public DbSet<Residence> Residences => Set<Residence>();

    public DbSet<ApplicationStatusHistory> ApplicationStatusChanges => Set<ApplicationStatusHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        
        builder.Entity<ApplicationUser>(user =>
        {
            user.Property(u => u.FirstName)
                .HasMaxLength(100).IsRequired();

            user.Property(u => u.LastName)
                .HasMaxLength(100).IsRequired();
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
            unit.Property(u => u.UnitNumber)
                .HasMaxLength(20);
            
            unit.Property(u => u.MonthlyRent)
                .HasPrecision(10, 2);
            
            unit.HasIndex(u => new { u.PropertyId, u.UnitNumber }).IsUnique();

            unit.HasOne(u => u.Property)
                .WithMany(p => p.Units)
                .OnDelete(DeleteBehavior.Cascade);

            unit.HasOne(u => u.UnitType)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

        });

        builder.Entity<RentalApplication>(application =>
        {
            application.Property(a => a.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            application.Property(a => a.FirstName).HasMaxLength(100);

            application.Property(a => a.LastName).HasMaxLength(100);

            application.Property(a => a.Phone).HasMaxLength(25);

            application.Property(a => a.Email).HasMaxLength(256);

            application.Property(a => a.CurrentAddress).HasMaxLength(250);

            application.HasIndex(a => a.Status);

            application.HasOne(a => a.Unit)
                .WithMany(u => u.Applications)
                .OnDelete(DeleteBehavior.Restrict);

            application.HasOne(a => a.Applicant)
                .WithMany()
                .HasForeignKey(a => a.ApplicantId)
                .OnDelete(DeleteBehavior.Restrict);

            application.HasMany(a => a.Residences)
                .WithOne()
                .HasForeignKey(r => r.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            application.HasMany(a => a.StatusHistory)
                .WithOne()
                .HasForeignKey(c => c.RentalApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

        });

        builder.Entity<Residence>(r =>
        {
            r.Property(r => r.Address).HasMaxLength(250);

            r.Property(r => r.LandlordName).HasMaxLength(100);

            r.Property(r => r.LandlordPhone).HasMaxLength(25);
        });

        builder.Entity<ApplicationStatusHistory>(h =>
        {
            h.Property(c => c.FromStatus)
                .HasConversion<string>()
                .HasMaxLength(20);

            h.Property(c => c.ToStatus)
                .HasConversion<string>()
                .HasMaxLength(20);

            h.Property(c => c.Comment)
                .HasMaxLength(1000);

            h.HasOne(c => c.ChangedByUser)
                .WithMany()
                .HasForeignKey(c => c.ChangedById)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
