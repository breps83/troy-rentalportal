using RentalPortal.Web.Domain;
using RentalPortal.Web.Enums;

namespace RentalPortal.Tests;

/// <summary>
/// Tests for the <see cref="RentalApplication"/> rules, checked against the spec
/// (docs/requirements.md): FR 3.a (two sections and a summary), FR 3.b (sections are saved
/// before Submit is available), FR 3.d (editable only in Draft or Returned), FR 4.b (statuses
/// and terminal statuses), FR 4.c (status history), and the applicant actions in the Challenge
/// section (submit and withdraw).
/// </summary>
/// <remarks>
/// Not covered yet, because the domain model doesn't have it:
/// the active lease check at submit and approval (FR 3.e).
/// </remarks>
public class RentalApplicationTests
{
    private static readonly DateTime _createdUtc = new(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _submittedUtc = new(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly _submittedToday = new(2026, 11, 2);
    private static readonly DateTime _withdrawnUtc = new(2026, 11, 3, 11, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _reviewedUtc = new(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly _reviewedToday = new(2026, 11, 4);
    private static readonly DateOnly _leaseStart = new(2026, 12, 1);
    private const string ManagerId = "manager-1";
    private static readonly Lease[] _noLeases = [];
    private static readonly DateOnly _moveIn = new(2022, 1, 1);
    private static readonly DateOnly _moveOut = new(2024, 6, 30);

    private static ApplicationUser NewApplicant() => new()
    {
        Id = "applicant-1",
        FirstName = "Bruce",
        LastName = "Willis",
        Email = "applicant3@rentalportal.test"
    };

    /// <summary>A new Draft application for unit 7, with nothing filled in.</summary>
    private static RentalApplication NewDraft() =>
        RentalApplication.NewApplication(unitId: 7, NewApplicant(), _createdUtc);

    private static void FillApplicantInfo(RentalApplication application) =>
        application.UpdateApplicant("Bruce", "Willis", "555-0100", "applicant3@rentalportal.test", "1 Main St, Springfield, IL 62701");

    private static Residence AddResidence(RentalApplication application, int id = 1)
    {
        var residence = application.AddResidence("10 Oak Ave, Springfield, IL 62701", "Pat Landlord", "555-0199", _moveIn, _moveOut);

        // Ids are normally assigned by the database; set one so the residence can be found by id.
        residence.Id = id;

        return residence;
    }

    /// <summary>A Draft application with both sections saved, ready to submit.</summary>
    private static RentalApplication NewCompleteDraft()
    {
        var application = NewDraft();
        FillApplicantInfo(application);
        AddResidence(application);
        application.SetResidenceHistoryCompleted();
        return application;
    }

    private static RentalApplication NewSubmitted()
    {
        var application = NewCompleteDraft();
        application.Submit("applicant-1", _submittedUtc, _submittedToday, _noLeases);
        return application;
    }

    // ---- Creating an application ----

    /// <summary>
    /// A new application starts as Draft for the chosen unit, with the applicant's name and email
    /// filled in from their account.
    /// </summary>
    [Fact]
    public void NewApplication_StartsAsDraftForTheUnit_WithAccountDetailsFilledIn()
    {
        // Act
        var application = NewDraft();

        // Assert
        Assert.Equal(ApplicationStatus.Draft, application.Status);
        Assert.Equal(7, application.UnitId);
        Assert.Equal("applicant-1", application.ApplicantId);
        Assert.Equal("Bruce", application.FirstName);
        Assert.Equal("Willis", application.LastName);
        Assert.Equal("applicant3@rentalportal.test", application.Email);
        Assert.Equal(_createdUtc, application.CreatedAtUtc);
        Assert.Null(application.SubmittedAtUtc);
    }

    /// <summary>FR 4.c: creating the application is the first entry in its status history.</summary>
    [Fact]
    public void NewApplication_RecordsDraftAsFirstStatusChange()
    {
        // Act
        var application = NewDraft();

        // Assert
        var change = Assert.Single(application.StatusHistory);
        Assert.Null(change.FromStatus);
        Assert.Equal(ApplicationStatus.Draft, change.ToStatus);
        Assert.Equal("applicant-1", change.ChangedById);
        Assert.Equal(_createdUtc, change.ChangedAtUtc);
    }

    /// <summary>
    /// FR 3.b and 3.d: a new Draft is editable but can't be submitted until both sections are saved.
    /// </summary>
    [Fact]
    public void NewApplication_IsEditable_ButCannotBeSubmitted()
    {
        // Act
        var application = NewDraft();

        // Assert
        Assert.True(application.IsEditable);
        Assert.False(application.IsTerminal);
        Assert.False(application.CanSubmit);
        Assert.False(application.ApplicantInfoCompleted);
        Assert.False(application.ResidenceHistoryCompleted);
    }

    // ---- Applicant Information section (FR 3.a.i) ----

    /// <summary>Saving the section trims each value and marks the section as saved.</summary>
    [Fact]
    public void UpdateApplicant_TrimsValues_AndMarksSectionCompleted()
    {
        // Arrange
        var application = NewDraft();

        // Act
        application.UpdateApplicant("  Bruce ", " Willis ", " 555-0100 ", " bruce@example.test ", " 1 Main St ");

        // Assert
        Assert.Equal("Bruce", application.FirstName);
        Assert.Equal("Willis", application.LastName);
        Assert.Equal("555-0100", application.Phone);
        Assert.Equal("bruce@example.test", application.Email);
        Assert.Equal("1 Main St", application.CurrentAddress);
        Assert.True(application.ApplicantInfoCompleted);
    }

    // ---- Residence History section (FR 3.a.ii, 3.c) ----

    [Fact]
    public void AddResidence_AddsTrimmedResidence()
    {
        // Arrange
        var application = NewDraft();

        // Act
        var residence = application.AddResidence(" 10 Oak Ave ", " Pat Landlord ", " 555-0199 ", _moveIn, _moveOut);

        // Assert
        Assert.Same(residence, Assert.Single(application.Residences));
        Assert.Equal("10 Oak Ave", residence.Address);
        Assert.Equal("Pat Landlord", residence.LandlordName);
        Assert.Equal("555-0199", residence.LandlordPhone);
        Assert.Equal(_moveIn, residence.MoveInDate);
        Assert.Equal(_moveOut, residence.MoveOutDate);
    }

    [Fact]
    public void AddResidence_MoveOutBeforeMoveIn_IsRejected()
    {
        // Arrange
        var application = NewDraft();

        // Act & Assert
        Assert.Throws<RuleException>(() =>
            application.AddResidence("10 Oak Ave", "Pat Landlord", "555-0199", _moveOut, _moveIn));
        Assert.Empty(application.Residences);
    }

    /// <summary>Moving in and out on the same day is allowed; only move-out before move-in is rejected.</summary>
    [Fact]
    public void AddResidence_MoveOutSameDayAsMoveIn_IsAllowed()
    {
        // Arrange
        var application = NewDraft();

        // Act
        application.AddResidence("10 Oak Ave", "Pat Landlord", "555-0199", _moveIn, _moveIn);

        // Assert
        Assert.Single(application.Residences);
    }

    [Fact]
    public void UpdateResidence_ChangesTheMatchingResidence()
    {
        // Arrange
        var application = NewDraft();
        var residence = AddResidence(application, id: 5);
        var newMoveOut = new DateOnly(2025, 1, 31);

        // Act
        application.UpdateResidence(5, "22 Elm St", "Sam Owner", "555-0123", _moveIn, newMoveOut);

        // Assert
        Assert.Equal("22 Elm St", residence.Address);
        Assert.Equal("Sam Owner", residence.LandlordName);
        Assert.Equal("555-0123", residence.LandlordPhone);
        Assert.Equal(newMoveOut, residence.MoveOutDate);
    }

    [Fact]
    public void RemoveResidence_RemovesTheMatchingResidence()
    {
        // Arrange
        var application = NewDraft();
        AddResidence(application, id: 1);
        AddResidence(application, id: 2);

        // Act
        application.RemoveResidence(1);

        // Assert
        Assert.Equal(2, Assert.Single(application.Residences).Id);
    }

    /// <summary>A residence id from another application (or a made-up one) is rejected.</summary>
    [Fact]
    public void UpdateOrRemoveResidence_UnknownId_IsRejected()
    {
        // Arrange
        var application = NewDraft();
        AddResidence(application, id: 1);

        // Act & Assert
        Assert.Throws<RuleException>(() =>
            application.UpdateResidence(99, "22 Elm St", "Sam Owner", "555-0123", _moveIn, _moveOut));
        Assert.Throws<RuleException>(() => application.RemoveResidence(99));
    }

    /// <summary>The Residence History section can't be saved without at least one prior residence.</summary>
    [Fact]
    public void SetResidenceHistoryCompleted_WithNoResidences_IsRejected()
    {
        // Arrange
        var application = NewDraft();

        // Act & Assert
        Assert.Throws<RuleException>(application.SetResidenceHistoryCompleted);
        Assert.False(application.ResidenceHistoryCompleted);
    }

    /// <summary>
    /// FR 3.b: any change to the residences after the section was saved means it has to be saved
    /// (Continue) again before Submit is available.
    /// </summary>
    [Fact]
    public void ChangingResidences_AfterSectionSaved_RequiresSavingItAgain()
    {
        // Add
        var added = NewCompleteDraft();
        AddResidence(added, id: 2);
        Assert.False(added.ResidenceHistoryCompleted);
        Assert.False(added.CanSubmit);

        // Update
        var updated = NewCompleteDraft();
        updated.UpdateResidence(1, "22 Elm St", "Sam Owner", "555-0123", _moveIn, _moveOut);
        Assert.False(updated.ResidenceHistoryCompleted);
        Assert.False(updated.CanSubmit);

        // Remove
        var removed = NewCompleteDraft();
        removed.RemoveResidence(1);
        Assert.False(removed.ResidenceHistoryCompleted);
        Assert.False(removed.CanSubmit);
    }

    // ---- Submit (FR 3.b.iii) ----

    /// <summary>FR 3.b.iii: Submit is available only once both sections have been saved.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Submit_BeforeBothSectionsSaved_IsRejected(bool saveApplicantInfo, bool saveResidenceHistory)
    {
        // Arrange
        var application = NewDraft();
        if (saveApplicantInfo)
        {
            FillApplicantInfo(application);
        }

        if (saveResidenceHistory)
        {
            AddResidence(application);
            application.SetResidenceHistoryCompleted();
        }

        // Act & Assert
        Assert.False(application.CanSubmit);
        Assert.Throws<RuleException>(() =>
            application.Submit("applicant-1", _submittedUtc, _submittedToday, _noLeases));
        Assert.Equal(ApplicationStatus.Draft, application.Status);
        Assert.Null(application.SubmittedAtUtc);
    }

    [Fact]
    public void Submit_WithBothSectionsSaved_MovesToSubmitted_AndRecordsHistory()
    {
        // Arrange
        var application = NewCompleteDraft();
        Assert.True(application.CanSubmit);

        // Act
        application.Submit("applicant-1", _submittedUtc, _submittedToday, _noLeases);

        // Assert
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Equal(_submittedUtc, application.SubmittedAtUtc);

        var change = application.StatusHistory.Last();
        Assert.Equal(ApplicationStatus.Draft, change.FromStatus);
        Assert.Equal(ApplicationStatus.Submitted, change.ToStatus);
        Assert.Equal("applicant-1", change.ChangedById);
        Assert.Equal(_submittedUtc, change.ChangedAtUtc);
    }

    [Fact]
    public void Submit_WhenAlreadySubmitted_IsRejected()
    {
        // Arrange
        var application = NewSubmitted();

        // Act & Assert
        Assert.False(application.CanSubmit);
        Assert.Throws<RuleException>(() =>
            application.Submit("applicant-1", _submittedUtc, _submittedToday, _noLeases));
    }

    /// <summary>FR 3.e: submit is rejected when the unit has a lease covering today.</summary>
    [Fact]
    public void Submit_UnitHasActiveLease_IsRejected()
    {
        // Arrange
        var application = NewCompleteDraft();

        // Active on Nov 2, 2026 (Jan 1 through Dec 31, 2026).
        var active = ExistingLease(new DateOnly(2026, 1, 1));

        // Act & Assert
        Assert.Throws<RuleException>(() =>
            application.Submit("applicant-1", _submittedUtc, _submittedToday, [active]));
        Assert.Equal(ApplicationStatus.Draft, application.Status);
        Assert.Null(application.SubmittedAtUtc);
    }

    /// <summary>
    /// FR 3.e: leases that aren't active today (ended, or not started yet) and leases on other units
    /// don't block submit.
    /// </summary>
    [Fact]
    public void Submit_NoActiveLeaseOnThisUnit_IsAllowed()
    {
        // Arrange
        var application = NewCompleteDraft();
        var ended = ExistingLease(new DateOnly(2025, 1, 1));
        var future = ExistingLease(new DateOnly(2026, 12, 1));
        var otherUnit = ExistingLease(new DateOnly(2026, 1, 1), unitId: 8);

        // Act
        application.Submit("applicant-1", _submittedUtc, _submittedToday, [ended, future, otherUnit]);

        // Assert
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
    }

    // ---- Read-only once submitted (FR 3.d) ----

    /// <summary>
    /// FR 3.d: outside Draft and Returned every section is read-only, and the model rejects changes
    /// even if a controller passes them through.
    /// </summary>
    [Fact]
    public void SubmittedApplication_IsReadOnly()
    {
        // Arrange
        var application = NewSubmitted();

        // Act & Assert
        Assert.False(application.IsEditable);
        Assert.Throws<RuleException>(() => FillApplicantInfo(application));
        Assert.Throws<RuleException>(() =>
            application.AddResidence("22 Elm St", "Sam Owner", "555-0123", _moveIn, _moveOut));
        Assert.Throws<RuleException>(() =>
            application.UpdateResidence(1, "22 Elm St", "Sam Owner", "555-0123", _moveIn, _moveOut));
        Assert.Throws<RuleException>(() => application.RemoveResidence(1));
        Assert.Throws<RuleException>(application.SetResidenceHistoryCompleted);
    }

    // ---- Withdraw (Challenge a, FR 4.b) ----

    /// <summary>
    /// An applicant can withdraw an application that isn't finished yet. Withdrawn is terminal
    /// (FR 4.b) and the change is recorded in the history (FR 4.c).
    /// </summary>
    [Fact]
    public void Withdraw_Draft_MovesToWithdrawn_AndRecordsHistory()
    {
        // Arrange
        var application = NewDraft();
        Assert.True(application.CanWithdraw);

        // Act
        application.Withdraw("applicant-1", _withdrawnUtc);

        // Assert
        Assert.Equal(ApplicationStatus.Withdrawn, application.Status);
        Assert.True(application.IsTerminal);

        var change = application.StatusHistory.Last();
        Assert.Equal(ApplicationStatus.Draft, change.FromStatus);
        Assert.Equal(ApplicationStatus.Withdrawn, change.ToStatus);
        Assert.Equal("applicant-1", change.ChangedById);
        Assert.Equal(_withdrawnUtc, change.ChangedAtUtc);
    }

    [Fact]
    public void Withdraw_Submitted_MovesToWithdrawn()
    {
        // Arrange
        var application = NewSubmitted();

        // Act
        application.Withdraw("applicant-1", _withdrawnUtc);

        // Assert
        Assert.Equal(ApplicationStatus.Withdrawn, application.Status);
    }

    /// <summary>
    /// FR 4.b: Withdrawn is terminal, so the application can't be withdrawn again, edited, or
    /// submitted.
    /// </summary>
    [Fact]
    public void WithdrawnApplication_IsTerminal_AndAcceptsNoFurtherChanges()
    {
        // Arrange
        var application = NewCompleteDraft();
        application.Withdraw("applicant-1", _withdrawnUtc);

        // Act & Assert
        Assert.False(application.IsEditable);
        Assert.False(application.CanWithdraw);
        Assert.False(application.CanSubmit);
        Assert.Throws<RuleException>(() => application.Withdraw("applicant-1", _withdrawnUtc));
        Assert.Throws<RuleException>(() =>
            application.Submit("applicant-1", _withdrawnUtc, DateOnly.FromDateTime(_withdrawnUtc), _noLeases));
        Assert.Throws<RuleException>(() => FillApplicantInfo(application));
    }

    // ---- Review: Approve, Return, Deny (FR 4.a) ----

    /// <summary>Only a Submitted application can be reviewed.</summary>
    [Fact]
    public void CanReview_IsTrueOnlyWhenSubmitted()
    {
        Assert.False(NewDraft().CanReview);
        Assert.True(NewSubmitted().CanReview);

        var withdrawn = NewSubmitted();
        withdrawn.Withdraw("applicant-1", _withdrawnUtc);
        Assert.False(withdrawn.CanReview);
    }

    /// <summary>
    /// Approving moves the application to Approved (terminal), records the manager and comment in
    /// the history, and creates a lease for the unit.
    /// </summary>
    [Fact]
    public void Approve_Submitted_MovesToApproved_RecordsHistory_AndCreatesLease()
    {
        // Arrange
        var application = NewSubmitted();

        // Act
        var lease = application.ApproveApplicationForLease(
            ManagerId, " Welcome aboard ", _reviewedUtc, _reviewedToday, _leaseStart, _noLeases);

        // Assert
        Assert.Equal(ApplicationStatus.Approved, application.Status);
        Assert.True(application.IsTerminal);
        Assert.False(application.CanReview);

        var change = application.StatusHistory.Last();
        Assert.Equal(ApplicationStatus.Submitted, change.FromStatus);
        Assert.Equal(ApplicationStatus.Approved, change.ToStatus);
        Assert.Equal(ManagerId, change.ChangedById);
        Assert.Equal(_reviewedUtc, change.ChangedAtUtc);
        Assert.Equal("Welcome aboard", change.Comment);

        Assert.Same(lease, application.Lease);
        Assert.Same(application, lease.RentalApplication);
        Assert.Equal(7, lease.UnitId);
        Assert.Equal(_leaseStart, lease.StartDate);
    }

    /// <summary>A comment is optional when approving.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Approve_WithoutComment_IsAllowed(string? comment)
    {
        // Arrange
        var application = NewSubmitted();

        // Act
        application.ApproveApplicationForLease(
            ManagerId, comment, _reviewedUtc, _reviewedToday, _leaseStart, _noLeases);

        // Assert
        Assert.Equal(ApplicationStatus.Approved, application.Status);
        Assert.NotNull(application.Lease);
    }

    [Fact]
    public void Return_Submitted_MovesToReturned_AndRecordsHistory()
    {
        // Arrange
        var application = NewSubmitted();

        // Act
        application.Return(ManagerId, " Please add a landlord phone ", _reviewedUtc);

        // Assert
        Assert.Equal(ApplicationStatus.Returned, application.Status);
        Assert.False(application.IsTerminal);
        Assert.Null(application.Lease);

        var change = application.StatusHistory.Last();
        Assert.Equal(ApplicationStatus.Submitted, change.FromStatus);
        Assert.Equal(ApplicationStatus.Returned, change.ToStatus);
        Assert.Equal(ManagerId, change.ChangedById);
        Assert.Equal(_reviewedUtc, change.ChangedAtUtc);
        Assert.Equal("Please add a landlord phone", change.Comment);
    }

    /// <summary>
    /// FR 3.d: a Returned application is editable again and, with its sections still saved, can be
    /// resubmitted.
    /// </summary>
    [Fact]
    public void ReturnedApplication_IsEditable_AndCanBeResubmitted()
    {
        // Arrange
        var application = NewSubmitted();
        application.Return(ManagerId, "Please fix", _reviewedUtc);

        // Act
        Assert.True(application.IsEditable);
        Assert.True(application.CanSubmit);
        application.Submit("applicant-1", _reviewedUtc.AddDays(1), _reviewedToday.AddDays(1), _noLeases);

        // Assert
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Equal(ApplicationStatus.Returned, application.StatusHistory.Last().FromStatus);
        Assert.True(application.CanReview);
    }

    [Fact]
    public void Deny_Submitted_MovesToDenied_AndRecordsHistory()
    {
        // Arrange
        var application = NewSubmitted();

        // Act
        application.Deny(ManagerId, "Incomplete rental history", _reviewedUtc);

        // Assert
        Assert.Equal(ApplicationStatus.Denied, application.Status);
        Assert.True(application.IsTerminal);
        Assert.False(application.IsEditable);
        Assert.Null(application.Lease);

        var change = application.StatusHistory.Last();
        Assert.Equal(ApplicationStatus.Submitted, change.FromStatus);
        Assert.Equal(ApplicationStatus.Denied, change.ToStatus);
        Assert.Equal(ManagerId, change.ChangedById);
        Assert.Equal("Incomplete rental history", change.Comment);
    }

    /// <summary>Return and Deny need a comment so the applicant knows why.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ReturnOrDeny_WithoutComment_IsRejected(string? comment)
    {
        // Arrange
        var application = NewSubmitted();
        var historyCount = application.StatusHistory.Count;

        // Act & Assert
        Assert.Throws<RuleException>(() => application.Return(ManagerId, comment!, _reviewedUtc));
        Assert.Throws<RuleException>(() => application.Deny(ManagerId, comment!, _reviewedUtc));
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Equal(historyCount, application.StatusHistory.Count);
    }

    /// <summary>Draft, Returned, Approved, Denied, and Withdrawn applications can't be reviewed.</summary>
    [Fact]
    public void Review_WhenNotSubmitted_IsRejected()
    {
        // Arrange
        var draft = NewCompleteDraft();

        var returned = NewSubmitted();
        returned.Return(ManagerId, "Please fix", _reviewedUtc);

        var approved = NewSubmitted();
        approved.ApproveApplicationForLease(ManagerId, null, _reviewedUtc, _reviewedToday, _leaseStart, _noLeases);

        var denied = NewSubmitted();
        denied.Deny(ManagerId, "No", _reviewedUtc);

        var withdrawn = NewSubmitted();
        withdrawn.Withdraw("applicant-1", _withdrawnUtc);

        // Act & Assert
        foreach (var application in new[] { draft, returned, approved, denied, withdrawn })
        {
            var status = application.Status;
            var lease = application.Lease;

            Assert.Throws<RuleException>(() =>
                application.ApproveApplicationForLease(
                    ManagerId, null, _reviewedUtc, _reviewedToday, _leaseStart, _noLeases));
            Assert.Throws<RuleException>(() => application.Return(ManagerId, "Again", _reviewedUtc));
            Assert.Throws<RuleException>(() => application.Deny(ManagerId, "Again", _reviewedUtc));

            Assert.Equal(status, application.Status);
            Assert.Same(lease, application.Lease);
        }
    }

    // ---- Approval checks: lease start date and existing leases (FR 2.c, FR 3.e) ----

    /// <summary>A lease on unit 7 (the unit used by these tests) with a 12-month term.</summary>
    private static Lease ExistingLease(DateOnly startDate, int unitId = 7) => new()
    {
        UnitId = unitId,
        StartDate = startDate,
        EndDate = startDate.AddMonths(Lease.DefaultTermMonths).AddDays(-1)
    };

    private static void AssertApprovalRejected(RentalApplication application, int historyCount)
    {
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Null(application.Lease);
        Assert.Equal(historyCount, application.StatusHistory.Count);
    }

    /// <summary>The lease can start today (the review date) but not before it.</summary>
    [Fact]
    public void Approve_StartDateInThePast_IsRejected()
    {
        // Arrange
        var application = NewSubmitted();
        var historyCount = application.StatusHistory.Count;
        var yesterday = _reviewedToday.AddDays(-1);

        // Act & Assert
        Assert.Throws<RuleException>(() =>
            application.ApproveApplicationForLease(
                ManagerId, null, _reviewedUtc, _reviewedToday, yesterday, _noLeases));
        Assert.Throws<RuleException>(() =>
            application.ApproveApplicationForLease(
                ManagerId, null, _reviewedUtc, _reviewedToday, default, _noLeases));
        AssertApprovalRejected(application, historyCount);
    }

    [Fact]
    public void Approve_StartDateToday_IsAllowed()
    {
        // Arrange
        var application = NewSubmitted();
        // Act
        var lease = application.ApproveApplicationForLease(
            ManagerId, null, _reviewedUtc, _reviewedToday, _reviewedToday, _noLeases);

        // Assert
        Assert.Equal(_reviewedToday, lease.StartDate);
    }

    /// <summary>
    /// "Today" is the local calendar date. In the evening in the US the UTC date is already
    /// tomorrow, and a lease starting on the local today must still be allowed.
    /// </summary>
    [Fact]
    public void Approve_UtcDateIsAheadOfLocalToday_StartDateTodayIsAllowed()
    {
        // Arrange
        var application = NewSubmitted();

        // 7 PM Eastern on Nov 4 is midnight UTC on Nov 5.
        var reviewedUtc = new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc);
        var localToday = new DateOnly(2026, 11, 4);

        // Act
        var lease = application.ApproveApplicationForLease(
            ManagerId, null, reviewedUtc, localToday, localToday, _noLeases);

        // Assert
        Assert.Equal(ApplicationStatus.Approved, application.Status);
        Assert.Equal(localToday, lease.StartDate);
    }

    /// <summary>FR 3.e: approval is rejected when the unit has a lease covering today.</summary>
    [Fact]
    public void Approve_UnitHasActiveLease_IsRejected()
    {
        // Arrange
        var application = NewSubmitted();
        var historyCount = application.StatusHistory.Count;

        // Active today (Nov 4, 2026) and ends Nov 30, 2026, before the new lease would start.
        var active = ExistingLease(new DateOnly(2025, 12, 1));

        // Act & Assert
        Assert.Throws<RuleException>(() =>
            application.ApproveApplicationForLease(
                ManagerId, null, _reviewedUtc, _reviewedToday, _leaseStart, [active]));
        AssertApprovalRejected(application, historyCount);
    }

    /// <summary>
    /// FR 3.e: a second lease for the same period is prevented, including a future lease that isn't
    /// active yet.
    /// </summary>
    [Theory]
    [InlineData("2026-12-01")]
    [InlineData("2027-06-01")]
    [InlineData("2027-11-30")]
    public void Approve_OverlapsAFutureLease_IsRejected(string existingStart)
    {
        // Arrange
        var application = NewSubmitted();
        var historyCount = application.StatusHistory.Count;
        var future = ExistingLease(DateOnly.Parse(existingStart));

        // Act & Assert
        Assert.Throws<RuleException>(() =>
            application.ApproveApplicationForLease(
                ManagerId, null, _reviewedUtc, _reviewedToday, _leaseStart, [future]));
        AssertApprovalRejected(application, historyCount);
    }

    /// <summary>
    /// Leases that ended before today, leases that start after the new term ends, and leases on
    /// other units don't block approval.
    /// </summary>
    [Fact]
    public void Approve_WithNonConflictingLeases_IsAllowed()
    {
        // Arrange
        var application = NewSubmitted();
        var ended = ExistingLease(new DateOnly(2025, 1, 1));
        var startsAfter = ExistingLease(new DateOnly(2027, 12, 1));
        var otherUnit = ExistingLease(_leaseStart, unitId: 8);

        // Act
        var lease = application.ApproveApplicationForLease(
            ManagerId, null, _reviewedUtc, _reviewedToday, _leaseStart, [ended, startsAfter, otherUnit]);

        // Assert
        Assert.Equal(ApplicationStatus.Approved, application.Status);
        Assert.Same(lease, application.Lease);
    }
}
