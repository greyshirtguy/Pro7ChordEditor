// A new song made from a ChordPro file and a song that is known to be good
// (NewSong.cs). spec/09-chordpro-files.md, rules 8 to 16.
//
// WHAT CAN BE PROVED HERE, alone, and is:
//
//   - That a song made from each ChordPro file of testdata/songs READS, in this app,
//     as the presentation Simple Presenter made from the same file reads
//     (testdata/files/made-by-simple-presenter): its groups, its slides, its words,
//     its chords and its key. Those files are the reference's own answer, made by its
//     `build`, with the lines to a slide that tools/simple-presenter-scripts/import.qml
//     gave each. This is held whichever song it is made to look like.
//   - That a slide of the new song IS the model's slide but for its ids, its words and
//     what is said of stretches of them: everything about how it looks came across.
//     Held on the one file to hand that ProPresenter itself wrote.
//   - That nothing of the model SONG's own is in the new song.
//   - That the file is made new, whole, and never over one that is there; and not at
//     all while ProPresenter is running.
//   - That the song made is a song like any other to this app: chords can be put on
//     it, and written out as ChordPro it reads back as the file it was made from.
//
// WHAT CANNOT: that ProPresenter opens it and shows its chords on a stage screen.
// That is the maintainer's to see (spec/09, the paragraph after rule 16), on both
// systems, before this is offered to anyone.

using System.Text;
using System.Text.RegularExpressions;
using Google.Protobuf;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Tests;
using Xunit;
using RV = Pro.SerializationInterop.RVProtoData;

namespace Pro7ChordEditor.Song.Tests;

public sealed class NewSongTests : IDisposable
{
    private const string ProPresenters = "made-by-propresenter/Rock Of Ages (Toplady).pro";

    private static readonly DateTimeOffset Now = new(2026, 10, 12, 9, 30, 0, TimeSpan.Zero);

    private static readonly Regex AnId = new("[0-9A-Fa-f]{8}(-[0-9A-Fa-f]{4}){3}-[0-9A-Fa-f]{12}");

    private readonly Workbench bench = new();

    public void Dispose() => bench.Dispose();

    private static ParsedSong Song(string name) => ChordProFile.Read(Path.Combine(TestData.Root, "songs", name));

    private static ParsedSong SongOf(string text) => Chords.ParseSong(text);

    private static RV.Presentation TestFile(string name) => RV.Presentation.Parser.ParseFrom(TestData.InFiles(
        TestData.PresentationPaths().Single(path => string.Equals(path, name, StringComparison.Ordinal) || path.EndsWith("/" + name, StringComparison.Ordinal))));

    // A folder of the workbench to be a library.
    private string Library(string name = "Songs")
    {
        var folder = Path.Combine(bench.Folder, name);
        Directory.CreateDirectory(folder);
        return folder;
    }

    // The song the new one is to look like: the app's own, for no name; or a test file.
    private ModelSong Model(string testFile) => testFile.Length == 0 ? ModelSong.OfTheApp : ModelSong.Read(bench.CopyOf(testFile));

    // What the editors are handed of a song, as lines to hold against another's:
    // everything but the text boxes' ids.
    private static List<string> AsRead(OpenSong song) =>
        [.. song.TextBoxes.Select(box =>
            $"slide {box.Slide} group {Spelt.Of(box.Group)} {(box.StartsItsGroup ? "its first" : "")} colour {box.GroupColour} {Spelt.Of(box.Words)} {Spelt.Of(box.Chords)} {(box.Blank ? "blank" : "")}")];

    // The slides of a presentation in the order they are shown, each with its text
    // boxes.
    private static List<RV.Cue> CuesAsShown(RV.Presentation presentation) =>
        [.. presentation.CueGroups.SelectMany(group => group.CueIdentifiers).Select(id => presentation.Cues.Single(cue => cue.Uuid.String == id.String))];

    private static IList<RV.Slide.Types.Element> ElementsOf(RV.Cue cue) =>
        cue.Actions.Single(action => action.Slide is not null).Slide.Presentation.BaseSlide.Elements;

    // How a cue LOOKS, as text: all that it is, but for the words of its text boxes and
    // what is said of stretches of them, and with each id written as the how-manieth
    // different id of the cue it is. Two cues that give the same text are the same in
    // everything else, and have the same ids in the same places as each other.
    private static string Looks(RV.Cue cue)
    {
        var copy = cue.Clone();
        foreach (var element in copy.Actions.SelectMany(action => action.Slide?.Presentation?.BaseSlide?.Elements ?? []))
        {
            if (element.Element_?.Text is { } text)
            {
                text.RtfData = ByteString.Empty;
                text.Attributes?.CustomAttributes.Clear();
            }
        }
        var numbered = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        return AnId.Replace(
            JsonFormatter.Default.Format(copy),
            id => numbered.TryGetValue(id.Value, out var known) ? known : numbered[id.Value] = "id " + (numbered.Count + 1));
    }

    private static List<string> IdsIn(IMessage message) => [.. AnId.Matches(JsonFormatter.Default.Format(message)).Select(found => found.Value)];

    private static string Bytes(IMessage message) => Encoding.Latin1.GetString(message.ToByteArray());

    // ---- Before it is brought in (rules 8 and 9)

    [Fact]
    public void WhatWasFoundInAFileIsItsHeadingItsPartsWithTheirLinesAndItsChords()
    {
        // By hand, from the files as they are written.
        var grace = NewSong.Found(Song("amazing-grace.cho"));
        Assert.Equal(("Amazing Grace", "John Newton", "G", 40, 13), (grace.Title, grace.Artist, grace.Key, grace.Chords, grace.Lines));
        Assert.Equal([new("Verse 1", 4), new("Verse 2", 4), new("Verse 3", 4), new("Ending", 1)], grace.Parts);

        var plain = NewSong.Found(Song("plain-song.cho"));
        Assert.Equal(("Plain Song", "Nobody At All", "G", 11), (plain.Title, plain.Artist, plain.Key, plain.Chords));
        Assert.Equal([new("Verse 1", 4), new("Chorus", 2), new("Outro", 1)], plain.Parts);

        // (A key is said as a musician writes it, and a song that names no artist has none.)
        var minor = NewSong.Found(Song("minor-key.cho"));
        Assert.Equal(("A Song / In A Minor Key", "", "F#m", 9), (minor.Title, minor.Artist, minor.Key, minor.Chords));
        Assert.Equal([new("Chorus", 2), new("Chorus 2", 1), new("The Middle Eight", 1)], minor.Parts);

        // A file with no heading at all: its parts, and no more is known of it.
        var holy = NewSong.Found(Song("holy-holy-holy.txt"));
        Assert.Equal(("", "", "", 22), (holy.Title, holy.Artist, holy.Key, holy.Chords));
        Assert.Equal([new("Verse 1", 4), new("Verse 2", 4), new("", 4)], holy.Parts);

        // How many slides so many lines to a slide makes: each part by itself, its last
        // slide perhaps shorter; and fewer than one line to a slide is one.
        Assert.Equal((7, 4, 13, 13, 7, 4), (grace.Slides(2), grace.Slides(4), grace.Slides(1), grace.Slides(0), grace.Slides(3), grace.Slides(99)));
        Assert.Equal(2, NewSong.UsualLinesToASlide);

        // What states a key that is none states none; and two chords written on one
        // place are one, as they will be kept.
        var odd = NewSong.Found(SongOf("{key: H}\nVerse 1\n[G][D]One [C]two\n"));
        Assert.Equal(("", 2), (odd.Key, odd.Chords));
    }

    [Fact]
    public void TheNameASongWillHaveIsItsTitleOrItsFilesNameMadeFitToBeAFiles()
    {
        // The title; or, of a file that states none, the file's own name.
        Assert.Equal("Amazing Grace", NewSong.NameFor(Song("amazing-grace.cho"), @"C:\somewhere\amazing-grace.cho"));
        Assert.Equal("holy-holy-holy", NewSong.NameFor(Song("holy-holy-holy.txt"), @"C:\somewhere\holy-holy-holy.txt"));
        Assert.Equal("my.song", NewSong.NameFor(Song("holy-holy-holy.txt"), @"C:\somewhere\my.song.cho"));
        // A slash is a hyphen (rule 16): the test file made from this song is named so.
        Assert.Equal("A Song - In A Minor Key", NewSong.NameFor(Song("minor-key.cho"), "minor-key.cho"));

        foreach (var (given, tidied) in new[]
        {
            ("  Amazing \t Grace \n", "Amazing Grace"),
            ("...hidden", "hidden"),
            (". . Song", "Song"),
            ("", "Song"),
            ("...", "Song"),
            (" \t ", "Song"),
            ("Ends with full stops...", "Ends with full stops"),
            ("Psalm 23: The Lord", "Psalm 23- The Lord"),
            ("What? Why <this>|that*", "What- Why -this--that-"),
            (@"C:\Songs\One", "C--Songs-One"),
            ("He said \"Come\"", "He said -Come-"),
            ("Caf" + (char)0xE9 + " (live) [2] & more, 'tis", "Caf" + (char)0xE9 + " (live) [2] & more, 'tis"),
            ("Bell" + (char)0x07 + "s", "Bell-s"),
            // A name Windows keeps for a device is that device whatever follows a full
            // stop: the stop is a hyphen. With none it is left, for a number.
            ("Nul.x", "Nul-x"),
            ("COM1.5 live", "COM1-5 live"),
            ("con .y.z", "con -y.z"),
            ("Con", "Con"),
            ("Console.x", "Console.x"),
            ("A.Con.x", "A.Con.x"),
        })
        {
            Assert.Equal(tidied, NewSong.Tidied(given));
            // (Tidied once, it is tidy.)
            Assert.Equal(tidied, NewSong.Tidied(tidied));
        }
    }

    [Fact]
    public void ANameTheLibraryHasAlreadyIsGivenANumber()
    {
        var library = Library();
        Assert.Equal("Amazing Grace", NewSong.FreeName("Amazing Grace", library));
        File.WriteAllBytes(Path.Combine(library, "Amazing Grace.pro"), [1]);
        Assert.Equal("Amazing Grace 2", NewSong.FreeName("Amazing Grace", library));
        // (Windows tells no capital from a small letter in a file's name.)
        File.WriteAllBytes(Path.Combine(library, "amazing grace 2.PRO"), [1]);
        Assert.Equal("Amazing Grace 3", NewSong.FreeName("Amazing Grace", library));
        // A folder of that name is in the way as a file is.
        Directory.CreateDirectory(Path.Combine(library, "Amazing Grace 3.pro"));
        Assert.Equal("Amazing Grace 4", NewSong.FreeName("Amazing Grace", library));
        // A file of another kind is not.
        File.WriteAllBytes(Path.Combine(library, "Plain Song.cho"), [1]);
        Assert.Equal("Plain Song", NewSong.FreeName("Plain Song", library));

        // The names Windows keeps for devices cannot be a file's: such a song has a
        // number from the first.
        foreach (var device in (string[])["Con", "PRN", "aux", "Nul", "COM1", "com9", "Lpt1", "LPT0", "COM" + (char)0xB9, "lpt" + (char)0xB2, "Com" + (char)0xB3])
        {
            Assert.Equal(device + " 2", NewSong.FreeName(device, library));
        }
        foreach (var free in (string[])["Console", "Com", "COM10", "Null", "A Con", "Lpt", "Prn2", "Con 2"])
        {
            Assert.Equal(free, NewSong.FreeName(free, library));
        }
        // A name is tidied before it is looked for.
        Assert.Equal("nul-x", NewSong.FreeName(" nul.x ", library));
        Assert.Equal("Amazing Grace 4", NewSong.FreeName("  Amazing   Grace. ", library));
    }

    // ---- The song that is made (rules 10 to 15)

    // The reference's own answer. Each file with the lines to a slide the test files
    // were made with; each made to look like the song the app carries, like one that
    // Simple Presenter wrote, and like one shaped as Multitracks' are.
    [Theory]
    [InlineData("amazing-grace.cho", 2, "Amazing Grace.pro", "")]
    [InlineData("amazing-grace.cho", 2, "Amazing Grace.pro", "Plain Song.pro")]
    [InlineData("amazing-grace.cho", 2, "Amazing Grace.pro", "Chords With No Words.pro")]
    [InlineData("plain-song.cho", 2, "Plain Song.pro", "")]
    [InlineData("plain-song.cho", 2, "Plain Song.pro", "Blank Slides.pro")]
    [InlineData("it-is-well.chordpro", 4, "It Is Well.pro", "")]
    [InlineData("it-is-well.chordpro", 4, "It Is Well.pro", "Amazing Grace (Chords).pro")]
    [InlineData("minor-key.cho", 2, "A Song - In A Minor Key.pro", "")]
    [InlineData("minor-key.cho", 2, "A Song - In A Minor Key.pro", "Chords With No Words.pro")]
    [InlineData("holy-holy-holy.txt", 2, "Holy Holy Holy.pro", "")]
    [InlineData("holy-holy-holy.txt", 2, "Holy Holy Holy.pro", "It Is Well.pro")]
    public void ASongMadeFromAFileReadsAsThePresentationSimplePresenterMadeFromIt(string file, int linesToASlide, string theirs, string model)
    {
        var song = Song(file);
        var expected = new OpenSong(bench.CopyOf(theirs));
        var library = Library();

        var made = NewSong.Make(song, expected.Name, linesToASlide, Model(model), library, () => false);

        Assert.Equal(Path.Combine(library, expected.Name + ".pro"), made.Path);
        var mine = new OpenSong(made.Path);
        Assert.Equal((expected.Name, expected.Key, expected.ShownKey, expected.SlideCount), (mine.Name, mine.Key, mine.ShownKey, mine.SlideCount));
        Assert.Equal(AsRead(expected), AsRead(mine));
        // (And it is not their file that was read twice.)
        Assert.NotEqual(expected.Id, mine.Id);
        Assert.True(mine.TextBoxes.Count > 2);
        // What is said of it afterwards is what is in it.
        var found = NewSong.Found(song);
        Assert.Equal((expected.Name, found.Parts.Count, mine.SlideCount, mine.UsedChords.Count), (made.Name, made.Parts, made.Slides, made.Chords));
        Assert.Equal((found.Slides(linesToASlide), found.Chords), (made.Slides, made.Chords));
        // Nothing wants mending in a song made here.
        Assert.Equal(0, mine.Findings.ShownTwice);
        Assert.False(mine.Findings.Shift.Likely);
    }

    // Blank Slides was given its blank slides afterwards, by hand in Simple Presenter's
    // editor, where its file has a line of one chord alone. So that one is held to its
    // file: a slide for each line, and those three with their chord on a stand-in.
    [Fact]
    public void ALineOfChordsAloneIsASlideOfStandInsWithItsChordsOnThem()
    {
        var made = NewSong.Make(Song("blank-slides.cho"), "Blank Slides", 1, ModelSong.OfTheApp, Library(), () => false);
        var mine = new OpenSong(made.Path);
        var standIn = ((char)0x200B).ToString();
        Assert.Equal(
            [
                "Intro|" + standIn + "|0:G",
                "Verse 1|Amazing grace how sweet the sound|0:G 18:C",
                "Verse 1|That saved a wretch like me|",
                "Verse 1|" + standIn + "|0:G",
                "Verse 1|I once was lost but now am found|2:D",
                "Verse 1|Was blind but now I see|",
                "Ending|" + standIn + "|0:G",
            ],
            mine.TextBoxes.Select(box => box.Group + "|" + box.Words + "|" + string.Join(" ", box.Chords.Select(chord => chord.At + ":" + chord.Name))));
        Assert.Equal("G", mine.Key);

        // And the ending of Amazing Grace, four chords with no words: a stand-in for
        // each with a wide space between, and each chord written over its own alone.
        var grace = RV.Presentation.Parser.ParseFrom(File.ReadAllBytes(NewSong.Make(Song("amazing-grace.cho"), "Grace", 2, ModelSong.OfTheApp, Library(), () => false).Path));
        var ending = ElementsOf(CuesAsShown(grace)[^1]).Single().Element_.Text;
        Assert.Equal(Chords.Placeholders(4), RtfWords.Read(ending.RtfData.Span));
        Assert.Equal(
            ["0-1 C", "2-3 G/B", "4-5 D", "6-7 G"],
            ending.Attributes.CustomAttributes.Where(Reading.IsAChord).Select(chord => chord.Range.Start + "-" + chord.Range.End + " " + chord.Chord));
    }

    // Everything about how the slide looks comes across with the copy (rule 11): held
    // on the file ProPresenter wrote, whose every slide is a cue with nothing in it but
    // its slide.
    [Fact]
    public void ASlideOfTheNewSongIsTheModelsFirstSlideButForItsIdsItsWordsAndWhatIsSaidOfStretchesOfThem()
    {
        var model = TestFile(ProPresenters);
        // The first slide as the song is SHOWN, which is not the first the file has.
        var first = CuesAsShown(model)[0];
        Assert.NotSame(model.Cues[0], first);
        Assert.Equal("Rock of Ages cleft for me\nLet me hide myself in thee", RtfWords.Read(ElementsOf(first).Single().Element_.Text.RtfData.Span));
        var song = Song("amazing-grace.cho");

        var made = NewSong.Made(song, "Amazing Grace", 2, model, "", Now);

        Assert.Equal(7, made.Cues.Count);
        var modelsIds = IdsIn(model).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var slides = song.Sections.SelectMany(part => Chords.SlidesOf(part, 2)).ToList();
        foreach (var (cue, n) in CuesAsShown(made).Select((cue, n) => (cue, n)))
        {
            // The same in everything but its words, and with ids in the same places.
            Assert.Equal(Looks(first), Looks(cue));
            // Its ids are its own: four of them (the cue, its action, its slide and its
            // text box), none of them the model's, none another slide's, and written
            // in capitals as the model's are.
            var ids = IdsIn(cue).Distinct().ToList();
            Assert.Equal(4, ids.Count);
            Assert.DoesNotContain(ids, modelsIds.Contains);
            Assert.All(ids, id => Assert.True(seen.Add(id), "an id is in two slides"));
            Assert.All(ids, id => Assert.Equal(id.ToUpperInvariant(), id));
            // The link that says when the text box is seen names the box's own id, as
            // on every slide of the model.
            var element = ElementsOf(cue).Single();
            Assert.Equal(element.Element_.Uuid.String, element.DataLinks.Single().VisibilityLink.Conditions.Single().ElementVisibility.OtherElementUuid.String);

            // Its words are the slide's; and what the model says of the whole of its
            // words (that they are in capitals) is said of the whole of these.
            var words = string.Join("\n", slides[n].Select(line => line.Text));
            var text = element.Element_.Text;
            Assert.Equal(words, RtfWords.Read(text.RtfData.Span));
            var said = text.Attributes.CustomAttributes;
            Assert.Equal(
                (RV.Graphics.Types.Text.Types.Attributes.Types.Capitalization.AllCaps, 0, words.Length),
                (said[0].Capitalization, said[0].Range.Start, said[0].Range.End));
            // And its chords after that, each with the range the rules give it.
            var chords = slides[n].SelectMany((line, at) => line.Chords.Select(chord => chord with { At = chord.At + slides[n].Take(at).Sum(before => before.Text.Length + 1) }));
            Assert.Equal(
                Chords.Ranges(words, chords).Select(range => $"{range.Start}-{range.End} {range.Name}"),
                said.Skip(1).Select(chord => $"{chord.Range.Start}-{chord.Range.End} {chord.Chord}"));
            Assert.All(said.Skip(1), chord => Assert.True(Reading.IsAChord(chord)));
            // The RTF is the model's up to where its words began.
            var opening = RtfWords.WithWords(ElementsOf(first).Single().Element_.Text.RtfData.Span, "")![..^1];
            Assert.True(text.RtfData.Span.StartsWith(opening));
        }
        // The groups: one for each part, by name, with no colour of its own and no tie
        // to anything, holding its slides' ids in order (rule 13).
        Assert.Equal(["Verse 1", "Verse 2", "Verse 3", "Ending"], made.CueGroups.Select(group => group.Group.Name));
        Assert.Equal([2, 2, 2, 1], made.CueGroups.Select(group => group.CueIdentifiers.Count));
        Assert.Equal(made.Cues.Select(cue => cue.Uuid.String), made.CueGroups.SelectMany(group => group.CueIdentifiers).Select(id => id.String));
        foreach (var group in made.CueGroups)
        {
            Assert.Equal(new RV.Group { Uuid = group.Group.Uuid, Name = group.Group.Name, HotKey = new RV.HotKey() }, group.Group);
            Assert.DoesNotContain(group.Group.Uuid.String, modelsIds);
            Assert.True(seen.Add(group.Group.Uuid.String));
        }
        Assert.DoesNotContain(made.Uuid.String, modelsIds);
        Assert.True(seen.Add(made.Uuid.String));
    }

    [Fact]
    public void TheTopOfTheNewSongIsTheModelsButForWhatIsTheModelSongsOwn()
    {
        var model = TestFile(ProPresenters);
        var made = NewSong.Made(Song("amazing-grace.cho"), "Amazing Grace 2", 2, model, "", Now);

        // Rule 14: the name; the key, both parts; and the title, artist and copyright
        // where ProPresenter keeps a song's. Of the model's CCLI block only whether it
        // is shown is kept: its author and its number are another song's.
        Assert.Equal("Amazing Grace 2", made.Name);
        Assert.Equal(
            new RV.Presentation.Types.Music
            {
                Original = new RV.MusicKeyScale { MusicKey = RV.MusicKeyScale.Types.MusicKey.G },
                User = new RV.MusicKeyScale { MusicKey = RV.MusicKeyScale.Types.MusicKey.G },
            },
            made.Music);
        Assert.True(model.Ccli.Display);
        Assert.Equal(
            new RV.Presentation.Types.CCLI { SongTitle = "Amazing Grace", ArtistCredits = "John Newton", Publisher = "Public domain", Display = true },
            made.Ccli);
        Assert.Equal(Now.ToUnixTimeSeconds(), made.LastModifiedDate.Seconds);

        // Everything else at the top is the model's, to the byte: with the name, the
        // id, the date, the key, the CCLI block, the groups and the slides put back as
        // the model has them, it IS the model.
        var back = made.Clone();
        back.Name = model.Name;
        back.Uuid = model.Uuid;
        back.LastModifiedDate = model.LastModifiedDate;
        back.Music = model.Music;
        back.Ccli = model.Ccli;
        back.CueGroups.Clear();
        back.CueGroups.AddRange(model.CueGroups);
        back.Cues.Clear();
        back.Cues.AddRange(model.Cues);
        Assert.Equal(model, back);
        Assert.Equal(model.ToByteArray(), back.ToByteArray());

        // And none of the model's words are in it anywhere.
        var bytes = Bytes(made);
        Assert.Contains("Rock of Ages", Bytes(model), StringComparison.OrdinalIgnoreCase);
        foreach (var theirs in (string[])["Rock", "Toplady", "Hastings", "cleft", "Nothing in my hand"])
        {
            Assert.DoesNotContain(theirs, bytes, StringComparison.OrdinalIgnoreCase);
        }
        Assert.Contains("mazing grace how sweet the sound", bytes, StringComparison.Ordinal);
    }

    // A model song with everything of its own that a song can have, and a first slide
    // that does things and has more than one text box.
    private static RV.Presentation AModelWithMuchOfItsOwn()
    {
        var model = TestFile(ProPresenters);
        model.Notes = "Notes of the model";
        model.Category = "Hymn";
        model.ChordChart = new RV.URL { Platform = RV.URL.Types.Platform.Macos, AbsoluteString = "file:///charts/chart-of-the-model.pdf" };
        var arrangement = new RV.Presentation.Types.Arrangement { Uuid = new RV.UUID { String = "AAAAAAAA-0000-4000-8000-00000000000A" }, Name = "Arrangement of the model" };
        arrangement.GroupIdentifiers.Add(model.CueGroups[0].Group.Uuid);
        model.Arrangements.Add(arrangement);
        model.SelectedArrangement = arrangement.Uuid.Clone();
        model.Timeline = new RV.Presentation.Types.Timeline { Duration = 99, Loop = true };
        model.LastDateUsed = new RV.Timestamp { Seconds = 1_700_000_000 };
        model.BibleReference = new RV.Presentation.Types.BibleReference { BookName = "Book of the model" };
        model.MultiTracksLicensing = new RV.Presentation.Types.MultiTracksLicensing { SongIdentifier = 7, CustomerIdentifier = "Customer of the model" };
        model.MusicKey = "Eb";
        model.Music = new RV.Presentation.Types.Music
        {
            Original = new RV.MusicKeyScale { MusicKey = RV.MusicKeyScale.Types.MusicKey.EFlat },
            User = new RV.MusicKeyScale { MusicKey = RV.MusicKeyScale.Types.MusicKey.F },
            OriginalMusicKey = "Key of the model",
        };
        model.SlideShowDuration = 5;
        model.Ccli.Album = "Album of the model";
        model.Ccli.Artwork = ByteString.CopyFromUtf8("Artwork of the model");
        model.Ccli.CopyrightYear = 1776;
        model.Ccli.Publisher = "Publisher of the model";
        model.Ccli.ArtistCredits = "Artist of the model";

        var cue = CuesAsShown(model)[0];
        cue.Name = "Cue of the model";
        cue.CompletionTargetType = RV.Cue.Types.CompletionTargetType.Next;
        cue.CompletionTargetUuid = new RV.UUID { String = "AAAAAAAA-0000-4000-8000-00000000000B" };
        cue.CompletionActionType = RV.Cue.Types.CompletionActionType.AfterTime;
        cue.CompletionActionUuid = new RV.UUID { String = "AAAAAAAA-0000-4000-8000-00000000000C" };
        cue.CompletionTime = 10;
        cue.TriggerTime = new RV.Cue.Types.TimecodeTime { Time = 3 };
        cue.HotKey = new RV.HotKey { ControlIdentifier = "Key of the model" };
        cue.PendingImports.Add(new RV.Cue.Types.PendingImportsEntry { Key = "Import of the model" });
        // Something the slide starts, before its slide among the cue's actions.
        cue.Actions.Insert(0, new RV.Action
        {
            Uuid = new RV.UUID { String = "AAAAAAAA-0000-4000-8000-00000000000D" },
            Name = "Macro of the model",
            IsEnabled = true,
            Type = RV.Action.Types.ActionType.Clear,
        });
        var action = cue.Actions[1];
        action.Label = new RV.Action.Types.Label { Text = "Label of the model", Color = new RV.Color { Red = 1, Alpha = 1 } };
        action.DelayTime = 3;
        var shown = action.Slide.Presentation;
        shown.Notes = new RV.PresentationSlide.Types.Notes { RtfData = ByteString.CopyFromUtf8("Slide notes of the model") };
        shown.ChordChart = new RV.URL { Platform = RV.URL.Types.Platform.Macos, AbsoluteString = "file:///charts/slide-chart-of-the-model.pdf" };

        var elements = shown.BaseSlide.Elements;
        var words = elements[0];
        RV.Slide.Types.Element Another(string id, string name, string says)
        {
            var box = words.Clone();
            box.Element_.Uuid = new RV.UUID { String = id };
            box.Element_.Name = name;
            box.DataLinks.Clear();
            box.Element_.Text.RtfData = ByteString.CopyFrom(RtfWords.WithWords(words.Element_.Text.RtfData.Span, says)!);
            box.Element_.Text.Attributes.CustomAttributes.Clear();
            return box;
        }
        RV.Graphics.Types.Text.Types.Attributes.Types.CustomAttribute Said(int start, int end, string? chord = null) => chord is null
            ? new() { Range = new RV.IntRange { Start = start, End = end }, Capitalization = RV.Graphics.Types.Text.Types.Attributes.Types.Capitalization.SmallCaps }
            : new() { Range = new RV.IntRange { Start = start, End = end }, Chord = chord };

        // On the box of words: something said of only some of the words, after the
        // first; and a chord of the model's.
        words.Element_.Text.Attributes.CustomAttributes.Add(Said(5, 9));
        words.Element_.Text.Attributes.CustomAttributes.Add(Said(0, 4, "Chord of the model"));
        // (And something said of no stretch at all, at the start.)
        words.Element_.Text.Attributes.CustomAttributes.Add(Said(0, 0));
        // A second text box with words of its own, and a chord.
        var second = Another("AAAAAAAA-0000-4000-8000-000000000001", "Second language", "Zweite Sprache of the model");
        second.Element_.Text.Attributes.CustomAttributes.Add(Said(0, 27));
        second.Element_.Text.Attributes.CustomAttributes.Add(Said(0, 6, "Am"));
        elements.Add(second);
        // One that shows something else, by a link.
        var linked = Another("AAAAAAAA-0000-4000-8000-000000000002", "Clock", "Linked words of the model");
        linked.DataLinks.Add(new RV.Slide.Types.Element.Types.DataLink { ClockText = new RV.Slide.Types.Element.Types.DataLink.Types.ClockText { ClockFormatString = "HH:mm" } });
        linked.Element_.Text.Attributes.CustomAttributes.Add(Said(0, 25));
        elements.Add(linked);
        // A shape, which comes on by a build that names it.
        var shape = new RV.Slide.Types.Element
        {
            Element_ = new RV.Graphics.Types.Element { Uuid = new RV.UUID { String = "AAAAAAAA-0000-4000-8000-000000000003" }, Name = "A shape", Opacity = 0.5 },
            BuildIn = new RV.Slide.Types.Element.Types.Build
            {
                Uuid = new RV.UUID { String = "AAAAAAAA-0000-4000-8000-000000000004" },
                ElementUUID = new RV.UUID { String = "AAAAAAAA-0000-4000-8000-000000000003" },
            },
            // (How it goes off names it in small letters: the same id.)
            BuildOut = new RV.Slide.Types.Element.Types.Build
            {
                Uuid = new RV.UUID { String = "AAAAAAAA-0000-4000-8000-000000000006" },
                ElementUUID = new RV.UUID { String = "aaaaaaaa-0000-4000-8000-000000000003" },
            },
            ChildBuilds = { new RV.Slide.Types.Element.Types.ChildBuild { Uuid = new RV.UUID { String = "AAAAAAAA-0000-4000-8000-000000000007" }, Index = 1 } },
        };
        elements.Add(shape);
        shown.BaseSlide.ElementBuildOrder.Add(new RV.UUID { String = "AAAAAAAA-0000-4000-8000-000000000004" });
        // A text box with nothing in it but a stray space, as files are full of, which
        // has a chord all the same.
        var stray = Another("AAAAAAAA-0000-4000-8000-000000000005", "Stray", " ");
        stray.Element_.Text.Attributes.CustomAttributes.Add(Said(0, 1));
        stray.Element_.Text.Attributes.CustomAttributes.Add(Said(0, 1, "Em"));
        elements.Add(stray);
        // And a link from the words' box to something that is not on the slide.
        words.DataLinks.Add(new RV.Slide.Types.Element.Types.DataLink
        {
            VisibilityLink = new RV.Slide.Types.Element.Types.DataLink.Types.VisibilityLink
            {
                Conditions =
                {
                    new RV.Slide.Types.Element.Types.DataLink.Types.VisibilityLink.Types.Condition
                    {
                        TimerVisibility = new RV.Slide.Types.Element.Types.DataLink.Types.VisibilityLink.Types.Condition.Types.TimerVisibility
                        {
                            TimerUuid = new RV.UUID { String = "AAAAAAAA-0000-4000-8000-00000000000E" },
                        },
                    },
                },
            },
        });
        return model;
    }

    [Fact]
    public void WhatTheModelSongSaysAndDoesIsNotInTheNewSongAndASlideIsCopiedAsItLooks()
    {
        var model = AModelWithMuchOfItsOwn();
        // (As any song, it reads; and its first slide is handed over as three boxes:
        // the two with words, and the one with a chord on its stray space.)
        Assert.Equal(3, Reading.TextBoxes(0, model, Reading.SlideRows(model)).Count);
        // A file that says nothing of what the song is, nor of its key.
        var song = SongOf("Verse 1\n[G]One [D]two\nThree\nFour [C]five\n\nChorus\nSix seven [Em]eight\n");

        var made = NewSong.Made(song, "New", 2, model, "Model", Now);

        // ---- The top.
        Assert.Equal("", made.Notes);
        Assert.Equal(new RV.URL { Platform = RV.URL.Types.Platform.Macos }, made.ChordChart);
        Assert.Empty(made.Arrangements);
        Assert.Null(made.SelectedArrangement);
        Assert.Equal(new RV.Presentation.Types.Timeline(), made.Timeline);
        Assert.Null(made.LastDateUsed);
        Assert.Null(made.BibleReference);
        Assert.Null(made.MultiTracksLicensing);
        Assert.Equal("", made.MusicKey);
        Assert.Equal(RV.Presentation.SlideShowOneofCase.None, made.SlideShowCase);
        // The file states no key: the model's is not kept.
        Assert.Null(made.Music);
        // It states no title, artist, copyright or number: the block has only whether
        // it is shown, which is how the model's songs are.
        Assert.Equal(new RV.Presentation.Types.CCLI { Display = true }, made.Ccli);
        // What is not the song's own is kept: what kind of thing it is, and how it looks.
        Assert.Equal("Hymn", made.Category);
        Assert.Equal((model.ApplicationInfo, model.Background, model.Transition, model.ContentDestination), (made.ApplicationInfo, made.Background, made.Transition, made.ContentDestination));

        // ---- Each slide.
        Assert.Equal(["Verse 1", "Chorus"], made.CueGroups.Select(group => group.Group.Name));
        var cues = CuesAsShown(made);
        Assert.Equal(3, cues.Count);
        var first = CuesAsShown(model)[0];
        var modelsIds = IdsIn(model).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] wanted = ["One two\nThree", "Four five", "Six seven eight"];
        foreach (var (cue, n) in cues.Select((cue, n) => (cue, n)))
        {
            // It does nothing but be shown: of the cue's actions only its slide; no
            // name, no going on by itself, no key, nothing waiting to be brought in.
            var action = cue.Actions.Single();
            Assert.Equal(RV.Action.Types.ActionType.PresentationSlide, action.Type);
            Assert.Equal(
                ("", RV.Cue.Types.CompletionTargetType.None, RV.Cue.Types.CompletionActionType.First, 0.0, true),
                (cue.Name, cue.CompletionTargetType, cue.CompletionActionType, cue.CompletionTime, cue.IsEnabled));
            Assert.Null(cue.CompletionTargetUuid);
            Assert.Null(cue.CompletionActionUuid);
            Assert.Null(cue.TriggerTime);
            Assert.Equal(new RV.HotKey(), cue.HotKey);
            Assert.Empty(cue.PendingImports);
            // Its label says nothing and has no colour, as a slide with none is written.
            Assert.Equal(new RV.Action.Types.Label { Color = new RV.Color() }, action.Label);
            Assert.Equal(0.0, action.DelayTime);
            Assert.True(action.IsEnabled);
            var shown = action.Slide.Presentation;
            Assert.Null(shown.Notes);
            Assert.Equal(new RV.URL { Platform = RV.URL.Types.Platform.Macos }, shown.ChordChart);

            // Its slide has the model's five elements, each as it looks.
            var elements = shown.BaseSlide.Elements;
            var theirs = first.Actions[1].Slide.Presentation.BaseSlide.Elements;
            Assert.Equal(["Default", "Second language", "Clock", "A shape", "Stray"], elements.Select(element => element.Element_.Name));
            var slideAlone = new RV.Cue { Actions = { action } };
            var theirsAlone = new RV.Cue { Actions = { first.Actions[1].Clone() } };
            theirsAlone.Actions[0].Label = new RV.Action.Types.Label { Color = new RV.Color() };
            theirsAlone.Actions[0].DelayTime = 0;
            theirsAlone.Actions[0].Slide.Presentation.Notes = null;
            theirsAlone.Actions[0].Slide.Presentation.ChordChart = new RV.URL { Platform = RV.URL.Types.Platform.Macos };
            Assert.Equal(Looks(theirsAlone), Looks(slideAlone));

            // The words go into the first text box. What was said of the whole of the
            // model's words is said of the whole of these; what was said of some of
            // them is gone, and so is the model's chord.
            var text = elements[0].Element_.Text;
            Assert.Equal(wanted[n], RtfWords.Read(text.RtfData.Span));
            var said = text.Attributes.CustomAttributes;
            Assert.Equal(
                (RV.Graphics.Types.Text.Types.Attributes.Types.Capitalization.AllCaps, 0, wanted[n].Length),
                (said[0].Capitalization, said[0].Range.Start, said[0].Range.End));
            Assert.All(said.Skip(1), chord => Assert.True(Reading.IsAChord(chord)));
            Assert.Equal(
                n switch { 0 => "0:G 4:D", 1 => "5:C", _ => "10:Em" },
                string.Join(" ", said.Skip(1).Select(chord => chord.Range.Start + ":" + chord.Chord)));

            // The second text box has no words now, nor anything said of any, and its
            // RTF is its own up to where its words began.
            var second = elements[1].Element_.Text;
            Assert.Equal("", RtfWords.Read(second.RtfData.Span));
            Assert.Empty(second.Attributes.CustomAttributes);
            Assert.Equal(RtfWords.WithWords(theirs[1].Element_.Text.RtfData.Span, "")!, second.RtfData.ToByteArray());
            // The box that shows a clock is exactly as it was, but for its id.
            var clock = elements[2].Clone();
            Assert.NotEqual(theirs[2].Element_.Uuid, clock.Element_.Uuid);
            clock.Element_.Uuid = theirs[2].Element_.Uuid;
            Assert.Equal(theirs[2], clock);
            // The box with a stray space keeps it, and what was said of it; its chord is gone.
            var stray = elements[4].Element_.Text;
            Assert.Equal(theirs[4].Element_.Text.RtfData, stray.RtfData);
            Assert.Equal(RV.Graphics.Types.Text.Types.Attributes.Types.Capitalization.SmallCaps, stray.Attributes.CustomAttributes.Single().Capitalization);

            // The ids. New: the cue's, its action's, its slide's, each of the five
            // elements', and each of the shape's three builds'. The builds still name
            // the shape (the one that named it in small letters too), and the order of
            // builds still names the build. What names a thing that is not on the
            // slide (a timer) is left.
            var shape = elements[3];
            Assert.Equal(shape.Element_.Uuid, shape.BuildIn.ElementUUID);
            Assert.Equal(shape.Element_.Uuid, shape.BuildOut.ElementUUID);
            Assert.Equal(shape.BuildIn.Uuid, shown.BaseSlide.ElementBuildOrder.Single());
            Assert.Equal(3, new[] { shape.BuildIn.Uuid.String, shape.BuildOut.Uuid.String, shape.ChildBuilds.Single().Uuid.String }.Distinct().Count());
            Assert.Equal(elements[0].Element_.Uuid, elements[0].DataLinks[0].VisibilityLink.Conditions.Single().ElementVisibility.OtherElementUuid);
            Assert.Equal("AAAAAAAA-0000-4000-8000-00000000000E", elements[0].DataLinks[1].VisibilityLink.Conditions.Single().TimerVisibility.TimerUuid.String);
            var ids = IdsIn(cue).Distinct().Where(id => id != "AAAAAAAA-0000-4000-8000-00000000000E").ToList();
            Assert.Equal(11, ids.Count);
            Assert.DoesNotContain(ids, modelsIds.Contains);
            Assert.All(ids, id => Assert.True(seen.Add(id), "an id is in two slides"));
        }

        // ---- And nothing the model song says is in it, anywhere; but what the linked
        // box holds, which is not shown and was left as it was.
        var bytes = Bytes(made);
        var models = Bytes(model);
        foreach (var theirs in (string[])
            ["Notes of the model", "chart-of-the-model", "Arrangement of the model", "Book of the model", "Customer of the model", "Key of the model",
             "Album of the model", "Artwork of the model", "Publisher of the model", "Artist of the model", "Toplady", "Cue of the model", "Import of the model",
             "Macro of the model", "Label of the model", "Slide notes of the model", "Zweite", "Chord of the model", "Rock of Ages"])
        {
            Assert.Contains(theirs, models, StringComparison.Ordinal);
            Assert.DoesNotContain(theirs, bytes, StringComparison.Ordinal);
        }
        Assert.Contains("Linked words of the model", bytes, StringComparison.Ordinal);
        // It reads, in this app, as the song that was asked for and nothing more.
        var path = bench.FileOf("New.pro", made.ToByteArray());
        Assert.Equal(
            ["Verse 1|One two\nThree|0:G 4:D", "Verse 1|Four five|5:C", "Chorus|Six seven eight|10:Em"],
            new OpenSong(path).TextBoxes.Select(box => box.Group + "|" + box.Words + "|" + string.Join(" ", box.Chords.Select(chord => chord.At + ":" + chord.Name))));
    }

    [Fact]
    public void TheSlideCopiedIsTheFirstShownThatHasWordsToReadOrFailingThatALineOfChordsAlone()
    {
        var song = SongOf("Verse 1\n[G]One two\n");
        string NameOfTheBox(RV.Presentation made) => ElementsOf(made.Cues.Single()).First(element => Reading.HasText(element)).Element_.Name;
        double OpacityOfTheBox(RV.Presentation made) => ElementsOf(made.Cues.Single()).First(element => Reading.HasText(element)).Element_.Opacity;

        // Each slide of the model told from the others by how far its text box can be
        // seen through: the copy is of the first as the song is SHOWN.
        var model = TestFile(ProPresenters);
        foreach (var (cue, n) in model.Cues.Select((cue, n) => (cue, n)))
        {
            ElementsOf(cue).Single().Element_.Opacity = 0.5 + (n / 100.0);
        }
        var first = CuesAsShown(model)[0];
        var which = model.Cues.IndexOf(first);
        Assert.True(which > 0);
        Assert.Equal(0.5 + (which / 100.0), OpacityOfTheBox(NewSong.Made(song, "New", 2, model, "Model", Now)));

        // A first slide that is blank is passed over (Blank Slides begins with one, an
        // intro), and so is one that is a line of chords alone, while a slide with
        // words to read comes after: Chords With No Words begins with a chord on a
        // stray space.
        foreach (var testFile in (string[])["Blank Slides.pro", "Chords With No Words.pro"])
        {
            var like = TestFile(testFile);
            var rows = Reading.SlideRows(like);
            var firstWithWords = Enumerable.Range(0, rows.Count).First(slide => Reading.TextBoxes(slide, like, rows).Any(box => box.Words.Any(char.IsLetter)));
            Assert.True(firstWithWords > 0, testFile);
            foreach (var (cue, n) in like.Cues.Select((cue, n) => (cue, n)))
            {
                foreach (var element in ElementsOf(cue))
                {
                    element.Element_.Opacity = 0.5 + (n / 100.0);
                }
            }
            Assert.Equal(0.5 + (rows[firstWithWords].Cue / 100.0), OpacityOfTheBox(NewSong.Made(song, "New", 2, like, "Model", Now)));
        }

        // A model with no words to read anywhere, but a line of chords alone: that
        // slide is the one.
        var chordsAlone = TestFile("Plain Song.pro");
        var outro = CuesAsShown(chordsAlone)[^1];
        Assert.Equal(Chords.Placeholders(3), RtfWords.Read(ElementsOf(outro).Single().Element_.Text.RtfData.Span));
        var only = new RV.Presentation { Name = "Only chords" };
        only.Cues.Add(outro.Clone());
        ElementsOf(only.Cues[0]).Single().Element_.Name = "The outro's box";
        var fromChordsAlone = NewSong.Made(song, "New", 2, only, "Only chords", Now);
        Assert.Equal("The outro's box", NameOfTheBox(fromChordsAlone));
        Assert.Equal("One two", RtfWords.Read(ElementsOf(fromChordsAlone.Cues.Single()).Single().Element_.Text.RtfData.Span));

        // A slide of chords alone is passed over for one with words to read that
        // comes after it.
        var introFirst = new RV.Presentation { Name = "Intro first" };
        introFirst.Cues.Add(outro.Clone());
        introFirst.Cues.Add(CuesAsShown(chordsAlone)[0].Clone());
        ElementsOf(introFirst.Cues[0]).Single().Element_.Name = "The intro's box";
        ElementsOf(introFirst.Cues[1]).Single().Element_.Name = "The verse's box";
        Assert.Equal("The verse's box", NameOfTheBox(NewSong.Made(song, "New", 2, introFirst, "Intro first", Now)));

        // A text box that shows something else by a link is never the one given the
        // words, though it comes first on the slide and has words in it.
        var linkedFirst = TestFile("Plain Song.pro");
        foreach (var cue in linkedFirst.Cues)
        {
            var linked = ElementsOf(cue).Single().Clone();
            linked.Element_.Uuid = new RV.UUID { String = OpenSong.NewId("A") };
            linked.Element_.Name = "Shows a clock";
            linked.DataLinks.Add(new RV.Slide.Types.Element.Types.DataLink { ClockText = new RV.Slide.Types.Element.Types.DataLink.Types.ClockText() });
            ElementsOf(cue).Insert(0, linked);
        }
        var besideALinkedBox = NewSong.Made(song, "New", 2, linkedFirst, "Plain Song", Now);
        var boxes = ElementsOf(besideALinkedBox.Cues.Single());
        Assert.Equal(["Shows a clock", "Lyrics"], boxes.Select(box => box.Element_.Name));
        Assert.Equal("One two", RtfWords.Read(boxes[1].Element_.Text.RtfData.Span));
        Assert.Equal(RtfWords.Read(ElementsOf(CuesAsShown(linkedFirst)[0])[0].Element_.Text.RtfData.Span), RtfWords.Read(boxes[0].Element_.Text.RtfData.Span));

        // A model whose slide has no id is copied all the same, and each copy has one,
        // by which its group names it.
        var noId = TestFile("Plain Song.pro");
        foreach (var cue in noId.Cues)
        {
            cue.Uuid = null;
        }
        noId.CueGroups.Clear();
        var withIds = NewSong.Made(SongOf("Verse 1\nOne\nTwo\nThree\n"), "New", 2, noId, "Plain Song", Now);
        Assert.Equal(2, withIds.Cues.Select(cue => cue.Uuid.String).Where(id => id.Length == 36).Distinct().Count());
        Assert.Equal(withIds.Cues.Select(cue => cue.Uuid.String), withIds.CueGroups.Single().CueIdentifiers.Select(id => id.String));

        // And a model with no chord chart of its own is given none.
        Assert.Null(only.ChordChart);
        Assert.Null(fromChordsAlone.ChordChart);
        Assert.Null(fromChordsAlone.Timeline);

        // And a model with no text at all cannot be one: refused, and it says which.
        var none = new RV.Presentation { Name = "Nothing" };
        none.Cues.Add(new RV.Cue { IsEnabled = true });
        var refused = Assert.Throws<Refusal>(() => NewSong.Made(song, "New", 2, none, "Nothing", Now));
        Assert.Equal(Refusal.Why.NotMade, refused.Reason);
        Assert.Equal((char)0x201C + "Nothing" + (char)0x201D + " has no slide with words on it, so a new song cannot be made to look like it. Nothing was made.", refused.Sentence);
        Assert.StartsWith("The song this app carries has no slide with words on it, ", Assert.Throws<Refusal>(() => NewSong.Made(song, "New", 2, none, "", Now)).Sentence, StringComparison.Ordinal);
        // (A song whose only slide is blank is such a one.)
        var blank = TestFile("Blank Slides.pro");
        var justTheIntro = new RV.Presentation { Name = "Blank" };
        justTheIntro.Cues.Add(CuesAsShown(blank)[0].Clone());
        Assert.Equal(Refusal.Why.NotMade, Assert.Throws<Refusal>(() => NewSong.Made(song, "New", 2, justTheIntro, "Blank", Now)).Reason);
    }

    [Fact]
    public void TheKeyAndWhatTheSongIsAreWrittenWhereProPresenterKeepsThem()
    {
        var model = TestFile("Plain Song.pro");
        RV.Presentation Of(string heading, RV.Presentation? like = null) =>
            NewSong.Made(SongOf(heading + "\nVerse 1\n[G]One two\n"), "New", 2, like ?? model, "Plain Song", Now);

        // The key, both parts; a minor one; and one that is none states none, though
        // the model song states its own.
        Assert.Equal("G", Reading.KeyName(model.Music.Original));
        var flat = Of("{key: Bb}").Music;
        Assert.Equal((RV.MusicKeyScale.Types.MusicKey.BFlat, RV.MusicKeyScale.Types.MusicScale.Major), (flat.Original.MusicKey, flat.Original.MusicScale));
        Assert.Equal(flat.Original, flat.User);
        Assert.NotSame(flat.Original, flat.User);
        var minor = Of("{key: f#m}").Music;
        Assert.Equal((RV.MusicKeyScale.Types.MusicKey.FSharp, RV.MusicKeyScale.Types.MusicScale.Minor), (minor.User.MusicKey, minor.User.MusicScale));
        Assert.Equal(minor.Original, minor.User);
        Assert.Null(Of("{key: H}").Music);
        Assert.Null(Of("").Music);

        // The title, the artist and the copyright; and the number, if it is one.
        Assert.Equal(
            new RV.Presentation.Types.CCLI { SongTitle = "A Title", ArtistCredits = "An Artist", Publisher = "1779 Somebody", SongNumber = 22025 },
            Of("{title: A Title}\n{artist: An Artist}\n{copyright: 1779 Somebody}\n{ccli: 22025}").Ccli);
        Assert.Equal(new RV.Presentation.Types.CCLI { SongTitle = "A Title" }, Of("{title: A Title}\n{ccli: CCLI 22025}").Ccli);
        Assert.Equal(0u, Of("{ccli: 99999999999}").Ccli.SongNumber);
        Assert.Equal(0u, Of("{ccli: -5}").Ccli.SongNumber);
        Assert.Equal(0u, Of("{ccli: +22025}").Ccli.SongNumber);
        Assert.Equal(4294967295u, Of("{ccli: 4294967295}").Ccli.SongNumber);
        // A file that says none of them: nothing of the model's is left in the block,
        // which is there because the model has one.
        Assert.Equal(new RV.Presentation.Types.CCLI { SongTitle = "Plain Song", ArtistCredits = "Nobody At All" }, model.Ccli);
        Assert.Equal(new RV.Presentation.Types.CCLI(), Of("").Ccli);
        // And made like a song that has no such block, it has none.
        var withNone = TestFile("Holy Holy Holy.pro");
        Assert.Null(withNone.Ccli);
        Assert.Null(Of("", withNone).Ccli);
        Assert.Equal(new RV.Presentation.Types.CCLI { ArtistCredits = "An Artist" }, Of("{artist: An Artist}", withNone).Ccli);
    }

    [Fact]
    public void WordsOfEveryAwkwardKindAreTheSlidesWordsWithTheirChordsWhereTheyWere()
    {
        var eAcute = (char)0xE9;
        var apostrophe = (char)0x2019;
        var party = new string([(char)0xD83C, (char)0xDF89]);
        var text = "Verse 1\n"
            + "[G]Caf" + eAcute + " au [D]lait, it" + apostrophe + "s here\n"
            + party + " [C]Joy " + party + " [G]to the world\n"
            + "a " + (char)0x5C + " b\twith a [Em]tab\n"
            + (char)0x9999 + (char)0x6E2F + " [Am]" + (char)0x9999 + "\n";
        var song = SongOf(text);
        var lines = song.Sections.Single().Lines;
        Assert.Equal(4, lines.Count);

        foreach (var model in (string[])["", "Amazing Grace.pro", "Chords With No Words.pro"])
        {
            var made = NewSong.Make(song, "Awkward", 4, Model(model), Library("Like " + (model.Length == 0 ? "the app's own" : model)), () => false);
            var box = new OpenSong(made.Path).TextBoxes.Single();
            Assert.Equal(string.Join("\n", lines.Select(line => line.Text)), box.Words);
            // (A place is counted in UTF-16 units: the party is two.)
            Assert.Equal(
                "0:G 8:D " + (lines[0].Text.Length + 1 + 3) + ":C " + (lines[0].Text.Length + 1 + 10) + ":G",
                string.Join(" ", box.Chords.Take(4).Select(chord => chord.At + ":" + chord.Name)));
            Assert.Equal(6, box.Chords.Count);
            // Nothing in the RTF is above ASCII but what the model's own opening has.
            var rtf = ElementsOf(RV.Presentation.Parser.ParseFrom(File.ReadAllBytes(made.Path)).Cues.Single()).Select(element => element.Element_?.Text).First(one => one is not null && RtfWords.Read(one.RtfData.Span).Length > 0)!.RtfData;
            Assert.Contains("\\uc0\\u233 ", rtf.ToStringUtf8(), StringComparison.Ordinal);
            Assert.Contains("\\uc0\\u55356 \\uc0\\u57225 ", rtf.ToStringUtf8(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SoManyLinesGoToASlideAsWereAskedForAndTheLastOfAPartMayBeShorter()
    {
        var song = Song("amazing-grace.cho");
        foreach (var (lines, slides) in new[] { (1, "1 1 1 1|1 1 1 1|1 1 1 1|1"), (2, "2 2|2 2|2 2|1"), (3, "3 1|3 1|3 1|1"), (4, "4|4|4|1"), (99, "4|4|4|1"), (0, "1 1 1 1|1 1 1 1|1 1 1 1|1"), (-3, "1 1 1 1|1 1 1 1|1 1 1 1|1") })
        {
            var made = NewSong.Made(song, "Grace", lines, TestFile(ProPresenters), "", Now);
            var linesOfEach = made.CueGroups.Select(group => string.Join(" ", group.CueIdentifiers.Select(id =>
                RtfWords.Read(ElementsOf(made.Cues.Single(cue => cue.Uuid.String == id.String)).Single().Element_.Text.RtfData.Span).Split('\n').Length)));
            Assert.Equal(slides, string.Join("|", linesOfEach));
            Assert.Equal(NewSong.Found(song).Slides(lines), made.Cues.Count);
        }
    }

    // What was made is read back, as any song is read, before a byte of it is written
    // (decision 106). Nothing made here fails that, so here a song that was made is
    // spoilt, one way at a time, and each must be refused.
    [Fact]
    public void WhatIsReadBackBeforeAnythingIsWrittenMustBeTheSongThatWasAskedFor()
    {
        var song = Song("plain-song.cho");
        var made = NewSong.Made(song, "Plain Song", 2, TestFile(ProPresenters), "", Now);
        Assert.Equal(4, made.Cues.Count);
        // As it was made, it passes.
        NewSong.Check(made.ToByteArray(), song, "Plain Song", 2);

        RV.Graphics.Types.Text Text(RV.Presentation of, int slide) => ElementsOf(CuesAsShown(of)[slide]).Single().Element_.Text;
        void Refused(string what, Action<RV.Presentation> spoil)
        {
            var spoilt = made.Clone();
            spoil(spoilt);
            var refusal = Assert.Throws<Refusal>(() => NewSong.Check(spoilt.ToByteArray(), song, "Plain Song", 2));
            Assert.True(
                refusal.Reason == Refusal.Why.NotMade && refusal.Sentence == "The song could not be made: what was made did not read back as the song that was asked for.",
                what + ": " + refusal.Sentence);
        }

        Refused("another name", spoilt => spoilt.Name = "Another");
        Refused("another key", spoilt => spoilt.Music.Original.MusicKey = RV.MusicKeyScale.Types.MusicKey.A);
        Refused("another key to be shown in", spoilt => spoilt.Music.User.MusicKey = RV.MusicKeyScale.Types.MusicKey.A);
        Refused("no key", spoilt => spoilt.Music = null);
        Refused("a group of another name", spoilt => spoilt.CueGroups[1].Group.Name = "Chorus 9");
        Refused("a group more", spoilt => spoilt.CueGroups.Add(new RV.Presentation.Types.CueGroup { Group = new RV.Group { Name = "Tag" } }));
        Refused("a slide that its group does not name", spoilt => spoilt.CueGroups[0].CueIdentifiers.RemoveAt(1));
        Refused("a slide named first by the group after its own", spoilt =>
        {
            var second = spoilt.CueGroups[0].CueIdentifiers[1];
            spoilt.CueGroups[0].CueIdentifiers.RemoveAt(1);
            spoilt.CueGroups[1].CueIdentifiers.Insert(0, second);
        });
        Refused("two slides the other way round", spoilt =>
        {
            var ids = spoilt.CueGroups[0].CueIdentifiers;
            (ids[0], ids[1]) = (ids[1], ids[0]);
        });
        Refused("a slide missing", spoilt =>
        {
            spoilt.CueGroups[2].CueIdentifiers.Clear();
            spoilt.Cues.RemoveAt(spoilt.Cues.Count - 1);
        });
        Refused("a slide more", spoilt =>
        {
            var more = spoilt.Cues[0].Clone();
            more.Uuid = new RV.UUID { String = OpenSong.NewId("A") };
            spoilt.Cues.Add(more);
            spoilt.CueGroups[2].CueIdentifiers.Add(more.Uuid.Clone());
        });
        Refused("one id for two slides", spoilt => spoilt.Cues.Add(spoilt.Cues[0].Clone()));
        Refused("a slide switched off", spoilt => CuesAsShown(spoilt)[1].IsEnabled = false);
        Refused("other words on a slide", spoilt => Text(spoilt, 0).RtfData = ByteString.CopyFrom(RtfWords.WithWords(Text(spoilt, 0).RtfData.Span, "One two three four\nFive six seven nine")!));
        Refused("no words on a slide", spoilt => Text(spoilt, 2).RtfData = ByteString.CopyFrom(RtfWords.WithWords(Text(spoilt, 2).RtfData.Span, "")!));
        Refused("a chord missing", spoilt => Writing.WriteChords(Reading.ChordsOf(Text(spoilt, 0)).Skip(1), Text(spoilt, 0)));
        Refused("a chord a place along", spoilt => Writing.WriteChords(Reading.ChordsOf(Text(spoilt, 1)).Select((chord, n) => n == 0 ? chord with { At = chord.At + 1 } : chord), Text(spoilt, 1)));
        Refused("a chord of another name", spoilt => Writing.WriteChords(Reading.ChordsOf(Text(spoilt, 3)).Select((chord, n) => n == 0 ? chord with { Name = "G7" } : chord), Text(spoilt, 3)));
        Refused("a chord on a slide that has none", spoilt =>
        {
            // (Plain Song's second slide has two chords on its first line and none on its second.)
            var text = Text(spoilt, 1);
            Writing.WriteChords([.. Reading.ChordsOf(text), new Chord(RtfWords.Read(text.RtfData.Span).Length - 1, "D")], text);
        });
        Refused("a second text box that is shown", spoilt =>
        {
            var elements = ElementsOf(CuesAsShown(spoilt)[0]);
            var another = elements[0].Clone();
            another.Element_.Uuid = new RV.UUID { String = OpenSong.NewId("A") };
            another.DataLinks.Clear();
            elements.Add(another);
        });
        Refused("the text box linked to show something else", spoilt =>
            ElementsOf(CuesAsShown(spoilt)[0])[0].DataLinks.Add(new RV.Slide.Types.Element.Types.DataLink { ClockText = new RV.Slide.Types.Element.Types.DataLink.Types.ClockText() }));

        // Bytes that are no presentation at all; and the song held to another's file,
        // or to other lines to a slide than it was made with.
        Assert.Equal(Refusal.Why.NotMade, Assert.Throws<Refusal>(() => NewSong.Check([0x0A, 0xFF, 0xFF], song, "Plain Song", 2)).Reason);
        Assert.Equal(Refusal.Why.NotMade, Assert.Throws<Refusal>(() => NewSong.Check(made.ToByteArray(), Song("amazing-grace.cho"), "Plain Song", 2)).Reason);
        Assert.Equal(Refusal.Why.NotMade, Assert.Throws<Refusal>(() => NewSong.Check(made.ToByteArray(), song, "Plain Song", 4)).Reason);
        Assert.Equal(Refusal.Why.NotMade, Assert.Throws<Refusal>(() => NewSong.Check(made.ToByteArray(), song, "Plain Song", 1)).Reason);
        // A song with no key, held as one: and one given a key it was not asked to have.
        var keyless = Song("holy-holy-holy.txt");
        var madeKeyless = NewSong.Made(keyless, "Holy", 2, TestFile(ProPresenters), "", Now);
        NewSong.Check(madeKeyless.ToByteArray(), keyless, "Holy", 2);

        // One id for two slides, in a file that otherwise READS as the song: the last
        // slide, which is of a part with no name, put first in the file under the first
        // slide's id, and named by no group. A group's naming of that id finds the
        // later of the two, which is the first slide; and the other, being in no group,
        // is shown last, where it belongs. Only the asking after ids tells.
        var twice = madeKeyless.Clone();
        var last = twice.Cues[^1];
        twice.Cues.RemoveAt(twice.Cues.Count - 1);
        last.Uuid = twice.Cues[0].Uuid.Clone();
        twice.Cues.Insert(0, last);
        twice.CueGroups[^1].CueIdentifiers.RemoveAt(twice.CueGroups[^1].CueIdentifiers.Count - 1);
        var asRead = Reading.TextBoxes(twice, Reading.SlideRows(twice)).Select(box => box.Group + "|" + box.Words).ToList();
        Assert.Equal(Reading.TextBoxes(madeKeyless, Reading.SlideRows(madeKeyless)).Select(box => box.Group + "|" + box.Words), asRead);
        Assert.Equal(Refusal.Why.NotMade, Assert.Throws<Refusal>(() => NewSong.Check(twice.ToByteArray(), keyless, "Holy", 2)).Reason);

        madeKeyless.Music = made.Music.Clone();
        Assert.Equal(Refusal.Why.NotMade, Assert.Throws<Refusal>(() => NewSong.Check(madeKeyless.ToByteArray(), keyless, "Holy", 2)).Reason);
    }

    // ---- The file (rule 16)

    [Fact]
    public void TheSongIsANewFileInTheLibraryAndNeverOverOneThatIsThere()
    {
        var library = Library();
        var other = Path.Combine(library, "Another Song.pro");
        File.WriteAllBytes(other, TestData.InFiles("made-by-simple-presenter/Plain Song.pro"));
        var otherWas = File.ReadAllBytes(other);
        var song = Song("minor-key.cho");

        // Its name is tidied: a slash is a hyphen.
        var first = NewSong.Make(song, " A Song / In A  Minor Key ", 2, ModelSong.OfTheApp, library, () => false);
        Assert.Equal((Path.Combine(library, "A Song - In A Minor Key.pro"), "A Song - In A Minor Key", 3, 3, 9), (first.Path, first.Name, first.Parts, first.Slides, first.Chords));
        var firstWas = File.ReadAllBytes(first.Path);
        Assert.Equal("A Song - In A Minor Key", RV.Presentation.Parser.ParseFrom(firstWas).Name);

        // Made again, it is a second song, with a number in the file's name and in its
        // own; and the first is as it was.
        var second = NewSong.Make(song, "A Song / In A Minor Key", 2, ModelSong.OfTheApp, library, () => false);
        Assert.Equal((Path.Combine(library, "A Song - In A Minor Key 2.pro"), "A Song - In A Minor Key 2"), (second.Path, second.Name));
        Assert.Equal("A Song - In A Minor Key 2", RV.Presentation.Parser.ParseFrom(File.ReadAllBytes(second.Path)).Name);
        Assert.Equal(firstWas, File.ReadAllBytes(first.Path));
        var third = NewSong.Make(song, "A Song - In A Minor Key", 2, ModelSong.OfTheApp, library, () => false);
        Assert.Equal("A Song - In A Minor Key 3", third.Name);
        // Each is a song of its own: no id of one is in another.
        var ids = new[] { first, second, third }.Select(made => IdsIn(RV.Presentation.Parser.ParseFrom(File.ReadAllBytes(made.Path))).ToHashSet()).ToList();
        Assert.False(ids[0].Overlaps(ids[1]) || ids[0].Overlaps(ids[2]) || ids[1].Overlaps(ids[2]));

        // Nothing else in the library was touched, and nothing is left lying beside.
        Assert.Equal(otherWas, File.ReadAllBytes(other));
        Assert.Equal(
            ["A Song - In A Minor Key 2.pro", "A Song - In A Minor Key 3.pro", "A Song - In A Minor Key.pro", "Another Song.pro"],
            Directory.EnumerateFileSystemEntries(library).Select(Path.GetFileName).Order(StringComparer.Ordinal));
        Assert.Empty(bench.Copies);

        // A model that is one of the library's own songs is only read.
        var like = NewSong.Make(song, "Like another", 2, ModelSong.Read(other), library, () => false);
        Assert.Equal(otherWas, File.ReadAllBytes(other));
        Assert.Equal("Like another", new OpenSong(like.Path).Name);
    }

    [Fact]
    public void NothingIsMadeWhileProPresenterIsRunningNorWithoutALibraryNorLikeAFileThatIsNoSong()
    {
        var library = Library();
        var song = Song("plain-song.cho");

        var running = Assert.Throws<Refusal>(() => NewSong.Make(song, "Plain Song", 2, ModelSong.OfTheApp, library, () => true));
        Assert.Equal((Refusal.Why.ProPresenterIsRunning, "ProPresenter is running. Quit it to make a song here. Nothing was made."), (running.Reason, running.Sentence));
        Assert.Empty(Directory.EnumerateFileSystemEntries(library));

        var nowhere = Assert.Throws<Refusal>(() => NewSong.Make(song, "Plain Song", 2, ModelSong.OfTheApp, Path.Combine(bench.Folder, "No such library"), () => false));
        Assert.Equal((Refusal.Why.NotMade, "There is no library to put the song in. Nothing was made."), (nowhere.Reason, nowhere.Sentence));
        Assert.False(Directory.Exists(Path.Combine(bench.Folder, "No such library")));

        var notASong = Assert.Throws<Refusal>(() => NewSong.Make(song, "Plain Song", 2, new ModelSong("Nonsense", (byte[])[0x0A, 0xFF, 0xFF]), library, () => false));
        Assert.Equal(Refusal.Why.CannotBeRead, notASong.Reason);
        Assert.StartsWith("Nonsense.pro could not be read as a ProPresenter presentation: ", notASong.Sentence, StringComparison.Ordinal);
        var gone = Assert.Throws<Refusal>(() => ModelSong.Read(Path.Combine(library, "Gone.pro")));
        Assert.Equal(Refusal.Why.CannotBeRead, gone.Reason);
        Assert.StartsWith("Gone.pro could not be read as a ProPresenter presentation: ", gone.Sentence, StringComparison.Ordinal);

        // A song like one with no words on any slide: it is named as its file is,
        // without the ending.
        var empty = bench.FileOf("No words at all.pro", new RV.Presentation { Name = "Empty" }.ToByteArray());
        var noWords = Assert.Throws<Refusal>(() => NewSong.Make(song, "Plain Song", 2, ModelSong.Read(empty), library, () => false));
        Assert.Equal(Refusal.Why.NotMade, noWords.Reason);
        Assert.StartsWith((char)0x201C + "No words at all" + (char)0x201D + " has no slide with words on it, ", noWords.Sentence, StringComparison.Ordinal);

        // A name too long for a file to have: the system says so, and nothing is left.
        var tooLong = Assert.Throws<Refusal>(() => NewSong.Make(song, new string('a', 400), 2, ModelSong.OfTheApp, library, () => false));
        Assert.Equal(Refusal.Why.NotMade, tooLong.Reason);
        Assert.StartsWith("The song could not be made: ", tooLong.Sentence, StringComparison.Ordinal);
        Assert.True(tooLong.Sentence.Length > "The song could not be made: ".Length);

        // And a file with no words in it makes no song.
        Assert.Equal(Refusal.Why.NotASong, Assert.Throws<Refusal>(() => NewSong.Make(SongOf("{title: Nothing}\n"), "Nothing", 2, ModelSong.OfTheApp, library, () => false)).Reason);
        Assert.Empty(Directory.EnumerateFileSystemEntries(library));
    }

    [Fact]
    public void ANewFileIsWrittenWholeAndOnlyWhereNoFileIs()
    {
        var library = Library();
        var path = Path.Combine(library, "New.pro");
        byte[] bytes = [1, 2, 3, 4];
        Assert.True(SongFile.WriteNew(bytes, path));
        Assert.Equal(bytes, File.ReadAllBytes(path));
        // The name is taken now: nothing is written, and what is there is as it was.
        Assert.False(SongFile.WriteNew([9, 9], path));
        Assert.Equal(bytes, File.ReadAllBytes(path));
        // A folder of that name is in the way too.
        Directory.CreateDirectory(Path.Combine(library, "A folder.pro"));
        Assert.False(SongFile.WriteNew([9, 9], Path.Combine(library, "A folder.pro")));
        Assert.Equal(["A folder.pro", "New.pro"], Directory.EnumerateFileSystemEntries(library).Select(Path.GetFileName).Order(StringComparer.Ordinal));
        // Where it cannot be written at all, it says why, and nothing is left.
        var failure = Assert.Throws<SongFile.Failure>(() => SongFile.WriteNew(bytes, Path.Combine(library, "No such folder", "New.pro")));
        Assert.NotEqual("", failure.Message);
        var refused = Assert.Throws<Refusal>(() => NewSong.Make(Song("plain-song.cho"), "Plain Song", 2, ModelSong.OfTheApp, Path.Combine(library, "New.pro"), () => false));
        Assert.Equal(Refusal.Why.NotMade, refused.Reason);
        Assert.Equal(["A folder.pro", "New.pro"], Directory.EnumerateFileSystemEntries(library).Select(Path.GetFileName).Order(StringComparer.Ordinal));
    }

    // ---- The song that was made is a song like any other

    [Theory]
    [InlineData("amazing-grace.cho", 2)]
    [InlineData("plain-song.cho", 3)]
    [InlineData("it-is-well.chordpro", 1)]
    [InlineData("blank-slides.cho", 2)]
    [InlineData("headings.cho", 2)]
    public void ASongMadeHereTakesChordsAndWrittenOutAsChordProReadsBackAsTheFileItWasMadeFrom(string file, int linesToASlide)
    {
        var song = Song(file);
        var made = NewSong.Make(song, NewSong.NameFor(song, file), linesToASlide, ModelSong.OfTheApp, Library(), () => false);
        var asMade = File.ReadAllBytes(made.Path);
        var open = bench.OpenAt(made.Path);

        // Written out as ChordPro and read again: the same parts, lines and chords as
        // the file it was made from (rule 19's test, from the other end).
        var back = Chords.ParseSong(ChordProFile.TextOf(open.Name, open.Key, open.TextBoxes));
        Assert.Equal(song.Sections.Select(part => part.Name), back.Sections.Select(part => part.Name));
        Assert.Equal(
            song.Sections.SelectMany(part => part.Lines).Select(line => Spelt.Of(line.Text) + " " + Spelt.Of(Chords.Tidy(line.Text, line.Chords))),
            back.Sections.SelectMany(part => part.Lines).Select(line => Spelt.Of(line.Text) + " " + Spelt.Of(line.Chords)));
        Assert.Equal(NewSong.Found(song).Key, back.Key);

        // A chord is put on it, saved, and taken off again: the file is as it was made.
        var box = open.TextBoxes.First(one => one.Words.Any(char.IsLetter));
        open.SetChords(box.Slide, box.Element, [.. box.Chords.Where(chord => chord.At != 0), new Chord(0, "Dsus4")]);
        Assert.Contains("Dsus4", new OpenSong(made.Path).UsedChords);
        Assert.Single(bench.Copies);
        Assert.Equal(asMade, bench.Copies[0].Bytes);
        open.Undo();
        Assert.Equal(asMade, File.ReadAllBytes(made.Path));
        // And its key is set as any song's is.
        open.SetKey("D");
        Assert.Equal(("D", "D"), (new OpenSong(made.Path).Key, new OpenSong(made.Path).ShownKey));
    }

    [Fact]
    public void TheSongTheAppCarriesIsTheOneFileToHandThatProPresenterWrote()
    {
        var carried = ModelSong.OfTheApp;
        Assert.Equal("", carried.Name);
        Assert.Equal(TestData.InFiles(ProPresenters), carried.Bytes.ToArray());
        // It will do as a model: a song with a slide of words.
        var made = NewSong.Made(SongOf("Verse 1\nOne two\n"), "New", 2, RV.Presentation.Parser.ParseFrom(carried.Bytes.Span), carried.Name, Now);
        Assert.Single(made.Cues);
    }
}
