using System.Net;
using RentalPortal.Integration.Tests.Infrastructure;
using RentalPortal.Web.Enums;

namespace RentalPortal.Integration.Tests.Tests;

/// <summary>
/// Requests the UI doesn't offer, sent straight to the controllers, to check that the server refuses them.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class PermissionsTests(IntegrationFixture fixture)
{
    private readonly IntegrationAppFactory _app = fixture.App;

    private readonly TestData _data = fixture.Data;

    [Fact]
    public async Task AnonymousUser_IsSentToLogin()
    {
        var client = _app.CreateNoRedirectClient();

        var response = await client.GetAsync("/Properties");

        AssertRedirectsTo(response, "/Account/Login");
    }

    [Fact]
    public async Task Applicant_CannotOpenProperties()
    {
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.GetAsync("/Properties");

        AssertRedirectsTo(response, "/Account/AccessDenied");
    }

    [Fact]
    public async Task Applicant_CannotCreateProperty()
    {
        var client = await LoginAsync(TestUsers.Applicant);
        var name = TestData.Unique("Not Allowed");

        var response = await client.PostFormAsync("/Properties/Create", new Dictionary<string, string>
        {
            ["Name"] = name,
            ["Street"] = "1 Main St",
            ["City"] = "Springfield",
            ["State"] = "IL",
            ["PostalCode"] = "62701"
        });

        AssertRedirectsTo(response, "/Account/AccessDenied");
        Assert.False(await _data.PropertyExistsAsync(name));
    }

    [Fact]
    public async Task Manager_CannotApply()
    {
        var unit = await _data.CreateUnitAsync();
        var client = await LoginAsync(TestUsers.Manager);

        var response = await client.PostFormAsync("/Applications/Apply", new Dictionary<string, string>
        {
            ["unitId"] = unit.Id.ToString()
        });

        AssertRedirectsTo(response, "/Account/AccessDenied");
        Assert.Equal(0, await _data.CountApplicationsAsync(unit.Id));
    }

    [Fact]
    public async Task Applicant_CannotViewAnotherApplicantsApplication()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.OtherApplicant, ApplicationStatus.Submitted);
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.GetAsync($"/Applications/Details/{applicationId}");

        AssertRedirectsTo(response, "/Account/AccessDenied");
    }

    [Fact]
    public async Task Applicant_CannotWithdrawAnotherApplicantsApplication()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.OtherApplicant, ApplicationStatus.Submitted);
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.PostFormAsync($"/Applications/Withdraw/{applicationId}", []);

        AssertRedirectsTo(response, "/Account/AccessDenied");

        var application = await _data.GetApplicationAsync(applicationId);
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
    }

    [Fact]
    public async Task Applicant_CannotEditSubmittedApplication()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Submitted);
        var before = await _data.GetApplicationAsync(applicationId);
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.PostFormAsync($"/Applications/Details/{applicationId}",
            new Dictionary<string, string>
            {
                ["command"] = "continue",
                ["CurrentSection"] = nameof(ApplicationSection.Info),
                ["ApplicantInfo.FirstName"] = "Changed",
                ["ApplicantInfo.LastName"] = before.LastName!,
                ["ApplicantInfo.Phone"] = before.Phone!,
                ["ApplicantInfo.Email"] = before.Email!,
                ["ApplicantInfo.CurrentAddress"] = before.CurrentAddress!
            });

        AssertRedirectsTo(response, $"/Applications/Details/{applicationId}");

        var after = await _data.GetApplicationAsync(applicationId);
        Assert.Equal(before.FirstName, after.FirstName);
    }

    [Fact]
    public async Task Manager_CannotReviewWithdrawnApplication()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Withdrawn);
        var client = await LoginAsync(TestUsers.Manager);

        var response = await client.PostFormAsync("/Applications/Review", new Dictionary<string, string>
        {
            ["ApplicationId"] = applicationId.ToString(),
            ["ReviewStatus"] = nameof(ApplicationReviewStatus.Approve)
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("\"success\":true", await response.Content.ReadAsStringAsync());

        var application = await _data.GetApplicationAsync(applicationId);
        Assert.Equal(ApplicationStatus.Withdrawn, application.Status);
    }

    [Fact]
    public async Task Applicant_CannotReadNotes()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Submitted);
        await _data.CreateNoteAsync(applicationId, TestUsers.Manager, "Private manager note.");
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.GetAsync($"/Applications/Notes/{applicationId}");

        AssertRedirectsTo(response, "/Account/AccessDenied");
    }

    [Fact]
    public async Task Applicant_DoesNotSeeNotesOnApplicationPage()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Submitted);
        await _data.CreateNoteAsync(applicationId, TestUsers.Manager, "Private manager note.");
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.GetAsync($"/Applications/Details/{applicationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Private manager note.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Applicant_CannotAddNote()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Submitted);
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.PostFormAsync("/Applications/AddNote", new Dictionary<string, string>
        {
            ["ApplicationId"] = applicationId.ToString(),
            ["Note"] = "Not allowed."
        });

        AssertRedirectsTo(response, "/Account/AccessDenied");
    }

    [Fact]
    public async Task Manager_CannotEditAnotherManagersNote()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Submitted);
        var noteId = await _data.CreateNoteAsync(applicationId, TestUsers.OtherManager, "Original note.");
        var client = await LoginAsync(TestUsers.Manager);

        var response = await client.PostFormAsync("/Applications/EditNote", new Dictionary<string, string>
        {
            ["ApplicationId"] = applicationId.ToString(),
            ["NoteId"] = noteId.ToString(),
            ["Note"] = "Changed."
        });

        AssertRedirectsTo(response, "/Account/AccessDenied");

        var note = await _data.GetNoteAsync(noteId);
        Assert.Equal("Original note.", note!.Note);
    }

    [Fact]
    public async Task Manager_CannotDeleteAnotherManagersNote()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Submitted);
        var noteId = await _data.CreateNoteAsync(applicationId, TestUsers.OtherManager, "Original note.");
        var client = await LoginAsync(TestUsers.Manager);

        var response = await client.PostFormAsync($"/Applications/DeleteNote/{noteId}", []);

        AssertRedirectsTo(response, "/Account/AccessDenied");
        Assert.NotNull(await _data.GetNoteAsync(noteId));
    }

    private async Task<HttpClient> LoginAsync(string email)
    {
        var client = _app.CreateNoRedirectClient();
        await client.LoginAsync(email);

        return client;
    }

    private static void AssertRedirectsTo(HttpResponseMessage response, string path)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = new Uri(new Uri("https://localhost"), response.Headers.Location!);
        Assert.Equal(path, location.AbsolutePath);
    }
}
