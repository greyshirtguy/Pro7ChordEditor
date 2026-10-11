// A chord's name: taking one apart, tidying one that was typed, and saying whether what
// is being typed could still become one. spec/02-chord-rules.md, "A chord's name". Held
// to testdata/vectors/chord-names.json.
//
// What a chord is taken to be. A name is a root note (a letter A to G, and perhaps
// sharps or flats), then whatever says what kind of chord it is ("m7", "sus4", "maj7",
// nothing at all), then perhaps a slash and a bass note. Only the two notes are
// understood. What stands between them is carried as it is, which is why a chord nobody
// has heard of ("G1", which a song from Multitracks has) still moves to another key
// rightly.
//
// A chord that is in a file already is never held to IsChord. "C /// | Am ///" is not
// one, and is kept, shown, moved and copied as it is: only what is newly typed is asked.
//
// Port of parts, couldBecome and tidied of reference/simple-presenter/src/chords.cpp.
// The Mac's is ChordNames.swift.

namespace Pro7ChordEditor.ChordRules;

public static partial class Chords
{
    // What a chord's name is written with after its root, besides letters and numbers:
    // # / + - ( ) the degree sign, o with a stroke, ^ . , the two music signs, and the
    // Greek capital delta.
    private static readonly char[] ChordSigns =
        ['#', '/', '+', '-', '(', ')', (char)0x00B0, (char)0x00F8, '^', '.', ',', Text.MusicSharp, Text.MusicFlat, (char)0x0394];

    private static bool IsChordCharacter(char c) => Text.IsLetterOrNumber(c) || Array.IndexOf(ChordSigns, c) >= 0;

    /// <summary>
    /// Takes a chord's name apart: its root, what kind it is, and its bass note. Not
    /// valid if it does not start with a note. The bass is what follows the LAST slash,
    /// if that is a note and nothing more: "C6/9" has a slash and no bass.
    /// </summary>
    public static Parts Parts(string chord)
    {
        var root = Notes.Read(chord, out var length);
        if (!root.Valid)
        {
            return new Parts("", "", "", false);
        }
        var rest = Text.Mid(chord, length);
        var bassName = "";
        var slash = rest.LastIndexOf('/');
        if (slash >= 0)
        {
            var after = Text.Mid(rest, slash + 1);
            var bass = Notes.Read(after, out var bassLength);
            if (bass.Valid && bassLength == after.Length)
            {
                bassName = Notes.Written(bass);
                rest = Text.Left(rest, slash);
            }
        }
        return new Parts(Notes.Written(root), rest, bassName, true);
    }

    /// <summary>
    /// Whether this could be the start of a chord that is being typed: empty, or a note
    /// followed only by what a chord's name is written with. It is what the editors let
    /// through as a chord is typed.
    /// </summary>
    public static bool CouldBecome(string typed)
    {
        if (typed.Length == 0)
        {
            return true;
        }
        if (!Notes.Read(typed, out var length).Valid)
        {
            return false;
        }
        for (var i = length; i < typed.Length; i++)
        {
            if (!IsChordCharacter(typed[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// A typed chord tidied: white space off its ends, and its notes as capitals with
    /// "#" and "b" for their sharps and flats. What is between the notes is the writer's
    /// business: "f#M7/a" is "F#M7/A".
    /// </summary>
    public static string Tidied(string typed)
    {
        var chord = Text.Trimmed(typed);
        var root = Notes.Read(chord, out var length);
        if (!root.Valid)
        {
            return chord;
        }
        var rest = Text.Mid(chord, length);
        var slash = rest.LastIndexOf('/');
        if (slash >= 0)
        {
            var after = Text.Mid(rest, slash + 1);
            var bass = Notes.Read(after, out var bassLength);
            if (bass.Valid && bassLength == after.Length)
            {
                rest = Text.Left(rest, slash + 1) + Notes.Written(bass);
            }
        }
        return Notes.Written(root) + rest;
    }

    /// <summary>
    /// Whether what was typed is a chord: it starts with a note, and has nothing in it
    /// that a chord's name is not written with. What the editors ask before putting a
    /// NEWLY TYPED chord on.
    /// </summary>
    public static bool IsChord(string typed) => Parts(typed).Valid && CouldBecome(typed);
}
