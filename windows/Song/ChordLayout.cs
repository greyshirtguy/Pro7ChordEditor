// Where the chords of one line stand, worked out from the widths of its words
// (spec/03-chord-sheet.md, rule 11; reference: qml/chordlayout.js). Held to
// testdata/vectors/layout.json.
//
// A chord stands over the character it belongs to: its left edge where that character
// starts. Two chords close together would run into each other (over a short word, or on
// a line of chords alone, where the characters they hang on are no wider than a space),
// so each is pushed right as far as it needs to clear the one before it by `gap`. That
// is all there is to it, and it is done here, in one place, so that everything that
// draws chords, and everything that asks what is under the pointer, goes by the same
// numbers.
//
// Nothing here measures anything. It is handed two ways of measuring: `advance`, how
// wide the first so-many UTF-16 units of the line's words are, and `bubble`, how wide a
// chord's bubble is with its margins. The window hands it WPF's; the tests hand it
// letters of a fixed width, as the vectors were made with.
//
// The Mac's is ChordLayout.swift.

using Pro7ChordEditor.ChordRules;

namespace Pro7ChordEditor.Song;

public static class ChordLayout
{
    /// <summary>Where the chords of a line stand.</summary>
    /// <param name="Xs">Where each chord's left edge is, from the line's.</param>
    /// <param name="Widths">How wide each chord is.</param>
    /// <param name="Width">How wide the line is with its chords.</param>
    /// <param name="WordsWidth">How wide the words are alone.</param>
    public sealed record Placed(IReadOnlyList<double> Xs, IReadOnlyList<double> Widths, double Width, double WordsWidth);

    /// <summary>
    /// Where the chords of a line stand. <paramref name="length"/> is how many units the
    /// line's words have; each chord's place is counted along the line.
    /// </summary>
    public static Placed Place(int length, IReadOnlyList<Chord> chords, Func<int, double> advance, Func<string, double> bubble, double gap)
    {
        var xs = new List<double>(chords.Count);
        var widths = new List<double>(chords.Count);
        var right = -1e9;
        foreach (var chord in chords)
        {
            var width = bubble(chord.Name);
            var x = Math.Max(advance(Math.Min(Math.Max(chord.At, 0), length)), right + gap);
            xs.Add(x);
            widths.Add(width);
            right = x + width;
        }
        var wordsWidth = advance(length);
        return new Placed(xs, widths, Math.Max(wordsWidth, right), wordsWidth);
    }

    /// <summary>
    /// Which character of the line is at <paramref name="x"/>: the one whose left edge
    /// is nearest on the left. Found by halving, a dozen measurements for the longest
    /// line.
    /// </summary>
    public static int CharacterAt(int length, double x, Func<int, double> advance)
    {
        var low = 0;
        var high = length;
        while (low < high)
        {
            var middle = (low + high + 1) >> 1;
            if (advance(middle) <= x)
            {
                low = middle;
            }
            else
            {
                high = middle - 1;
            }
        }
        return Math.Min(low, Math.Max(0, length - 1));
    }
}
