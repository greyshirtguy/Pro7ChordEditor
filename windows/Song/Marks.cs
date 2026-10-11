// The marks beside a song's name in the list: what can be said of the song at a glance.
// spec/06-library.md, rules 9 to 11.
//
// Of the five the page names, four are worked out here from the file: that it has
// chords, that something in it wants mending (what the checks of spec/08-repair.md
// find: Checks.cs), that it is one of the first Windows editor's "(Chords)" copies, and
// that it cannot be read. The fifth, that copies of it are kept, is not a thing of the
// file's and is asked of the copies (Copies.cs).
//
// A mark needs the file to be read, which for a big library takes a while. So the names
// are listed at once, the marks are worked out afterwards and off the thread that draws,
// and they are kept (by AppData) under the file's path, with its size and time of last
// change: the next start has them at once, and a file that has changed since is read
// again. They are kept with a number that says which reading made them (`Version`
// below), so that marks an older build worked out, which knew less, are not believed.
//
// Reading a file for its marks is only reading. `Read` opens nothing for change.
//
// The Mac's is Marks.swift.

using System.Text.Json.Serialization;
using Google.Protobuf;
using RV = Pro.SerializationInterop.RVProtoData;

namespace Pro7ChordEditor.Song;

/// <param name="Chords">How many chords the song has, on the slides and boxes the editors would show.</param>
/// <param name="IsOldEditorsCopy">Whether its name ends " (Chords)", as the first Windows editor named the copies it saved beside the songs it was given (spec/08-repair.md, D).</param>
/// <param name="CannotBeRead">Whether the file could not be read as a presentation.</param>
/// <param name="ShownTwice">Check 1: how many of its chords are written so that ProPresenter shows them twice. In every text box of the file, shown by the editors or not (Checks.cs).</param>
/// <param name="DoubtedKey">Check 3: the key the song says it is in, if its chords make that doubtful; else "".</param>
/// <param name="LikelierKey">Check 3: the key its chords look like, where the stated one is doubted; else "".</param>
/// <param name="LooksMovedLeft">Check 2: whether its chords look as if they were put on by hand in the first Windows editor. It counts for nothing unless that check's offer is switched on.</param>
public sealed record Marks(
    int Chords = 0,
    bool IsOldEditorsCopy = false,
    bool CannotBeRead = false,
    int ShownTwice = 0,
    string DoubtedKey = "",
    string LikelierKey = "",
    bool LooksMovedLeft = false)
{
    /// <summary>What the first Windows editor put after the name of a copy it saved.</summary>
    public const string OldEditorsEnding = " (Chords)";

    /// <summary>
    /// Which reading of a file these marks are: a number that goes up whenever `Read`
    /// comes to say more, or other, of the same file. Marks kept under another number
    /// are worked out again (AppData.cs).
    ///
    ///   1  whether it has chords, is a "(Chords)" copy, cannot be read
    ///   2  and what the checks find in it
    /// </summary>
    public const int Version = 2;

    // (Worked out from the count, and so not written down with it.)
    [JsonIgnore]
    public bool HasChords => Chords > 0;

    /// <summary>
    /// Whether something in it wants mending: chords shown twice, or a key that does not
    /// fit its chords; and, only where that check's offer is switched on
    /// (<paramref name="offeringToMoveBack"/>), chords that look as if they were put on
    /// by hand in the first Windows editor.
    ///
    /// (Not a song that states no key at all. The same sum can say what key its chords
    /// look like, and the sheet does; but nothing in it is broken.)
    /// </summary>
    public bool WantsMending(bool offeringToMoveBack = false) =>
        ShownTwice > 0 || DoubtedKey.Length > 0 || (offeringToMoveBack && LooksMovedLeft);

    /// <summary>Reads a song's file and says what its marks are.</summary>
    public static Marks Read(ListedSong file)
    {
        var copy = file.Name.EndsWith(OldEditorsEnding, StringComparison.Ordinal);
        RV.Presentation presentation;
        try
        {
            presentation = RV.Presentation.Parser.ParseFrom(File.ReadAllBytes(file.Path));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidProtocolBufferException or NotSupportedException or ArgumentException)
        {
            return new Marks(IsOldEditorsCopy: copy, CannotBeRead: true);
        }
        var chords = 0;
        foreach (var row in Reading.SlideRows(presentation))
        {
            foreach (var element in Reading.ElementsOf(row, presentation))
            {
                if (Reading.ShowsItsOwnWords(element))
                {
                    chords += Reading.ChordsOf(element.Element_?.Text).Count;
                }
            }
        }
        var found = Checks.Of(presentation);
        return new Marks(
            chords,
            copy,
            ShownTwice: found.ShownTwice,
            DoubtedKey: found.Key.Doubtful ? found.StatedKey : "",
            LikelierKey: found.Key.Doubtful ? found.Key.Suggested : "",
            LooksMovedLeft: found.Shift.Likely);
    }
}

/// <summary>The marks worked out before, each with the size and time its file had then.</summary>
public sealed class KeptMarks
{
    /// <summary>What is kept of one file.</summary>
    public sealed record Entry(long Size, DateTime Modified, Marks Marks);

    private readonly Dictionary<string, Entry> entries = new(Paths.ById);

    public KeptMarks()
    {
    }

    /// <summary>From what was written down before: each file's path, and what was kept of it.</summary>
    public KeptMarks(IEnumerable<KeyValuePair<string, Entry>> kept)
    {
        foreach (var (path, entry) in kept)
        {
            entries[path] = entry;
        }
    }

    /// <summary>Everything kept, by path, to be written down.</summary>
    public IReadOnlyDictionary<string, Entry> Entries => entries;

    /// <summary>What was worked out for this file, if the file is still as it was then.</summary>
    public Marks? MarksFor(ListedSong file) =>
        entries.TryGetValue(file.Id, out var entry) && entry.Size == file.Size && entry.Modified == file.Modified ? entry.Marks : null;

    public void Keep(Marks marks, ListedSong file) => entries[file.Id] = new Entry(file.Size, file.Modified, marks);

    public int Count => entries.Count;
}

/// <summary>What the list can be narrowed to (rule 11).</summary>
public enum MarkFilter
{
    All,
    WithChords,
    WithoutChords,
    WantingMending,
}

public static class MarkFilters
{
    /// <summary>
    /// Whether a song with these marks is let through. One whose marks are not worked
    /// out yet is let through only by <see cref="MarkFilter.All"/>; and one that cannot
    /// be read is not said to be without chords: nobody knows. What wants mending is
    /// Marks' to say, and goes by whether the offer of check 2 is switched on.
    /// </summary>
    public static bool Lets(this MarkFilter filter, Marks? marks, bool offeringToMoveBack = false) => filter switch
    {
        MarkFilter.WithChords => marks is { HasChords: true },
        MarkFilter.WithoutChords => marks is { HasChords: false, CannotBeRead: false },
        MarkFilter.WantingMending => marks is not null && marks.WantsMending(offeringToMoveBack),
        _ => true,
    };
}
