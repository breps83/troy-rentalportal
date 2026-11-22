using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace RentalPortal.E2E.Tests.Infrastructure;

/// <summary>
/// Drives the shared modal (#app-modal, wwwroot/js/site.js). A trigger with data-modal-url fetches a
/// partial into the modal. A POST either re-renders the partial with errors (the modal stays open) or
/// returns JSON success (the modal closes and data-refresh-target reloads, or the page redirects).
/// </summary>
public class Modal(IPage page)
{
    // Only the open modal: the last partial's HTML stays in the DOM after it closes.
    public ILocator Root => page.Locator("#app-modal.show");

    public ILocator Title => Root.Locator(".modal-title");

    /// <summary>
    /// Clicks a trigger and waits until the modal is open and showing <paramref name="title"/>.
    /// </summary>
    public async Task OpenAsync(ILocator trigger, string title)
    {
        await trigger.ClickAsync();
        await Expect(Root).ToBeVisibleAsync();
        await Expect(Title).ToHaveTextAsync(title);
    }

    public ILocator Field(string label) => Root.GetByLabel(label, new() { Exact = true });

    public ILocator Button(string name) => Root.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    /// <summary>
    /// Clicks the submit button and waits for the modal to close (the save succeeded).
    /// </summary>
    public async Task SubmitAsync(string buttonName = "Save")
    {
        await Button(buttonName).ClickAsync();
        await Expect(page.Locator("#app-modal")).ToBeHiddenAsync();
    }

    /// <summary>
    /// Clicks the submit button when the save should fail, then waits for the modal to show
    /// <paramref name="error"/> (a field error or a summary error).
    /// </summary>
    public async Task SubmitExpectingErrorAsync(string error, string buttonName = "Save")
    {
        await Button(buttonName).ClickAsync();
        await Expect(Root.GetByText(error)).ToBeVisibleAsync();
        await Expect(Root).ToBeVisibleAsync();
    }

    public async Task CancelAsync()
    {
        await Button("Cancel").ClickAsync();
        await Expect(page.Locator("#app-modal")).ToBeHiddenAsync();
    }
}
