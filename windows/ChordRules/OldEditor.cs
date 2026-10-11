// Where the first Windows editor meant a chord to be, and whether a song's chords look
// as if they were put on in it. Held to testdata/vectors/line-break-shift.json.
//
// This is check 2 of spec/08-repair.md, and what it is about is told there in full. In
// short: that editor read a text box's words with their line breaks lost, so a chord put
// on by hand in it was saved at a place counted along the words WITHOUT their line
// breaks. ProPresenter counts each line break as one. So such a chord sits to the left
// of where it was meant, by one character for each line break above it.
//
// NEW, and a GUESS about that editor until the maintainer has seen the fault in a real
// file: the plan has this check built and left switched off until then
// (spec/11-plan.md, M8). Only the two plain functions are here, because their vectors
// are among those milestone M1 is held to. Finding such songs and mending them is M8.
//
// The reference is `unshifted` and `line_break_shift` in tools/make-fixtures.py, ported
// by way of the Mac's OldEditorShift.swift. That is Python, which counts a string in
// whole characters where the file, and that editor, count UTF-16 units. For words from
// beyond the first 65,536 characters the two would differ, and here it is units, as
// everywhere (AGENTS.md). The vectors have no such words in them. White space, for
// where a word starts, is Python's: Qt's list and U+001C to U+001F.
//
// (The editor in question is windows/old. Its fault is in how it read a text box, with
// WPF's reader of RTF; this app reads one with RtfWords.cs.)

namespace Pro7ChordEditor.ChordRules;

public static class OldEditor
{
    /// <summary>What <see cref="LineBreakShift"/> finds in a song.</summary>
    /// <param name="ChordsBelowTheFirstLine">How many chords stand below the first line of their text box (once moved to where Unshifted puts them): the only ones that can show the fault.</param>
    /// <param name="AtTheStartOfAWord">How many of those are at the start of a word as they are.</param>
    /// <param name="IfMovedBack">How many would be if they were moved.</param>
    /// <param name="Likely">Whether moving them puts at least four more of them, and at least a quarter more of all of them, on the starts of words. It is never certain, so it is only ever offered, with the song shown both ways.</param>
    public readonly record struct Shift(int ChordsBelowTheFirstLine, int AtTheStartOfAWord, int IfMovedBack, bool Likely);

    /// <summary>
    /// The place a chord was meant for, if it was saved by the old editor at
    /// <paramref name="savedAt"/>: that place counted along the words with their line
    /// breaks left out, turned back into a place in the words as they are.
    /// </summary>
    public static int Unshifted(string words, int savedAt)
    {
        var left = savedAt;
        var at = 0;
        var lines = words.Split(Text.LineBreak);
        for (var n = 0; n < lines.Length; n++)
        {
            var line = lines[n];
            if (left < line.Length || n == lines.Length - 1)
            {
                return at + Math.Min(left, Math.Max(0, line.Length - 1));
            }
            left -= line.Length;
            at += line.Length + 1;
        }
        return at;
    }

    /// <summary>
    /// Whether a song's chords look as if they were put on by hand in the old editor:
    /// sitting a little to the left of the starts of words on every line but the first.
    /// Each text box is its words and the places its chords start at.
    /// </summary>
    public static Shift LineBreakShift(IEnumerable<(string Words, IReadOnlyList<int> Starts)> boxes)
    {
        int below = 0, asIs = 0, moved = 0;
        foreach (var (words, starts) in boxes)
        {
            var firstBreak = words.IndexOf(Text.LineBreak, StringComparison.Ordinal);
            if (firstBreak < 0)
            {
                continue;
            }
            foreach (var start in starts)
            {
                var there = Unshifted(words, start);
                if (there <= firstBreak)
                {
                    continue;
                }
                below++;
                if (StartsAWord(words, start))
                {
                    asIs++;
                }
                if (StartsAWord(words, there))
                {
                    moved++;
                }
            }
        }
        var likely = below > 0 && moved - asIs >= 4 && (moved - asIs) * 4 >= below;
        return new Shift(below, asIs, moved, likely);
    }

    /// <summary>
    /// Whether a place is the first character of a word: not white space, with white
    /// space or nothing before it.
    /// </summary>
    internal static bool StartsAWord(string words, int at) =>
        at >= 0 && at < words.Length && !Text.IsPythonSpace(words[at]) && (at == 0 || Text.IsPythonSpace(words[at - 1]));
}
