using System.Security.Claims;
using RentalPortal.Web.Domain;
using RentalPortal.Web.Extensions;

namespace RentalPortal.Tests;

/// <summary>
/// Tests for the view and edit permission rules in <see cref="RentalApplicationExtensions"/>:
/// anyone who owns the application or is a property manager can view it (FR 3.d, FR 4), and only
/// the owner can edit it, and only while it is Draft or Returned (FR 3.d).
/// </summary>
/// <remarks>
/// Returned isn't covered yet, because the domain model has no Return action to reach it (FR 4.a).
/// </remarks>
public class RentalApplicationExtensionsTests
{
    private const string OwnerId = "applicant-1";
    private const string OtherApplicantId = "applicant-2";
    private const string ManagerId = "manager-1";

    private static readonly DateTime _createdUtc = new(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _submittedUtc = new(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _withdrawnUtc = new(2026, 11, 3, 11, 0, 0, DateTimeKind.Utc);

    private static ClaimsPrincipal SignedInAs(string userId, string role) =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Role, role)],
            authenticationType: "Test"));

    private static ClaimsPrincipal Owner() => SignedInAs(OwnerId, Roles.Applicant);

    private static ClaimsPrincipal OtherApplicant() => SignedInAs(OtherApplicantId, Roles.Applicant);

    private static ClaimsPrincipal Manager() => SignedInAs(ManagerId, Roles.PropertyManager);

    private static RentalApplication NewDraft() =>
        RentalApplication.NewApplication(
            unitId: 7,
            new ApplicationUser
            {
                Id = OwnerId,
                FirstName = "Bruce",
                LastName = "Willis",
                Email = "a@rentalportal.test"
            },
            _createdUtc);

    private static RentalApplication NewSubmitted()
    {
        var application = NewDraft();
        application.UpdateApplicant("Bruce", "Willis", "555-0100", "a@rentalportal.test", "1 Main St, Springfield, IL");
        application.AddResidence("10 Oak Ave, Springfield, IL", "Pat Landlord", "555-0199",
            new DateOnly(2022, 1, 1), new DateOnly(2024, 6, 30));
        application.SetResidenceHistoryCompleted();
        application.Submit(OwnerId, _submittedUtc);
        return application;
    }

    private static RentalApplication NewWithdrawn()
    {
        var application = NewDraft();
        application.Withdraw(OwnerId, _withdrawnUtc);
        return application;
    }

    // ---- UserCanView ----

    /// <summary>The applicant who owns an application can view it.</summary>
    [Fact]
    public void UserCanView_Owner_IsTrue()
    {
        // Act
        var canView = NewDraft().UserCanView(Owner());

        // Assert
        Assert.True(canView);
    }

    /// <summary>An applicant can't view someone else's application.</summary>
    [Fact]
    public void UserCanView_OtherApplicant_IsFalse()
    {
        // Act
        var canView = NewDraft().UserCanView(OtherApplicant());

        // Assert
        Assert.False(canView);
    }

    /// <summary>A property manager can view any application, even one they don't own.</summary>
    [Fact]
    public void UserCanView_Manager_IsTrue()
    {
        // Act
        var canView = NewSubmitted().UserCanView(Manager());

        // Assert
        Assert.True(canView);
    }

    /// <summary>The owner can still view an application after it can no longer be edited.</summary>
    [Fact]
    public void UserCanView_OwnerOfWithdrawnApplication_IsTrue()
    {
        // Act
        var canView = NewWithdrawn().UserCanView(Owner());

        // Assert
        Assert.True(canView);
    }

    // ---- UserCanEdit ----

    /// <summary>The owner can edit their application while it is a Draft.</summary>
    [Fact]
    public void UserCanEdit_OwnerOfDraft_IsTrue()
    {
        // Act
        var canEdit = NewDraft().UserCanEdit(Owner());

        // Assert
        Assert.True(canEdit);
    }

    /// <summary>The owner can't edit their application once it has been submitted.</summary>
    [Fact]
    public void UserCanEdit_OwnerOfSubmitted_IsFalse()
    {
        // Act
        var canEdit = NewSubmitted().UserCanEdit(Owner());

        // Assert
        Assert.False(canEdit);
    }

    /// <summary>The owner can't edit an application they withdrew.</summary>
    [Fact]
    public void UserCanEdit_OwnerOfWithdrawn_IsFalse()
    {
        // Act
        var canEdit = NewWithdrawn().UserCanEdit(Owner());

        // Assert
        Assert.False(canEdit);
    }

    /// <summary>An applicant can't edit someone else's application, even while it is a Draft.</summary>
    [Fact]
    public void UserCanEdit_OtherApplicantOnDraft_IsFalse()
    {
        // Act
        var canEdit = NewDraft().UserCanEdit(OtherApplicant());

        // Assert
        Assert.False(canEdit);
    }

    /// <summary>A property manager can view a Draft but can't edit it.</summary>
    [Fact]
    public void UserCanEdit_ManagerOnDraft_IsFalse()
    {
        // Act
        var canEdit = NewDraft().UserCanEdit(Manager());

        // Assert
        Assert.False(canEdit);
    }
}
