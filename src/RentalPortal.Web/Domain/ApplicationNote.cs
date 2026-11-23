namespace RentalPortal.Web.Domain;

public class ApplicationNote
{
    public const int MaxLength = 4000;

    public int Id { get; private set; }

    public int RentalApplicationId { get; private set; }

    public string Note { get; private set; } = null!;

    public string CreatedById { get; private set; } = null!;

    public ApplicationUser CreatedByUser { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    public string? UpdatedById { get; private set; }

    public ApplicationUser? UpdatedByUser { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public static ApplicationNote Create(
        int applicationId,
        string userId,
        string note,
        DateTime nowUtc)
    {
        return new ApplicationNote
        {
            RentalApplicationId = applicationId,
            Note = ValidateNote(note),
            CreatedById = userId,
            CreatedAtUtc = nowUtc
        };
    }

    public bool IsAuthor(string userId) => CreatedById == userId;

    public void Edit(
        string userId,
        string note,
        DateTime nowUtc)
    {
        if (!IsAuthor(userId))
            throw new RuleException("Only the author can edit this note.");

        Note = ValidateNote(note);

        UpdatedById = userId;

        UpdatedAtUtc = nowUtc;
    }

    private static string ValidateNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
            throw new RuleException("Please add a note.");

        note = note.Trim();

        if (note.Length > MaxLength)
            throw new RuleException($"A note cannot be longer than {MaxLength} characters.");

        return note;
    }
    
}
