// What the first Windows editor left in a song, found: the checks of
// spec/08-repair.md, made on a song's file as it is. Held to
// testdata/files/expected/repairs.json, which has what they find in every test file.
//
// NEW: Simple Presenter has none of this. The reference is tools/make-fixtures.py
// (`ranges_over_line_breaks`, `line_break_shift`, `key_check`). The rules of two of the
// three are in ChordRules (OldEditor.cs, KeyFit.cs), each held to vectors of its own;
// this is where they are asked of a file.
//
// WHICH TEXT BOXES: every one the file has on a slide. Of every cue, switched on or
// not, of every action of it that is a slide, every element that has text with RTF in
// it, in the file's order. Not only those the editors show. The page does not say which;
// the reference goes through every text box of the file, and so did the editor whose
// doing this is, which wrote to the first element of every slide of every cue, whatever
// that was (windows/old). So a song is marked for what is in its file, wherever in it,
// and a mending mends all of it. (No test file has a text box the editors do not show
// with a chord on it, so repairs.json does not tell the two apart: NOTES.md.)
//
// A chord here is as the file has it: its range's start AND its end, which is what
// check 1 is about, and which the editors are never handed (Reading.cs).
//
// Nothing here changes anything. Putting right is OpenSong's (`Mend`, `MoveBack`,
// `SetKey`).

using Google.Protobuf;
using Pro7ChordEditor.ChordRules;
using RV = Pro.SerializationInterop.RVProtoData;

namespace Pro7ChordEditor.Song;

/// <summary>What the checks find in a song (spec/08-repair.md, checks 1 to 3).</summary>
/// <param name="ShownTwice">Check 1: how many chords have a range that runs over a line break, which makes ProPresenter show the chord again at the start of the next line. Any at all is the first Windows editor's mark.</param>
/// <param name="Shift">Check 2: whether the chords look as if they were put on by hand in that editor, each a little to the left of where it was meant on every line but a first.</param>
/// <param name="StatedKey">The key the song says its chords are in, or "" if it says none.</param>
/// <param name="Key">Check 3: which keys the song's chords fit, whether the stated one is to be doubted, and which to offer.</param>
public sealed record Findings(int ShownTwice, OldEditor.Shift Shift, string StatedKey, KeyFit.Answer Key)
{
    /// <summary>What that editor wrote for the key when nothing was chosen in its list of keys: the first of them (spec/08-repair.md, C).</summary>
    public const string OldEditorsKey = "Ab";

    /// <summary>
    /// Whether the key is nearly certainly that editor's doing: it is doubtful, it is
    /// the one that editor wrote by accident, and the song has that editor's mark.
    /// </summary>
    public bool KeyIsTheOldEditors => Key.Doubtful && ShownTwice > 0 && string.Equals(StatedKey, OldEditorsKey, StringComparison.Ordinal);
}

/// <summary>A chord as the file has it: the stretch of the words it is written over, and its name.</summary>
internal readonly record struct WrittenChord(int Start, int End, string Name);

/// <summary>A text box as the checks see it: its words, and its chords in the file's order.</summary>
internal sealed record CheckedBox(string Words, IReadOnlyList<WrittenChord> Chords);

public static class Checks
{
    /// <summary>What the checks find in a song's file.</summary>
    /// <exception cref="Refusal">The file cannot be read, or is no presentation.</exception>
    public static Findings Read(string path)
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            throw Refusal.CannotBeRead(Path.GetFileName(path), exception.Message);
        }
        try
        {
            return Of(RV.Presentation.Parser.ParseFrom(bytes));
        }
        catch (InvalidProtocolBufferException)
        {
            throw Refusal.CannotBeRead(Path.GetFileName(path), Refusal.NoPresentation);
        }
    }

    internal static Findings Of(RV.Presentation presentation)
    {
        var boxes = Boxes(presentation);
        var stated = presentation.Music is null ? "" : Reading.KeyName(presentation.Music.Original);
        return new Findings(
            boxes.Sum(ShownTwice),
            OldEditor.LineBreakShift(boxes.Select(box => (box.Words, (IReadOnlyList<int>)[.. box.Chords.Select(chord => chord.Start)]))),
            stated,
            KeyFit.Likely([.. boxes.SelectMany(box => box.Chords.Select(chord => chord.Name))], stated));
    }

    /// <summary>
    /// Every text of the file's slides, in the file's order: what the checks look at,
    /// and what a mending writes.
    /// </summary>
    internal static IEnumerable<RV.Graphics.Types.Text> Texts(RV.Presentation presentation) =>
        presentation.Cues.SelectMany(Texts);

    /// <summary>Every text of a cue's slides.</summary>
    internal static IEnumerable<RV.Graphics.Types.Text> Texts(RV.Cue cue)
    {
        foreach (var action in cue.Actions)
        {
            foreach (var element in action.Slide?.Presentation?.BaseSlide?.Elements ?? [])
            {
                // (A text with no RTF has no words for a chord to be among, and the
                // reference passes it over.)
                if (element.Element_?.Text is { RtfData.IsEmpty: false } text)
                {
                    yield return text;
                }
            }
        }
    }

    internal static List<CheckedBox> Boxes(RV.Presentation presentation) => [.. Texts(presentation).Select(Box)];

    internal static CheckedBox Box(RV.Graphics.Types.Text text) => new(Reading.WordsOf(text), Written(text));

    /// <summary>A text's chords as the file has them, in the file's order.</summary>
    internal static List<WrittenChord> Written(RV.Graphics.Types.Text text)
    {
        var written = new List<WrittenChord>();
        foreach (var attribute in text.Attributes?.CustomAttributes ?? [])
        {
            if (Reading.IsAChord(attribute))
            {
                written.Add(new WrittenChord(attribute.Range?.Start ?? 0, attribute.Range?.End ?? 0, attribute.Chord));
            }
        }
        return written;
    }

    /// <summary>
    /// Check 1, for one text box: how many of its chords have a range with a line break
    /// strictly inside it (the range starts before the break and ends after it).
    /// </summary>
    internal static int ShownTwice(CheckedBox box)
    {
        var breaks = new List<int>();
        for (var place = 0; place < box.Words.Length; place++)
        {
            if (box.Words[place] == Words.LineBreak)
            {
                breaks.Add(place);
            }
        }
        return box.Chords.Count(chord => breaks.Any(at => chord.Start < at && at < chord.End));
    }
}
