// The marks beside a song's name in the list: what can be said of the song at a glance.
// spec/06-library.md, rules 9 to 11.
//
// So far two of the four the page names: that it has chords, and that it is one of the
// first Windows editor's "(Chords)" copies. That copies of it are kept is milestone M4,
// and that something in it wants mending is M8.
//
// A mark needs the file to be read, which for a big library takes a while. So the names
// are listed at once, the marks are worked out afterwards and off the thread that draws,
// and they are kept (by AppData) under the file's path, with its size and time of last
// change: the next start has them at once, and a file that has changed since is read
// again.
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
public sealed record Marks(int Chords = 0, bool IsOldEditorsCopy = false, bool CannotBeRead = false)
{
    /// <summary>What the first Windows editor put after the name of a copy it saved.</summary>
    public const string OldEditorsEnding = " (Chords)";

    // (Worked out from the count, and so not written down with it.)
    [JsonIgnore]
    public bool HasChords => Chords > 0;

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
        return new Marks(chords, copy);
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
}

public static class MarkFilters
{
    /// <summary>
    /// Whether a song with these marks is let through. One whose marks are not worked
    /// out yet is let through only by <see cref="MarkFilter.All"/>; and one that cannot
    /// be read is not said to be without chords: nobody knows.
    /// </summary>
    public static bool Lets(this MarkFilter filter, Marks? marks) => filter switch
    {
        MarkFilter.WithChords => marks is { HasChords: true },
        MarkFilter.WithoutChords => marks is { HasChords: false, CannotBeRead: false },
        _ => true,
    };
}
