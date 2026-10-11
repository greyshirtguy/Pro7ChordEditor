// A key's own chords and what a chord being typed might become; a text box's chords in
// order, as the file writes them, and when its words change; and chords as ChordPro
// text. Held to diatonic.json, completions.json, tidy.json, ranges.json, carried.json
// and chordpro.json. Every case of each is run (Vectors.cs).

using System.Globalization;
using Xunit;

namespace Pro7ChordEditor.ChordRules.Tests;

public class SuggestionsAndBoxesTests
{
    [Fact]
    public void Diatonic()
    {
        var held = new Held("diatonic.json");
        using var vector = Held.Read("diatonic.json");
        foreach (var one in vector.RootElement.GetProperty("cases").EnumerateArray())
        {
            var key = Held.Text(one, "key");
            held.Case($"Diatonic({Held.Show(key)})", Chords.Diatonic(key), Held.Texts(one, "out"));
        }
        held.Done();
    }

    [Fact]
    public void Completions()
    {
        var held = new Held("completions.json");
        using var vector = Held.Read("completions.json");
        foreach (var one in vector.RootElement.GetProperty("cases").EnumerateArray())
        {
            var (typed, key, used) = (Held.Text(one, "typed"), Held.Text(one, "key"), Held.Texts(one, "used"));
            held.Case(
                $"Completions({Held.Show(typed)}, {Held.Show(key)}, [{string.Join(" ", used)}])",
                Chords.Completions(typed, key, used),
                Held.Texts(one, "out"));
        }
        held.Done();
    }

    [Fact]
    public void Tidy()
    {
        var held = new Held("tidy.json");
        using var vector = Held.Read("tidy.json");
        var n = 0;
        foreach (var one in vector.RootElement.GetProperty("cases").EnumerateArray())
        {
            var text = Held.Text(one, "text");
            held.Case($"case {n++}: Tidy({Held.Show(text)}, ...)", Chords.Tidy(text, Held.Chords(one, "chords")), Held.Chords(one, "out"));
        }
        held.Done();
    }

    // MINE, NOT A VECTOR. The place Tidy brings a chord to can be asked for by itself
    // (spec/08-repair.md, check 0: where a chord that is on no character of its words is
    // shown), and is the same place, for every place in and around a text.
    [Fact]
    public void OnACharacterIsWhereTidyPutsAChord()
    {
        string[] texts = ["a", "ab\ncd", "ab\n\ncd", "\nab", "ab\n", "\n", "\n\n", "ab\n\n\n", "ab\ncd\nef"];
        foreach (var text in texts)
        {
            for (var at = -2; at <= text.Length + 2; at++)
            {
                Assert.Equal([new Chord(Chords.OnACharacter(text, at), "G")], Chords.Tidy(text, [new Chord(at, "G")]));
            }
        }
        // And never past a place that was after it: places in order are in order still.
        foreach (var text in texts)
        {
            for (var at = -2; at <= text.Length + 2; at++)
            {
                Assert.True(Chords.OnACharacter(text, at) <= Chords.OnACharacter(text, at + 1), text.Length + " long, at " + at);
            }
        }
        // By hand. On the break that ends a line: the character before it.
        Assert.Equal(1, Chords.OnACharacter("ab\ncd", 2));
        // Past the end: the last character.
        Assert.Equal(4, Chords.OnACharacter("ab\ncd", 9));
        // Past the end, where the last character is a line break: the one before that.
        Assert.Equal(1, Chords.OnACharacter("ab\n", 7));
        // On an empty line, which is its line break and nothing else: it stays.
        Assert.Equal(3, Chords.OnACharacter("ab\n\ncd", 3));
        // A break at the very start has no character before it.
        Assert.Equal(0, Chords.OnACharacter("\nab", 0));
        // Before the start: the first character.
        Assert.Equal(0, Chords.OnACharacter("ab", -3));
        // A text with no characters has no place but 0, though Tidy keeps no chord there.
        Assert.Equal(0, Chords.OnACharacter("", 5));
        Assert.Empty(Chords.Tidy("", [new Chord(5, "G")]));
    }

    [Fact]
    public void Ranges()
    {
        var held = new Held("ranges.json");
        using var vector = Held.Read("ranges.json");
        var n = 0;
        foreach (var one in vector.RootElement.GetProperty("cases").EnumerateArray())
        {
            var text = Held.Text(one, "text");
            var got = Chords.Ranges(text, Held.Chords(one, "chords")).Select(Written);
            var expected = one.GetProperty("out").EnumerateArray().Select(range =>
                Written(new ChordRange(range.GetProperty("start").GetInt32(), range.GetProperty("end").GetInt32(), range.GetProperty("name").GetString()!)));
            held.Case($"case {n++}: Ranges({Held.Show(text)}, ...)", got, expected);
        }
        held.Done();

        static string Written(ChordRange range) =>
            range.Start.ToString(CultureInfo.InvariantCulture) + "-" + range.End.ToString(CultureInfo.InvariantCulture) + "=" + range.Name;
    }

    [Fact]
    public void Carried()
    {
        var held = new Held("carried.json");
        using var vector = Held.Read("carried.json");
        var n = 0;
        foreach (var one in vector.RootElement.GetProperty("cases").EnumerateArray())
        {
            var (before, after) = (Held.Text(one, "before"), Held.Text(one, "after"));
            held.Case(
                $"case {n++}: Carried({Held.Show(before)}, {Held.Show(after)}, ...)",
                Chords.Carried(before, after, Held.Chords(one, "chords")),
                Held.Chords(one, "out"));
        }
        held.Done();
    }

    [Fact]
    public void ChordPro()
    {
        var held = new Held("chordpro.json");
        using var vector = Held.Read("chordpro.json");
        var chordPro = vector.RootElement;

        foreach (var one in chordPro.GetProperty("toLine").EnumerateArray())
        {
            var text = Held.Text(one, "text");
            held.Case($"ToChordPro(line {Held.Show(text)})", Chords.ToChordPro(new Line(text, Held.Chords(one, "chords"))), Held.Text(one, "out"));
        }
        foreach (var one in chordPro.GetProperty("toText").EnumerateArray())
        {
            var text = Held.Text(one, "text");
            held.Case($"ToChordPro(text {Held.Show(text)})", Chords.ToChordPro(text, Held.Chords(one, "chords")), Held.Text(one, "out"));
        }
        foreach (var one in chordPro.GetProperty("fromLine").EnumerateArray())
        {
            var typed = Held.Text(one, "in");
            var line = Chords.FromChordPro(typed);
            held.Case($"FromChordPro({Held.Show(typed)}).Text", line.Text, Held.Text(one, "text"));
            held.Case($"FromChordPro({Held.Show(typed)}).Chords", line.Chords, Held.Chords(one, "chords"));
        }
        foreach (var one in chordPro.GetProperty("chordsOf").EnumerateArray())
        {
            var typed = Held.Text(one, "in");
            held.Case($"ChordsOf({Held.Show(typed)})", Chords.ChordsOf(typed), Held.Chords(one, "out"));
        }
        foreach (var one in chordPro.GetProperty("withoutChords").EnumerateArray())
        {
            var typed = Held.Text(one, "in");
            held.Case($"WithoutChords({Held.Show(typed)})", Chords.WithoutChords(typed), Held.Text(one, "out"));
        }
        foreach (var one in chordPro.GetProperty("placeholders").EnumerateArray())
        {
            var count = one.GetProperty("count").GetInt32();
            var standIn = Held.Text(one, "standIn");
            held.Case($"Placeholders({count}, {Held.Show(standIn)})", Chords.Placeholders(count, standIn[0]), Held.Text(one, "out"));
        }
        foreach (var one in chordPro.GetProperty("isPlaceholders").EnumerateArray())
        {
            var line = Held.Text(one, "in");
            held.Case($"IsPlaceholders({Held.Show(line)})", Chords.IsPlaceholders(line), one.GetProperty("out").GetBoolean());
        }
        held.Done();
    }

    // The stand-in an import writes, when none is named, is the zero-width space.
    [Fact]
    public void AStandInIsAZeroWidthSpaceUnlessAnotherIsNamed()
    {
        Assert.Equal([(char)0x200B, (char)0x2001, (char)0x200B], Chords.Placeholders(2).ToCharArray());
    }
}
