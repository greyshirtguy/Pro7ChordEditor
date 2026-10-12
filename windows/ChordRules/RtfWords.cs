// The words of a text box: the plain text of the RTF the file keeps them in. Held to
// testdata/vectors/rtf-text.json.
//
// A chord's place is a number counted along this plain text, so both apps must get the
// same plain text from the same RTF, unit for unit. spec/01-file-format.md ("The words,
// and counting along them") has the rules as a table. This is a port of the reader they
// were taken from, reference/simple-presenter/src/rtf.cpp, with everything about fonts,
// colours and sizes left out: that reader builds a model of the formatted text, and this
// one only wants the characters, so it keeps no more of an RTF's state than decides
// whether a character is words (which destination a group is, and how many characters
// to pass over after a numbered one). It was ported by way of the Mac's,
// mac/Core/Sources/ChordRules/RTFWords.swift, which is this file in Swift: two ports of
// one reader agree by construction, where a reader of the platform's own might not.
//
// Why not WPF's reader of RTF. The first Windows editor used it and lost the line
// breaks, which is where its trouble with places came from (spec/08-repair.md, B).
//
// Reading is nearly all of it. The apps never write RTF, with one exception: the
// characters that chords with no words hang on (spec/01-file-format.md, "Chords with no
// words"). That is at the foot of this file, and it is done by changing bytes in place:
// the reader says which bytes of the RTF each character of the plain text was written
// with, the bytes of one line's characters are taken out and others put there, and
// every other byte of the RTF is left exactly as it was. Nothing here reads an RTF into
// a model and writes it out again in its own words, as the reference does: that would
// rewrite the RTF of someone's library for the sake of one line. It is the riskiest
// thing the app does to a file, and it is held to
// testdata/vectors/stand-ins-in-rtf.json, whose reference is tools/standins.py.
//
// What is a guess. The dialect in every file to hand is the one Apple's text system
// writes. The other usual spellings (a paragraph break and a line break as words of
// their own, bytes written straight in) are taken in because ProPresenter on Windows may
// write them. What it does write is NOT KNOWN yet (spec/01, "Not known"): looking is
// this app's session's to do before milestone M3.
//
// No backslash followed by a "u" and digits is written anywhere in this file, in code
// or in comment, though that is what a numbered character looks like in RTF: typed into
// a file by an agent it can arrive as one character (mac/NOTES.md, windows/NOTES.md).

using System.Globalization;
using System.Text;

namespace Pro7ChordEditor.ChordRules;

public static class RtfWords
{
    private const byte Backslash = 0x5C;

    // U+2028, which the control word "line" is read as, and which is a line break in
    // the plain text.
    private const char LineSeparator = (char)0x2028;

    // Windows code page 1252 differs from Latin-1 only in 0x80 to 0x9F.
    private static readonly char[] Cp1252High =
    [
        (char)0x20AC, (char)0x0081, (char)0x201A, (char)0x0192, (char)0x201E, (char)0x2026, (char)0x2020, (char)0x2021,
        (char)0x02C6, (char)0x2030, (char)0x0160, (char)0x2039, (char)0x0152, (char)0x008D, (char)0x017D, (char)0x008F,
        (char)0x0090, (char)0x2018, (char)0x2019, (char)0x201C, (char)0x201D, (char)0x2022, (char)0x2013, (char)0x2014,
        (char)0x02DC, (char)0x2122, (char)0x0161, (char)0x203A, (char)0x0153, (char)0x009D, (char)0x017E, (char)0x0178,
    ];

    // UTF-8 read strictly: bytes that are not UTF-8 are an error, and are never mended.
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>The plain text of an RTF: its lines joined by single line breaks.</summary>
    public static string Read(ReadOnlySpan<byte> rtf)
    {
        var reader = new Reader(rtf.ToArray());
        reader.Read();
        return reader.Words;
    }

    private static char FromCp1252(byte value) => value >= 0x80 && value <= 0x9F ? Cp1252High[value - 0x80] : (char)value;

    private static bool IsLetter(byte value) => (value >= 0x41 && value <= 0x5A) || (value >= 0x61 && value <= 0x7A);

    private static int HexDigit(byte value) =>
        value >= (byte)'0' && value <= (byte)'9' ? value - (byte)'0'
        : value >= (byte)'a' && value <= (byte)'f' ? value - (byte)'a' + 10
        : value >= (byte)'A' && value <= (byte)'F' ? value - (byte)'A' + 10
        : -1;

    /// <summary>Where one unit of the plain text is written in the RTF.</summary>
    /// <param name="From">Its first byte. A control word's is its backslash.</param>
    /// <param name="To">One past its last byte. A control word's last is its last letter or digit, or the space that ends it if it has one.</param>
    /// <param name="OpenBefore">A control word with no space after it ends exactly where this begins: a plain space put in here would be taken for that word's end, and would not be text.</param>
    /// <param name="OpenAfter">This is itself such a control word: the same goes for a space put in after it.</param>
    /// <param name="Depth">How many groups are open where it is written.</param>
    private readonly record struct Written(int From, int To, bool OpenBefore, bool OpenAfter, int Depth);

    private sealed class Reader(byte[] rtf)
    {
        // What the text of a group is: words, or one of the tables and lists that are
        // not. (The reference tells the font table and the two colour tables apart, to
        // read them. Nothing in any of them is words, so here they are all "not words".)
        private struct State
        {
            internal bool NotWords;

            // How many characters after a numbered one stand in for it, for readers that
            // do not know numbered characters, and are to be passed over (the control
            // word "uc"). One, unless told otherwise.
            internal int UnicodeSkip;

            // A backslash and a star have been met in this group: the next control word
            // names a destination that a reader may skip if it does not know it.
            internal bool Starred;
        }

        private readonly StringBuilder text = new();
        private readonly List<State> stack = [];
        private State state = new() { UnicodeSkip = 1 };
        private int pos;
        private int pendingSkip;

        // Where the last control word that had no space after it ended.
        private int openEnd = -1;

        /// <summary>The plain text, once read.</summary>
        internal string Words { get; private set; } = "";

        /// <summary>Where each unit of the text is written: as many of these as of those.</summary>
        internal List<Written> Where { get; } = [];

        internal void Read()
        {
            while (pos < rtf.Length)
            {
                var c = rtf[pos++];
                switch (c)
                {
                    case (byte)'{':
                        stack.Add(state);
                        state.Starred = false;
                        pendingSkip = 0;
                        break;
                    case (byte)'}':
                        if (stack.Count > 0)
                        {
                            state = stack[^1];
                            stack.RemoveAt(stack.Count - 1);
                        }
                        pendingSkip = 0;
                        break;
                    case Backslash:
                        Control();
                        break;
                    case 0x0D or 0x0A:
                        // A bare line end in the file is nothing.
                        break;
                    default:
                        if (c < 0x80)
                        {
                            Character((char)c, pos - 1);
                        }
                        else
                        {
                            pos--;
                            RawHighBytes();
                        }
                        break;
                }
            }
            // (The reference ends by making a last paragraph of what follows the last
            // break, even when that is nothing, so that text ending with a break ends
            // with an empty line. With each break written as it is met, that is already
            // so.)
            Words = text.Replace(LineSeparator, Text.LineBreak).ToString();
        }

        // Strict RTF escapes everything above ASCII, but files written by other tools
        // have UTF-8 written straight in. A run of high bytes is UTF-8 if it is valid as
        // that, else it is read by the code page.
        private void RawHighBytes()
        {
            var start = pos;
            while (pos < rtf.Length && rtf[pos] >= 0x80)
            {
                pos++;
            }
            string? decoded;
            try
            {
                decoded = StrictUtf8.GetString(rtf, start, pos - start);
            }
            catch (DecoderFallbackException)
            {
                decoded = null;
            }
            if (decoded is null)
            {
                for (var n = start; n < pos; n++)
                {
                    Character(FromCp1252(rtf[n]), n, n + 1);
                }
                return;
            }
            // Each character's units are written with that character's bytes: one from
            // beyond the first 65,536 is two units and one stretch of bytes.
            var at = start;
            Span<char> units = stackalloc char[2];
            foreach (var rune in decoded.EnumerateRunes())
            {
                var next = at + rune.Utf8SequenceLength;
                var count = rune.EncodeToUtf16(units);
                for (var n = 0; n < count; n++)
                {
                    Character(units[n], at, next);
                }
                at = next;
            }
        }

        private void Control()
        {
            if (pos >= rtf.Length)
            {
                return;
            }
            var c = rtf[pos];
            // Where its backslash is, and whether a control word with no space after it
            // ends just there (asked now: this may be such a word itself, and would then
            // be the last one).
            var from = pos - 1;
            var open = from == openEnd;

            if (!IsLetter(c))
            {
                pos++;
                switch (c)
                {
                    case (byte)'\'':
                        // Two hexadecimal digits: a character of the code page. What is
                        // not two such digits is read as nought.
                        byte value = 0;
                        if (pos + 2 <= rtf.Length && HexDigit(rtf[pos]) >= 0 && HexDigit(rtf[pos + 1]) >= 0)
                        {
                            value = (byte)(HexDigit(rtf[pos]) * 16 + HexDigit(rtf[pos + 1]));
                        }
                        pos += 2;
                        Character(FromCp1252(value), from, openBefore: open);
                        break;
                    case (byte)'*':
                        state.Starred = true;
                        break;
                    case 0x0A or 0x0D:
                        // Apple's text system writes a paragraph break as a backslash at
                        // the end of the line. This is how ProPresenter writes the end of
                        // every line.
                        if (!state.NotWords)
                        {
                            EndParagraph(from, open);
                        }
                        break;
                    case (byte)'~':
                        Character((char)0x00A0, from, openBefore: open);
                        break;
                    case (byte)'_':
                        Character((char)0x2011, from, openBefore: open);
                        break;
                    case (byte)'-':
                        break;
                    default:
                        // A backslash before a backslash or a brace: the character itself.
                        Character((char)c, from, openBefore: open);
                        break;
                }
                return;
            }

            var wordStart = pos;
            while (pos < rtf.Length && IsLetter(rtf[pos]))
            {
                pos++;
            }
            var word = Encoding.ASCII.GetString(rtf, wordStart, pos - wordStart);

            var numberStart = pos;
            if (pos < rtf.Length && rtf[pos] == (byte)'-')
            {
                pos++;
            }
            while (pos < rtf.Length && rtf[pos] >= (byte)'0' && rtf[pos] <= (byte)'9')
            {
                pos++;
            }
            // (A number too large to hold is nought, as it is to the reference; so is a
            // minus sign with no digits after it.)
            var number = int.TryParse(
                Encoding.ASCII.GetString(rtf, numberStart, pos - numberStart),
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out var parsed) ? parsed : 0;
            // A space ends a control word and is not text.
            if (pos < rtf.Length && rtf[pos] == (byte)' ')
            {
                pos++;
            }
            else
            {
                openEnd = pos;
            }

            ControlWord(word, number, from, open);
        }

        private void ControlWord(string word, int number, int from, bool openBefore)
        {
            // The first control word after a brace, a backslash and a star names an
            // optional destination. None of those is words.
            if (state.Starred)
            {
                state.Starred = false;
                state.NotWords = true;
                return;
            }
            if (state.NotWords)
            {
                return;
            }
            switch (word)
            {
                case "fonttbl" or "colortbl" or "info" or "stylesheet" or "pict" or "header" or "footer" or "listtable" or "listoverridetable":
                    state.NotWords = true;
                    break;
                case "par":
                    EndParagraph(from, openBefore);
                    break;
                case "line":
                    Character(LineSeparator, from, openBefore: openBefore);
                    break;
                case "tab":
                    Character('\t', from, openBefore: openBefore);
                    break;
                case "u":
                    // The UTF-16 unit of that number; RTF writes those over 32767 as
                    // negative. Two of these for one character (an emoji) are two units,
                    // and two places.
                    var skip = state.UnicodeSkip;
                    Character(unchecked((char)(ushort)(number < 0 ? number + 65536 : number)), from, openBefore: openBefore);
                    pendingSkip = skip;
                    break;
                case "uc":
                    state.UnicodeSkip = number;
                    break;
                default:
                    // Any other control word is format.
                    break;
            }
        }

        // A character that was written with the bytes from `from` to where the reading
        // has got to, or to `to`. `openBefore` is given by what is itself a control word.
        private void Character(char unit, int from, int? to = null, bool? openBefore = null)
        {
            if (pendingSkip > 0)
            {
                pendingSkip--;
                return;
            }
            if (!state.NotWords)
            {
                text.Append(unit);
                Note(from, to ?? pos, openBefore ?? from == openEnd);
            }
        }

        private void EndParagraph(int from, bool openBefore)
        {
            text.Append(Text.LineBreak);
            Note(from, pos, openBefore);
        }

        // (`to` can be past the end of the RTF, for a backslash and an apostrophe with
        // the file ending before its two digits. It is brought back to the end, so that
        // nothing below ever asks for bytes that are not there.)
        private void Note(int from, int to, bool openBefore) =>
            Where.Add(new Written(from, Math.Min(to, rtf.Length), openBefore, to == openEnd, stack.Count));
    }

    // ---- Writing: the characters that chords with no words hang on, and nothing else
    //
    // spec/01-file-format.md, "Chords with no words" and "Chords onto a blank slide".
    // NEW: the reference does neither of these this way (see the head of this file).
    //
    // Both answer with nothing where the thing cannot be done cleanly, and both read
    // back what they made before they answer with it: an RTF that comes from here has
    // exactly the plain text that was meant. Whoever asks refuses the change on nothing.

    /// <summary>
    /// The bytes these characters are written with: a plain space as itself, and the
    /// two that stand in for words (U+200B, and the U+2001 between two of them) as
    /// Apple's text system writes any character above ASCII: a backslash, "uc0", a
    /// backslash, "u", the character's number, and a space to end it. Nothing else is
    /// ever written here, and anything else is nothing.
    /// </summary>
    internal static byte[]? BytesOf(string standIns)
    {
        var bytes = new List<byte>();
        foreach (var unit in standIns)
        {
            if (unit == ' ')
            {
                bytes.Add(0x20);
            }
            else if (unit == Text.ZeroWidthSpace || unit == Text.EmQuad)
            {
                bytes.Add(Backslash);
                bytes.AddRange("uc0"u8);
                bytes.Add(Backslash);
                bytes.Add((byte)'u');
                bytes.AddRange(Encoding.ASCII.GetBytes(((int)unit).ToString(CultureInfo.InvariantCulture)));
                bytes.Add(0x20);
            }
            else
            {
                return null;
            }
        }
        return [.. bytes];
    }

    /// <summary>
    /// The RTF with one line of its plain text made these stand-ins, and every byte that
    /// is not that line's characters left as it was.
    ///
    ///   - The line's characters are found by the bytes each was written with.
    ///   - What the line has already of what it is to become, from its start, is kept
    ///     as it is written. The characters after that are taken out: their own bytes,
    ///     and none of the control words that lie between them, which say how what
    ///     follows the line is set (in a song from Multitracks every other stand-in is
    ///     in another font). What is still to come is put where the first of those
    ///     was, or after the line's last character if none was taken out.
    ///   - A plain space put in just after a control word that has no space of its own
    ///     gets one before it, or it would be that word's end and not text.
    ///
    /// Nothing, if the text has no such line; if the line is empty, there being no
    /// character to say where it is; if two of the line's characters were written with
    /// the same bytes; or if what was made does not read back as the same text with
    /// that line changed and nothing else.
    /// </summary>
    public static byte[]? WithLine(ReadOnlySpan<byte> rtfBytes, int line, string standIns)
    {
        var rtf = rtfBytes.ToArray();
        var reader = new Reader(rtf);
        reader.Read();
        var text = reader.Words;
        if (LineOf(text, line) is not (int start, int end))
        {
            return null;
        }

        var same = 0;
        while (start + same < end && same < standIns.Length && text[start + same] == standIns[same])
        {
            same++;
        }
        var coming = standIns[same..];
        var put = BytesOf(coming);
        if (put is null)
        {
            return null;
        }
        var going = reader.Where.GetRange(start + same, end - start - same);
        for (var n = 0; n + 1 < going.Count; n++)
        {
            if (going[n + 1].From < going[n].To)
            {
                return null;
            }
        }

        // Where what is coming goes, and whether a space there would be lost.
        var at = going.Count > 0 ? going[0].From : reader.Where[end - 1].To;
        var open = going.Count > 0 ? going[0].OpenBefore : reader.Where[end - 1].OpenAfter;
        if (open && put.Length > 0 && put[0] == 0x20)
        {
            put = [0x20, .. put];
        }

        var made = new List<byte>(rtf.Length + put.Length);
        made.AddRange(rtf.AsSpan(0, at));
        made.AddRange(put);
        var from = at;
        foreach (var one in going)
        {
            made.AddRange(rtf.AsSpan(from, one.From - from));
            from = one.To;
        }
        made.AddRange(rtf.AsSpan(from));

        byte[] result = [.. made];
        var meant = string.Concat(text.AsSpan(0, start + same), coming, text.AsSpan(end));
        return string.Equals(Read(result), meant, StringComparison.Ordinal) ? result : null;
    }

    /// <summary>
    /// Where a line's characters are written: from the first byte of its first to one
    /// past the last byte of its last. Nothing outside it is changed by WithLine, which
    /// is what the tests hold it to. Nothing for a line that is not there, or is empty.
    /// </summary>
    internal static (int From, int To)? SpanOfLine(ReadOnlySpan<byte> rtf, int line)
    {
        var reader = new Reader(rtf.ToArray());
        reader.Read();
        return LineOf(reader.Words, line) is (int start, int end) ? (reader.Where[start].From, reader.Where[end - 1].To) : null;
    }

    /// <summary>
    /// A new RTF for a text box that has no words: the opening of another's, up to
    /// where its words begin (its fonts, its colours and how its first words are set),
    /// then these stand-ins, then as many closing braces as that opening left open.
    ///
    /// Nothing, if the other has no character at all to say where its words begin, or
    /// if what was made does not read back as the stand-ins and nothing else. (So it
    /// is for stand-ins on plain spaces after an opening that ends in a control word
    /// with no space of its own: the first would be taken for that word's end. The
    /// apps put zero-width spaces here and nothing else, which begin with a backslash,
    /// so the care WithLine takes of that is not taken twice.)
    /// </summary>
    public static byte[]? WithTheOpeningOf(ReadOnlySpan<byte> modelBytes, string standIns)
    {
        var model = modelBytes.ToArray();
        var reader = new Reader(model);
        reader.Read();
        var put = BytesOf(standIns);
        if (reader.Where.Count == 0 || put is null)
        {
            return null;
        }
        var first = reader.Where[0];
        byte[] made = [.. model.AsSpan(0, first.From), .. put, .. Enumerable.Repeat((byte)'}', first.Depth)];
        return string.Equals(Read(made), standIns, StringComparison.Ordinal) ? made : null;
    }

    /// <summary>
    /// A new RTF for a text box that is given other words altogether: the opening of
    /// the RTF it has (or of another's), up to where its words begin, which is its
    /// fonts, its colours and how its words are set; then these words; then as many
    /// closing braces as that opening left open. spec/09-chordpro-files.md, rule 12:
    /// for a song made from a ChordPro file, whose slides are copies of a slide that
    /// is known to be good, with the song's own words in them.
    ///
    /// How the words are written:
    ///   - a line break as a backslash and a line feed, which is how ProPresenter
    ///     writes the end of every line;
    ///   - a backslash and the two curly brackets with a backslash before them;
    ///   - every other character of ASCII that can be seen, and the space, as itself;
    ///   - anything else (a letter with an accent, a curly apostrophe, a tab, half of
    ///     an emoji) as Apple's text system writes any character above ASCII: a
    ///     backslash, "uc0", a backslash, "u", the number of its UTF-16 unit, never
    ///     negative, and a space to end it.
    ///
    /// If the opening ends in a control word that has no space of its own, and the
    /// words begin with a plain character, a space is put between: the first letters
    /// would otherwise be read as more of that word.
    ///
    /// Nothing, if the RTF has no character at all to say where its words begin; or if
    /// what was made does not read back as exactly these words. Whoever asks refuses
    /// the change on nothing.
    /// </summary>
    public static byte[]? WithWords(ReadOnlySpan<byte> modelBytes, string words)
    {
        var model = modelBytes.ToArray();
        var reader = new Reader(model);
        reader.Read();
        if (reader.Where.Count == 0)
        {
            return null;
        }
        var first = reader.Where[0];
        var put = WordsWritten(words);
        var made = new List<byte>(first.From + put.Count + first.Depth + 1);
        made.AddRange(model.AsSpan(0, first.From));
        if (first.From > 0 && put.Count > 0 && put[0] != Backslash && IsLetterOrDigit(model[first.From - 1]))
        {
            made.Add(0x20);
        }
        made.AddRange(put);
        made.AddRange(Enumerable.Repeat((byte)'}', first.Depth));
        byte[] whole = [.. made];
        return string.Equals(Read(whole), words, StringComparison.Ordinal) ? whole : null;
    }

    // The bytes a text box's words are written with (WithWords).
    private static List<byte> WordsWritten(string words)
    {
        var bytes = new List<byte>(words.Length + 16);
        foreach (var unit in words)
        {
            if (unit == Text.LineBreak)
            {
                bytes.Add(Backslash);
                bytes.Add(0x0A);
            }
            else if (unit is (char)0x5C or '{' or '}')
            {
                bytes.Add(Backslash);
                bytes.Add((byte)unit);
            }
            else if (unit >= 0x20 && unit < 0x7F)
            {
                bytes.Add((byte)unit);
            }
            else
            {
                bytes.Add(Backslash);
                bytes.AddRange("uc0"u8);
                bytes.Add(Backslash);
                bytes.Add((byte)'u');
                bytes.AddRange(Encoding.ASCII.GetBytes(((int)unit).ToString(CultureInfo.InvariantCulture)));
                bytes.Add(0x20);
            }
        }
        return bytes;
    }

    private static bool IsLetterOrDigit(byte one) => one is (>= (byte)'a' and <= (byte)'z') or (>= (byte)'A' and <= (byte)'Z') or (>= (byte)'0' and <= (byte)'9');

    // Where a line of the plain text is: from its first unit, to one past its last.
    // Nothing for a line that is not there, or is empty.
    private static (int Start, int End)? LineOf(string text, int line)
    {
        if (line < 0)
        {
            return null;
        }
        var start = 0;
        for (var n = 0; n < line; n++)
        {
            var next = text.IndexOf(Text.LineBreak, start);
            if (next < 0)
            {
                return null;
            }
            start = next + 1;
        }
        var end = text.IndexOf(Text.LineBreak, start);
        if (end < 0)
        {
            end = text.Length;
        }
        return end > start ? (start, end) : null;
    }
}
