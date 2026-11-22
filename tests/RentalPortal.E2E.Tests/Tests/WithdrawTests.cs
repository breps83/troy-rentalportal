using System.Text.RegularExpressions;
using Microsoft.Playwright;
using RentalPortal.E2E.Tests.Infrastructure;
using RentalPortal.Web.Enums;
using Xunit.Abstractions;
using static Microsoft.Playwright.Assertions;

namespace RentalPortal.E2E.Tests.Tests;

public class WithdrawTests(E2EFixture fixture, ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private ILocator WithdrawButton => Page.GetByRole(AriaRole.Button, new() { Name = "Withdraw application" });

    [Theory]
    [InlineData(ApplicationStatus.Draft)]
    [InlineData(ApplicationStatus.Submitted)]
    [InlineData(ApplicationStatus.Returned)]
    [Trait("Requirement", "FR 4.b")]
    [Trait("Requirement", "TR 1.b")]
    [Trait("Requirement", "Roles")]
    public async Task Applicant_WithdrawsOpenApplication(ApplicationStatus status)
    {
        var property = await Data.CreatePropertyAsync();
        var applicationId = await Data.CreateApplicationAsync(property.Units[0].Id, TestUsers.Applicant, status);

        await LoginAsAsync(TestUsers.Applicant);
        await Page.GotoAsync($"/Applications/Details/{applicationId}");

        await Step("FR 4.b", $"Withdraw a {status} application; the modal asks for confirmation");
        await Modal.OpenAsync(WithdrawButton, "Withdraw application");
        await Expect(Modal.Root).ToContainTextAsync("This can't be undone.");
        await Modal.SubmitAsync("Withdraw");

        await Step("FR 4.b", "The application list shows it as Withdrawn");
        await Expect(Page).ToHaveURLAsync(new Regex("/Applications$"));
        await Expect(Alert("Your application was withdrawn.")).ToBeVisibleAsync();
        Assert.Equal(ApplicationStatus.Withdrawn, await Data.GetStatusAsync(applicationId));

        await Step("FR 4.b", "Withdrawn is terminal: no Withdraw button anymore");
        await Page.GotoAsync($"/Applications/Details/{applicationId}");
        await Expect(Page.Locator("#application-status")).ToContainTextAsync("Withdrawn");
        await Expect(WithdrawButton).ToHaveCountAsync(0);
    }

    [Theory]
    [InlineData(ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Denied)]
    [Trait("Requirement", "FR 4.b")]
    public async Task TerminalApplication_CannotBeWithdrawn(ApplicationStatus status)
    {
        var property = await Data.CreatePropertyAsync();
        var applicationId = await Data.CreateApplicationAsync(property.Units[0].Id, TestUsers.Applicant, status);

        await LoginAsAsync(TestUsers.Applicant);
        await Page.GotoAsync($"/Applications/Details/{applicationId}");

        await Step("FR 4.b", $"{status} is terminal: no Withdraw button");
        await Expect(Page.Locator("#application-status")).ToContainTextAsync(status.ToString());
        await Expect(WithdrawButton).ToHaveCountAsync(0);
    }
}
