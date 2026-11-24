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

    /// <summary>
    /// Creates an application note.
    /// </summary>
    /// <param name="applicationId">The identifier of the rental application.</param>
    /// <param name="userId">The identifier of the user creating the note.</param>
    /// <param name="note">The note text.</param>
    /// <param name="nowUtc">The UTC date and time when the note is created.</param>
    /// <returns>A new application note.</returns>
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

    /// <summary>
    /// Determines whether the specified user created this item.
    /// </summary>
    /// <param name="userId">The ID of the user to compare with the creator ID.</param>
    /// <returns><see langword="true"/> if the specified user created this item; otherwise, <see langword="false"/>.</returns>
    public bool IsAuthor(string userId) => CreatedById == userId;

    /// <summary>
    /// Edits the note and records the user and UTC timestamp of the update.
    /// </summary>
    /// <param name="userId">The identifier of the user editing the note.</param>
    /// <param name="note">The updated note text.</param>
    /// <param name="nowUtc">The UTC date and time of the update.</param>
    /// <exception cref="RuleException">Thrown when the user is not the author or the note is invalid.</exception>
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
