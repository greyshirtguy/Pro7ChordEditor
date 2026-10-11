// A text box's chords: put in order and made fit to be kept, as the stretches of the
// words the file writes them over, and where they stand when the words under them are
// changed. spec/02-chord-rules.md, "A box's chords, in order", and
// spec/01-file-format.md, "Chords". Held to testdata/vectors/tidy.json, ranges.json and
// carried.json.
//
// Everything that is written to a file goes through Tidy and Ranges, so these two are
// what a saved song's chords are made of.
//
// How a chord is written. ProPresenter does not keep where a chord stands but a stretch
// of characters it belongs to. What is written here is what Multitracks writes, its
// songs being the ones ProPresenter is given with chords and so the best measure there
// is of what it expects:
//
//   - on a line of words, from the chord to the next chord or to the end of its line,
//     whichever comes first, and never empty;
//   - on a line of chords alone (IsPlaceholders), the chord's own stand-in and no more,
//     one character, so that the wide space between two chords is in neither: three
//     chords are 0 to 1, 2 to 3 and 4 to 5.
//
// The first Windows editor ran every chord's stretch on to the next chord wherever that
// was, over line breaks, and ProPresenter then drew the chord again on the next line
// (spec/08-repair.md, A). A range written here never crosses a line break.
//
// Port of tidy, carried and ranges of reference/simple-presenter/src/chords.cpp. The
// Mac's is ChordsInABox.swift.

namespace Pro7ChordEditor.ChordRules;

public static partial class Chords
{
    /// <summary>
    /// The chords of a text box put in order and made fit to be kept:
    ///   - names are trimmed, and a chord with no name is dropped;
    ///   - each place is brought onto a character the text has: below 0 to 0, past the
    ///     end to the last character;
    ///   - a chord on a line break goes to the character before it, if that is not a
    ///     line break too (the end of a line has no character of its own that shows);
    ///   - two in one place: the later one given stands, in the earlier one's position;
    ///   - then sorted by place, keeping order among equals.
    /// An empty text has no chords.
    /// </summary>
    public static IReadOnlyList<Chord> Tidy(string text, IEnumerable<Chord> chords)
    {
        var result = new List<Chord>();
        if (text.Length == 0)
        {
            return result;
        }
        foreach (var given in chords)
        {
            var name = Text.Trimmed(given.Name);
            if (name.Length == 0)
            {
                continue;
            }
            var at = OnACharacter(text, given.At);
            var chord = new Chord(at, name);
            var same = result.FindIndex(other => other.At == at);
            if (same >= 0)
            {
                result[same] = chord;
            }
            else
            {
                result.Add(chord);
            }
        }
        return result.OrderBy(chord => chord.At).ToList();
    }

    /// <summary>
    /// The place <see cref="Tidy"/> brings a chord's place to: onto a character the text
    /// has (below 0 to 0, past the end to the last character), and off a line break onto
    /// the character before it, if that is not a line break too. In a text with no
    /// characters it is 0, where Tidy keeps no chord at all.
    ///
    /// It is Tidy's own sum, taken out so that it can be asked for by itself. NEW
    /// (spec/08-repair.md, check 0): a chord the file has past the end of its words, or
    /// on a line break, is shown at this place, so that it can be seen and dealt with;
    /// and that is where it is written the next time its box's chords are.
    /// </summary>
    public static int OnACharacter(string text, int at)
    {
        if (text.Length == 0)
        {
            return 0;
        }
        at = Math.Clamp(at, 0, text.Length - 1);
        if (text[at] == Text.LineBreak && at > 0 && text[at - 1] != Text.LineBreak)
        {
            at--;
        }
        return at;
    }

    /// <summary>
    /// The chords of a text box whose words have been changed, where they stand in the
    /// new words. What the old and new words have in common at their start and at their
    /// end is taken to be the same words: a chord there stays on its character. A chord
    /// in the part that changed goes to the start of what replaced it. So correcting a
    /// word, or adding a line, leaves the chords of the rest where they were.
    ///
    /// Neither app changes words, so nothing uses this to begin with. It is here for the
    /// day one does, and for a mending of a song whose words were changed elsewhere.
    /// </summary>
    public static IReadOnlyList<Chord> Carried(string before, string after, IReadOnlyList<Chord> chords)
    {
        if (string.Equals(before, after, StringComparison.Ordinal) || chords.Count == 0)
        {
            return Tidy(after, chords);
        }
        var same = 0;
        var shorter = Math.Min(before.Length, after.Length);
        while (same < shorter && before[same] == after[same])
        {
            same++;
        }
        var tail = 0;
        while (tail < shorter - same && before[before.Length - 1 - tail] == after[after.Length - 1 - tail])
        {
            tail++;
        }
        // Two that end up in one place: the first stands, the way they read.
        var apart = new List<Chord>();
        foreach (var chord in chords)
        {
            var at = chord.At;
            if (at >= before.Length - tail)
            {
                at += after.Length - before.Length;
            }
            else if (at >= same)
            {
                at = same;
            }
            if (!apart.Exists(other => other.At == at))
            {
                apart.Add(chord with { At = at });
            }
        }
        return Tidy(after, apart);
    }

    /// <summary>
    /// The stretch of the text each chord is written over in the file (tidied first).
    /// On a line of chords alone, its own place and no more: one long. On any other
    /// line, from its place to the next chord's place or the end of its line, whichever
    /// comes first, and never empty.
    /// </summary>
    public static IReadOnlyList<ChordRange> Ranges(string text, IEnumerable<Chord> chords)
    {
        var ordered = Tidy(text, chords);
        var result = new List<ChordRange>();
        for (var i = 0; i < ordered.Count; i++)
        {
            var chord = ordered[i];
            // The chord's own line: from after the line break before it to the next one.
            var lineStart = chord.At > 0 ? text.LastIndexOf(Text.LineBreak, chord.At - 1) + 1 : 0;
            var end = text.IndexOf(Text.LineBreak, chord.At);
            if (end < 0)
            {
                end = text.Length;
            }
            if (IsPlaceholders(text[lineStart..end]))
            {
                // A line of chords alone: the chord's own stand-in and no more, so that
                // the wide space between two chords belongs to neither of them.
                end = chord.At + 1;
            }
            else if (i + 1 < ordered.Count)
            {
                end = Math.Min(end, ordered[i + 1].At);
            }
            result.Add(new ChordRange(chord.At, Math.Max(end, chord.At + 1), chord.Name));
        }
        return result;
    }
}
