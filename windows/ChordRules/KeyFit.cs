// Which key a song's chords are most likely in, and whether the key it states is to be
// doubted. Held to testdata/vectors/key-fit.json.
//
// NEW: Simple Presenter has nothing like it. The reference is tools/keyfit.py, which has
// the rule and its reasons at its top, and this is a port of its `likely`, by way of the
// Mac's KeyFit.swift. What it is for is spec/08-repair.md: the first Windows editor
// could save a song with its key set to A flat major when the key was really something
// else, and a song typed by hand often names no key at all. Neither app can know the
// right key, but both can say "these chords look like G" and let the person decide.
//
// The rule, in short. A chord is a note and a kind (minor, diminished, or else major;
// sevenths, suspensions and a bass note are not looked at). A key has seven chords of
// its own, and a chord fits a key if it is one of them. A key's score is the share of
// the song's chords that fit it. The twenty-six keys a picker offers are scored. The
// stated key is doubtful only when the song has at least four chords, the best score is
// at least 0.75, and the stated key's score is more than 0.25 below it.
//
// Three things are as the Python has them and not as the rest of this project does, so
// that the two give the same answer for every input and not only for the vectors
// (spec/02-chord-rules.md, "To the letter"):
//
//   - A chord's name is read by the Python's own expression, which is not Notes.Read:
//     its sharps or flats are a run of ONE sign ("#" or "b" or either music sign), and a
//     name with a line break in it is no chord.
//   - White space, for trimming a name, is Python's: Qt's list and U+001C to U+001F.
//   - A score is rounded to four places as Python's `round` does it, which goes by the
//     exact value of the number as the machine holds it, a tie going to the even digit.
//     Multiplying by ten thousand, rounding and dividing does not always give the same.
//     It is done here by writing the number out to four places and reading it back
//     (PythonRound4 says why that is the same). The comparisons are then made on the
//     rounded scores, as the Python makes them.

using System.Globalization;

namespace Pro7ChordEditor.ChordRules;

public static class KeyFit
{
    /// <param name="Scores">Every offered key's share of the chords that fit it, to four places.</param>
    /// <param name="Likeliest">The keys with the best score, the likeliest first. Empty if no chord fits any.</param>
    /// <param name="Doubtful">Whether the stated key is far enough from the likeliest to say so.</param>
    /// <param name="Suggested">The key to offer (when the stated one is doubtful, or none is stated), or "".</param>
    public sealed record Answer(IReadOnlyDictionary<string, double> Scores, IReadOnlyList<string> Likeliest, bool Doubtful, string Suggested);

    // A chord as this rule sees it: one of the twelve notes, and "m", "dim" or "" for major.
    private readonly record struct NoteAndKind(int Pitch, string Kind);

    /// <summary>
    /// <paramref name="chords"/> is every chord of the song, one entry for each time it
    /// is used. <paramref name="stated"/> is the key the song says it is in, or "" if it
    /// says none.
    /// </summary>
    public static Answer Likely(IReadOnlyList<string> chords, string stated)
    {
        var offered = Chords.MajorKeys().Concat(Chords.MinorKeys()).ToList();
        var read = new List<NoteAndKind>();
        foreach (var chord in chords)
        {
            if (Read(chord) is { } one)
            {
                read.Add(one);
            }
        }

        var scores = new Dictionary<string, double>(StringComparer.Ordinal);
        var tonics = new Dictionary<string, NoteAndKind?>(StringComparer.Ordinal);
        foreach (var key in offered)
        {
            var own = new List<NoteAndKind>();
            foreach (var chord in Chords.Diatonic(key))
            {
                if (Read(chord) is { } one)
                {
                    own.Add(one);
                }
            }
            tonics[key] = own.Count > 0 ? own[0] : null;
            var fits = read.Count(one => own.Contains(one));
            scores[key] = read.Count == 0 ? 0.0 : PythonRound4((double)fits / read.Count);
        }
        var best = scores.Count == 0 ? 0.0 : scores.Values.Max();

        // Which way the song's own chords are written, to tell F# from Gb.
        int WrittenWith(char mark) => chords.Count(chord =>
        {
            var name = Text.PythonStripped(chord);
            return name.Length >= 2 && Notes.Letters.Contains(Text.AsciiRaised(name[0]), StringComparison.Ordinal) && name[1] == mark;
        });
        var flatsWritten = WrittenWith('b');
        var sharpsWritten = WrittenWith('#');

        // Among the best, the one whose own first chord (G in G, Em in E minor) is used
        // most comes first; then the major one; then the one spelt the way the song's
        // chords are spelt; then as a picker lists them. (OrderBy keeps the order of
        // those that are equal, so the last of these needs no saying.)
        var likeliest = offered
            .Where(key => best > 0 && scores[key] == best)
            .OrderByDescending(key => read.Count(one => one == tonics[key]))
            .ThenBy(key => key[^1] == 'm')
            .ThenBy(key =>
            {
                var second = key.Length > 1 ? key[1] : (char)0;
                return (second == 'b' && sharpsWritten > flatsWritten) || (second == '#' && flatsWritten > sharpsWritten);
            })
            .ToList();

        var isStated = stated.Length > 0;
        var statedScore = isStated && scores.TryGetValue(stated, out var score) ? score : 0.0;
        var enough = read.Count >= 4 && best >= 0.75;
        var doubtful = isStated && enough && statedScore < best - 0.25;
        var suggested = (doubtful || !isStated) && enough && likeliest.Count > 0 ? likeliest[0] : "";
        return new Answer(scores, likeliest, doubtful, suggested);
    }

    /// <summary>
    /// A chord's note and kind, or nothing for what does not start with a note. The
    /// Python's expression, on the name with the white space off its ends:
    ///
    ///     ^([A-Ga-g])(#+|b+|music sharps|music flats)?(.*)$
    /// </summary>
    private static NoteAndKind? Read(string chord)
    {
        var name = Text.PythonStripped(chord);
        if (name.Length == 0)
        {
            return null;
        }
        var letter = Notes.Letters.IndexOf(Text.AsciiRaised(name[0]), StringComparison.Ordinal);
        // ("." is anything but a line break, so a name with one inside is no match.)
        if (letter < 0 || name.Contains(Text.LineBreak, StringComparison.Ordinal))
        {
            return null;
        }
        var i = 1;
        var accidental = 0;
        if (i < name.Length && name[i] is '#' or 'b' or Text.MusicSharp or Text.MusicFlat)
        {
            var mark = name[i];
            while (i < name.Length && name[i] == mark)
            {
                accidental += mark is '#' or Text.MusicSharp ? 1 : -1;
                i++;
            }
        }
        var pitch = ((Notes.Semitones[letter] + accidental) % 12 + 12) % 12;
        var rest = Text.Mid(name, i);
        // A bass note is not looked at: a letter, then sharps or flats, after the last slash.
        var slash = rest.LastIndexOf('/');
        if (slash >= 0)
        {
            var after = Text.Mid(rest, slash + 1);
            if (after.Length > 0 && Notes.Letters.Contains(Text.AsciiRaised(after[0]), StringComparison.Ordinal))
            {
                var marks = after[1..];
                if (marks.Length == 0 || marks.All(c => c == '#') || marks.All(c => c == 'b'))
                {
                    rest = Text.Left(rest, slash);
                }
            }
        }
        string kind;
        if (rest.StartsWith("dim", StringComparison.Ordinal) || rest.StartsWith((char)0x00B0))
        {
            kind = "dim";
        }
        else if (rest.StartsWith('m') && !rest.StartsWith("maj", StringComparison.Ordinal))
        {
            kind = "m";
        }
        else
        {
            kind = "";
        }
        return new NoteAndKind(pitch, kind);
    }

    /// <summary>
    /// Python's <c>round(x, 4)</c>: the nearest number of four places to the exact value
    /// the machine holds, a tie going to the even digit. .NET writes a number out to a
    /// given count of places exactly so (since .NET Core 3.0: it works the digits out
    /// from the exact value, and rounds a tie to the even one), where its Math.Round
    /// first multiplies, which is not the exact value any more. The tests hold this to
    /// the ties there are: a score is a count over a count, and is exactly half way only
    /// for an odd number of thirty-seconds (and of sixty-fourths, and so on).
    /// </summary>
    internal static double PythonRound4(double x) =>
        double.Parse(x.ToString("F4", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
}
