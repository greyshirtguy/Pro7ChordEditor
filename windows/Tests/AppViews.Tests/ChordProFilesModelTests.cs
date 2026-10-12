// ChordPro files in the window's model (LibraryModel.ChordProFiles.cs): what is shown
// of a file before a song is made of it, what the person may change, the making, and
// the song that is open written as a file. spec/09-chordpro-files.md, rules 8 and 16 to
// 19. With no window: the views only show this (NewSongPanel).
//
// That the song made IS the right song is the layer below's to prove
// (Song.Tests/NewSongTests.cs). Here: that the window's model asks for the right one,
// says what it found and did, and leaves the library alone until it is told to make it.

using System.IO;
using System.Text;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Song;
using Pro7ChordEditor.Tests;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class ChordProFilesModelTests
{
    private static readonly string OpenQ = ((char)0x201C).ToString();
    private static readonly string CloseQ = ((char)0x201D).ToString();

    private static string Q(string name) => OpenQ + name + CloseQ;

    private static void OnAMadeUpPC(Func<MadeUpPC, Task> what) => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        await what(pc);
    });

    private static async Task<LibraryModel> Started(MadeUpPC pc, string? open = null)
    {
        var model = pc.Model();
        await model.StartAsync();
        // (The first library the PC has is an empty one.)
        if (model.Library is not null)
        {
            model.ChooseLibrary(pc.Library("Songs"));
        }
        await model.MarksAreWorkedOutAsync();
        if (open is not null)
        {
            model.Open(pc.Song(open));
        }
        return model;
    }

    // One of the ChordPro files of the test data.
    internal static string ASong(string name) => Path.Combine(TestData.Root, "songs", name);

    // A file of the made-up PC that is no part of any library, with this in it.
    internal static string AFile(MadeUpPC pc, string name, string text)
    {
        var path = Path.Combine(pc.Documents, name);
        File.WriteAllText(path, text, new UTF8Encoding(false));
        return path;
    }

    // Every file under ProPresenter's folder, with its bytes: to see that nothing was touched.
    private static Dictionary<string, string> Everything(MadeUpPC pc) =>
        Directory.EnumerateFiles(pc.Place, "*", SearchOption.AllDirectories).ToDictionary(path => path, path => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))));

    private static IEnumerable<string> BoxesOf(string path) =>
        new OpenSong(path).TextBoxes.Select(box => box.Group + "|" + box.Words + "|" + string.Join(" ", box.Chords.Select(chord => chord.At + ":" + chord.Name)));

    // ---- Before it is brought in (rule 8)

    [Fact]
    public void AFileChosenIsReadAndWhatWasFoundIsShownAndNothingIsMade() => OnAMadeUpPC(async pc =>
    {
        var model = await Started(pc);
        var before = Everything(pc);
        Assert.True(model.CanBringInAFile);
        Assert.Null(model.NewSong);

        model.BringIn(ASong("amazing-grace.cho"));

        var asked = model.NewSong!;
        Assert.Equal("New song from amazing-grace.cho", asked.Heading);
        Assert.Equal(
            [
                ("Title", "Amazing Grace"),
                ("Artist", "John Newton"),
                ("Key", "G"),
                ("Parts", "Verse 1 (4 lines), Verse 2 (4 lines), Verse 3 (4 lines), Ending (1 line)"),
                ("Chords", "40 chords in all"),
            ],
            asked.Summary);
        // The name it will have: the title; but the library has that song already.
        Assert.Equal(("Amazing Grace", "Amazing Grace 2"), (asked.Name, asked.WillBeNamed));
        Assert.Equal(Q("Songs") + " has a song named " + Q("Amazing Grace") + " already. This one will be named " + Q("Amazing Grace 2") + ".", asked.NameNote);
        // The library it goes into is the one that is open; two lines to a slide.
        Assert.Equal(("Songs", 2, "which makes 7 slides"), (asked.Library, asked.LinesToASlide, asked.Slides));
        // What it can be made to look like: the app's own, then the library's songs as
        // they are listed. With no song open, the app's own.
        Assert.Equal(["A song this app carries", .. MadeUpPC.Songs], asked.LookAlikes.Select(one => one.Name));
        Assert.Null(asked.LookAlikes[0].Id);
        Assert.Equal(pc.Song("Plain Song"), asked.LookAlikes.Single(one => one.Name == "Plain Song").Id);
        Assert.Same(asked.LookAlikes[0], asked.Like);
        Assert.True(asked.CanMake);
        Assert.Null(asked.Standing);
        // Nothing was made, and nothing in the library was touched.
        Assert.Equal(before, Everything(pc));
        Assert.Equal(MadeUpPC.Songs, model.Songs.Select(song => song.Name));
        Assert.Contains("Read amazing-grace.cho.", pc.Log, StringComparison.Ordinal);

        // Cancel: the panel is put away, and still nothing was made.
        model.DontMakeTheSong();
        Assert.Null(model.NewSong);
        Assert.Equal(before, Everything(pc));
        // (Asked with no panel there, the making does nothing.)
        model.MakeTheSong();
        Assert.Equal(before, Everything(pc));
    });

    [Fact]
    public void AFileWithNoHeadingSaysWhatItHasNotAndIsNamedAsItsFileIs() => OnAMadeUpPC(async pc =>
    {
        var model = await Started(pc);
        model.BringIn(ASong("holy-holy-holy.txt"));
        var asked = model.NewSong!;
        Assert.Equal(
            [
                ("Title", "None given"),
                ("Artist", "None given"),
                ("Key", "None given"),
                ("Parts", "Verse 1 (4 lines), Verse 2 (4 lines), a part with no name (4 lines)"),
                ("Chords", "22 chords in all"),
            ],
            asked.Summary);
        Assert.Equal(("holy-holy-holy", "holy-holy-holy", ""), (asked.Name, asked.WillBeNamed, asked.NameNote));
        model.DontMakeTheSong();

        // One part of one line, with one chord; and a song with none.
        model.BringIn(AFile(pc, "One.cho", "Verse 1\n[G]One\n"));
        Assert.Equal((("Parts", "Verse 1 (1 line)"), ("Chords", "1 chord in all"), "which makes 1 slide"), (model.NewSong!.Summary[3], model.NewSong.Summary[4], model.NewSong.Slides));
        model.DontMakeTheSong();
        model.BringIn(AFile(pc, "No chords.cho", "Verse 1\nOne\nTwo\nThree\n"));
        Assert.Equal((("Chords", "None"), "which makes 2 slides"), (model.NewSong!.Summary[4], model.NewSong.Slides));
    });

    [Fact]
    public void TheSongThatIsOpenIsTheOneToLookLikeUnlessItCannotBeRead() => OnAMadeUpPC(async pc =>
    {
        var model = await Started(pc, "Plain Song");
        model.BringIn(ASong("amazing-grace.cho"));
        Assert.Equal(("Plain Song", pc.Song("Plain Song")), (model.NewSong!.Like.Name, model.NewSong.Like.Id));
        model.DontMakeTheSong();

        model.Open(pc.Song("Nonsense"));
        Assert.NotNull(model.Shown!.Problem);
        model.BringIn(ASong("amazing-grace.cho"));
        Assert.Equal("A song this app carries", model.NewSong!.Like.Name);
    });

    [Fact]
    public void TheNameTheLinesToASlideAndTheSongToLookLikeCanBeChanged() => OnAMadeUpPC(async pc =>
    {
        var model = await Started(pc);
        var before = Everything(pc);
        model.BringIn(ASong("amazing-grace.cho"));

        // A name the library has not: it will be that, and nothing is said of it.
        model.NameTheNewSong("Grace Again");
        Assert.Equal(("Grace Again", "Grace Again", ""), (model.NewSong!.Name, model.NewSong.WillBeNamed, model.NewSong.NameNote));
        // Spaces at its ends are not worth a sentence.
        model.NameTheNewSong("  Grace Again ");
        Assert.Equal(("  Grace Again ", "Grace Again", ""), (model.NewSong!.Name, model.NewSong.WillBeNamed, model.NewSong.NameNote));
        // What a file's name cannot have in it is; and so is a name with nothing in it.
        model.NameTheNewSong("Grace / Again: live?");
        Assert.Equal(("Grace - Again- live-", "As a file can be named, that is " + Q("Grace - Again- live-") + "."), (model.NewSong!.WillBeNamed, model.NewSong.NameNote));
        model.NameTheNewSong("");
        Assert.Equal(("Song", "As a file can be named, that is " + Q("Song") + "."), (model.NewSong!.WillBeNamed, model.NewSong.NameNote));
        // And one the library has, after it is tidied.
        model.NameTheNewSong("plain   song");
        Assert.Equal(
            ("plain song 2", Q("Songs") + " has a song named " + Q("plain song") + " already. This one will be named " + Q("plain song 2") + "."),
            (model.NewSong!.WillBeNamed, model.NewSong.NameNote));

        // The lines to a slide: one at least, and twelve at most.
        foreach (var (lines, taken, slides) in new[] { (4, 4, "which makes 4 slides"), (1, 1, "which makes 13 slides"), (0, 1, "which makes 13 slides"), (-7, 1, "which makes 13 slides"), (3, 3, "which makes 7 slides"), (500, 12, "which makes 4 slides") })
        {
            model.SetLinesToASlide(lines);
            Assert.Equal((taken, slides), (model.NewSong!.LinesToASlide, model.NewSong.Slides));
        }

        // The song to look like: one of the list's, by its id; the app's own, by none.
        model.MakeItLookLike(pc.Song("It Is Well"));
        Assert.Equal("It Is Well", model.NewSong!.Like.Name);
        model.MakeItLookLike(pc.Song("No such song"));
        Assert.Equal("It Is Well", model.NewSong!.Like.Name);
        model.MakeItLookLike(null);
        Assert.Equal("A song this app carries", model.NewSong!.Like.Name);
        // And in all of it nothing was made.
        Assert.Equal(before, Everything(pc));
    });

    // ---- Making it

    [Fact]
    public void TheSongIsMadeListedAndOpenedAndWhatWasMadeIsSaid() => OnAMadeUpPC(async pc =>
    {
        var model = await Started(pc);
        var before = Everything(pc);
        var changes = 0;
        model.PropertyChanged += (_, _) => changes++;
        model.BringIn(ASong("amazing-grace.cho"));
        Assert.True(changes > 0);

        model.MakeTheSong();

        // The panel is put away. The song is in the library, under the name it was said
        // it would have, and is the one that is open.
        Assert.Null(model.NewSong);
        var made = pc.Song("Amazing Grace 2");
        Assert.True(File.Exists(Path.Combine(pc.Place, "Libraries", "Songs", "Amazing Grace 2.pro")));
        Assert.Equal(["A Song - In A Minor Key", "Amazing Grace", "Amazing Grace 2", "Blank Slides"], model.Songs.Select(song => song.Name).Take(4));
        Assert.Equal(made, model.SongId);
        Assert.Equal(("Amazing Grace 2", 40, "G", 7, null), (model.Shown!.Name, model.Shown.Chords, model.Shown.Key, model.Shown.Slides, model.Shown.Problem));
        Assert.Equal(Q("Amazing Grace 2") + " was made from amazing-grace.cho: 4 parts, 7 slides, 40 chords.", model.Notice);
        Assert.Null(model.Refused);
        Assert.Contains("Made Amazing Grace 2.pro from amazing-grace.cho.", pc.Log, StringComparison.Ordinal);
        // It reads as the song of that name the test data has, which Simple Presenter
        // made from the same file.
        Assert.Equal(BoxesOf(Path.Combine(pc.Place, "Libraries", "Songs", "Amazing Grace.pro")), BoxesOf(Path.Combine(pc.Place, "Libraries", "Songs", "Amazing Grace 2.pro")));
        // Its marks are worked out as any song's are, and it wants no mending.
        await model.MarksAreWorkedOutAsync();
        Assert.Equal((40, 0, ""), (model.Marks[made].Chords, model.Marks[made].ShownTwice, model.Marks[made].DoubtedKey));
        // Nothing else in the library was touched, and no copy of anything was kept.
        var after = Everything(pc);
        Assert.Equal(before.Count + 1, after.Count);
        Assert.All(before, file => Assert.Equal(file.Value, after[file.Key]));
        Assert.Empty(pc.AppData.Copies().Songs());

        // It is a song like any other: a chord typed on it is saved, with a copy kept first.
        model.Show(LibraryModel.Editor.ChordPro);
        Assert.True(model.TextMayBecome(ChordProModelTests.With("That saved", "[Am]That saved", model)));
        Assert.True(model.Settle());
        Assert.Equal(41, model.Shown!.Chords);
        Assert.Single(pc.AppData.Copies().Songs());

        // Made again from the same file, it is a third song.
        model.BringIn(ASong("amazing-grace.cho"));
        Assert.Equal(("Amazing Grace 3", "Amazing Grace 2"), (model.NewSong!.WillBeNamed, model.NewSong.Like.Name));
        model.NameTheNewSong("One / More");
        model.SetLinesToASlide(4);
        model.MakeTheSong();
        Assert.Equal(pc.Song("One - More"), model.SongId);
        Assert.Equal(Q("One - More") + " was made from amazing-grace.cho: 4 parts, 4 slides, 40 chords.", model.Notice);
    });

    [Fact]
    public void ItIsMadeToLookLikeTheSongThatWasChosen() => OnAMadeUpPC(async pc =>
    {
        var model = await Started(pc);
        string FontOf(string song)
        {
            var presentation = Pro.SerializationInterop.RVProtoData.Presentation.Parser.ParseFrom(File.ReadAllBytes(Path.Combine(pc.Place, "Libraries", "Songs", song + ".pro")));
            return presentation.Cues[0].Actions.Single(action => action.Slide is not null).Slide.Presentation.BaseSlide.Elements.First(element => element.Element_?.Text is not null).Element_.Text.Attributes.Font.Name;
        }

        // The song ProPresenter wrote is in another font than those Simple Presenter made.
        Assert.NotEqual(FontOf("Plain Song"), FontOf("Rock Of Ages (Toplady)"));
        model.BringIn(ASong("plain-song.cho"));
        model.NameTheNewSong("Like Rock Of Ages");
        model.MakeItLookLike(pc.Song("Rock Of Ages (Toplady)"));
        model.MakeTheSong();
        Assert.Equal(FontOf("Rock Of Ages (Toplady)"), FontOf("Like Rock Of Ages"));

        model.BringIn(ASong("plain-song.cho"));
        // (The song just made is open, and so is the one offered.)
        Assert.Equal("Like Rock Of Ages", model.NewSong!.Like.Name);
        model.NameTheNewSong("Like Plain Song");
        model.MakeItLookLike(pc.Song("Plain Song"));
        model.MakeTheSong();
        Assert.Equal(FontOf("Plain Song"), FontOf("Like Plain Song"));

        // And the app's own is the song ProPresenter wrote.
        model.BringIn(ASong("plain-song.cho"));
        model.NameTheNewSong("Like the app's own");
        model.MakeItLookLike(null);
        model.MakeTheSong();
        Assert.Equal(FontOf("Rock Of Ages (Toplady)"), FontOf("Like the app's own"));
        Assert.Null(model.Refused);
    });

    [Fact]
    public void NothingIsMadeWhileProPresenterIsRunningAndThePanelSaysSo() => OnAMadeUpPC(async pc =>
    {
        var model = await Started(pc);
        var before = Everything(pc);
        model.BringIn(ASong("plain-song.cho"));
        model.NameTheNewSong("New Plain Song");
        // It is started while the panel is being read: the making is refused, the panel
        // stays and says why, and the band is there.
        pc.ProPresenterIsRunning = true;
        model.MakeTheSong();
        Assert.Equal(before, Everything(pc));
        Assert.True(model.ProPresenterIsRunning);
        Assert.Equal(
            ("ProPresenter is running. Quit it to make a song here.", false, null, "New Plain Song"),
            (model.NewSong!.Standing, model.NewSong.CanMake, model.NewSong.Problem, model.NewSong.Name));
        // Asked again all the same, nothing is made.
        model.MakeTheSong();
        Assert.Equal(before, Everything(pc));

        // Quit, and the panel says so no longer; and the song is made.
        pc.ProPresenterIsRunning = false;
        model.ProPresenterMayHaveChanged();
        Assert.Equal((null, true), (model.NewSong!.Standing, model.NewSong.CanMake));
        model.MakeTheSong();
        Assert.Equal(pc.Song("New Plain Song"), model.SongId);

        // A panel that comes while it is running says so from the first.
        pc.ProPresenterIsRunning = true;
        model.ProPresenterMayHaveChanged();
        model.BringIn(ASong("plain-song.cho"));
        Assert.Equal(("ProPresenter is running. Quit it to make a song here.", false), (model.NewSong!.Standing, model.NewSong.CanMake));
    });

    [Fact]
    public void WhatIsRefusedIsSaidInThePanelWhichStays() => OnAMadeUpPC(async pc =>
    {
        var model = await Started(pc);
        var before = Everything(pc);
        model.BringIn(ASong("plain-song.cho"));
        // Like a file that is no song.
        model.MakeItLookLike(pc.Song("Nonsense"));
        model.MakeTheSong();
        Assert.Equal(
            ("Nonsense.pro could not be read as a ProPresenter presentation: it is not a presentation, or it is damaged.", "Nonsense", false),
            (model.NewSong!.Problem, model.NewSong.Like.Name, model.NewSong.ProPresenterIsRunning));
        Assert.Equal(model.NewSong.Problem, model.NewSong.Standing);
        Assert.True(model.NewSong.CanMake);
        Assert.Equal(before, Everything(pc));
        Assert.Contains("A song was not made from plain-song.cho.", pc.Log, StringComparison.Ordinal);
        // Another is chosen: what was said is said no longer.
        model.MakeItLookLike(pc.Song("Amazing Grace"));
        Assert.Null(model.NewSong!.Problem);

        // Like a song taken out of the library since the panel came.
        model.MakeItLookLike(pc.Song("It Is Well"));
        File.Delete(Path.Combine(pc.Place, "Libraries", "Songs", "It Is Well.pro"));
        model.MakeTheSong();
        Assert.StartsWith("It Is Well.pro could not be read as a ProPresenter presentation: ", model.NewSong!.Problem, StringComparison.Ordinal);
        // A change of the name, or of the lines, puts the sentence away too.
        model.NameTheNewSong("Another name");
        Assert.Null(model.NewSong!.Problem);
        model.MakeTheSong();
        Assert.NotNull(model.NewSong!.Problem);
        model.SetLinesToASlide(3);
        Assert.Null(model.NewSong!.Problem);

        // Like a song with no words on any slide: a blank one, made of Blank Slides' first.
        model.MakeItLookLike(pc.Song("Amazing Grace"));
        model.MakeTheSong();
        Assert.Null(model.NewSong);
        Assert.Equal(pc.Song("Another name"), model.SongId);
    });

    [Fact]
    public void AFileThatIsNoSongSaysSoAndNothingCanBeMadeOfIt() => OnAMadeUpPC(async pc =>
    {
        var model = await Started(pc);
        var before = Everything(pc);

        model.BringIn(AFile(pc, "Only a heading.cho", "{title: Nothing}\n# no more\n"));
        var asked = model.NewSong!;
        Assert.Equal(("New song from Only a heading.cho", "Only a heading.cho has no words in it.", false), (asked.Heading, asked.Problem, asked.CanMake));
        Assert.Null(asked.Found);
        Assert.Empty(asked.Summary);
        Assert.Equal(("", ""), (asked.Slides, asked.NameNote));
        Assert.Equal(asked.Problem, asked.Standing);
        Assert.Contains("Could not read Only a heading.cho.", pc.Log, StringComparison.Ordinal);
        // Nothing there does anything but put it away.
        model.NameTheNewSong("A name");
        model.SetLinesToASlide(5);
        model.MakeItLookLike(pc.Song("Plain Song"));
        model.MakeTheSong();
        Assert.Same(asked, model.NewSong);
        Assert.Equal(before, Everything(pc));
        model.DontMakeTheSong();
        Assert.Null(model.NewSong);

        // A file that is not there, and one too large to be a song.
        model.BringIn(Path.Combine(pc.Documents, "Not there.cho"));
        Assert.StartsWith("Not there.cho could not be read: ", model.NewSong!.Problem, StringComparison.Ordinal);
        model.DontMakeTheSong();
        model.BringIn(AFile(pc, "Large.cho", new string('x', ChordProFile.MostBytes + 1)));
        Assert.Equal("Large.cho is too large to be a song.", model.NewSong!.Problem);
        Assert.Equal(before, Everything(pc));
    });

    [Fact]
    public void NoFileIsBroughtInWithNoLibraryToPutASongInNorWhileSomethingElseIsBeingAsked() => OnAMadeUpPC(async pc =>
    {
        // Before the app has started; and on a PC with no ProPresenter folder.
        var early = pc.Model();
        Assert.False(early.CanBringInAFile);
        early.BringIn(ASong("plain-song.cho"));
        Assert.Null(early.NewSong);
        pc.HasAProPresenterFolder = false;
        var nowhere = await Started(pc);
        Assert.Null(nowhere.Library);
        Assert.False(nowhere.CanBringInAFile);
        nowhere.BringIn(ASong("plain-song.cho"));
        Assert.Null(nowhere.NewSong);
        pc.HasAProPresenterFolder = true;

        // While Mend All is asking, and while a copy's chords are asked to be put in.
        var model = await Started(pc);
        model.ChooseLibrary(pc.Library("Old copies"));
        await model.MarksAreWorkedOutAsync();
        Assert.True(model.CanBringInAFile);
        model.Filter = MarkFilter.WantingMending;
        model.AskToMendAll();
        Assert.NotNull(model.MendAllQuestion);
        Assert.False(model.CanBringInAFile);
        model.BringIn(ASong("plain-song.cho"));
        Assert.Null(model.NewSong);
        model.DontMendAll();
        Assert.True(model.CanBringInAFile);
        model.Filter = MarkFilter.All;
        // And while a question is over the window: here, whether a song is to be reverted.
        model.Open(pc.Song("Plain Song (Chords)", "Old copies"));
        model.Take(LibraryModel.OfferAction.Mend);
        model.AskToRevert();
        Assert.NotNull(model.Asked);
        Assert.False(model.CanBringInAFile);
        model.BringIn(ASong("plain-song.cho"));
        Assert.Null(model.NewSong);
        // (Nor is the song written out from under a question.)
        Assert.False(model.CanWriteChordPro);
        var under = Path.Combine(pc.Documents, "Under a question.cho");
        model.WriteAsChordPro(under);
        Assert.False(File.Exists(under));
        model.Answer(false);
        Assert.True(model.CanBringInAFile && model.CanWriteChordPro);
        // While the earlier versions of a song are being looked at.
        model.ShowEarlierVersions();
        Assert.NotNull(model.Earlier);
        Assert.False(model.CanBringInAFile || model.CanWriteChordPro);
        model.BringIn(ASong("plain-song.cho"));
        Assert.Null(model.NewSong);
        model.CloseEarlierVersions();
        // While what Mend All did is still to be read.
        model.Filter = MarkFilter.WantingMending;
        model.AskToMendAll();
        await model.MendAllAsync();
        Assert.True(model.MendingAll is { Finished: true });
        Assert.False(model.CanBringInAFile || model.CanWriteChordPro);
        model.CloseMendAll();
        model.Filter = MarkFilter.All;
        Assert.True(model.CanBringInAFile && model.CanWriteChordPro);
        // And while a new song is itself being asked about: a second file is not read
        // over the first.
        model.BringIn(ASong("plain-song.cho"));
        var asked = model.NewSong;
        Assert.False(model.CanBringInAFile || model.CanWriteChordPro);
        model.BringIn(ASong("amazing-grace.cho"));
        Assert.Same(asked, model.NewSong);
        model.DontMakeTheSong();
        Assert.True(model.CanBringInAFile && model.CanWriteChordPro);

        // A library with no songs in it takes one: it is made like the app's own.
        model.ChooseLibrary(pc.Library("Kids"));
        Assert.Empty(model.Songs);
        model.BringIn(ASong("plain-song.cho"));
        Assert.Equal(["A song this app carries"], model.NewSong!.LookAlikes.Select(one => one.Name));
        Assert.Equal(("Kids", "Plain Song", ""), (model.NewSong.Library, model.NewSong.WillBeNamed, model.NewSong.NameNote));
        model.MakeTheSong();
        Assert.Equal(["Plain Song"], model.Songs.Select(song => song.Name));
        Assert.Equal(pc.Song("Plain Song", "Kids"), model.SongId);
        Assert.Equal(
            BoxesOf(Path.Combine(pc.Place, "Libraries", "Songs", "Plain Song.pro")),
            BoxesOf(Path.Combine(pc.Place, "Libraries", "Kids", "Plain Song.pro")));
    });

    [Fact]
    public void TheSongThatWasOpenIsNotLeftIfWhatWasTypedInItCannotBeSavedThoughTheSongIsMade() => OnAMadeUpPC(async pc =>
    {
        var model = await Started(pc, "Plain Song");
        model.Show(LibraryModel.Editor.ChordPro);
        Assert.True(model.TextMayBecome(ChordProModelTests.With("One two", "One [A]two", model)));
        model.BringIn(ASong("amazing-grace.cho"));
        model.NameTheNewSong("Made meanwhile");
        // Something else changes Plain Song's file before the song is made.
        File.WriteAllBytes(Path.Combine(pc.Place, "Libraries", "Songs", "Plain Song.pro"), TestData.InFiles("made-by-simple-presenter/It Is Well.pro"));

        model.MakeTheSong();

        // The song is made and listed; Plain Song is still the song that is open, and
        // says why what was typed was not saved.
        Assert.Null(model.NewSong);
        Assert.Contains("Made meanwhile", model.Songs.Select(song => song.Name));
        Assert.True(File.Exists(Path.Combine(pc.Place, "Libraries", "Songs", "Made meanwhile.pro")));
        Assert.Equal(pc.Song("Plain Song"), model.SongId);
        Assert.Equal("This song's file has been changed by something else since it was opened here. Nothing was changed. Open the song again to see it as it is now.", model.Refused);
    });

    // ---- Writing one (rules 17 to 19)

    [Fact]
    public void TheSongThatIsOpenIsWrittenAsAChordProFileWhereThePersonSaysAndTheLibraryIsNotTouched() => OnAMadeUpPC(async pc =>
    {
        var model = await Started(pc);
        // With no song open there is nothing to write.
        Assert.False(model.CanWriteChordPro);
        var nowhere = Path.Combine(pc.Documents, "Nothing.cho");
        model.WriteAsChordPro(nowhere);
        Assert.False(File.Exists(nowhere));
        model.Open(pc.Song("Nonsense"));
        Assert.False(model.CanWriteChordPro);

        model.Open(pc.Song("Plain Song"));
        Assert.True(model.CanWriteChordPro);
        Assert.Equal("Plain Song.cho", model.NameForAChordProFile);
        var before = Everything(pc);
        var path = Path.Combine(pc.Documents, "Plain Song.cho");

        model.WriteAsChordPro(path);

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
            File.ReadAllText(path));
        Assert.Equal(Q("Plain Song") + " was written as Plain Song.cho.", model.Notice);
        Assert.Contains("Wrote Plain Song.cho.", pc.Log, StringComparison.Ordinal);
        Assert.Null(model.Refused);
        // Nothing in the library was touched, and no copy was kept: nothing was changed.
        Assert.Equal(before, Everything(pc));
        Assert.Empty(pc.AppData.Copies().Songs());
        // Read as a file, it is the song.
        var back = ChordProFile.Read(path);
        Assert.Equal(("Plain Song", "G", 3), (back.Title, back.Key, back.Sections.Count));

        // What is being typed in the song is saved first, and is in the file.
        model.Show(LibraryModel.Editor.ChordPro);
        Assert.True(model.TextMayBecome(ChordProModelTests.With("Thirteen", "[Am7]Thirteen", model)));
        var second = Path.Combine(pc.Documents, "Again.cho");
        model.WriteAsChordPro(second);
        Assert.Contains("\n[Am7]Thirteen\n", File.ReadAllText(second), StringComparison.Ordinal);
        Assert.Contains("Am7", new OpenSong(Path.Combine(pc.Place, "Libraries", "Songs", "Plain Song.pro")).UsedChords);
        Assert.Equal(Q("Plain Song") + " was written as Again.cho.", model.Notice);

        // Where it cannot be written, that is said, and nothing is said to be written.
        var into = Path.Combine(pc.Documents, "No such folder", "Plain Song.cho");
        model.WriteAsChordPro(into);
        Assert.StartsWith("Plain Song.cho could not be written: ", model.Refused, StringComparison.Ordinal);
        Assert.False(File.Exists(into));
        Assert.Contains("Could not write Plain Song.cho.", pc.Log, StringComparison.Ordinal);

        // A name that a file cannot have is offered as one it can.
        model.Open(pc.Song("A Song - In A Minor Key"));
        Assert.Equal("A Song - In A Minor Key.cho", model.NameForAChordProFile);
    });
}
