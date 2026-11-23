namespace RentalPortal.Web.ViewModels.Applications;

public record ApplicationNoteViewModel(
    int Id,
    int ApplicationId,
    string Note,
    string CreatedBy,
    DateTime CreatedAtUtc,
    string? UpdatedBy,
    DateTime? UpdatedUtc,
    bool IsAuthor)
{

}
