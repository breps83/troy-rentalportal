using System.Text.RegularExpressions;
using Microsoft.Playwright;
using RentalPortal.E2E.Tests.Infrastructure;
using RentalPortal.Web.Enums;
using Xunit.Abstractions;
using static Microsoft.Playwright.Assertions;

namespace RentalPortal.E2E.Tests.Tests;

/// <summary>
/// "Roles and permissions" in the checklist: the UI shows only what a role can do, and the server
/// refuses the rest even when the request is crafted by hand.
/// </summary>
public class RolesAndPermissionsTests(E2EFixture fixture, ITestOutputHelper output)
    : E2ETestBase(fixture, output)
{
    private ILocator AccessDeniedHeading => Page.GetByRole(AriaRole.Heading, new() { Name = "Access denied" });

    [Fact]
    [Trait("Requirement", "Roles")]
    public async Task Applicant_SeesOnlyApplicantActions()
    {
        var property = await Data.CreatePropertyAsync();

        await LoginAsAsync(TestUsers.Applicant);

        await Step("Roles", "Applicant navbar: Available Units and My Applications, no Properties");
        await Expect(NavBar.GetByRole(AriaRole.Link, new() { Name = "Available Units" })).ToBeVisibleAsync();
        await Expect(NavBar.GetByRole(AriaRole.Link, new() { Name = "My Applications" })).ToBeVisibleAsync();
        await Expect(NavBar.GetByRole(AriaRole.Link, new() { Name = "Properties", Exact = true }))
            .ToHaveCountAsync(0);

        await Step("Roles", "Applicants get an Apply button on available units");
        await Page.GotoAsync("/Listings");
        var group = Page.Locator("tbody").Filter(new() { HasText = property.Name });
        await Expect(group.GetByRole(AriaRole.Button, new() { Name = "Apply for Unit" })).ToBeVisibleAsync();
        await Expect(group.GetByRole(AriaRole.Button, new() { Name = "Edit" })).ToHaveCountAsync(0);
        await Expect(group.GetByRole(AriaRole.Button, new() { Name = "Delete" })).ToHaveCountAsync(0);

        await Step("Roles", "Manager pages are refused for an applicant");
        await Page.GotoAsync("/Properties");
        await Expect(AccessDeniedHeading).ToBeVisibleAsync();
        await Page.GotoAsync($"/Properties/Details/{property.Id}");
        await Expect(AccessDeniedHeading).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("Requirement", "Roles")]
    public async Task Manager_SeesOnlyManagerActions()
    {
        var property = await Data.CreatePropertyAsync();
        var draftId = await Data.CreateApplicationAsync(
            property.Units[0].Id, TestUsers.Applicant, ApplicationStatus.Draft);

        await LoginAsAsync(TestUsers.Manager);

        await Step("Roles", "Manager navbar includes Properties");
        await Expect(NavBar.GetByRole(AriaRole.Link, new() { Name = "Properties", Exact = true })).ToBeVisibleAsync();

        await Step("Roles", "Managers can browse units and edit or delete them, but can't apply");
        await Page.GotoAsync("/Listings");
        var group = Page.Locator("tbody").Filter(new() { HasText = property.Name });
        await Expect(group).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Apply for Unit" })).ToHaveCountAsync(0);
        await Expect(group.GetByRole(AriaRole.Button, new() { Name = "Edit" })).ToBeVisibleAsync();
        await Expect(group.GetByRole(AriaRole.Button, new() { Name = "Delete" })).ToBeVisibleAsync();

        await Step("Roles", "Managers see an application read-only, with no edit or withdraw buttons");
        await Page.GotoAsync($"/Applications/Details/{draftId}");
        await Expect(Page.Locator("#application-status")).ToContainTextAsync("Draft");
        var breadcrumb = Page.GetByRole(AriaRole.Navigation, new() { Name = "breadcrumb" });
        await Expect(breadcrumb.GetByRole(AriaRole.Link, new() { Name = "Applications", Exact = true }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Continue" })).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Withdraw application" })).ToHaveCountAsync(0);

        await Step("Roles", "No Review button while the application is a Draft");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Review application" })).ToHaveCountAsync(0);
    }

    [Fact]
    [Trait("Requirement", "Roles")]
    [Trait("Requirement", "FR 3.d")]
    public async Task OtherApplicant_CannotViewOrEditAnApplication()
    {
        var property = await Data.CreatePropertyAsync();
        var applicationId = await Data.CreateApplicationAsync(
            property.Units[0].Id, TestUsers.Applicant, ApplicationStatus.Draft);

        await LoginAsAsync(TestUsers.OtherApplicant);

        await Step("Roles", "Another applicant's application is refused");
        await Page.GotoAsync($"/Applications/Details/{applicationId}");
        await Expect(AccessDeniedHeading).ToBeVisibleAsync();

        await Step("FR 3.d", "A crafted post to another applicant's application is refused on the server");
        var response = await PostFormAsync($"/Applications/Details/{applicationId}", new()
        {
            ["command"] = "continue",
            ["CurrentSection"] = "Info",
            ["ApplicantInfo.FirstName"] = "Mallory",
            ["ApplicantInfo.LastName"] = "Intruder",
            ["ApplicantInfo.Phone"] = "217-555-0111",
            ["ApplicantInfo.Email"] = "mallory@example.com",
            ["ApplicantInfo.CurrentAddress"] = "1 Hack St"
        });

        Assert.Equal(302, response.Status);
        Assert.Contains("/Account/AccessDenied", response.Headers["location"]);
    }

    [Fact]
    [Trait("Requirement", "FR 3.d")]
    public async Task SubmittedApplication_RejectsCraftedEdit()
    {
        var property = await Data.CreatePropertyAsync();
        var applicationId = await Data.CreateApplicationAsync(
            property.Units[0].Id, TestUsers.Applicant, ApplicationStatus.Submitted);

        await LoginAsAsync(TestUsers.Applicant);
        await Page.GotoAsync($"/Applications/Details/{applicationId}");

        await Step("FR 3.d", "The page offers no way to edit a submitted application");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Continue" })).ToHaveCountAsync(0);

        await Step("FR 3.d", "A crafted post is refused with a message, not saved");
        var response = await PostFormAsync($"/Applications/Details/{applicationId}", new()
        {
            ["command"] = "continue",
            ["CurrentSection"] = "Info",
            ["ApplicantInfo.FirstName"] = "Changed",
            ["ApplicantInfo.LastName"] = "Name",
            ["ApplicantInfo.Phone"] = "217-555-0111",
            ["ApplicantInfo.Email"] = "changed@example.com",
            ["ApplicantInfo.CurrentAddress"] = "1 Changed St"
        });

        Assert.Equal(302, response.Status);

        await Page.GotoAsync($"/Applications/Details/{applicationId}");
        await Expect(Alert("This application is Submitted and can no longer be edited.")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Changed Name")).ToHaveCountAsync(0);
    }

    [Fact]
    [Trait("Requirement", "Roles")]
    public async Task Anonymous_IsSentToLogin()
    {
        await Step("Roles", "Pages that need an account redirect to Log in");

        foreach (var path in new[] { "/Listings", "/Applications", "/Properties" })
        {
            await Page.GotoAsync(path);
            await Expect(Page).ToHaveURLAsync(new Regex("/Account/Login"));
        }
    }
}
