using Microsoft.Playwright;
using RentalPortal.E2E.Tests.Infrastructure;
using RentalPortal.Web.Enums;
using Xunit.Abstractions;
using static Microsoft.Playwright.Assertions;

namespace RentalPortal.E2E.Tests.Tests;

public class ApplicationListTests(E2EFixture fixture, ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private ILocator Rows => Page.Locator("table tbody tr");

    [Fact]
    [Trait("Requirement", "FR 5.a")]
    [Trait("Requirement", "Roles")]
    public async Task Applicant_SeesOnlyOwnApplications()
    {
        var applicant = await Data.CreateApplicantAsync();
        var property = await Data.CreatePropertyAsync(unitCount: 2);
        var ownId = await Data.CreateApplicationAsync(property.Units[0].Id, applicant, ApplicationStatus.Submitted);
        await Data.CreateApplicationAsync(property.Units[1].Id, TestUsers.OtherApplicant, ApplicationStatus.Submitted);

        await LoginAsAsync(applicant);

        await Step("FR 5.a", "My Applications lists only this applicant's applications");
        await NavBar.GetByRole(AriaRole.Link, new() { Name = "My Applications" }).ClickAsync();
        await Expect(Rows).ToHaveCountAsync(1);
        await Expect(Rows.First).ToContainTextAsync($"{property.Name} · Unit 101");
        await Expect(Rows.First.GetByRole(AriaRole.Cell).First).ToHaveTextAsync(ownId.ToString());

        await Step("Roles", "No Applicant column for applicants");
        await Expect(Page.GetByRole(AriaRole.Columnheader, new() { Name = "Applicant" })).ToHaveCountAsync(0);

        await Step("FR 5.a", "Open goes to the application");
        await Rows.First.GetByRole(AriaRole.Link, new() { Name = "Open" }).ClickAsync();
        await Expect(Page.Locator("#application-status")).ToContainTextAsync("Submitted");
    }

    [Fact]
    [Trait("Requirement", "FR 5.a")]
    public async Task Manager_SeesAllApplications_AndFiltersByStatusAndProperty()
    {
        var property = await Data.CreatePropertyAsync(unitCount: 3);
        await Data.CreateApplicationAsync(property.Units[0].Id, TestUsers.Applicant, ApplicationStatus.Draft);
        await Data.CreateApplicationAsync(property.Units[1].Id, TestUsers.Applicant, ApplicationStatus.Submitted);
        await Data.CreateApplicationAsync(property.Units[2].Id, TestUsers.OtherApplicant, ApplicationStatus.Denied);

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync("/Applications");

        await Step("FR 5.a", "Managers see every applicant's applications, with an Applicant column");
        await Expect(Page.GetByRole(AriaRole.Columnheader, new() { Name = "Applicant" })).ToBeVisibleAsync();

        await Step("FR 5.a", "Filter by property");
        await Page.GetByLabel("Property").ChooseAsync(property.Name);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Filter" }).ClickAsync();
        await Expect(Rows).ToHaveCountAsync(3);
        await Expect(Rows.Filter(new() { HasText = "Arnold Schwarzenegger" })).ToHaveCountAsync(2);
        await Expect(Rows.Filter(new() { HasText = "Sylvester Stallone" })).ToHaveCountAsync(1);

        await Step("FR 5.a", "Filter by status and property together");
        await Page.GetByLabel("Status").ChooseAsync("Submitted");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Filter" }).ClickAsync();
        await Expect(Rows).ToHaveCountAsync(1);
        await Expect(Rows.First).ToContainTextAsync("Unit 102");
        await Expect(Rows.First).ToContainTextAsync("Submitted");

        await Step("FR 5.a", "Clear removes the filters");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Clear" }).ClickAsync();
        await Expect(Page.GetByLabel("Status")).ToHaveValueAsync("");
        await Expect(Page.GetByLabel("Property")).ToHaveValueAsync("");
    }
}
