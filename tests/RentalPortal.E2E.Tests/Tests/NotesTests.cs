using Microsoft.Playwright;
using RentalPortal.E2E.Tests.Infrastructure;
using RentalPortal.Web.Enums;
using Xunit.Abstractions;
using static Microsoft.Playwright.Assertions;

namespace RentalPortal.E2E.Tests.Tests;

/// <summary>
/// B3: property manager notes, only the author can edit or delete them, never shown to applicants.
/// </summary>
public class NotesTests(E2EFixture fixture, ITestOutputHelper output) : E2ETestBase(fixture, output)
{
    private ILocator Notes => Page.Locator("#application-notes");

    private ILocator NotesTab => Page.GetByRole(AriaRole.Tab, new() { Name = "Notes" });

    private ILocator NoteItem(string text) => Notes.GetByRole(AriaRole.Listitem).Filter(new() { HasText = text });

    private async Task<int> CreateSubmittedApplicationAsync()
    {
        var property = await Data.CreatePropertyAsync();

        return await Data.CreateApplicationAsync(
            property.Units[0].Id, TestUsers.Applicant, ApplicationStatus.Submitted);
    }

    [Fact]
    [Trait("Requirement", "B3")]
    [Trait("Requirement", "TR 1.b")]
    public async Task Manager_AddsEditsAndDeletesOwnNote()
    {
        var applicationId = await CreateSubmittedApplicationAsync();

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync($"/Applications/Details/{applicationId}");
        await NotesTab.ClickAsync();
        await Expect(Notes).ToContainTextAsync("No notes yet.");

        await Step("B3", "A note can't be saved empty");
        await Modal.OpenAsync(Notes.GetByRole(AriaRole.Button, new() { Name = "Add note" }), "Add note");
        await Modal.SubmitExpectingErrorAsync("Please enter a note.");

        await Step("B3", "Add a note; it appears in the Notes tab with its author");
        await Modal.Field("Note").EnterAsync("Called the prior landlord.");
        await Modal.SubmitAsync();
        await Expect(NoteItem("Called the prior landlord.")).ToContainTextAsync("Bernie Lomax");

        await Step("B3", "Edit the note; it shows who edited it");
        await Modal.OpenAsync(
            NoteItem("Called the prior landlord.").GetByRole(AriaRole.Button, new() { Name = "Edit" }),
            "Edit note");
        await Modal.Field("Note").EnterAsync("Prior landlord confirmed no late payments.");
        await Modal.SubmitAsync();
        await Expect(NoteItem("Prior landlord confirmed no late payments."))
            .ToContainTextAsync("edited by Bernie Lomax");

        await Step("B3", "Delete the note");
        await Modal.OpenAsync(
            NoteItem("Prior landlord confirmed no late payments.")
                .GetByRole(AriaRole.Button, new() { Name = "Delete" }),
            "Delete note");
        await Modal.SubmitAsync("Delete");
        await Expect(Notes).ToContainTextAsync("No notes yet.");
    }

    [Fact]
    [Trait("Requirement", "B3")]
    public async Task AnotherManagersNote_CantBeEditedOrDeleted()
    {
        var applicationId = await CreateSubmittedApplicationAsync();
        var noteId = await Data.CreateNoteAsync(applicationId, TestUsers.OtherManager, "Waiting on pay stubs.");

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync($"/Applications/Details/{applicationId}");

        await Step("B3", "Another manager's note shows no Edit or Delete, and says why");
        await NotesTab.ClickAsync();
        var note = NoteItem("Waiting on pay stubs.");
        await Expect(note).ToContainTextAsync("Only the author can edit");
        await Expect(note.GetByRole(AriaRole.Button)).ToHaveCountAsync(0);

        await Step("B3", "Deleting it with a crafted request is refused on the server");
        var response = await PostFormAsync($"/Applications/DeleteNote/{noteId}", new());
        Assert.Contains("/Account/AccessDenied", response.Headers["location"]);
        Assert.True(await Data.NoteExistsAsync(noteId));
    }

    [Fact]
    [Trait("Requirement", "B3")]
    [Trait("Requirement", "FR 4.a")]
    public async Task ReviewInternalNote_IsSavedToNotes()
    {
        var applicationId = await CreateSubmittedApplicationAsync();

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync($"/Applications/Details/{applicationId}");

        await Step("B3", "An internal note entered in the review is saved to the Notes tab");
        await Modal.OpenAsync(Page.GetByRole(AriaRole.Button, new() { Name = "Review application" }),
            "Review application");
        await Modal.Field("Decision").ChooseAsync("Approve");
        await Modal.Field("Internal note").EnterAsync("Approved after reference check.");
        await Modal.SubmitAsync();

        await NotesTab.ClickAsync();
        await Expect(NoteItem("Approved after reference check.")).ToContainTextAsync("Bernie Lomax");
    }

    [Fact]
    [Trait("Requirement", "B3")]
    public async Task MultiLineNote_KeepsItsLineBreaks()
    {
        var applicationId = await CreateSubmittedApplicationAsync();
        await Data.CreateNoteAsync(applicationId, TestUsers.Manager, "Called the prior landlord.\nLeft a voicemail.");

        await LoginAsAsync(TestUsers.Manager);
        await Page.GotoAsync($"/Applications/Details/{applicationId}");
        await NotesTab.ClickAsync();

        await Step("B3", "A note written on two lines shows on two lines");
        // InnerText is the text as rendered, so the line break is only there if the page keeps it.
        var text = await NoteItem("Called the prior landlord.").InnerTextAsync();
        Assert.Contains("Called the prior landlord.\nLeft a voicemail.", text);
    }

    [Fact]
    [Trait("Requirement", "B3")]
    public async Task Applicant_NeverSeesNotes()
    {
        var applicationId = await CreateSubmittedApplicationAsync();
        await Data.CreateNoteAsync(applicationId, TestUsers.Manager, "Private manager note.");

        await LoginAsAsync(TestUsers.Applicant);
        await Page.GotoAsync($"/Applications/Details/{applicationId}");

        await Step("B3", "The applicant's page has no Notes tab and no note text");
        await Expect(NotesTab).ToHaveCountAsync(0);
        await Expect(Notes).Not.ToBeAttachedAsync();
        await Expect(Page.GetByText("Private manager note.")).ToHaveCountAsync(0);

        await Step("B3", "Requesting the notes directly as the applicant is refused");
        var response = await Context.APIRequest.GetAsync($"/Applications/Notes/{applicationId}",
            new() { MaxRedirects = 0 });
        Assert.Contains("/Account/AccessDenied", response.Headers["location"]);
    }
}
