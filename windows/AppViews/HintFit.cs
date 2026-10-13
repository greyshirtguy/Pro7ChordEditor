// How the hints are fitted into the width there is: how many of them, on one line or
// one under another, and how large. spec/03-chord-sheet.md, rule 53a.
//
// Arithmetic, on widths that are measured once for each run of words and remembered;
// no window in it. Where there is not room for all the hints the last are left off; but
// not one that is needed (the two sentences on copying and pasting are one thing, and
// the second is the point of it). Those go one under the other where they do not fit
// side by side; where one of them is still too long they are drawn smaller, a step at a
// time; and at their smallest they are cut off at the right.
//
// WHY IT IS ARITHMETIC, and the window is not asked how big the line would like to be:
// what the line says changes as often as the pointer crosses a chord, and whatever is
// asked of the window's own laying out then, is asked that often (spec/06, rule 6e: on
// the Mac that was most of what a move of the pointer cost). So how tall the line is,
// is worked out here from what it says and how wide it is, and a change of its words
// lays out nothing unless the line itself is to be taller.
//
// The Mac's is `HintFit`, in SongView.swift, and the sums are the same. The sizes are
// Windows' own: its keys are named in words, so a key is as wide as its word.

namespace Pro7ChordEditor.AppViews;

public static class HintFit
{
    /// <summary>How the hints are fitted.</summary>
    /// <param name="Count">How many of the hints are said, from the first.</param>
    /// <param name="Stacked">Whether they are one under another, and not side by side.</param>
    /// <param name="Size">How many times their usual size they are drawn.</param>
    public readonly record struct Fit(int Count, bool Stacked, double Size);

    /// <summary>
    /// How wide some words are at a size, plain or (for the sign on a key) a little
    /// bolder: asked of whatever draws them.
    /// </summary>
    public delegate double Measure(string words, double size, bool onAKey);

    /// <summary>The sizes the needed hints are tried at, one under the other: as they come, and two steps smaller.</summary>
    public static readonly IReadOnlyList<double> Sizes = [1.0, 0.9, 0.8];

    /// <summary>The size of the words, and of the sign on a key.</summary>
    public const double WordSize = 12.5;

    public const double KeySize = 12;

    /// <summary>The room between two hints, between the parts of one, and between two lines of them.</summary>
    public const double Between = 18;

    public const double Within = 5;

    public const double Lines = 4;

    /// <summary>How tall the line is, and the room it keeps at its sides and over and under what it says.</summary>
    public const double Height = 30;

    public const double Sides = 12;

    public const double OverAndUnder = 5;

    /// <summary>How tall a key is drawn, the least width of one, and the room its sign has either side.</summary>
    public const double KeyHeight = 20;

    public const double KeyLeast = 22;

    public const double KeyPad = 6;

    /// <summary>How wide a key is drawn: its sign and the room either side of it, and never less than a key is.</summary>
    public static double WidthOfKey(string key, double size, Measure measure) =>
        Math.Max(KeyLeast * size, Math.Ceiling(measure(key, KeySize * size, true)) + (2 * KeyPad * size));

    /// <summary>How wide one hint is: its words before its keys, its keys, and what it says.</summary>
    public static double WidthOf(SheetHints.Hint hint, double size, Measure measure)
    {
        var parts = 0;
        var width = 0.0;
        if (hint.Lead.Length > 0)
        {
            width += Math.Ceiling(measure(hint.Lead, WordSize * size, false));
            parts++;
        }
        foreach (var key in hint.Keys)
        {
            width += WidthOfKey(key, size, measure);
            parts++;
        }
        if (hint.Says.Length > 0)
        {
            width += Math.Ceiling(measure(hint.Says, WordSize * size, false));
            parts++;
        }
        return width + (Within * size * Math.Max(0, parts - 1));
    }

    /// <summary>How wide the first so many of these hints are side by side.</summary>
    public static double WidthOf(IReadOnlyList<SheetHints.Hint> hints, int count, double size, Measure measure)
    {
        var width = 0.0;
        for (var n = 0; n < count; n++)
        {
            width += WidthOf(hints[n], size, measure);
        }
        return width + (Between * size * Math.Max(0, count - 1));
    }

    /// <summary>
    /// How the hints are fitted into a width: the width they have to themselves, the
    /// room at the line's sides taken off.
    /// </summary>
    public static Fit Of(IReadOnlyList<SheetHints.Hint> hints, double room, Measure measure)
    {
        if (hints.Count == 0)
        {
            return new Fit(0, false, 1);
        }
        // (How many of them, from the first, may not be left off.)
        var needed = hints.TakeWhile(hint => hint.Needed).Count();
        for (var count = hints.Count; count >= Math.Max(1, needed); count--)
        {
            if (WidthOf(hints, count, 1, measure) <= room)
            {
                return new Fit(count, false, 1);
            }
        }
        if (needed <= 1)
        {
            // (One hint, and no room for it: it is cut off.)
            return new Fit(1, false, 1);
        }
        foreach (var size in Sizes)
        {
            if (hints.Take(needed).All(hint => WidthOf(hint, size, measure) <= room))
            {
                return new Fit(needed, true, size);
            }
        }
        return new Fit(needed, true, Sizes[^1]);
    }

    /// <summary>
    /// How tall the line is for a fit: its usual height, or what the hints need one
    /// under another, if that is more.
    /// </summary>
    public static double HeightOf(Fit fit)
    {
        if (!fit.Stacked)
        {
            return Height;
        }
        var line = KeyHeight * fit.Size;
        return Math.Max(Height, Math.Ceiling((fit.Count * line) + ((fit.Count - 1) * Lines * fit.Size) + (2 * OverAndUnder)));
    }
}
