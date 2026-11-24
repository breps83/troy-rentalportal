using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.Domain;
using RentalPortal.Web.Enums;
using RentalPortal.Web.Extensions;

namespace RentalPortal.Web.Services;

/// <summary>
/// Coordinates rental application lifecycle operations, including starting, submitting, withdrawing, and reviewing
/// applications.
/// </summary>
/// <param name="db">The database context used to access and persist rental applications and related records.</param>
/// <param name="time">The time provider used to determine current dates and timestamps.</param>
/// <param name="logger">The logger used to record application and lease service operations.</param>
public class RentalApplicationService(
    AppDbContext db,
    TimeProvider time,
    ILogger<RentalApplicationService> logger)
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
        if (existing is not null)
            return existing;

        var today = time.GetToday();

        // confirm the unit exists and is not leased today
        var available = await db.Units.AnyAsync(u =>
            u.Id == unitId &&
            !u.Leases.Any(l => l.StartDate <= today && today <= l.EndDate));

        if (!available)
        {
            logger.LogWarning("Unit {UnitId} is not available; user {UserId} can't apply", unitId, applicant.Id);
            return null;
        }

        // start a new application
        var application = RentalApplication.NewApplication(unitId, applicant, Now);

        db.RentalApplications.Add(application);

        await db.SaveChangesAsync();

        return application;
    }

    public async Task<Result> SubmitAsync(RentalApplication application, string userId)
    {
        var today = time.GetToday();

        if (application.ApplicantId != userId)
            return Rejected(application, "submit", userId, "Failed. This may not be your application.");

        if (!application.CanSubmit)
            return Rejected(application, "submit", userId, "Please complete both sections before submitting.");

        var unitLeases = await db.Leases
            .Where(l => l.UnitId == application.UnitId &&
                l.StartDate <= today && today <= l.EndDate)
            .ToListAsync();

        try
        {
            application.Submit(userId, Now, today, unitLeases);
        }
        catch (RuleException ex)
        {
            return Rejected(application, "submit", userId, ex.Message);
        }

        await db.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> WithdrawApplicationAsync(RentalApplication application, string userId)
    {
        if (application.ApplicantId != userId)
            return Rejected(application, "withdraw", userId, "You can only withdraw your own application.");

        if (!application.CanWithdraw)
        {
            return Rejected(application, "withdraw", userId,
                $"An application that is {application.Status} can't be withdrawn.");
        }

        application.Withdraw(userId, Now);

        await db.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ReviewApplicationAsync(
        RentalApplication application,
        ApplicationReviewStatus reviewStatus,
        string? comment,
        string? internalNote,
        string userId)
    {
        if (!application.CanReview)
            return Rejected(application, "review", userId, "The application needs to be submitted before review.");

        if (reviewStatus != ApplicationReviewStatus.Approve && string.IsNullOrWhiteSpace(comment))
            return Result.Failure("A comment is required.");

        Lease? lease = null;

        try
        {
            switch (reviewStatus)
            {
                case ApplicationReviewStatus.Approve:
                    var unitLeases = await db.Leases
                        .Where(l => l.UnitId == application.UnitId)
                        .ToListAsync();

                    var today = time.GetToday();

                    // lease starts today
                    lease = application.ApproveApplicationForLease(
                        userId,
                        comment,
                        Now,
                        today,
                        today,
                        unitLeases);

                    db.Leases.Add(lease);
                    break;

                case ApplicationReviewStatus.Return:
                    application.Return(
                        userId,
                        comment!,
                        Now);
                    break;

                case ApplicationReviewStatus.Deny:
                    application.Deny(
                        userId,
                        comment!,
                        Now);
                    break;

                default:
                    return Result.Failure("Invalid review status");
            }

            if(!string.IsNullOrWhiteSpace(internalNote))
            {
                db.ApplicationNotes.Add(ApplicationNote.Create(
                    application.Id,
                    userId,
                    internalNote,
                    Now));
            }
        }
        catch (RuleException ex)
        {
            return Rejected(application, "review", userId, ex.Message);
        }

        await db.SaveChangesAsync();

        return Result.Success();
    }

    // logs a status change that was refused, then returns the failure
    private Result Rejected(RentalApplication application, string action, string userId, string error)
    {
        logger.LogWarning("Can't {Action} application {ApplicationId} for user {UserId}: {Error}",
            action, application.Id, userId, error);

        return Result.Failure(error);
    }
}
