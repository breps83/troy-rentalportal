using Microsoft.Playwright;
using RentalPortal.E2E.Tests.Infrastructure;
using Xunit.Abstractions;
using static Microsoft.Playwright.Assertions;

namespace RentalPortal.E2E.Tests.Tests;

public class AccountTests(E2EFixture fixture, ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    [Theory]
    [InlineData("Applicant", false)]
    [InlineData("Property Manager", true)]
    [Trait("Requirement", "FR 1.a")]
    [Trait("Requirement", "FR 1.a.i")]
    public async Task Register_WithChosenAccountType_SignsInWithThatRole(string accountType, bool isManager)
    {
        var email = $"e2e-{Guid.NewGuid().ToString("N")[..8]}@rentalportal.test";

        await Step("FR 1.a.i", $"Sign up as {accountType}");
        await Page.GotoAsync("/Account/Register");
        await Page.GetByLabel("First Name").EnterAsync("Jordan");
        await Page.GetByLabel("Last Name").EnterAsync("Rivera");
        await Page.GetByLabel("Email").EnterAsync(email);
        await Page.GetByLabel("Password", new() { Exact = true }).EnterSecretAsync(TestUsers.Password);
        await Page.GetByLabel("Confirm password").EnterSecretAsync(TestUsers.Password);
        await Page.GetByLabel("Account Type").ChooseAsync(accountType);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Register" }).ClickAsync();

        await Step("FR 1.a", "The new user is signed in");
        await Expect(NavBar.GetByText(email, new() { Exact = true })).ToBeVisibleAsync();

        await Step("FR 1.a.i", "The navbar matches the chosen role");
        var propertiesLink = NavBar.GetByRole(AriaRole.Link, new() { Name = "Properties", Exact = true });

        if (isManager)
            await Expect(propertiesLink).ToBeVisibleAsync();
        else
            await Expect(propertiesLink).ToHaveCountAsync(0);
    }

    [Fact]
    [Trait("Requirement", "FR 1.a.i")]
    public async Task Register_WithoutAccountType_ShowsError()
    {
        await Step("FR 1.a.i", "Sign-up requires an account type");
        await Page.GotoAsync("/Account/Register");
        await Page.GetByLabel("First Name").EnterAsync("Jordan");
        await Page.GetByLabel("Last Name").EnterAsync("Rivera");
        await Page.GetByLabel("Email").EnterAsync("no-type@rentalportal.test");
        await Page.GetByLabel("Password", new() { Exact = true }).EnterSecretAsync(TestUsers.Password);
        await Page.GetByLabel("Confirm password").EnterSecretAsync(TestUsers.Password);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Register" }).ClickAsync();

        await Expect(Page.GetByText("Choose an account type.")).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Account/Register"));
    }

    [Fact]
    [Trait("Requirement", "FR 1.a")]
    public async Task Login_ThenLogout_Works()
    {
        await Step("FR 1.a", "A wrong password is refused");
        await Page.GotoAsync("/Account/Login");
        await Page.GetByLabel("Email").EnterAsync(TestUsers.Applicant);
        await Page.GetByLabel("Password").EnterSecretAsync("WrongPassword1!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
        await Expect(Page.GetByText("Invalid login attempt.")).ToBeVisibleAsync();

        await Step("FR 1.a", "Log in with the right password");
        await LoginAsAsync(TestUsers.Applicant);
        await Expect(NavBar.GetByText(TestUsers.Applicant, new() { Exact = true })).ToBeVisibleAsync();

        await Step("FR 1.a", "Log out");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log out" }).ClickAsync();
        await Expect(NavBar.GetByRole(AriaRole.Link, new() { Name = "Log in" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Log out" })).ToHaveCountAsync(0);
    }
}
