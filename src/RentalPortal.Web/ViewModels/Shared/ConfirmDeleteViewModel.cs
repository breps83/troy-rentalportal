namespace RentalPortal.Web.ViewModels.Shared;

public class ConfirmDeleteViewModel
{
    public required string Title { get; init; }

    public required string Message { get; init; }

    public required string ActionUrl { get; init; }

    public string? Error { get; init; }

}
