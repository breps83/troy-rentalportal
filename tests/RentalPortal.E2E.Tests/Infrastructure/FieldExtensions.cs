using Microsoft.Playwright;

namespace RentalPortal.E2E.Tests.Infrastructure;

/// <summary>
/// Fill and select the way a viewer can follow. Playwright's video cursor only moves for pointer
/// actions, so in videos these first move the cursor to the field (a hover, not a click, so the test
/// behaves the same with or without video). Use these instead of FillAsync and SelectOptionAsync.
/// </summary>
public static class FieldExtensions
{
    /// <summary>
    /// How actions are annotated in videos: the cursor, an outline on the element, and the action's
    /// name (for example "Fill "305" GetByLabel("Unit number")") in the top-right corner.
    /// </summary>
    public static ScreencastShowActionsOptions ActionAnnotations => new ScreencastShowActionsOptions
    {
        Duration = 1000,
        Position = AnnotatePosition.TopRight
    };

    public static async Task EnterAsync(this ILocator field, string value)
    {
        if (E2ESettings.Annotate)
            await field.HoverAsync();

        await field.FillAsync(value);
    }

    /// <summary>
    /// Fills a password. Action annotations show the typed value, so they're switched off while it's
    /// filled; the video shows the cursor move to the field but not the value.
    /// </summary>
    public static async Task EnterSecretAsync(this ILocator field, string value)
    {
        if (!E2ESettings.Annotate)
        {
            await field.FillAsync(value);
            return;
        }

        await field.HoverAsync();
        await field.Page.Screencast.HideActionsAsync();
        await field.FillAsync(value);
        await field.Page.Screencast.ShowActionsAsync(ActionAnnotations);
    }

    public static async Task ChooseAsync(this ILocator select, string label)
    {
        if (E2ESettings.Annotate)
            await select.HoverAsync();

        await select.SelectOptionAsync(new SelectOptionValue { Label = label });
    }
}
