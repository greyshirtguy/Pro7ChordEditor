// The two rules that are new here and whose reference is Python: which key a song is
// likely in (tools/keyfit.py), and where the first Windows editor meant a chord to be
// (tools/make-fixtures.py). Held to key-fit.json and line-break-shift.json. Every case
// of each is run (Vectors.cs).
//
// And, mine and not vectors, the two things about them that .NET could get wrong where
// Python does not, which spec/02-chord-rules.md names ("To the letter"): how a score is
// rounded, and what counts as white space.

using System.Globalization;
using Xunit;

namespace Pro7ChordEditor.ChordRules.Tests;

public class NewRulesTests
{
    [Fact]
    public void WhichKeyASongIsLikelyIn()
    {
        var held = new Held("key-fit.json");
        using var vector = Held.Read("key-fit.json");
        foreach (var one in vector.RootElement.GetProperty("cases").EnumerateArray())
        {
            var about = Held.Text(one, "about");
            var answer = KeyFit.Likely(Held.Texts(one, "chords"), Held.Text(one, "stated"));

            held.Case($"{about}: likeliest", answer.Likeliest, Held.Texts(one, "likeliest"));
            held.Case($"{about}: doubtful", answer.Doubtful, one.GetProperty("doubtful").GetBoolean());
            held.Case($"{about}: suggested", answer.Suggested, Held.Text(one, "suggested"));
            // Every offered key's score, and no key more.
            var expected = one.GetProperty("scores").EnumerateObject().OrderBy(score => score.Name, StringComparer.Ordinal)
                .Select(score => score.Name + "=" + Exactly(score.Value.GetDouble()));
            var got = answer.Scores.OrderBy(score => score.Key, StringComparer.Ordinal).Select(score => score.Key + "=" + Exactly(score.Value));
            held.Case($"{about}: scores", got, expected);
        }
        held.Done();

        // A number written so that two are written alike only if they are the same number.
        static string Exactly(double number) => number.ToString("R", CultureInfo.InvariantCulture);
    }

    // Mine, not a vector. A score is a count of chords over a count of chords, and is
    // exactly half way between two numbers of four places only for an odd number of
    // thirty-seconds (or of sixty-fourths, and so on). Python's round, which the vectors
    // were made with, sends such a tie to the even digit, by the exact value the machine
    // holds. Rounding that first multiplies by ten thousand gets some of these wrong.
    [Theory]
    [InlineData(1, 32, 0.0312)]
    [InlineData(3, 32, 0.0938)]
    [InlineData(5, 32, 0.1562)]
    [InlineData(7, 32, 0.2188)]
    [InlineData(9, 32, 0.2812)]
    [InlineData(27, 32, 0.8438)]
    [InlineData(1, 64, 0.0156)]
    [InlineData(1, 3, 0.3333)]
    [InlineData(2, 3, 0.6667)]
    [InlineData(8, 11, 0.7273)]
    public void AScoreIsRoundedAsPythonRoundsIt(int fits, int chords, double expected)
    {
        Assert.Equal(expected, KeyFit.PythonRound4((double)fits / chords));
    }

    [Fact]
    public void WhereTheFirstEditorMeantAChordToBe()
    {
        var held = new Held("line-break-shift.json");
        using var vector = Held.Read("line-break-shift.json");

        foreach (var one in vector.RootElement.GetProperty("unshifted").EnumerateArray())
        {
            var words = Held.Text(one, "words");
            var savedAt = one.GetProperty("savedAt").GetInt32();
            held.Case($"Unshifted({Held.Show(words)}, {savedAt})", OldEditor.Unshifted(words, savedAt), one.GetProperty("meantFor").GetInt32());
        }
        foreach (var song in vector.RootElement.GetProperty("songs").EnumerateArray())
        {
            var about = Held.Text(song, "about");
            var boxes = song.GetProperty("textBoxes").EnumerateArray().Select(box =>
                (Held.Text(box, "words"), (IReadOnlyList<int>)box.GetProperty("chords").EnumerateArray().Select(chord => chord.GetProperty("start").GetInt32()).ToList()));
            var shift = OldEditor.LineBreakShift(boxes);
            var expected = song.GetProperty("out");

            held.Case($"{about}: chords below the first line", shift.ChordsBelowTheFirstLine, expected.GetProperty("chordsBelowTheFirstLine").GetInt32());
            held.Case($"{about}: at the start of a word", shift.AtTheStartOfAWord, expected.GetProperty("atTheStartOfAWord").GetInt32());
            held.Case($"{about}: if moved back", shift.IfMovedBack, expected.GetProperty("ifMovedBack").GetInt32());
            held.Case($"{about}: likely", shift.Likely, expected.GetProperty("likely").GetBoolean());
        }
        held.Done();
    }
}
