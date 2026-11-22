using Microsoft.Playwright;
using RentalPortal.E2E.Tests.Infrastructure;
using RentalPortal.Web.Enums;
using Xunit.Abstractions;
using static Microsoft.Playwright.Assertions;

namespace RentalPortal.E2E.Tests.Tests;

public class UnitsTests(E2EFixture fixture, ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private ILocator UnitRow(string unitNumber) =>
        Page.Locator("#unit-list tbody tr").Filter(new() { Has = Page.GetByRole(AriaRole.Cell, new() {
            Name = unitNumber, Exact = true }) });

    private ILocator AddUnitButton => Page.GetByRole(AriaRole.Button, new() { Name = "Add unit" });

    [Fact]
    [Trait("Requirement", "FR 2.a")]
    [Trait("Requirement", "TR 1.b")]
    public async Task AddUnit_ThroughModal_AppearsInUnitList()
    {
        var property = await Data.CreatePropertyAsync();

        await LoginAsAsync(TestUsers.Manager);

        await Step("FR 2.a", "View Units opens the property's units page");
        await Page.GotoAsync("/Properties");
        await Page.Locator("#property-list tbody tr").Filter(new() { HasText = property.Name })
            .GetByRole(AriaRole.Link, new() { Name = "View Units" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = property.Name })).ToBeVisibleAsync();

        await Step("FR 2.a", "Add a unit: number, bedrooms, monthly rent, and unit type");
        await Modal.OpenAsync(AddUnitButton, "Add unit");
        await Modal.Field("Unit number").EnterAsync("305");
        await Modal.Field("Unit type").ChooseAsync("Condo");
        await Modal.Field("Bedrooms").EnterAsync("1");
        await Modal.Field("Monthly rent").EnterAsync("1250");
        await Modal.SubmitAsync();

        await Expect(UnitRow("305")).ToContainTextAsync("Condo");
        await Expect(UnitRow("305")).ToContainTextAsync("$1,250");
    }

    [Fact]
    [Trait("Requirement", "TR 1.b")]
    public async Task AddUnit_WithDuplicateNumber_ReRendersModalWithServerError()
    {
        var property = await Data.CreatePropertyAsync();

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync($"/Properties/Details/{property.Id}");

        await Step("TR 1.b", "A server-side validation error re-renders the same partial in the modal");
        await Modal.OpenAsync(AddUnitButton, "Add unit");
        await Modal.Field("Unit number").EnterAsync("101");
        await Modal.Field("Unit type").ChooseAsync("Apartment");
        await Modal.Field("Bedrooms").EnterAsync("2");
        await Modal.Field("Monthly rent").EnterAsync("1400");
        await Modal.SubmitExpectingErrorAsync("This property already has a unit with that number.");
        await Expect(Modal.Field("Monthly rent")).ToHaveValueAsync("1400");
    }

    [Fact]
    [Trait("Requirement", "FR 2.a")]
    [Trait("Requirement", "TR 1.b")]
    public async Task EditAndDeleteUnit_ThroughModal()
    {
        var property = await Data.CreatePropertyAsync(unitCount: 2);

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync($"/Properties/Details/{property.Id}");

        await Step("FR 2.a", "Edit a unit's rent");
        await Modal.OpenAsync(UnitRow("101").GetByRole(AriaRole.Button, new() { Name = "Edit" }), "Edit unit 101");
        await Modal.Field("Monthly rent").EnterAsync("1999");
        await Modal.SubmitAsync();
        await Expect(UnitRow("101")).ToContainTextAsync("$1,999");

        await Step("FR 2.a", "Delete a unit after confirming");
        await Modal.OpenAsync(UnitRow("102").GetByRole(AriaRole.Button, new() { Name = "Delete" }), "Delete unit");
        await Expect(Modal.Root).ToContainTextAsync("Delete unit 102?");
        await Modal.SubmitAsync("Delete");
        await Expect(UnitRow("102")).ToHaveCountAsync(0);
        await Expect(UnitRow("101")).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("Requirement", "FR 2.a")]
    public async Task DeleteUnit_WithApplications_IsRefusedWithMessage()
    {
        var property = await Data.CreatePropertyAsync();
        await Data.CreateApplicationAsync(property.Units[0].Id, TestUsers.Applicant, ApplicationStatus.Draft);

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync($"/Properties/Details/{property.Id}");

        await Step("FR 2.a", "A unit with applications can't be deleted; the modal says why");
        await Modal.OpenAsync(UnitRow("101").GetByRole(AriaRole.Button, new() { Name = "Delete" }), "Delete unit");
        await Expect(Modal.Root.Locator(".alert-danger"))
            .ToHaveTextAsync("This unit has rental applications and can't be deleted.");
        await Expect(Modal.Button("Delete")).ToHaveCountAsync(0);
    }

    [Fact]
    [Trait("Requirement", "FR 2.b")]
    public async Task InactiveUnitType_ShowsOnExistingUnit_ButCantBeChosen()
    {
        var property = await Data.CreatePropertyAsync();
        await Data.CreateUnitAsync(property.Id, "PH1", "Penthouse");
        var penthouseId = await Data.GetUnitTypeIdAsync("Penthouse");

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync($"/Properties/Details/{property.Id}");

        await Step("FR 2.b", "An existing unit still shows its inactive type, marked inactive");
        await Expect(UnitRow("PH1")).ToContainTextAsync("Penthouse");
        await Expect(UnitRow("PH1").GetByText("inactive")).ToBeVisibleAsync();

        await Step("FR 2.b", "Editing that unit keeps the inactive type selected");
        await Modal.OpenAsync(UnitRow("PH1").GetByRole(AriaRole.Button, new() { Name = "Edit" }), "Edit unit PH1");
        await Expect(Modal.Field("Unit type").Locator("option:checked")).ToHaveTextAsync("Penthouse (inactive)");
        await Modal.CancelAsync();

        await Step("FR 2.b", "A new unit can't choose the inactive type");
        await Modal.OpenAsync(AddUnitButton, "Add unit");
        await Expect(Modal.Field("Unit type").Locator("option", new() { HasText = "Penthouse" }))
            .ToHaveCountAsync(0);

        await Step("FR 2.b", "The server refuses it even if the option is added to the page by hand");
        await Modal.Field("Unit type").EvaluateAsync(
            "(select, id) => select.add(new Option('Penthouse', id))", penthouseId.ToString());
        await Modal.Field("Unit number").EnterAsync("PH2");
        await Modal.Field("Unit type").ChooseAsync("Penthouse");
        await Modal.Field("Bedrooms").EnterAsync("3");
        await Modal.Field("Monthly rent").EnterAsync("3200");
        await Modal.SubmitExpectingErrorAsync("This unit type is inactive and can't be selected.");
    }
}
