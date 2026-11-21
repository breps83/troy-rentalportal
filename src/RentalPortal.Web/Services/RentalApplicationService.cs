using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.Domain;


namespace RentalPortal.Web.Services;

public class RentalApplicationService(AppDbContext db, TimeProvider time)
{
    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public async Task<RentalApplication?> StartApplicationAsync(int unitId, ApplicationUser applicant)
    {
        // check for existing applications
        var existing = await db.RentalApplications.FirstOrDefaultAsync(a =>
            a.UnitId == unitId &&
            a.ApplicantId == applicant.Id &&
            (
                a.Status == ApplicationStatus.Draft ||
                a.Status == ApplicationStatus.Submitted ||
                a.Status == ApplicationStatus.Returned
            ));

        // existing application found, return it
        if(existing is not null)        
            return existing;
        
        // confirm the unit exists
        if(!await db.Units.AnyAsync(u => u.Id == unitId))        
            return null;
        
        // start a new application
        var application = RentalApplication.NewApplication(unitId, applicant, Now);

        db.RentalApplications.Add(application);

        await db.SaveChangesAsync();

        return application;
    }

}
