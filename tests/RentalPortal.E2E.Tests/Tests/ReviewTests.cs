using System.Globalization;
using Microsoft.Playwright;
using RentalPortal.E2E.Tests.Infrastructure;
using RentalPortal.Web.Enums;
using Xunit.Abstractions;
using static Microsoft.Playwright.Assertions;

namespace RentalPortal.E2E.Tests.Tests;

/// <summary>
/// FR 4: property managers review submitted applications. Approval issues a lease (FR 2.c, FR 3.e).
/// </summary>
public class ReviewTests(E2EFixture fixture, ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private ILocator Status => Page.Locator("#application-status");

    private ILocator History => Page.Locator("#application-history");

    private ILocator ReviewButton => Page.GetByRole(AriaRole.Button, new() { Name = "Review application" });

    private async Task ReviewAsync(string decision, string? comment)
    {
        await Modal.OpenAsync(ReviewButton, "Review application");
        await Modal.Field("Decision").ChooseAsync(decision);

        if (comment != null)
            await Modal.Field("Comment").EnterAsync(comment);
    }

    [Fact]
    [Trait("Requirement", "FR 4.a")]
    [Trait("Requirement", "FR 4.c")]
    [Trait("Requirement", "FR 2.c")]
    [Trait("Requirement", "TR 1.b")]
    public async Task Approve_IssuesTwelveMonthLease_AndRecordsHistory()
    {
        var property = await Data.CreatePropertyAsync();
        var applicationId = await Data.CreateApplicationAsync(
            property.Units[0].Id, TestUsers.Applicant, ApplicationStatus.Submitted);

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync($"/Applications/Details/{applicationId}");

        await Step("FR 4.a", "Review a submitted application in the modal and approve it");
        await ReviewAsync("Approve", "Welcome aboard.");
        await Modal.SubmitAsync();

        await Step("FR 2.c", "Approval creates a lease starting today, for twelve months");
        await Expect(Status).ToContainTextAsync("Approved");
        var today = DateTime.Today;
        var leaseText = today.ToString("MMM d, yyyy", CultureInfo.CurrentCulture) + " – " +
                        today.AddMonths(12).AddDays(-1).ToString("MMM d, yyyy", CultureInfo.CurrentCulture);
        await Expect(Status).ToContainTextAsync(leaseText);
        await Expect(ReviewButton).ToHaveCountAsync(0);

        await Step("FR 4.c", "History shows each status change with who, when, and the comment");
        var history = History.Locator("tbody tr");
        await Expect(history).ToHaveCountAsync(3);
        await Expect(history.Nth(2)).ToContainTextAsync("Submitted →");
        await Expect(history.Nth(2)).ToContainTextAsync("Approved");
        await Expect(history.Nth(2)).ToContainTextAsync("Bernie Lomax");
        await Expect(history.Nth(2)).ToContainTextAsync("Welcome aboard.");

        await Step("FR 4.c", "The applicant sees the outcome but not the history");
        await SwitchUserAsync(TestUsers.Applicant);
        await Page.GotoAsync($"/Applications/Details/{applicationId}");
        await Expect(Status).ToContainTextAsync("Approved");
        await Expect(History).ToBeAttachedAsync();
        await Expect(History.Locator("tr")).ToHaveCountAsync(0);

        await Step("FR 4.c", "Requesting the history section directly as the applicant returns no history");
        var response = await Context.APIRequest.GetAsync($"/Applications/History/{applicationId}");
        Assert.True(response.Ok, $"Expected 200 but got {response.Status}");
        Assert.DoesNotContain("<tr", await response.TextAsync());
    }

    [Fact]
    [Trait("Requirement", "FR 4.a")]
    [Trait("Requirement", "FR 4.b")]
    [Trait("Requirement", "FR 4.c")]
    [Trait("Requirement", "TR 1.b")]
    [Trait("Requirement", "Roles")]
    public async Task Return_RequiresComment_ThenApplicantCorrectsAndResubmits()
    {
        var property = await Data.CreatePropertyAsync();
        var applicationId = await Data.CreateApplicationAsync(
            property.Units[0].Id, TestUsers.Applicant, ApplicationStatus.Submitted);

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync($"/Applications/Details/{applicationId}");

        await Step("FR 4.a", "Return without a comment is refused in the modal");
        await ReviewAsync("Return", comment: null);
        await Modal.SubmitExpectingErrorAsync("A comment is required!");

        // Marker on the window: it is lost if the page does a full reload instead of a partial refresh.
        await Page.EvaluateAsync("() => window.e2eNoReload = true");

        await Step("FR 4.b", "Return with a comment");
        await Modal.Field("Comment").EnterAsync("Please add a second prior residence.");
        await Modal.SubmitAsync();
        await Expect(Status).ToContainTextAsync("Returned");
        await Expect(Status).ToContainTextAsync("Please add a second prior residence.");

        await Step("TR 1.b", "Status and history both refresh in place, without reloading the page");
        var lastChange = History.Locator("tbody tr").Last;
        await Expect(lastChange).ToContainTextAsync("Submitted →");
        await Expect(lastChange).ToContainTextAsync("Returned");
        await Expect(lastChange).ToContainTextAsync("Please add a second prior residence.");
        Assert.True(await Page.EvaluateAsync<bool>("() => window.e2eNoReload === true"),
            "The page reloaded instead of refreshing the status and history sections.");

        await Step("Roles", "The applicant can edit the returned application again");
        await SwitchUserAsync(TestUsers.Applicant);
        await Page.GotoAsync($"/Applications/Details/{applicationId}?section=Info");
        await Expect(Page.GetByLabel("Phone")).ToBeEditableAsync();
        await Page.GetByLabel("Phone").EnterAsync("248-555-0222");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue" }).ClickAsync();

        await Step("Roles", "And resubmit it");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit" }).ClickAsync();
        await Expect(Alert("Thank you. Your application has been submitted!")).ToBeVisibleAsync();
        await Expect(Status).ToContainTextAsync("Submitted");
    }

    [Fact]
    [Trait("Requirement", "FR 4.a")]
    [Trait("Requirement", "FR 4.b")]
    public async Task Deny_WithComment_IsTerminal()
    {
        var property = await Data.CreatePropertyAsync();
        var applicationId = await Data.CreateApplicationAsync(
            property.Units[0].Id, TestUsers.Applicant, ApplicationStatus.Submitted);

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync($"/Applications/Details/{applicationId}");

        await Step("FR 4.a", "Deny requires a comment");
        await ReviewAsync("Deny", comment: null);
        await Modal.SubmitExpectingErrorAsync("A comment is required!");
        await Modal.Field("Comment").EnterAsync("Income requirements not met.");
        await Modal.SubmitAsync();

        await Step("FR 4.b", "Denied is terminal: no further review");
        await Expect(Status).ToContainTextAsync("Denied");
        await Expect(ReviewButton).ToHaveCountAsync(0);
    }

    [Fact]
    [Trait("Requirement", "FR 3.e")]
    public async Task SecondApproval_ForLeasedUnit_IsRefused_AndOtherApplicationUnchanged()
    {
        var property = await Data.CreatePropertyAsync();
        var unitId = property.Units[0].Id;
        var firstId = await Data.CreateApplicationAsync(unitId, TestUsers.Applicant, ApplicationStatus.Submitted);
        var secondId = await Data.CreateApplicationAsync(unitId, TestUsers.OtherApplicant, ApplicationStatus.Submitted);

        await LoginAsAsync(TestUsers.Manager);

        await Step("FR 3.e", "Approving the first application leases the unit");
        await Page.GotoAsync($"/Applications/Details/{firstId}");
        await ReviewAsync("Approve", comment: null);
        await Modal.SubmitAsync();
        await Expect(Status).ToContainTextAsync("Approved");

        await Step("FR 3.e", "The other open application for the unit is left unchanged");
        await Page.GotoAsync($"/Applications/Details/{secondId}");
        await Expect(Status).ToContainTextAsync("Submitted");

        await Step("FR 3.e", "Approving it is refused with an error, preventing a second lease");
        await ReviewAsync("Approve", comment: null);
        await Modal.SubmitExpectingErrorAsync("This unit already has a lease for that period.");
        Assert.Equal(ApplicationStatus.Submitted, await Data.GetStatusAsync(secondId));
    }
}
