using Microsoft.Playwright;
using RentalPortal.E2E.Tests.Infrastructure;
using RentalPortal.Web.Enums;
using Xunit.Abstractions;
using static Microsoft.Playwright.Assertions;

namespace RentalPortal.E2E.Tests.Tests;

public class PropertiesTests(E2EFixture fixture, ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private ILocator PropertyRow(string name) =>
        Page.Locator("#property-list tbody tr").Filter(new() { HasText = name });

    [Fact]
    [Trait("Requirement", "FR 2.a")]
    [Trait("Requirement", "TR 1.b")]
    public async Task AddProperty_ThroughModal_AppearsInList()
    {
        var name = TestData.Unique("Maple Court");

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync("/Properties");

        await Step("TR 1.b", "Add property opens the form in a modal");
        await Modal.OpenAsync(Page.GetByRole(AriaRole.Button, new() { Name = "Add property" }), "Add property");

        await Step("TR 1.b", "Invalid input keeps the modal open with errors");
        await Modal.Field("Name").EnterAsync(name);
        await Modal.Field("Street Address").EnterAsync("200 Lincoln Ave");
        await Modal.Field("City").EnterAsync("Springfield");
        await Modal.Field("State").EnterAsync("M1");
        await Modal.Field("PostalCode").EnterAsync("6270");
        await Modal.SubmitExpectingErrorAsync("Please enter a valid state abbreviation.");
        await Expect(Modal.Root.GetByText("Please enter a valid postal code.")).ToBeVisibleAsync();

        await Step("FR 2.a", "A valid property saves, the modal closes, and the list refreshes");
        await Modal.Field("State").EnterAsync("IL");
        await Modal.Field("PostalCode").EnterAsync("62701");
        await Modal.SubmitAsync();
        await Expect(PropertyRow(name)).ToContainTextAsync("200 Lincoln Ave");
    }

    [Fact]
    [Trait("Requirement", "FR 2.a")]
    [Trait("Requirement", "TR 1.b")]
    public async Task EditProperty_ThroughModal_UpdatesList()
    {
        var property = await Data.CreatePropertyAsync();
        var newName = TestData.Unique("Renamed Lofts");

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync("/Properties");

        await Step("FR 2.a", "Edit opens the property in a modal with its current values");
        await Modal.OpenAsync(
            PropertyRow(property.Name).GetByRole(AriaRole.Button, new() { Name = "Edit" }), "Edit property");
        await Expect(Modal.Field("Name")).ToHaveValueAsync(property.Name);

        await Step("FR 2.a", "Saving updates the row in place");
        await Modal.Field("Name").EnterAsync(newName);
        await Modal.SubmitAsync();
        await Expect(PropertyRow(newName)).ToBeVisibleAsync();
        await Expect(PropertyRow(property.Name)).ToHaveCountAsync(0);
    }

    [Fact]
    [Trait("Requirement", "FR 2.a")]
    [Trait("Requirement", "TR 1.b")]
    public async Task DeleteProperty_RemovesItAndItsUnits()
    {
        var property = await Data.CreatePropertyAsync(unitCount: 2);

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync("/Properties");

        await Step("FR 2.a", "Delete asks for confirmation in a modal");
        await Modal.OpenAsync(
            PropertyRow(property.Name).GetByRole(AriaRole.Button, new() { Name = "Delete" }), "Delete Property");
        await Expect(Modal.Root).ToContainTextAsync($"Delete {property.Name} and all of its units?");

        await Step("FR 2.a", "Confirming removes the property and its units");
        await Modal.SubmitAsync("Delete");
        await Expect(PropertyRow(property.Name)).ToHaveCountAsync(0);
        Assert.Equal(0, await Data.CountUnitsAsync(property.Id));
    }

    [Fact]
    [Trait("Requirement", "FR 2.a")]
    public async Task DeleteProperty_WithApplications_IsRefusedWithMessage()
    {
        var property = await Data.CreatePropertyAsync();
        await Data.CreateApplicationAsync(property.Units[0].Id, TestUsers.Applicant, ApplicationStatus.Draft);

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync("/Properties");

        await Step("FR 2.a", "A property with applications can't be deleted; the modal says why");
        await Modal.OpenAsync(
            PropertyRow(property.Name).GetByRole(AriaRole.Button, new() { Name = "Delete" }), "Delete Property");
        await Expect(Modal.Root.Locator(".alert-danger"))
            .ToHaveTextAsync("This property has units with rental applications and can't be removed.");
        await Expect(Modal.Button("Delete")).ToHaveCountAsync(0);

        await Modal.CancelAsync();
        await Expect(PropertyRow(property.Name)).ToBeVisibleAsync();
    }
}
