// Sharps and flats drawn as the music signs, U+266F and U+266D, where they are clearly a
// sharp or a flat: how the chord sheet draws a chord's name. spec/02-chord-rules.md,
// "Sharps and flats drawn as signs". Held to testdata/vectors/signs.json.
//
// Not from chords.cpp: new here, the maintainer's (2026-10-14), "just a visual skin -
// not changing the underlying data". Its reference is tools/signs.py, whose head says
// the rule and why each part of it is there. The Mac's is Signs.swift.
//
// What WithSigns gives is only ever drawn. Nothing that is written, copied or carried is
// made from it, and the ChordPro text never has it (spec/03-chord-sheet.md, rule 10a). A
// "#" and its sign are one unit each, and so are a "b" and its, so a name drawn is as
// long as the name, and a place in one is the same place in the other: the caret in the
// box a chord is typed in never moves for a sign (rule 30a).

namespace Pro7ChordEditor.ChordRules;

public static partial class Chords
{
    /// <summary>
    /// A chord's name as the chord sheet draws it: "F#m7" with the sharp sign, "Bb" and
    /// "C7b9" and "Eb/G" with the flat sign. The root's sharps and flats, the bass's, and
    /// between them a "#" or "b" that has a digit 0 to 9 straight after it; only for a
    /// name that is a chord whose root is a capital. Anything else is given back as it
    /// is: "N.C.", "Bb riff", and "b3/b5", a chord in numbers, whose small b may be no
    /// note at all.
    /// </summary>
    public static string WithSigns(string name)
    {
        if (name.Length == 0 || !IsCapitalNote(name[0]) || !IsChord(name))
        {
            return name;
        }
        var shown = name.ToCharArray();
        Notes.Read(name, out var root);
        Signed(shown, 1, root);
        // The bass, as Parts finds one: after the last slash, a note and nothing more.
        var end = name.Length;
        var slash = Text.Mid(name, root).LastIndexOf('/');
        if (slash >= 0)
        {
            var at = root + slash + 1;
            if (Notes.Read(Text.Mid(name, at), out var length).Valid && at + length == name.Length)
            {
                end = root + slash;
                if (IsCapitalNote(name[at]))
                {
                    Signed(shown, at + 1, name.Length);
                }
            }
        }
        // Between the notes, a "#" or "b" with a digit straight after it. ("Bm7b", half
        // typed, keeps its "b" until the 5 comes.)
        for (var i = root; i < end; i++)
        {
            if (name[i] is '#' or 'b' && i + 1 < name.Length && Text.IsAsciiDigit(name[i + 1]))
            {
                shown[i] = name[i] == '#' ? Text.MusicSharp : Text.MusicFlat;
            }
        }
        return new string(shown);
    }

    /// <summary>
    /// The other way: every sharp sign as "#" and every flat sign as "b", and nothing
    /// else changed. What the box a chord is typed in holds, while it shows the signs, is
    /// read so before anything is done with it.
    /// </summary>
    public static string WithoutSigns(string text) => text.Replace(Text.MusicSharp, '#').Replace(Text.MusicFlat, 'b');

    private static bool IsCapitalNote(char c) => c >= 'A' && c <= 'G';

    // The "#" and "b" of the units from `from` up to `to` as their signs.
    private static void Signed(char[] name, int from, int to)
    {
        for (var i = from; i < to; i++)
        {
            name[i] = name[i] switch
            {
                '#' => Text.MusicSharp,
                'b' => Text.MusicFlat,
                _ => name[i],
            };
        }
    }
}
