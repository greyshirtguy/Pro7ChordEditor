// A ChordPro file read from a disk, and a song written to one (ChordProFile.cs).
// spec/09-chordpro-files.md, rules 1 and 7, and 17 to 19.
//
// What a file SAYS is held by the rules' own tests to testdata/vectors/parse-song.json
// (ChordRules.Tests/ChordProSongTests.cs). Here: that each file of testdata/songs, read
// as a file, is what its text read by the rules is; a file's bytes as text; what is
// refused; and the page's test of writing, which is that a file written from a song and
// read again "gives the same parts, lines and chords".
//
// That last is held where it is true and SAID where it is not: a group whose name is
// not a part's name, and slides that are in no group, come back as more of the part
// before them, by the rules of reading (rules 3 and 5). NOTES.md has it for the
// maintainer.

using System.Text;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Tests;
using Xunit;

namespace Pro7ChordEditor.Song.Tests;

public sealed class ChordProFileTests : IDisposable
{
    private readonly Workbench bench = new();

    public void Dispose() => bench.Dispose();

    private static string SongPath(string name) => Path.Combine(TestData.Root, "songs", name);

    private static string InTestData(string name) =>
        Path.Combine(TestData.Root, "files", TestData.PresentationPaths().Single(path => path.EndsWith("/" + name, StringComparison.Ordinal)));

    // A line as its words and its chords, each at its place: for holding two against
    // each other.
    private static string Said(Line line) => Spelt.Of(line.Text) + " " + Spelt.Of(line.Chords);

    private static string Said(ParsedSong song) =>
        $"title {Spelt.Of(song.Title)} artist {Spelt.Of(song.Artist)} key {Spelt.Of(song.Key)} copyright {Spelt.Of(song.Copyright)} ccli {Spelt.Of(song.Ccli)}\n"
        + string.Join("\n", song.Sections.Select(part => "  " + Spelt.Of(part.Name) + "\n" + string.Join("\n", part.Lines.Select(line => "    " + Said(line)))));

    // The groups of a song as its text boxes have them, each with the lines of its
    // slides that have anything on them: what a ChordPro file written from it is to
    // read back as, where every group is named as a part is.
    private static List<(string Name, List<string> Lines)> GroupsOf(IReadOnlyList<TextBox> boxes)
    {
        var groups = new List<(string Name, List<string> Lines)>();
        foreach (var box in boxes)
        {
            // (Slides in no group are a group of their own here, with no name.)
            if (groups.Count == 0 || box.StartsItsGroup || !string.Equals(box.Group, groups[^1].Name, StringComparison.Ordinal))
            {
                groups.Add((box.Group, []));
            }
            var start = 0;
            foreach (var words in Words.Lines(box.Words))
            {
                var from = start;
                var chords = box.Chords.Where(chord => chord.At >= from && chord.At < from + Math.Max(1, words.Length)).Select(chord => new Chord(chord.At - from, chord.Name)).ToList();
                if (words.Trim().Length > 0 || chords.Count > 0)
                {
                    groups[^1].Lines.Add(Said(new Line(words, chords)));
                }
                start += words.Length + 1;
            }
        }
        return [.. groups.Where(group => group.Lines.Count > 0)];
    }

    // ---- Reading

    [Fact]
    public void EachTestSongReadAsAFileIsWhatItsTextReadsAs()
    {
        var names = Directory.EnumerateFiles(Path.Combine(TestData.Root, "songs")).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(["amazing-grace.cho", "blank-slides.cho", "headings.cho", "holy-holy-holy.txt", "it-is-well.chordpro", "minor-key.cho", "plain-song.cho"], names);
        foreach (var name in names)
        {
            var read = ChordProFile.Read(SongPath(name!));
            Assert.True(read.Sections.Count > 0, name);
            // (Every test song is UTF-8 with no mark: the rules' own test reads them so.)
            Assert.Equal(Said(Chords.ParseSong(Encoding.UTF8.GetString(File.ReadAllBytes(SongPath(name!))))), Said(read));
        }
        // One by hand, so that the comparison above is not of nothing with nothing.
        var minor = ChordProFile.Read(SongPath("minor-key.cho"));
        Assert.Equal(("A Song / In A Minor Key", "F#m", 3), (minor.Title, minor.Key, minor.Sections.Count));
        Assert.Equal(["Chorus", "Chorus 2", "The Middle Eight"], minor.Sections.Select(part => part.Name));
    }

    [Fact]
    public void AFilesBytesAreReadAsUtf8WithOrWithoutAMarkAndOtherwiseAsLatin1()
    {
        var eAcute = ((char)0xE9).ToString();
        var text = "{title: Caf" + eAcute + "}\nVerse 1\n[G]Caf" + eAcute + " au [D]lait\n";
        var utf8 = Encoding.UTF8.GetBytes(text);
        Assert.Equal(text, ChordProFile.TextOf(utf8));
        // With the mark at its start, which is not part of the text.
        Assert.Equal(text, ChordProFile.TextOf([0xEF, 0xBB, 0xBF, .. utf8]));
        // As an old file typed on Windows has it: one byte for the letter, which is no
        // UTF-8.
        var latin1 = Encoding.Latin1.GetBytes(text);
        Assert.NotEqual(utf8, latin1);
        Assert.Equal(text, ChordProFile.TextOf(latin1));
        Assert.Equal("", ChordProFile.TextOf([]));
        Assert.Equal("", ChordProFile.TextOf([0xEF, 0xBB, 0xBF]));

        // And as files: each reads as the same song, whatever its line ends.
        foreach (var (name, bytes) in new[]
        {
            ("utf8.cho", utf8),
            ("marked.cho", (byte[])[0xEF, 0xBB, 0xBF, .. utf8]),
            ("latin1.cho", latin1),
            ("windows.cho", Encoding.UTF8.GetBytes(text.Replace("\n", "\r\n", StringComparison.Ordinal))),
            ("old-mac.cho", Encoding.UTF8.GetBytes(text.Replace('\n', '\r'))),
        })
        {
            var song = ChordProFile.Read(bench.FileOf(name, bytes));
            Assert.True(("Caf" + eAcute, 1, "Verse 1") == (song.Title, song.Sections.Count, song.Sections[0].Name), name);
            Assert.Equal(Said(new Line("Caf" + eAcute + " au lait", [new Chord(0, "G"), new Chord(8, "D")])), Said(song.Sections[0].Lines.Single()));
        }
    }

    [Fact]
    public void AFileThatIsNotThereOrIsTooLargeOrHasNoWordsInItIsRefusedAndSaysWhich()
    {
        var missing = Assert.Throws<Refusal>(() => ChordProFile.Read(Path.Combine(bench.Folder, "Not there.cho")));
        Assert.Equal(Refusal.Why.NotASong, missing.Reason);
        Assert.StartsWith("Not there.cho could not be read: ", missing.Sentence, StringComparison.Ordinal);

        // Two megabytes is the most: a byte over is not a song.
        Assert.Equal(2 * 1024 * 1024, ChordProFile.MostBytes);
        var most = new byte[ChordProFile.MostBytes];
        Array.Fill(most, (byte)'\n');
        "Verse 1\nla la\n"u8.CopyTo(most);
        Assert.Single(ChordProFile.Read(bench.FileOf("as large as may be.cho", most)).Sections);
        var tooLarge = Assert.Throws<Refusal>(() => ChordProFile.Read(bench.FileOf("large.cho", [.. most, (byte)'\n'])));
        Assert.Equal((Refusal.Why.NotASong, "large.cho is too large to be a song."), (tooLarge.Reason, tooLarge.Sentence));

        // Nothing but what says what the song is, and remarks: no words.
        var empty = Assert.Throws<Refusal>(() => ChordProFile.Read(bench.FileOf("empty.cho", "{title: Nothing}\n# only this\n\n"u8.ToArray())));
        Assert.Equal((Refusal.Why.NotASong, "empty.cho has no words in it."), (empty.Reason, empty.Sentence));
        Assert.Equal("nothing.cho has no words in it.", Assert.Throws<Refusal>(() => ChordProFile.Read(bench.FileOf("nothing.cho", []))).Sentence);
    }

    // ---- Writing

    // The page's test (rule 19): written from a song and read again, a file gives the
    // same parts, lines and chords. So it does of every test song whose groups are all
    // named as parts are.
    [Theory]
    [InlineData("Amazing Grace.pro")]
    [InlineData("It Is Well.pro")]
    [InlineData("Plain Song.pro")]
    [InlineData("Rock Of Ages (Toplady).pro")]
    [InlineData("Blank Slides.pro")]
    [InlineData("Amazing Grace (Chords).pro")]
    public void ASongWrittenAsChordProAndReadAgainHasTheSamePartsLinesAndChords(string file)
    {
        var song = new OpenSong(InTestData(file));
        var text = ChordProFile.TextOf(song.Name, song.Key, song.TextBoxes);
        var back = ChordProFile.Read(bench.FileOf("written.cho", Encoding.UTF8.GetBytes(text)));

        Assert.Equal(song.Name, back.Title);
        Assert.Equal(song.Key, back.Key);
        var groups = GroupsOf(song.TextBoxes);
        Assert.True(groups.Count > 0);
        Assert.Equal(groups.Select(group => group.Name), back.Sections.Select(part => part.Name));
        for (var n = 0; n < groups.Count; n++)
        {
            Assert.Equal(groups[n].Lines, back.Sections[n].Lines.Select(Said));
        }
        Assert.Equal(song.UsedChords.Count, back.Sections.Sum(part => part.Lines.Sum(line => line.Chords.Count)));
    }

    [Fact]
    public void ASongIsWrittenWithAHeadingAndEachGroupUnderItsNameAndAnEmptyLineBetweenSlides()
    {
        var song = new OpenSong(InTestData("Plain Song.pro"));
        Assert.Equal(
            """
            {title: Plain Song}
            {key: G}

            {comment: Verse 1}
            [G]One two three [D]four
            Five six [Em]seven eight

            [C]Nine ten e[G/B]leven twelve
            Thirteen

            {comment: Chorus}
            [C]Sing it [G]out
            [D]Sing it loud

            {comment: Outro}
            [G] [D/F#] [Em]

            """.ReplaceLineEndings("\n"),
            ChordProFile.TextOf(song.Name, song.Key, song.TextBoxes));

        // A song whose first slides are in no group: they are parted from the heading
        // as a group's are, by one empty line, and from each other by one.
        TextBox Loose(int slide, string words) => new(slide, "", null, false, "id", words, [], false);
        Assert.Equal(
            "{title: Loose}\n\nOne\nTwo\n\nThree\n\n{comment: Chorus}\nFour\n",
            ChordProFile.TextOf("Loose", "", [Loose(0, "One\nTwo"), Loose(1, "Three"), new TextBox(2, "Chorus", null, true, "id", "Four", [], false)]));

        // A song that states no key has no line for one; and what is in a heading is on
        // one line and cannot end it.
        var keyless = new OpenSong(InTestData("Holy Holy Holy.pro"));
        var written = ChordProFile.TextOf("Holy {Holy}\nHoly", keyless.Key, keyless.TextBoxes);
        Assert.StartsWith("{title: Holy (Holy) Holy}\n\n{comment: Verse 1}\n", written, StringComparison.Ordinal);
        Assert.DoesNotContain("{key", written, StringComparison.Ordinal);
    }

    // Where the page's test does not hold, and why: said, and held as it is.
    [Fact]
    public void AGroupNotNamedAsAPartIsAndSlidesInNoGroupReadBackAsMoreOfThePartBeforeThem()
    {
        // "The Middle Eight" is no part's name, so its {comment} begins no part (rule 3):
        // its line is read as the second chorus's.
        var minor = new OpenSong(InTestData("A Song - In A Minor Key.pro"));
        Assert.Equal(["Chorus", "Chorus 2", "The Middle Eight"], GroupsOf(minor.TextBoxes).Select(group => group.Name));
        var text = ChordProFile.TextOf(minor.Name, minor.Key, minor.TextBoxes);
        Assert.Contains("\n{comment: The Middle Eight}\n[Bm7]Named by its [C#7sus4]label\n", text, StringComparison.Ordinal);
        var back = Chords.ParseSong(text);
        Assert.Equal(["Chorus", "Chorus 2"], back.Sections.Select(part => part.Name));
        Assert.Equal(2, back.Sections[1].Lines.Count);
        // Nothing of the song is lost by it: every line and every chord is there.
        Assert.Equal(GroupsOf(minor.TextBoxes).SelectMany(group => group.Lines), back.Sections.SelectMany(part => part.Lines.Select(Said)));
        Assert.Equal((minor.Name, minor.Key), (back.Title, back.Key));

        // Holy Holy Holy's last two slides are in no group: with no name over them and
        // only an empty line before, they are read as more of Verse 2 (rule 5).
        var holy = new OpenSong(InTestData("Holy Holy Holy.pro"));
        Assert.Equal(["Verse 1", "Verse 2", ""], GroupsOf(holy.TextBoxes).Select(group => group.Name));
        var again = Chords.ParseSong(ChordProFile.TextOf(holy.Name, holy.Key, holy.TextBoxes));
        Assert.Equal(["Verse 1", "Verse 2"], again.Sections.Select(part => part.Name));
        Assert.Equal(GroupsOf(holy.TextBoxes).SelectMany(group => group.Lines), again.Sections.SelectMany(part => part.Lines.Select(Said)));
    }

    [Fact]
    public void AFileIsWrittenWhereItIsSaidToBeAsUtf8WithNoMarkAndChangesNothingElse()
    {
        var song = bench.Open("Amazing Grace.pro");
        var songWas = File.ReadAllBytes(song.Path);
        var text = ChordProFile.TextOf(song.Name, song.Key, song.TextBoxes);
        var path = Path.Combine(bench.Folder, "Amazing Grace" + ChordProFile.Ending);
        Assert.EndsWith(".cho", path, StringComparison.Ordinal);
        ChordProFile.Write(path, text + "{c: caf" + (char)0xE9 + "}\n");
        var written = File.ReadAllBytes(path);
        Assert.False(written.AsSpan().StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]));
        Assert.Equal(text + "{c: caf" + (char)0xE9 + "}\n", Encoding.UTF8.GetString(written));
        // The song it was written from is as it was, and no copy of it was asked for.
        Assert.True(File.ReadAllBytes(song.Path).AsSpan().SequenceEqual(songWas));
        Assert.Empty(bench.Copies);
        // A file that cannot be written says so.
        var refused = Assert.Throws<Refusal>(() => ChordProFile.Write(Path.Combine(bench.Folder, "No such folder", "x.cho"), text));
        Assert.Equal(Refusal.Why.NotSaved, refused.Reason);
        Assert.StartsWith("x.cho could not be written: ", refused.Sentence, StringComparison.Ordinal);
    }
}
