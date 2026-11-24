using System.Text.RegularExpressions;
using Microsoft.Playwright;
using RentalPortal.E2E.Tests.Infrastructure;
using RentalPortal.Web.Enums;
using Xunit.Abstractions;
using static Microsoft.Playwright.Assertions;

namespace RentalPortal.E2E.Tests.Tests;

public class ListingsTests(E2EFixture fixture, ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private ILocator PropertyGroup(string name) => Page.Locator("tbody").Filter(new() { HasText = name });

    private static ILocator UnitRow(ILocator group, string unitNumber) =>
        group.GetByRole(AriaRole.Row).Filter(new() { Has = group.Page.GetByRole(AriaRole.Cell, new() {
            Name = unitNumber, Exact = true }) });

    [Fact]
    [Trait("Requirement", "FR 2.a")]
    public async Task AvailableUnits_ListsUnitsFromNavbar()
    {
        var property = await Data.CreatePropertyAsync(unitCount: 2);

        await LoginAsAsync(TestUsers.Applicant);

        await Step("FR 2.a", "Available Units is linked from the navbar");
        await NavBar.GetByRole(AriaRole.Link, new() { Name = "Available Units" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Available Units" })).ToBeVisibleAsync();

        await Step("FR 2.a", "Units are grouped by property with type, bedrooms, and rent");
        var group = PropertyGroup(property.Name);
        await Expect(group).ToContainTextAsync("100 Main St");
        await Expect(UnitRow(group, "101")).ToContainTextAsync("Apartment");
        await Expect(UnitRow(group, "101")).ToContainTextAsync("$1,501");
        await Expect(UnitRow(group, "102")).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("Requirement", "FR 2.c")]
    public async Task LeasedUnit_IsNotListed()
    {
        var property = await Data.CreatePropertyAsync(unitCount: 2);
        await Data.CreateApplicationAsync(property.Units[0].Id, TestUsers.OtherApplicant, ApplicationStatus.Approved);

        await LoginAsAsync(TestUsers.Applicant);
        await Page.GotoAsync("/Listings");

        await Step("FR 2.c", "A unit whose lease covers today is not available");
        var group = PropertyGroup(property.Name);
        await Expect(UnitRow(group, "102")).ToBeVisibleAsync();
        await Expect(UnitRow(group, "101")).ToHaveCountAsync(0);
    }

    [Fact]
    [Trait("Requirement", "FR 2.b")]
    public async Task InactiveUnitType_OnAvailableUnits_BadgeShownToManagersOnly()
    {
        var property = await Data.CreatePropertyAsync();
        await Data.CreateUnitAsync(property.Id, "PH1", "Penthouse");

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync("/Listings");

        await Step("FR 2.b", "A manager sees the inactive type marked inactive on Available Units");
        var row = UnitRow(PropertyGroup(property.Name), "PH1");
        await Expect(row).ToContainTextAsync("Penthouse");
        await Expect(row.GetByText("inactive", new() { Exact = true })).ToBeVisibleAsync();

        await SwitchUserAsync(TestUsers.Applicant);
        await Page.GotoAsync("/Listings");

        await Step("FR 2.b", "An applicant sees the same unit's type without the inactive badge");
        row = UnitRow(PropertyGroup(property.Name), "PH1");
        await Expect(row).ToContainTextAsync("Penthouse");
        await Expect(row.GetByText("inactive", new() { Exact = true })).ToHaveCountAsync(0);
    }

    [Fact]
    [Trait("Requirement", "FR 2.a")]
    public async Task ApplyForUnit_StartsApplication_AndReopensIt()
    {
        var property = await Data.CreatePropertyAsync();

        await LoginAsAsync(TestUsers.Applicant);
        await Page.GotoAsync("/Listings");

        await Step("FR 2.a", "Apply for Unit starts an application for that unit");
        await UnitRow(PropertyGroup(property.Name), "101")
            .GetByRole(AriaRole.Button, new() { Name = "Apply for Unit" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = $"{property.Name} – Unit 101" }))
            .ToBeVisibleAsync();
        await Expect(StatusBadge).ToHaveTextAsync("Draft");
        var breadcrumb = Page.GetByRole(AriaRole.Navigation, new() { Name = "breadcrumb" });
        await Expect(breadcrumb.GetByRole(AriaRole.Link, new() { Name = "My Applications" })).ToBeVisibleAsync();
        var firstUrl = Page.Url;

        await Step("FR 2.a", "Applying again reopens the same open application");
        await Page.GotoAsync("/Listings");
        await UnitRow(PropertyGroup(property.Name), "101")
            .GetByRole(AriaRole.Button, new() { Name = "Apply for Unit" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(Regex.Escape(new Uri(firstUrl).AbsolutePath)));
    }

    [Fact]
    [Trait("Requirement", "FR 2.a")]
    [Trait("Requirement", "TR 1.b")]
    public async Task Manager_EditsAndDeletesUnit_FromAvailableUnits()
    {
        var property = await Data.CreatePropertyAsync(unitCount: 2);

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync("/Listings");
        var group = PropertyGroup(property.Name);

        await Step("FR 2.a", "Edit a unit's rent from Available Units");
        await Modal.OpenAsync(UnitRow(group, "101").GetByRole(AriaRole.Button, new() { Name = "Edit" }),
            "Edit unit 101");
        await Modal.Field("Monthly rent").EnterAsync("1999");
        await Modal.SubmitAsync();

        await Step("TR 1.b", "The page reloads and shows the new rent");
        await Expect(UnitRow(group, "101")).ToContainTextAsync("$1,999");

        await Step("FR 2.a", "Delete a unit from Available Units after confirming");
        await Modal.OpenAsync(UnitRow(group, "102").GetByRole(AriaRole.Button, new() { Name = "Delete" }),
            "Delete unit");
        await Expect(Modal.Root).ToContainTextAsync("Delete unit 102?");
        await Modal.SubmitAsync("Delete");
        await Expect(UnitRow(group, "102")).ToHaveCountAsync(0);
        await Expect(UnitRow(group, "101")).ToBeVisibleAsync();
        Assert.Equal(1, await Data.CountUnitsAsync(property.Id));
    }

    [Fact]
    [Trait("Requirement", "FR 2.c")]
    [Trait("Requirement", "Roles")]
    public async Task ApplyForUnit_LeasedSinceThePageLoaded_IsRefused()
    {
        var property = await Data.CreatePropertyAsync();

        await LoginAsAsync(TestUsers.Applicant);
        await Page.GotoAsync("/Listings");
        var applyButton = UnitRow(PropertyGroup(property.Name), "101")
            .GetByRole(AriaRole.Button, new() { Name = "Apply for Unit" });
        await Expect(applyButton).ToBeVisibleAsync();

        // Someone else's application is approved while this page is open.
        await Data.CreateApplicationAsync(property.Units[0].Id, TestUsers.OtherApplicant, ApplicationStatus.Approved);

        await Step("FR 2.c", "Applying for a unit that was leased in the meantime is refused on the server");
        await applyButton.ClickAsync();
        await Expect(Alert("This unit is not available.")).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/Listings"));
    }
}
