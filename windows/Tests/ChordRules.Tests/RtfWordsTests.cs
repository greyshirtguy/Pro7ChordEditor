// The words of an RTF, and the one thing the apps write into one: held to rtf-text.json
// and stand-ins-in-rtf.json. Every case of each is run (Vectors.cs).
//
// The first is what every place in every song is counted along, and the second is the
// riskiest thing the app does to a file (spec/01-file-format.md, "Chords with no
// words"), so beside the vectors there are checks here of what must be true whatever
// the answer: that nothing outside the one line is changed, and that what comes back
// reads as what was meant.

using System.Globalization;
using System.Text.Json;
using Xunit;

namespace Pro7ChordEditor.ChordRules.Tests;

public class RtfWordsTests
{
    [Fact]
    public void TheWordsOfAnRtf()
    {
        var held = new Held("rtf-text.json");
        using var vector = Held.Read("rtf-text.json");
        foreach (var one in vector.RootElement.GetProperty("cases").EnumerateArray())
        {
            var name = Held.Text(one, "name");
            var words = RtfWords.Read(one.GetProperty("rtfBase64").GetBytesFromBase64());

            // Unit by unit: this is the count that a chord's place is a number in.
            held.Case($"{name}: its units", Units(words), Units(one.GetProperty("units").EnumerateArray().Select(unit => (char)unit.GetInt32())));
            held.Case($"{name}: its length", words.Length, one.GetProperty("length").GetInt32());
        }
        held.Done();
    }

    [Fact]
    public void StandInsWrittenIntoALineInPlace()
    {
        var held = new Held("stand-ins-in-rtf.json");
        using var vector = Held.Read("stand-ins-in-rtf.json");
        foreach (var one in vector.RootElement.GetProperty("inPlace").EnumerateArray())
        {
            var name = Held.Text(one, "name");
            var rtf = one.GetProperty("rtfBase64").GetBytesFromBase64();
            var line = one.GetProperty("line").GetInt32();
            var standIns = Chords.Placeholders(one.GetProperty("count").GetInt32(), (char)one.GetProperty("standIn").GetInt32());

            var made = RtfWords.WithLine(rtf, line, standIns);

            held.Case($"{name}: the RTF it becomes", Base64(made), Base64(Expected(one)));
            if (made is not null)
            {
                held.Case($"{name}: nothing outside the line's own bytes is changed", OutsideTheLineIsAsItWas(rtf, made, line), true);
                held.Case($"{name}: it reads back as meant", RtfWords.Read(made), WithLine(RtfWords.Read(rtf), line, standIns));
            }
        }
        held.Done();
    }

    [Fact]
    public void StandInsAfterTheOpeningOfAnotherBox()
    {
        var held = new Held("stand-ins-in-rtf.json");
        using var vector = Held.Read("stand-ins-in-rtf.json");
        foreach (var one in vector.RootElement.GetProperty("fromAnOpening").EnumerateArray())
        {
            var name = Held.Text(one, "name");
            var model = one.GetProperty("modelBase64").GetBytesFromBase64();
            var standIns = Chords.Placeholders(one.GetProperty("count").GetInt32());

            var made = RtfWords.WithTheOpeningOf(model, standIns);

            held.Case($"{name}: the RTF that is made", Base64(made), Base64(Expected(one)));
            if (made is not null)
            {
                held.Case($"{name}: it reads back as the stand-ins and nothing else", RtfWords.Read(made), standIns);
            }
        }
        held.Done();
    }

    // Mine, not a vector: what is refused, and why each is.
    [Fact]
    public void WhatCannotBeDoneCleanlyIsRefused()
    {
        byte[] rtf = [.. "{"u8, 0x5C, .. "rtf1 Amazing grace"u8, 0x5C, 0x0A, .. "how sweet}"u8];
        Assert.Equal("Amazing grace\nhow sweet", RtfWords.Read(rtf));

        // No such line.
        Assert.Null(RtfWords.WithLine(rtf, 2, Chords.Placeholders(1)));
        Assert.Null(RtfWords.WithLine(rtf, -1, Chords.Placeholders(1)));
        // What is put must be stand-ins: a plain space, a zero-width space, an em quad.
        Assert.Null(RtfWords.WithLine(rtf, 0, "G"));
        Assert.Null(RtfWords.WithTheOpeningOf(rtf, "G"));
        // A line with no character has nothing to say where it is.
        byte[] empty = [.. "{"u8, 0x5C, .. "rtf1 one"u8, 0x5C, 0x0A, 0x5C, 0x0A, .. "three}"u8];
        Assert.Equal("one\n\nthree", RtfWords.Read(empty));
        Assert.Null(RtfWords.WithLine(empty, 1, Chords.Placeholders(1)));
        // And a box with no character at all has no opening to give.
        Assert.Null(RtfWords.WithTheOpeningOf("{}"u8, Chords.Placeholders(1)));
    }

    // Mine, not a vector: a line that already is what it is to become is not touched at
    // all, so that a chord renamed on a line of chords alone changes no byte of the RTF.
    [Fact]
    public void ALineThatIsAlreadySoComesBackTheSameBytes()
    {
        using var vector = Held.Read("stand-ins-in-rtf.json");
        var tried = 0;
        foreach (var one in vector.RootElement.GetProperty("inPlace").EnumerateArray())
        {
            var rtf = one.GetProperty("rtfBase64").GetBytesFromBase64();
            var line = one.GetProperty("line").GetInt32();
            var lines = RtfWords.Read(rtf).Split('\n');
            if (line >= lines.Length || !Chords.IsPlaceholders(lines[line]))
            {
                continue;
            }
            var made = RtfWords.WithLine(rtf, line, lines[line]);
            if (made is not null)
            {
                tried++;
                Assert.True(rtf.AsSpan().SequenceEqual(made), Held.Text(one, "name") + ": the line was already so, and bytes were changed");
            }
        }
        Assert.True(tried > 0, "no line of the vectors was a line of stand-ins already");
    }

    // ---- New words after an RTF's own opening (spec/09-chordpro-files.md, rule 12)
    //
    // MINE: no vector has this, and Simple Presenter does it another way (it writes the
    // whole RTF in its own words). What is held: that what is made reads back as the
    // words, for every RTF the vectors have and for words of every awkward kind; that
    // the opening is the model's own, byte for byte; and, by hand, the bytes themselves.

    [Fact]
    public void NewWordsAreWrittenAfterTheOpeningOfEveryRtfTheVectorsHaveAndReadBackAsThemselves()
    {
        string[] wanted =
        [
            "Amazing grace how sweet the sound\nThat saved a wretch like me",
            "One line",
            "3 times round",
            " a space first, and one last ",
            "Caf" + (char)0xE9 + " au lait: it" + (char)0x2019 + "s {good} \\ or not",
            "a\tb",
            "Joy " + (char)0xD83C + (char)0xDF89 + " to the world",
            new([(char)0x9999, (char)0x6E2F, '\n', (char)0xFFFD, (char)0x7F, (char)0xA0]),
            "\n\nthree empty lines before, and one after\n",
            Chords.Placeholders(3),
        ];
        using var vector = Held.Read("rtf-text.json");
        var models = 0;
        foreach (var one in vector.RootElement.GetProperty("cases").EnumerateArray())
        {
            var model = one.GetProperty("rtfBase64").GetBytesFromBase64();
            var name = Held.Text(one, "name");
            var had = RtfWords.Read(model);
            if (had.Length == 0)
            {
                // A box with no character has no opening to give.
                Assert.Null(RtfWords.WithWords(model, wanted[0]));
                continue;
            }
            models++;
            foreach (var words in wanted)
            {
                var made = RtfWords.WithWords(model, words);
                Assert.True(made is not null, name + ": nothing was made for " + Units(words));
                Assert.True(string.Equals(words, RtfWords.Read(made), StringComparison.Ordinal), name + ": what was made does not read back as " + Units(words));
            }
            // The opening is the model's own: what is made for one set of words and
            // for another begins the same, and begins as the model does, for as far as
            // the model has no word of its own.
            var a = RtfWords.WithWords(model, "Amazing")!;
            var b = RtfWords.WithWords(model, "Blessed")!;
            var same = 0;
            while (same < a.Length && same < b.Length && a[same] == b[same])
            {
                same++;
            }
            // (But for the one space that parts a control word from the words, where
            // the model's first character was written with a backslash.)
            Assert.True(
                same > 1 && (model.AsSpan(0, same).SequenceEqual(a.AsSpan(0, same)) || (a[same - 1] == 0x20 && model.AsSpan(0, same - 1).SequenceEqual(a.AsSpan(0, same - 1)))),
                name + ": the opening is not the model's");
            // After it there is nothing above ASCII, whatever the words.
            Assert.DoesNotContain(RtfWords.WithWords(model, wanted[4])!.AsSpan(same).ToArray(), one => one > 0x7E);
            Assert.Equal((byte)'A', a[same]);
            // And none of the model's own words are left in it.
            Assert.Equal("Amazing", RtfWords.Read(a));
        }
        Assert.True(models >= 20, "fewer RTFs with words than there were are in the vectors");
    }

    [Fact]
    public void NewWordsByHandTheBytesThemselves()
    {
        byte[] model = [.. "{"u8, 0x5C, .. "rtf1"u8, 0x5C, .. "ansi{"u8, 0x5C, .. "fonttbl"u8, 0x5C, .. "f0 Helvetica;}"u8, 0x5C, .. "f0"u8, 0x5C, .. "fs96 Old words}"u8];
        Assert.Equal("Old words", RtfWords.Read(model));
        byte[] opening = model[..^"Old words}".Length];

        // A line break is a backslash and a line feed; a brace and a backslash have a
        // backslash before them; a letter with an accent is its number.
        var words = "A{b}" + (char)0x5C + "c\n" + (char)0xE9;
        byte[] expected = [.. opening, .. "A"u8, 0x5C, .. "{b"u8, 0x5C, .. "}"u8, 0x5C, 0x5C, .. "c"u8, 0x5C, 0x0A, 0x5C, .. "uc0"u8, 0x5C, .. "u233 }"u8];
        Assert.Equal(expected, RtfWords.WithWords(model, words));
        // A unit of the upper half is written as it is, and never as a negative number.
        byte[] high = [.. opening, 0x5C, .. "uc0"u8, 0x5C, .. "u55356 "u8, 0x5C, .. "uc0"u8, 0x5C, .. "u57225 }"u8];
        Assert.Equal(high, RtfWords.WithWords(model, new([(char)0xD83C, (char)0xDF89])));

        // An opening that ends in a control word with no space of its own (here, the
        // one that stands before a character written by its number): words that begin
        // with a plain character are parted from it by a space, or "3" would be read as
        // more of that word's number; words that begin with a backslash need none.
        byte[] numbered = [.. "{"u8, 0x5C, .. "rtf1"u8, 0x5C, .. "ansi"u8, 0x5C, .. "f0"u8, 0x5C, .. "uc0"u8, 0x5C, .. "u8203 }"u8];
        Assert.Equal(((char)0x200B).ToString(), RtfWords.Read(numbered));
        byte[] upTo = numbered[..^"?u8203 }".Length];
        Assert.Equal((byte)'0', upTo[^1]);
        Assert.Equal([.. upTo, .. " 3 times}"u8], RtfWords.WithWords(numbered, "3 times")!);
        Assert.Equal([.. upTo, .. "  3}"u8], RtfWords.WithWords(numbered, " 3")!);
        Assert.Equal([.. upTo, 0x5C, .. "{x"u8, 0x5C, .. "}}"u8], RtfWords.WithWords(numbered, "{x}")!);

        // Braces left open by the opening are closed again, each of them.
        byte[] nested = [.. "{"u8, 0x5C, .. "rtf1{"u8, 0x5C, .. "b {"u8, 0x5C, .. "i deep}}}"u8];
        Assert.Equal("deep", RtfWords.Read(nested));
        Assert.Equal([.. nested[..^"deep}}}".Length], .. "new}}}"u8], RtfWords.WithWords(nested, "new")!);

        // No words at all: the opening, and its braces closed. And a box with no
        // character has no opening to give.
        Assert.Equal([.. opening, .. "}"u8], RtfWords.WithWords(model, "")!);
        Assert.Equal("", RtfWords.Read(RtfWords.WithWords(model, "")!));
        Assert.Null(RtfWords.WithWords("{}"u8, "words"));
        Assert.Null(RtfWords.WithWords([], "words"));
    }

    private static byte[]? Expected(JsonElement one) =>
        one.GetProperty("outBase64").ValueKind == JsonValueKind.Null ? null : one.GetProperty("outBase64").GetBytesFromBase64();

    private static string Base64(byte[]? bytes) => bytes is null ? "(refused)" : Convert.ToBase64String(bytes);

    private static string Units(IEnumerable<char> units) =>
        string.Join(" ", units.Select(unit => ((int)unit).ToString(CultureInfo.InvariantCulture)));

    // The words with one line made these.
    private static string WithLine(string words, int line, string standIns)
    {
        var lines = words.Split('\n');
        lines[line] = standIns;
        return string.Join('\n', lines);
    }

    // Before the line's first byte and after its last, the RTF is what it was: the made
    // one starts as the old one does up to there, and ends as it does from there.
    private static bool OutsideTheLineIsAsItWas(byte[] was, byte[] made, int line)
    {
        if (RtfWords.SpanOfLine(was, line) is not (int from, int to))
        {
            return false;
        }
        var after = was.Length - to;
        return made.Length >= from + after
            && was.AsSpan(0, from).SequenceEqual(made.AsSpan(0, from))
            && was.AsSpan(to).SequenceEqual(made.AsSpan(made.Length - after));
    }
}
