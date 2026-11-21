using RentalPortal.Web.Domain;

namespace RentalPortal.Tests;

/// <summary>
/// Tests for the <see cref="RentalApplication"/> rules, checked against the spec
/// (docs/requirements.md): FR 3.a (two sections and a summary), FR 3.b (sections are saved
/// before Submit is available), FR 3.d (editable only in Draft or Returned), FR 4.b (statuses
/// and terminal statuses), FR 4.c (status history), and the applicant actions in the Challenge
/// section (submit and withdraw).
/// </summary>
/// <remarks>
/// Not covered yet, because the domain model doesn't have them:
/// Return, Approve, and Deny (FR 4.a), resubmitting a returned application, and the active lease
/// check at submit and approval (FR 3.e).
/// </remarks>
public class RentalApplicationTests
{
    private static readonly DateTime _createdUtc = new(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _submittedUtc = new(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _withdrawnUtc = new(2026, 11, 3, 11, 0, 0, DateTimeKind.Utc);

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
        application.Submit("applicant-1", _submittedUtc);
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
        Assert.Throws<RuleException>(() => application.Submit("applicant-1", _submittedUtc));
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
        application.Submit("applicant-1", _submittedUtc);

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
        Assert.Throws<RuleException>(() => application.Submit("applicant-1", _submittedUtc));
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
        Assert.Throws<RuleException>(() => application.Submit("applicant-1", _withdrawnUtc));
        Assert.Throws<RuleException>(() => FillApplicantInfo(application));
    }
}
