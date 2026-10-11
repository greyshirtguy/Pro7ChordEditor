// A song read from a ChordPro file: what it is called, and its parts with their lines
// and chords. spec/09-chordpro-files.md, "Reading one", has the rules one by one. Held to
// testdata/vectors/parse-song.json, which has what this gives for each file of
// testdata/songs. What is done with it (making a presentation) is milestone M9.
//
// What is read. The directives that say what the song is are kept ({title}, {artist},
// {key}, {copyright}, {ccli}, and their short forms). Those that mark its parts make
// sections ({start_of_verse}, {start_of_chorus}, {start_of_bridge} and their ends and
// short forms, with a label if one is given; {comment: Verse 1}, and a line that is only
// "Verse 1:" or "[Verse 1]" too, since most files found in the wild mark their parts
// that way). An empty line ends a section that has no name, so that a file with no
// directives at all still comes in as its verses. Tablature and anything else in a
// {start_of_...} this does not know is left out; so is a line starting "#".
//
// The reference finds a directive and a heading with two regular expressions. They are
// done by hand here, as on the Mac, so that what they match does not hang on which
// regular expressions a platform has (Qt's, Foundation's and .NET's differ over what a
// space and a letter are); `Directive` and `HeadingOf` say what each matches. One thing
// is narrower than the reference, on purpose (spec/02-chord-rules.md, "To the letter"):
// a heading's words are matched in the plain letters A to Z, of either case. Qt's
// expression would also take a long s for an "s" and the Kelvin sign for a "k".
//
// Port of parseSong and slidesOf of reference/simple-presenter/src/chords.cpp, by way of
// the Mac's ChordProSong.swift, which is where the two expressions were first written
// out by hand.

using System.Globalization;
using System.Text;

namespace Pro7ChordEditor.ChordRules;

public static partial class Chords
{
    // The words a line that only names a part can start with, in the order they are tried.
    private static readonly string[] HeadingWords =
    [
        "verse", "chorus", "pre-chorus", "pre chorus", "prechorus", "bridge", "intro", "outro", "ending", "tag", "interlude",
        "instrumental", "refrain", "turnaround", "vamp", "coda", "hook", "post-chorus", "post chorus", "postchorus",
    ];

    /// <summary>Reads ChordPro text.</summary>
    public static ParsedSong ParseSong(string text)
    {
        string title = "", artist = "", key = "", copyright = "", ccli = "";
        var sections = new List<Section>();
        var name = "";
        var lines = new List<Line>();
        // Inside a part the file marked with a directive, where an empty line is only a gap.
        var marked = false;
        var skipping = false;
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        void Close()
        {
            if (lines.Count > 0)
            {
                sections.Add(new Section(name, lines));
            }
            name = "";
            lines = [];
            marked = false;
        }

        void Open(string kind, string label)
        {
            Close();
            marked = true;
            if (label.Length > 0)
            {
                name = label;
                return;
            }
            // Verses are numbered as they come; a chorus or a bridge is not, the first time.
            var n = counts[kind] = counts.GetValueOrDefault(kind) + 1;
            name = string.Equals(kind, "Verse", StringComparison.Ordinal) || n > 1
                ? kind + " " + n.ToString(CultureInfo.InvariantCulture)
                : kind;
        }

        // Line ends of every kind are one line end.
        var normal = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        foreach (var raw in normal.Split(Text.LineBreak))
        {
            var line = Text.Trimmed(raw);
            if (line.StartsWith('#'))
            {
                continue;
            }
            if (Directive(line) is (string directiveName, string directiveValue))
            {
                var directive = Lowered(directiveName);
                var value = Text.Trimmed(directiveValue);
                switch (directive)
                {
                    case "title" or "t":
                        title = value;
                        break;
                    case "artist" or "subtitle" or "st":
                        if (artist.Length == 0)
                        {
                            artist = value;
                        }
                        break;
                    case "key":
                        key = Tidied(value);
                        break;
                    case "copyright":
                        copyright = value;
                        break;
                    case "ccli":
                        ccli = value;
                        break;
                    case "start_of_verse" or "sov":
                        Open("Verse", value);
                        break;
                    case "start_of_chorus" or "soc":
                        Open("Chorus", value);
                        break;
                    case "start_of_bridge" or "sob":
                        Open("Bridge", value);
                        break;
                    case "comment" or "c" or "ci" or "comment_italic":
                        var commented = HeadingOf(value);
                        if (commented.Length > 0)
                        {
                            Close();
                            name = commented;
                            marked = true;
                        }
                        break;
                    default:
                        if (directive.StartsWith("start_of_", StringComparison.Ordinal) || directive is "sot" or "sog")
                        {
                            // Tablature, a grid, or something newer: not words to show.
                            Close();
                            skipping = true;
                        }
                        else if (directive.StartsWith("end_of_", StringComparison.Ordinal)
                            || (directive.Length == 3 && directive.StartsWith("eo", StringComparison.Ordinal)))
                        {
                            Close();
                            skipping = false;
                        }
                        break;
                }
                continue;
            }
            if (skipping)
            {
                continue;
            }
            if (line.Length == 0)
            {
                // Between the parts of a file that marks none, and inside a marked part
                // only a gap.
                if (!marked || name.Length == 0)
                {
                    Close();
                }
                continue;
            }
            var heading = HeadingOf(line);
            if (heading.Length > 0)
            {
                Close();
                name = heading;
                marked = false;
                continue;
            }
            var kept = FromChordPro(line);
            if (Text.Trimmed(kept.Text).Length > 0 || kept.Chords.Count > 0)
            {
                if (!IsPlaceholders(kept.Text))
                {
                    // Spaces at the ends are of no use on a slide, and the chords move
                    // with the words.
                    var lead = 0;
                    while (lead < kept.Text.Length && Text.IsSpace(kept.Text[lead]))
                    {
                        lead++;
                    }
                    var trimmed = Text.Trimmed(kept.Text);
                    var last = Math.Max(0, trimmed.Length - 1);
                    kept = new Line(trimmed, kept.Chords.Select(chord => chord with { At = Math.Clamp(chord.At - lead, 0, last) }).ToList());
                }
                lines.Add(kept);
            }
        }
        Close();
        return new ParsedSong(title, artist, key, copyright, ccli, sections);
    }

    /// <summary>
    /// A section's lines dealt into slides of so many lines each (the last may have
    /// fewer). Less than one line to a slide is taken as one.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<Line>> SlidesOf(Section section, int linesPerSlide)
    {
        var each = Math.Max(1, linesPerSlide);
        var slides = new List<IReadOnlyList<Line>>();
        for (var i = 0; i < section.Lines.Count; i += each)
        {
            slides.Add(section.Lines.Skip(i).Take(each).ToList());
        }
        return slides;
    }

    /// <summary>The white space a regular expression's \s means in the reference: the six of ASCII.</summary>
    private static bool IsPlainSpace(char c) => c == ' ' || (c >= (char)0x09 && c <= (char)0x0D);

    /// <summary>
    /// A directive: "{name}" or "{name: value}", and nothing else on the line. The name
    /// is what comes before a colon, a closing brace or a space. Spaces around the name
    /// and the value do not count. Nothing, for a line that is not one. The reference's
    /// expression:
    ///
    ///     ^\{\s*([^:}\s]+)\s*(?::\s*(.*?))?\s*\}$
    /// </summary>
    private static (string Name, string Value)? Directive(string line)
    {
        if (line.Length < 2 || line[0] != '{' || line[^1] != '}')
        {
            return null;
        }
        var end = line.Length - 1;
        var i = 1;
        while (i < end && IsPlainSpace(line[i]))
        {
            i++;
        }
        var nameStart = i;
        while (i < end && line[i] != ':' && line[i] != '}' && !IsPlainSpace(line[i]))
        {
            i++;
        }
        var name = line[nameStart..i];
        if (name.Length == 0)
        {
            return null;
        }
        while (i < end && IsPlainSpace(line[i]))
        {
            i++;
        }
        if (i == end)
        {
            return (name, "");
        }
        if (line[i] != ':')
        {
            return null;
        }
        i++;
        while (i < end && IsPlainSpace(line[i]))
        {
            i++;
        }
        var valueEnd = end;
        while (valueEnd > i && IsPlainSpace(line[valueEnd - 1]))
        {
            valueEnd--;
        }
        return (name, line[i..valueEnd]);
    }

    /// <summary>
    /// A directive's name in small letters, as the reference makes them (Qt's toLower),
    /// which is by Unicode's rules whatever language the computer is set to. Only what
    /// can come out as plain letters matters, since the names it is held against are
    /// plain: A to Z, the Kelvin sign (which is a "k"), and the capital I with a dot,
    /// which Unicode makes an "i" and a dot after it, two units, and .NET by itself
    /// would make an "i" alone.
    /// </summary>
    private static string Lowered(string name)
    {
        var result = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (c == (char)0x0130)
            {
                result.Append('i').Append((char)0x0307);
            }
            else
            {
                result.Append(char.ToLowerInvariant(c));
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// "Verse 1", "CHORUS:", "[Bridge]", "Pre-Chorus 2": a line that only names a part,
    /// as that name ("VERSE 1" as "Verse 1"); empty if it is not one. Once any square
    /// brackets round it and one colon after it are off, it is one of the words above,
    /// then perhaps a number (with perhaps a space before it and perhaps one letter
    /// after it: "2b"), and nothing more. The reference's expression, without regard to
    /// case:
    ///
    ///     ^(verse|chorus|pre[- ]?chorus|bridge|...|post[- ]?chorus)( ?\d+[a-z]?)?$
    /// </summary>
    private static string HeadingOf(string line)
    {
        var text = Text.Trimmed(line);
        if (text.StartsWith('[') && text.EndsWith(']'))
        {
            text = Text.Trimmed(Text.Mid(text, 1, text.Length - 2));
        }
        if (text.EndsWith(':'))
        {
            text = text[..^1];
        }
        var lowered = string.Concat(text.Select(Text.AsciiLowered));
        var word = Array.Find(HeadingWords, one => lowered.StartsWith(one, StringComparison.Ordinal));
        if (word is null)
        {
            return "";
        }
        var rest = lowered[word.Length..];
        if (rest.Length > 0)
        {
            if (rest[0] == ' ')
            {
                rest = rest[1..];
            }
            var digits = 0;
            while (digits < rest.Length && Text.IsAsciiDigit(rest[digits]))
            {
                digits++;
            }
            if (digits == 0)
            {
                return "";
            }
            rest = rest[digits..];
            if (rest.Length > 0 && Text.IsAsciiLetter(rest[0]))
            {
                rest = rest[1..];
            }
            if (rest.Length > 0)
            {
                return "";
            }
        }
        // "VERSE 1" as "Verse 1": a capital to start, and after a space or a hyphen.
        var name = new StringBuilder(lowered.Length);
        var start = true;
        foreach (var c in lowered)
        {
            name.Append(start && Text.IsAsciiLetter(c) ? Text.AsciiRaised(c) : c);
            start = c == ' ' || c == '-';
        }
        return name.ToString();
    }
}
