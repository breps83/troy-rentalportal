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

    [Fact]
    public async Task Applicant_CannotGoBackOnAnotherApplicantsApplication()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.OtherApplicant, ApplicationStatus.Draft);
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.PostFormAsync($"/Applications/Details/{applicationId}",
            new Dictionary<string, string>
            {
                ["command"] = "back",
                ["CurrentSection"] = nameof(ApplicationSection.ResidenceHistory)
            });

        AssertRedirectsTo(response, "/Account/AccessDenied");
    }

    [Fact]
    public async Task Applicant_CannotCompleteResidenceHistoryOfAnotherApplicantsApplication()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.OtherApplicant, ApplicationStatus.Draft);
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.PostFormAsync($"/Applications/Details/{applicationId}",
            new Dictionary<string, string>
            {
                ["command"] = "continue",
                ["CurrentSection"] = nameof(ApplicationSection.ResidenceHistory)
            });

        AssertRedirectsTo(response, "/Account/AccessDenied");
    }

    [Fact]
    public async Task Applicant_CannotSubmitAnotherApplicantsApplication()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.OtherApplicant, ApplicationStatus.Draft);
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.PostFormAsync($"/Applications/Details/{applicationId}",
            new Dictionary<string, string>
            {
                ["command"] = "submit",
                ["CurrentSection"] = nameof(ApplicationSection.Summary)
            });

        AssertRedirectsTo(response, "/Account/AccessDenied");

        var application = await _data.GetApplicationAsync(applicationId);
        Assert.Equal(ApplicationStatus.Draft, application.Status);
    }

    [Fact]
    public async Task Applicant_CannotOpenWithdrawForAnotherApplicantsApplication()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.OtherApplicant, ApplicationStatus.Submitted);
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.GetAsync($"/Applications/Withdraw/{applicationId}");

        AssertRedirectsTo(response, "/Account/AccessDenied");
    }

    [Fact]
    public async Task Applicant_WithdrawModalForWithdrawnApplication_ShowsError()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Withdrawn);
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.GetAsync($"/Applications/Withdraw/{applicationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("This application is Withdrawn and can no longer be withdrawn.",
            await response.Content.ReadAsStringAsync());
    }

    /// <summary>A second withdraw, for example from a double-click or a stale modal, is refused.</summary>
    [Fact]
    public async Task Applicant_CannotWithdrawTwice()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Withdrawn);
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.PostFormAsync($"/Applications/Withdraw/{applicationId}", []);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"success\":true", html);
        Assert.Contains("An application that is Withdrawn can&#x27;t be withdrawn.", html);
    }

    [Theory]
    [InlineData("/Applications/EditNote/{0}")]
    [InlineData("/Applications/DeleteNote/{0}")]
    public async Task Manager_CannotOpenAnotherManagersNote(string urlFormat)
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Submitted);
        var noteId = await _data.CreateNoteAsync(applicationId, TestUsers.OtherManager, "Original note.");
        var client = await LoginAsync(TestUsers.Manager);

        var response = await client.GetAsync(string.Format(urlFormat, noteId));

        AssertRedirectsTo(response, "/Account/AccessDenied");
    }

    [Fact]
    public async Task Applicant_CannotListAnotherApplicantsResidences()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.OtherApplicant, ApplicationStatus.Draft);
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.GetAsync($"/Residences/List?applicationId={applicationId}");

        AssertRedirectsTo(response, "/Account/AccessDenied");
    }

    [Fact]
    public async Task Manager_CannotOpenResidences()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Submitted);
        var client = await LoginAsync(TestUsers.Manager);

        var response = await client.GetAsync($"/Residences/List?applicationId={applicationId}");

        AssertRedirectsTo(response, "/Account/AccessDenied");
    }

    [Theory]
    [InlineData("/Residences/Create?applicationId={0}")]
    [InlineData("/Residences/Edit/{1}")]
    [InlineData("/Residences/Delete/{1}")]
    public async Task Applicant_CannotOpenResidenceModalsOnAnotherApplicantsApplication(string urlFormat)
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.OtherApplicant, ApplicationStatus.Draft);
        var residence = (await _data.GetResidencesAsync(applicationId)).Single();
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.GetAsync(string.Format(urlFormat, applicationId, residence.Id));

        AssertRedirectsTo(response, "/Account/AccessDenied");
    }

    [Theory]
    [InlineData("/Residences/Create?applicationId={0}")]
    [InlineData("/Residences/Edit/{1}")]
    [InlineData("/Residences/Delete/{1}")]
    public async Task Applicant_ResidenceModalsOnSubmittedApplication_ShowError(string urlFormat)
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Submitted);
        var residence = (await _data.GetResidencesAsync(applicationId)).Single();
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.GetAsync(string.Format(urlFormat, applicationId, residence.Id));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("This application is Submitted and can no longer be edited.",
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Applicant_CannotAddResidenceToAnotherApplicantsApplication()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.OtherApplicant, ApplicationStatus.Draft);
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.PostFormAsync("/Residences/Create", ResidenceFields(applicationId));

        AssertRedirectsTo(response, "/Account/AccessDenied");
        Assert.Single(await _data.GetResidencesAsync(applicationId));
    }

    [Fact]
    public async Task Applicant_CannotEditAnotherApplicantsResidence()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.OtherApplicant, ApplicationStatus.Draft);
        var before = (await _data.GetResidencesAsync(applicationId)).Single();
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.PostFormAsync("/Residences/Edit", ResidenceFields(applicationId, before.Id));

        AssertRedirectsTo(response, "/Account/AccessDenied");
        var after = (await _data.GetResidencesAsync(applicationId)).Single();
        Assert.Equal(before.Address, after.Address);
    }

    [Fact]
    public async Task Applicant_CannotDeleteAnotherApplicantsResidence()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.OtherApplicant, ApplicationStatus.Draft);
        var residence = (await _data.GetResidencesAsync(applicationId)).Single();
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.PostFormAsync($"/Residences/Delete/{residence.Id}", []);

        AssertRedirectsTo(response, "/Account/AccessDenied");
        Assert.Single(await _data.GetResidencesAsync(applicationId));
    }

    [Fact]
    public async Task Applicant_CannotAddResidenceToSubmittedApplication()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Submitted);
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.PostFormAsync("/Residences/Create", ResidenceFields(applicationId));

        await AssertNotEditableAsync(response, "Submitted");
        Assert.Single(await _data.GetResidencesAsync(applicationId));
    }

    [Fact]
    public async Task Applicant_CannotEditResidenceOfSubmittedApplication()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Submitted);
        var before = (await _data.GetResidencesAsync(applicationId)).Single();
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.PostFormAsync("/Residences/Edit", ResidenceFields(applicationId, before.Id));

        await AssertNotEditableAsync(response, "Submitted");
        var after = (await _data.GetResidencesAsync(applicationId)).Single();
        Assert.Equal(before.Address, after.Address);
    }

    [Fact]
    public async Task Applicant_CannotDeleteResidenceOfWithdrawnApplication()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Withdrawn);
        var residence = (await _data.GetResidencesAsync(applicationId)).Single();
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.PostFormAsync($"/Residences/Delete/{residence.Id}", []);

        await AssertNotEditableAsync(response, "Withdrawn");
        Assert.Single(await _data.GetResidencesAsync(applicationId));
    }

    /// <summary>A form command the page doesn't offer is a bad request, and changes nothing.</summary>
    [Fact]
    public async Task Applicant_UnknownFormCommand_IsBadRequest()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Draft);
        var client = await LoginAsync(TestUsers.Applicant);

        var response = await client.PostFormAsync($"/Applications/Details/{applicationId}",
            new Dictionary<string, string>
            {
                ["command"] = "approve",
                ["CurrentSection"] = nameof(ApplicationSection.Summary)
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var application = await _data.GetApplicationAsync(applicationId);
        Assert.Equal(ApplicationStatus.Draft, application.Status);
    }

    /// <summary>A record that doesn't exist (id 0 is never used) is not found, not an error page.</summary>
    [Theory]
    [InlineData(TestUsers.Manager, "/Properties/Edit/0")]
    [InlineData(TestUsers.Manager, "/Properties/Delete/0")]
    [InlineData(TestUsers.Manager, "/Units/Create?propertyId=0")]
    [InlineData(TestUsers.Manager, "/Units/Edit/0")]
    [InlineData(TestUsers.Manager, "/Units/Delete/0")]
    [InlineData(TestUsers.Manager, "/Applications/Details/0")]
    [InlineData(TestUsers.Manager, "/Applications/Review/0")]
    [InlineData(TestUsers.Manager, "/Applications/Status/0")]
    [InlineData(TestUsers.Manager, "/Applications/History/0")]
    [InlineData(TestUsers.Manager, "/Applications/Notes/0")]
    [InlineData(TestUsers.Manager, "/Applications/AddNote/0")]
    [InlineData(TestUsers.Manager, "/Applications/EditNote/0")]
    [InlineData(TestUsers.Manager, "/Applications/DeleteNote/0")]
    [InlineData(TestUsers.Applicant, "/Applications/Withdraw/0")]
    [InlineData(TestUsers.Applicant, "/Residences/List?applicationId=0")]
    [InlineData(TestUsers.Applicant, "/Residences/Create?applicationId=0")]
    [InlineData(TestUsers.Applicant, "/Residences/Edit/0")]
    [InlineData(TestUsers.Applicant, "/Residences/Delete/0")]
    public async Task MissingRecord_IsNotFound(string email, string url)
    {
        var client = await LoginAsync(email);

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Form fields for a valid residence. With <paramref name="residenceId"/>, the fields edit that residence.
    /// </summary>
    private static Dictionary<string, string> ResidenceFields(int applicationId, int residenceId = 0) => new()
    {
        ["Id"] = residenceId.ToString(),
        ["ApplicationId"] = applicationId.ToString(),
        ["Address"] = "99 Changed St, Springfield, IL 62701",
        ["LandlordName"] = "Changed Landlord",
        ["LandlordPhone"] = "217-555-0123",
        ["MoveInDate"] = "2019-01-01",
        ["MoveOutDate"] = "2021-05-31"
    };

    /// <summary>
    /// The residence modal shows the not-editable error instead of saving.
    /// </summary>
    private static async Task AssertNotEditableAsync(HttpResponseMessage response, string status)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"success\":true", html);
        Assert.Contains($"This application is {status} and can no longer be edited.", html);
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
