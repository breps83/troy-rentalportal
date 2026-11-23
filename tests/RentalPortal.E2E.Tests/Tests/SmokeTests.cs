using Microsoft.Playwright;
using RentalPortal.E2E.Tests.Infrastructure;
using RentalPortal.Web.Enums;
using Xunit.Abstractions;
using static Microsoft.Playwright.Assertions;

namespace RentalPortal.E2E.Tests.Tests;

/// <summary>
/// Every screen opens for the roles that can see it, with its main heading and no errors.
/// </summary>
public class SmokeTests(E2EFixture fixture, ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    [Fact]
    public async Task AnonymousScreens_Load()
    {
        await Step("Screens", "Log in and Register open without logging in");

        await Page.GotoAsync("/Account/Login");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Log in" })).ToBeVisibleAsync();

        await Page.GotoAsync("/Account/Register");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Create an account" })).ToBeVisibleAsync();

        await Step("Screens", "Home and pages that need an account send you to Log in");

        await Page.GotoAsync("/");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Account/Login"));

        await Page.GotoAsync("/Listings");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Account/Login"));

        await Step("Screens", "A missing page shows the error page, not Log in");

        await Page.GotoAsync("/NoSuchPage");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "404 - Page not found." })).ToBeVisibleAsync();
        await Expect(Page).Not.ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Account/Login"));
    }

    [Fact]
    public async Task ApplicantScreens_Load()
    {
        var property = await Data.CreatePropertyAsync();
        var applicationId = await Data.CreateApplicationAsync(
            property.Units[0].Id, TestUsers.Applicant, ApplicationStatus.Draft);

        await LoginAsAsync(TestUsers.Applicant);

        await Step("Screens", "Home opens for an applicant");
        await Page.GotoAsync("/");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Welcome to Rental Portal" })).ToBeVisibleAsync();

        await Step("Screens", "Available Units opens for an applicant");
        await Page.GotoAsync("/Listings");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Available Units" })).ToBeVisibleAsync();

        await Step("Screens", "My Applications opens for an applicant");
        await Page.GotoAsync("/Applications");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Applications" })).ToBeVisibleAsync();

        await Step("Screens", "Each section of an application opens");
        foreach (var section in new[] { "Info", "ResidenceHistory", "Summary" })
        {
            await Page.GotoAsync($"/Applications/Details/{applicationId}?section={section}");
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = $"{property.Name} – Unit 101" }))
                .ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task ManagerScreens_Load()
    {
        var property = await Data.CreatePropertyAsync();
        var applicationId = await Data.CreateApplicationAsync(
            property.Units[0].Id, TestUsers.Applicant, ApplicationStatus.Submitted);

        await LoginAsAsync(TestUsers.Manager);

        await Step("Screens", "Home opens for a property manager");
        await Page.GotoAsync("/");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Welcome to Rental Portal" })).ToBeVisibleAsync();

        await Step("Screens", "Properties opens for a property manager");
        await Page.GotoAsync("/Properties");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Properties" })).ToBeVisibleAsync();

        await Step("Screens", "A property's units page opens");
        await Page.GotoAsync($"/Properties/Details/{property.Id}");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = property.Name })).ToBeVisibleAsync();

        await Step("Screens", "Available Units and Applications open for a property manager");
        await Page.GotoAsync("/Listings");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Available Units" })).ToBeVisibleAsync();
        await Page.GotoAsync("/Applications");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Applications" })).ToBeVisibleAsync();

        await Step("Screens", "A submitted application opens for a property manager");
        await Page.GotoAsync($"/Applications/Details/{applicationId}");
        await Expect(StatusBadge).ToHaveTextAsync("Submitted");
    }
}
