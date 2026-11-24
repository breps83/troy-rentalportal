using Microsoft.Playwright;
using RentalPortal.E2E.Tests.Infrastructure;
using RentalPortal.Web.Enums;
using Xunit.Abstractions;
using static Microsoft.Playwright.Assertions;

namespace RentalPortal.E2E.Tests.Tests;

/// <summary>
/// FR 3: the single-page application with Applicant Information, Residence History, and Summary.
/// </summary>
public class ApplicationWizardTests(E2EFixture fixture, ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private ILocator CurrentStep => Page.Locator("[aria-current=step]");

    private ILocator SectionCard => Page.Locator(".card-header.fw-semibold");

    private ILocator Button(string name) => Page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    [Fact]
    [Trait("Requirement", "FR 3.a")]
    [Trait("Requirement", "FR 3.b")]
    [Trait("Requirement", "FR 3.c")]
    [Trait("Requirement", "Roles")]
    public async Task Applicant_CompletesBothSections_AndSubmits()
    {
        var property = await Data.CreatePropertyAsync();
        var applicationId = await Data.CreateApplicationAsync(
            property.Units[0].Id, TestUsers.Applicant, ApplicationStatus.Draft, completeSections: false);

        await LoginAsAsync(TestUsers.Applicant);
        await Page.GotoAsync($"/Applications/Details/{applicationId}");

        await Step("FR 3.b", "The application opens on Applicant Information, one section at a time");
        await Expect(CurrentStep).ToContainTextAsync("Applicant Information");
        await Expect(Page.GetByLabel("Phone")).ToBeVisibleAsync();
        await Expect(Button("Add residence")).ToHaveCountAsync(0);

        await Step("FR 3.a", "Applicant Information: name, phone, email, current address");
        await Expect(Page.GetByLabel("First Name")).ToHaveValueAsync("Arnold");
        await Expect(Page.GetByLabel("Email")).ToHaveValueAsync(TestUsers.Applicant);

        await Step("FR 3.b", "Continue with a missing field shows the error and stays on the section");
        await Button("Continue").ClickAsync();
        await Expect(Page.GetByText("The Phone field is required.")).ToBeVisibleAsync();
        await Expect(CurrentStep).ToContainTextAsync("Applicant Information");

        await Step("FR 3.b", "Continue with valid input saves and moves to Residence History");
        await Page.GetByLabel("Phone").EnterAsync("217-555-0123");
        await Page.GetByLabel("Street address").EnterAsync("500 Lincoln Ave");
        await Page.GetByLabel("City").EnterAsync("Springfield");
        await Page.GetByLabel("State").EnterAsync("IL");
        await Page.GetByLabel("Postal code").EnterAsync("62701");
        await Button("Continue").ClickAsync();
        await Expect(CurrentStep).ToContainTextAsync("Residence History");
        await Expect(Page.Locator("li", new() { HasText = "Applicant Information" }).GetByLabel("saved"))
            .ToBeVisibleAsync();

        await Step("FR 3.b", "Continue with no residences shows an alert and stays on the section");
        await Button("Continue").ClickAsync();
        await Expect(Alert("Please add at least one prior residence.")).ToBeVisibleAsync();
        await Expect(CurrentStep).ToContainTextAsync("Residence History");

        await Step("FR 3.c", "Add a residence through the modal");
        await Modal.OpenAsync(Button("Add residence"), "Add residence");
        await Modal.Field("Street address").EnterAsync("77 Lake St");
        await Modal.Field("City").EnterAsync("Springfield");
        await Modal.Field("State").EnterAsync("IL");
        await Modal.Field("Postal code").EnterAsync("62704");
        await Modal.Field("Landlord name").EnterAsync("Morgan Lee");
        await Modal.Field("Landlord phone").EnterAsync("217-555-0144");
        await Modal.Field("Move In Date").EnterAsync("2020-08-01");
        await Modal.Field("Move Out Date").EnterAsync("2024-07-31");
        await Modal.SubmitAsync();
        await Expect(Page.Locator("#residence-history")).ToContainTextAsync("77 Lake St, Springfield, IL 62704");

        await Step("FR 3.b", "Back returns to the previous section");
        await Button("Back").ClickAsync();
        await Expect(CurrentStep).ToContainTextAsync("Applicant Information");
        await Expect(Page.GetByLabel("Phone")).ToHaveValueAsync("217-555-0123");
        await Button("Continue").ClickAsync();

        await Step("FR 3.a", "Continue moves to the Summary, a read-only view of both sections");
        await Button("Continue").ClickAsync();
        await Expect(CurrentStep).ToContainTextAsync("Summary");
        await Expect(Page.GetByText("217-555-0123")).ToBeVisibleAsync();
        await Expect(Page.GetByText("77 Lake St, Springfield, IL 62704")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Textbox)).ToHaveCountAsync(0);
        await Expect(Button("Edit")).ToHaveCountAsync(0);

        await Step("FR 3.b", "Submit from the Summary");
        await Button("Submit").ClickAsync();
        await Expect(Alert("Thank you. Your application has been submitted!")).ToBeVisibleAsync();
        await Expect(StatusBadge).ToHaveTextAsync("Submitted");
    }

    [Fact]
    [Trait("Requirement", "FR 3.b")]
    public async Task Summary_HasNoSubmit_UntilBothSectionsAreSaved()
    {
        var property = await Data.CreatePropertyAsync();
        var applicationId = await Data.CreateApplicationAsync(
            property.Units[0].Id, TestUsers.Applicant, ApplicationStatus.Draft, completeSections: false);

        await LoginAsAsync(TestUsers.Applicant);

        await Step("FR 3.b", "Opening the Summary directly doesn't offer Submit while sections are unsaved");
        await Page.GotoAsync($"/Applications/Details/{applicationId}?section=Summary");
        await Expect(CurrentStep).ToContainTextAsync("Summary");
        await Expect(Button("Submit")).ToHaveCountAsync(0);
    }

    [Fact]
    [Trait("Requirement", "FR 3.d")]
    public async Task SubmittedApplication_IsReadOnly()
    {
        var property = await Data.CreatePropertyAsync();
        var applicationId = await Data.CreateApplicationAsync(
            property.Units[0].Id, TestUsers.Applicant, ApplicationStatus.Submitted);

        await LoginAsAsync(TestUsers.Applicant);
        await Page.GotoAsync($"/Applications/Details/{applicationId}?section=Info");

        await Step("FR 3.d", "The same section renders read-only: text instead of inputs, no Continue");
        await Expect(Page.GetByText("217-555-0100")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Textbox)).ToHaveCountAsync(0);
        await Expect(Button("Continue")).ToHaveCountAsync(0);

        await Step("FR 3.d", "Next and Previous links move between read-only sections");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Next" }).ClickAsync();
        await Expect(CurrentStep).ToContainTextAsync("Residence History");
        await Expect(Button("Add residence")).ToHaveCountAsync(0);
        await Expect(Button("Edit")).ToHaveCountAsync(0);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Previous" }).ClickAsync();
        await Expect(CurrentStep).ToContainTextAsync("Applicant Information");
        await Expect(SectionCard).ToHaveTextAsync("Applicant Information");
    }

    [Fact]
    [Trait("Requirement", "FR 3.e")]
    public async Task Submit_WhenUnitWasLeasedMeanwhile_IsRefused()
    {
        var property = await Data.CreatePropertyAsync();
        var applicationId = await Data.CreateApplicationAsync(
            property.Units[0].Id, TestUsers.Applicant, ApplicationStatus.Draft);
        await Data.CreateApplicationAsync(property.Units[0].Id, TestUsers.OtherApplicant, ApplicationStatus.Approved);

        await LoginAsAsync(TestUsers.Applicant);
        await Page.GotoAsync($"/Applications/Details/{applicationId}?section=Summary");

        await Step("FR 3.e", "Submit is refused with an error when the unit has an active lease");
        await Button("Submit").ClickAsync();
        await Expect(Alert("This unit already has an active lease.")).ToBeVisibleAsync();
        await Expect(StatusBadge).ToHaveTextAsync("Draft");
    }
}
