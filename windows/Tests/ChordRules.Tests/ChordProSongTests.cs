// A ChordPro file read, held to parse-song.json: what ParseSong gives for each file of
// testdata/songs, and how many of a part's lines land on each slide. Every case is run
// (Vectors.cs).
//
// And one rule the vectors do not hold, kept here as the Mac app keeps it
// (mac/Core/Tests/ChordRulesTests/NotHeldByVectorsTests.swift, and "Still wanted" in
// testdata/README.md): what is expected of it was read from the reference's code and
// not got from it. If it ever disagrees with a vector made later, the vector is right.

using System.Globalization;
using System.Text;
using Pro7ChordEditor.Tests;
using Xunit;

namespace Pro7ChordEditor.ChordRules.Tests;

public class ChordProSongTests
{
    [Fact]
    public void EverySongOfTheTestData()
    {
        var held = new Held("parse-song.json");
        using var vector = Held.Read("parse-song.json");
        foreach (var one in vector.RootElement.GetProperty("cases").EnumerateArray())
        {
            var file = Held.Text(one, "file");
            // (The files are UTF-8, and one has Windows line ends on purpose. Reading a
            // file that is not UTF-8, or is too large to be a song, is milestone M9's.)
            var bytes = File.ReadAllBytes(Path.Combine(TestData.Root, "songs", file));
            var song = Chords.ParseSong(Encoding.UTF8.GetString(bytes));

            held.Case($"{file}: title", song.Title, Held.Text(one, "title"));
            held.Case($"{file}: artist", song.Artist, Held.Text(one, "artist"));
            held.Case($"{file}: key", song.Key, Held.Text(one, "key"));
            held.Case($"{file}: copyright", song.Copyright, Held.Text(one, "copyright"));
            held.Case($"{file}: ccli", song.Ccli, Held.Text(one, "ccli"));

            var expected = one.GetProperty("sections").EnumerateArray().ToList();
            held.Case($"{file}: the names of its parts", song.Sections.Select(section => section.Name), expected.Select(section => Held.Text(section, "name")));
            for (var s = 0; s < Math.Min(song.Sections.Count, expected.Count); s++)
            {
                var section = song.Sections[s];
                var lines = expected[s].GetProperty("lines").EnumerateArray().ToList();
                held.Case($"{file}, part {s}: its lines", section.Lines.Select(line => line.Text), lines.Select(line => Held.Text(line, "text")));
                for (var l = 0; l < Math.Min(section.Lines.Count, lines.Count); l++)
                {
                    held.Case($"{file}, part {s}, line {l}: its chords", section.Lines[l].Chords, Held.Chords(lines[l], "chords"));
                }
                // SlidesOf(section, n) for n of 1 to 4: how many lines land on each slide.
                foreach (var each in expected[s].GetProperty("linesOnEachSlide").EnumerateObject())
                {
                    var n = int.Parse(each.Name, CultureInfo.InvariantCulture);
                    held.Case(
                        $"{file}, part {s}: SlidesOf(part, {n})",
                        Counts(Chords.SlidesOf(section, n).Select(slide => slide.Count)),
                        Counts(each.Value.EnumerateArray().Select(count => count.GetInt32())));
                }
            }
        }
        held.Done();

        static IEnumerable<string> Counts(IEnumerable<int> counts) => counts.Select(count => count.ToString(CultureInfo.InvariantCulture));
    }

    // SlidesOf: the lines themselves, in order, and none twice. (The vector holds how
    // many land on each slide.)
    [Fact]
    public void ASlideHasItsLinesInOrder()
    {
        var lines = Enumerable.Range(0, 5).Select(n => new Line("line " + n.ToString(CultureInfo.InvariantCulture), [])).ToList();
        var slides = Chords.SlidesOf(new Section("Verse 1", lines), 2);

        Assert.Equal(lines, slides.SelectMany(slide => slide));
        // Less than one line to a slide is taken as one.
        Assert.Equal(5, Chords.SlidesOf(new Section("Verse 1", lines), 0).Count);
    }

    // Mine, not a vector. ParseSong: a line is trimmed, and its chords go with its words.
    // (The vectors have a line with two spaces after its first chord, and no chord after
    // the spaces to show whether it moved.)
    [Fact]
    public void SpacesAfterALinesFirstChordGoAndTheChordsAfterThemMoveWithTheWords()
    {
        var line = Chords.ParseSong("[G]  Amazing [C]grace").Sections[0].Lines[0];

        Assert.Equal("Amazing grace", line.Text);
        Assert.Equal([new Chord(0, "G"), new Chord(8, "C")], line.Chords);
    }
}
