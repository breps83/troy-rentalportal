using RentalPortal.Web.Domain;

namespace RentalPortal.Tests;

/// <summary>
/// Tests for the <see cref="ApplicationNote"/> rules: only the author can edit a note.
/// </summary>
public class ApplicationNoteTests
{
    private static readonly DateTime _now = new(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);

    private static ApplicationNote NewNote() =>
        ApplicationNote.Create(applicationId: 3, userId: "manager-1", note: "Called the landlord.", _now);

    [Fact]
    public void IsAuthor_OnlyForTheUserWhoCreatedIt()
    {
        // Act
        var note = NewNote();

        // Assert
        Assert.True(note.IsAuthor("manager-1"));
        Assert.False(note.IsAuthor("manager-2"));
    }

    [Fact]
    public void Edit_ByTheAuthor_UpdatesTheNote()
    {
        // Arrange
        var note = NewNote();

        // Act
        note.Edit("manager-1", "Landlord confirmed.", _now.AddHours(1));

        // Assert
        Assert.Equal("Landlord confirmed.", note.Note);
        Assert.Equal("manager-1", note.UpdatedById);
        Assert.Equal(_now.AddHours(1), note.UpdatedAtUtc);
    }

    [Fact]
    public void Edit_ByAnotherUser_IsRejected()
    {
        // Arrange
        var note = NewNote();

        // Act and assert
        Assert.Throws<RuleException>(() => note.Edit("manager-2", "Changed.", _now.AddHours(1)));
        Assert.Equal("Called the landlord.", note.Note);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyNote_IsRejected(string text)
    {
        // Act and assert
        Assert.Throws<RuleException>(() => ApplicationNote.Create(3, "manager-1", text, _now));
    }

    [Fact]
    public void Create_TooLong_IsRejected()
    {
        // Arrange
        var text = new string('a', ApplicationNote.MaxLength + 1);

        // Act and assert
        Assert.Throws<RuleException>(() => ApplicationNote.Create(3, "manager-1", text, _now));
    }

    [Fact]
    public void Create_AtMaxLengthAfterTrimming_IsAllowed()
    {
        // Arrange
        var text = "  " + new string('a', ApplicationNote.MaxLength) + "  ";

        // Act
        var note = ApplicationNote.Create(3, "manager-1", text, _now);

        // Assert
        Assert.Equal(ApplicationNote.MaxLength, note.Note.Length);
    }

    [Fact]
    public void Create_TrimsTheNote()
    {
        // Act
        var note = ApplicationNote.Create(3, "manager-1", "  Called the landlord.  ", _now);

        // Assert
        Assert.Equal("Called the landlord.", note.Note);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Edit_EmptyNote_IsRejected(string text)
    {
        // Arrange
        var note = NewNote();

        // Act and assert
        Assert.Throws<RuleException>(() => note.Edit("manager-1", text, _now.AddHours(1)));
        Assert.Equal("Called the landlord.", note.Note);
        Assert.Null(note.UpdatedAtUtc);
    }

    [Fact]
    public void Edit_TooLong_IsRejected()
    {
        // Arrange
        var note = NewNote();
        var text = new string('a', ApplicationNote.MaxLength + 1);

        // Act and assert
        Assert.Throws<RuleException>(() => note.Edit("manager-1", text, _now.AddHours(1)));
        Assert.Equal("Called the landlord.", note.Note);
    }
}
