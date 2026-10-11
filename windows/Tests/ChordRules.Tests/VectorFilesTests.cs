// That the vectors are found, and arrive whole on this computer.
//
// The rules themselves are milestone M1's: each will have a file of tests here that
// reads every case of its vector file. These tests are milestone M0's, and prove what
// those will stand on: that testdata/vectors is where TestData says, that every file
// of it reads as what testdata/README.md says a vector file is, and that the
// characters in them that cannot be seen (a zero-width space, U+200B, and an em quad,
// U+2001) are still those characters, unit for unit, after git has checked the files
// out on Windows and .NET has read them. A place in a song's words is a count of such
// units, so a vector that arrived with one of them changed would hold the rules to
// the wrong answer.

using System.Text.Json;
using Pro7ChordEditor.Tests;
using Xunit;

namespace Pro7ChordEditor.ChordRules.Tests;

public class VectorFilesTests
{
    // Every file there is, found by looking. If none were found the test would fail
    // for want of anything to try, and not pass for having tried nothing.
    public static TheoryData<string> EveryVectorFile => new(TestData.VectorNames());

    [Theory]
    [MemberData(nameof(EveryVectorFile))]
    public void AVectorFileIsOneObjectThatSaysWhatIsInIt(string name)
    {
        using var vector = JsonDocument.Parse(TestData.Vector(name));

        Assert.Equal(JsonValueKind.Object, vector.RootElement.ValueKind);
        Assert.True(vector.RootElement.TryGetProperty("about", out var about), $"{name} has no \"about\"");
        Assert.Equal(JsonValueKind.String, about.ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(about.GetString()), $"{name} has an \"about\" that says nothing");
    }

    // spec/02-chord-rules.md, "Keys": twenty-one names for a key's note, numbered from
    // A flat, and thirteen keys of each kind for a picker to offer, from C.
    [Fact]
    public void KeysJsonHasTheKeysThePageCounts()
    {
        using var vector = JsonDocument.Parse(TestData.Vector("keys.json"));
        var keys = vector.RootElement;

        var names = keys.GetProperty("names").EnumerateArray().ToList();
        Assert.Equal(21, names.Count);
        Assert.Equal(Enumerable.Range(0, 21), names.Select(name => name.GetProperty("number").GetInt32()));
        Assert.Equal("Ab", names[0].GetProperty("major").GetString());
        Assert.Equal("Abm", names[0].GetProperty("minor").GetString());
        Assert.Equal("G#", names[20].GetProperty("major").GetString());

        var major = keys.GetProperty("majorKeys").EnumerateArray().Select(key => key.GetString()).ToList();
        var minor = keys.GetProperty("minorKeys").EnumerateArray().Select(key => key.GetString()).ToList();
        Assert.Equal(13, major.Count);
        Assert.Equal(13, minor.Count);
        Assert.Equal("C", major[0]);
        Assert.Equal("Cm", minor[0]);
    }

    // chordpro.json, `placeholders`: so many stand-ins with an em quad between each two
    // (spec/02-chord-rules.md). The answers are held here to nothing but their own
    // shape, which is enough to show whether the two characters came through: each is
    // one unit, so three stand-ins are five units, and anything that had turned either
    // into the bytes it is kept as, or into a question mark, would give another count
    // or another unit.
    [Fact]
    public void TheCharactersThatCannotBeSeenArriveAsThemselves()
    {
        // Written as their numbers. Typed as themselves they cannot be seen in this
        // file, and typed as a backslash, a "u" and four digits they have arrived in a
        // file as the character itself (here, and on the Mac: mac/NOTES.md, "At M1").
        const char emQuad = (char)0x2001;
        const char zeroWidthSpace = (char)0x200B;

        using var vector = JsonDocument.Parse(TestData.Vector("chordpro.json"));
        var cases = vector.RootElement.GetProperty("placeholders").EnumerateArray().ToList();
        Assert.NotEmpty(cases);

        var standIns = new HashSet<char>();
        foreach (var one in cases)
        {
            var count = one.GetProperty("count").GetInt32();
            var standIn = one.GetProperty("standIn").GetString()!;
            var answer = one.GetProperty("out").GetString()!;

            Assert.Equal(1, standIn.Length);
            standIns.Add(standIn[0]);

            var expected = new char[Math.Max(0, 2 * count - 1)];
            for (var place = 0; place < expected.Length; place++)
            {
                expected[place] = place % 2 == 0 ? standIn[0] : emQuad;
            }
            Assert.Equal(expected, answer.ToCharArray());
        }

        // And the file does try the stand-in that cannot be seen, so that the above was
        // not passed by plain spaces alone.
        Assert.Contains(zeroWidthSpace, standIns);
        Assert.Contains(cases, one => one.GetProperty("out").GetString()!.Contains(emQuad, StringComparison.Ordinal));
    }
}
