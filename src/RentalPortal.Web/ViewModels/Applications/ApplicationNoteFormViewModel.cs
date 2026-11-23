using RentalPortal.Web.Domain;
using System.ComponentModel.DataAnnotations;

namespace RentalPortal.Web.ViewModels.Applications;

public class ApplicationNoteFormViewModel
{
    public int ApplicationId { get; set; }

    public int? NoteId { get; set; }

    [Required(ErrorMessage = "Please enter a note.")]
    [StringLength(ApplicationNote.MaxLength)]
    [Display(Name = "Note")]
    public string? Note { get; set; }
}
