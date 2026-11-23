using Microsoft.Playwright;
using RentalPortal.E2E.Tests.Infrastructure;
using RentalPortal.Web.Enums;
using Xunit.Abstractions;
using static Microsoft.Playwright.Assertions;

namespace RentalPortal.E2E.Tests.Tests;

public class ResidencesTests(E2EFixture fixture, ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private ILocator History => Page.Locator("#residence-history");

    private ILocator ResidenceRow(string address) => History.Locator("tbody tr").Filter(new() { HasText = address });

    [Fact]
    [Trait("Requirement", "FR 3.c")]
    [Trait("Requirement", "TR 1.b")]
    public async Task Residences_AddEditAndRemove_ThroughModal()
    {
        var property = await Data.CreatePropertyAsync();
        var applicationId = await Data.CreateApplicationAsync(
            property.Units[0].Id, TestUsers.Applicant, ApplicationStatus.Draft);

        await LoginAsAsync(TestUsers.Applicant);
        await Page.GotoAsync($"/Applications/Details/{applicationId}?section=ResidenceHistory");

        await Step("FR 3.c", "Add a residence; a move-out before move-in is refused in the modal");
        await Modal.OpenAsync(Page.GetByRole(AriaRole.Button, new() { Name = "Add residence" }), "Add residence");
        await Modal.Field("Address").EnterAsync("9 Pine Ct, Clawson, MI 48017");
        await Modal.Field("Landlord name").EnterAsync("Riley Park");
        await Modal.Field("Landlord phone").EnterAsync("217-555-0177");
        await Modal.Field("Move In Date").EnterAsync("2019-05-01");
        await Modal.Field("Move Out Date").EnterAsync("2018-05-01");
        await Modal.SubmitExpectingErrorAsync("Move Out date can't be before Move In Date");

        await Modal.Field("Move Out Date").EnterAsync("2021-04-30");
        await Modal.SubmitAsync();
        await Expect(ResidenceRow("9 Pine Ct, Clawson, MI 48017")).ToContainTextAsync("Riley Park");

        await Step("FR 3.c", "Edit the residence");
        await Modal.OpenAsync(
            ResidenceRow("9 Pine Ct").GetByRole(AriaRole.Button, new() { Name = "Edit" }), "Edit residence");
        await Expect(Modal.Field("Landlord name")).ToHaveValueAsync("Riley Park");
        await Modal.Field("Landlord name").EnterAsync("Riley Parker");
        await Modal.SubmitAsync();
        await Expect(ResidenceRow("9 Pine Ct")).ToContainTextAsync("Riley Parker");

        await Step("FR 3.c", "Remove the residence after confirming");
        await Modal.OpenAsync(
            ResidenceRow("9 Pine Ct").GetByRole(AriaRole.Button, new() { Name = "Delete" }), "Delete residence");
        await Modal.SubmitAsync("Delete");
        await Expect(ResidenceRow("9 Pine Ct")).ToHaveCountAsync(0);
        await Expect(ResidenceRow("45 Oak Ave")).ToBeVisibleAsync();
    }
}
