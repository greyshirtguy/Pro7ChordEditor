// Chords as ChordPro text and back, and the stand-ins of a line that has chords and no
// words. spec/02-chord-rules.md, "Chords as ChordPro text". Held to
// testdata/vectors/chordpro.json.
//
// ChordPro writes a chord in square brackets before the character it is played on:
// "[G]Amazing [C]grace".
//
// TWO READERS, on purpose. FromChordPro reads a line of a ChordPro FILE (ChordProSong.cs):
// a bracket that holds what could not be a chord is dropped ("[*Coda]"), and a line of
// chords alone becomes stand-ins. ChordsOf reads what was typed in the ChordPro EDITOR:
// every bracket that is not empty is taken as a chord, and the places are counted along
// the words as they are, with no stand-ins made, because the editor compares what it
// reads with the words it started from (WithoutChords).
//
// CHORDS WITH NO WORDS. A song has lines that are only chords: an intro, a turnaround.
// There are no words to hang them on, so the file has invisible ones. As Multitracks
// writes it, the line's text is one stand-in character for each chord, with an em quad
// (U+2001) between them, and each chord's place is its stand-in: 0, 2, 4 along the
// line. The stand-in is a zero-width space (U+200B) in some songs and a plain space in
// others. spec/01-file-format.md, "Chords with no words".
//
// Port of toChordPro, fromChordPro, chordsOf, withoutChords, placeholders and
// isPlaceholders of reference/simple-presenter/src/chords.cpp. The Mac's is
// ChordPro.swift.

using System.Text;

namespace Pro7ChordEditor.ChordRules;

public static partial class Chords
{
    /// <summary>
    /// A line as ChordPro writes it, each chord in square brackets before the character
    /// it stands at. A line of chords alone is written as its chords with one space
    /// between, "[C] [G] [Am]", which is how such a line is written by hand; the
    /// stand-ins they hang on are the file's business.
    /// </summary>
    public static string ToChordPro(Line line)
    {
        var result = new StringBuilder();
        var ordered = line.Chords.OrderBy(chord => chord.At).ToList();
        var next = 0;
        var alone = IsPlaceholders(line.Text);
        for (var i = 0; i <= line.Text.Length; i++)
        {
            while (next < ordered.Count && ordered[next].At <= i)
            {
                if (alone && result.Length > 0)
                {
                    result.Append(' ');
                }
                result.Append('[').Append(ordered[next].Name).Append(']');
                next++;
            }
            if (i < line.Text.Length && !alone)
            {
                result.Append(line.Text[i]);
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// A whole text box's words, of several lines, with its chords: each line as
    /// ChordPro writes it, joined by line breaks. A chord belongs to the line its place
    /// is in; an empty line is taken as one place wide, so that a chord on it is not
    /// lost.
    /// </summary>
    public static string ToChordPro(string text, IReadOnlyList<Chord> chords)
    {
        var lines = new List<string>();
        var start = 0;
        foreach (var textLine in text.Split(Text.LineBreak))
        {
            var own = new List<Chord>();
            foreach (var chord in chords)
            {
                if (chord.At >= start && chord.At < start + Math.Max(1, textLine.Length))
                {
                    own.Add(chord with { At = chord.At - start });
                }
            }
            lines.Add(ToChordPro(new Line(textLine, own)));
            start += textLine.Length + 1;
        }
        return string.Join(Text.LineBreak, lines);
    }

    /// <summary>
    /// A line of a ChordPro FILE read: its words, and its chords with their places along
    /// the words. A bracket that holds what could not be a chord is dropped. A chord
    /// after the last word hangs on the last character, there being none after it. A
    /// line of chords alone becomes stand-ins, one zero-width space for each chord, with
    /// the chords on places 0, 2, 4.
    /// </summary>
    public static Line FromChordPro(string line)
    {
        var text = new StringBuilder();
        var chords = new List<Chord>();
        var i = 0;
        while (i < line.Length)
        {
            var c = line[i];
            var close = c == '[' ? line.IndexOf(']', i) : -1;
            if (close > i)
            {
                var inside = Tidied(line[(i + 1)..close]);
                if (IsChord(inside))
                {
                    chords.Add(new Chord(text.Length, inside));
                }
                i = close + 1;
                continue;
            }
            text.Append(c);
            i++;
        }
        var words = text.ToString();
        // Chords with no words: one stand-in each.
        if (Text.Trimmed(words).Length == 0 && chords.Count > 0)
        {
            return new Line(Placeholders(chords.Count), chords.Select((chord, n) => chord with { At = n * 2 }).ToList());
        }
        var last = Math.Max(0, words.Length - 1);
        return new Line(words, chords.Select(chord => chord with { At = Math.Min(chord.At, last) }).ToList());
    }

    /// <summary>
    /// The chords in typed ChordPro text, each at the place it has among the words AS
    /// TYPED, every bracket that is not empty taken as a chord, its name tidied. For the
    /// ChordPro editor. Read without making stand-ins: the places are wanted along the
    /// words as they are.
    /// </summary>
    public static IReadOnlyList<Chord> ChordsOf(string chordPro)
    {
        var result = new List<Chord>();
        var at = 0;
        var i = 0;
        while (i < chordPro.Length)
        {
            var close = chordPro[i] == '[' ? chordPro.IndexOf(']', i) : -1;
            if (close > i)
            {
                var inside = Tidied(chordPro[(i + 1)..close]);
                if (inside.Length > 0)
                {
                    result.Add(new Chord(at, inside));
                }
                i = close + 1;
                continue;
            }
            at++;
            i++;
        }
        return result;
    }

    /// <summary>
    /// ChordPro text with its chords taken out: the words alone. An editor that lets only
    /// the chords be changed compares this with the words it started from. A chord is a
    /// "[", then anything but a bracket or a line break, then a "]".
    ///
    /// (The reference takes them out with a regular expression. It is done by hand here,
    /// as on the Mac, so that what is taken out does not hang on whose regular
    /// expressions a platform has.)
    /// </summary>
    public static string WithoutChords(string chordPro)
    {
        var result = new StringBuilder();
        var i = 0;
        while (i < chordPro.Length)
        {
            if (chordPro[i] == '[')
            {
                // The first bracket or line break after it says whether this opens a chord.
                var end = i + 1;
                while (end < chordPro.Length && chordPro[end] != '[' && chordPro[end] != ']' && chordPro[end] != Text.LineBreak)
                {
                    end++;
                }
                if (end < chordPro.Length && chordPro[end] == ']')
                {
                    i = end + 1;
                    continue;
                }
            }
            result.Append(chordPro[i]);
            i++;
        }
        return result.ToString();
    }

    /// <summary>
    /// What stands for so many chords with no words under them: that many stand-ins with
    /// an em quad (U+2001) between them. An import writes zero-width spaces (U+200B);
    /// a line that is changed keeps whichever it had.
    /// </summary>
    public static string Placeholders(int count, char standIn = Text.ZeroWidthSpace)
    {
        var result = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                result.Append(Text.EmQuad);
            }
            result.Append(standIn);
        }
        return result.ToString();
    }

    /// <summary>
    /// Whether a line is a line of stand-ins: not empty, and nothing in it but U+200B and
    /// white space. (A line of plain spaces counts. Whether it is a line of chords or
    /// only a stray space in a text box is for whoever asks to tell, by whether it has
    /// chords.)
    /// </summary>
    public static bool IsPlaceholders(string line)
    {
        foreach (var c in line)
        {
            if (c != Text.ZeroWidthSpace && !Text.IsSpace(c))
            {
                return false;
            }
        }
        return line.Length > 0;
    }
}
