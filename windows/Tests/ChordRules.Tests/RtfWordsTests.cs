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
