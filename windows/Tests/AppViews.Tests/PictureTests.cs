// Pictures of the window, to be looked at (Pictures.cs says where they go).
//
// A test here cannot tell whether a picture looks right: only eyes can, and whoever
// changes how something is drawn is to open the pictures and say what was seen
// (spec/10-platforms.md). What a test CAN tell is that a picture was drawn at all, at
// the size asked for, and has something in it; a picture that failed to draw would
// otherwise be a blank one that nobody happened to open.
//
// Each is a whole view, on a made-up PC (MadeUpPC.cs), in the dark look or the light
// one as the test says, whatever this PC is set to. So far: the lists, a song shown to
// be looked at, its earlier versions, and All Copies. From M5 on, the editors, and the
// cases spec/10 lists.

using System.IO;
using System.Windows;
using Pro7ChordEditor.Song;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class PictureTests
{
    // Draws a view that a test makes on a made-up PC, and holds the picture to what a
    // test can tell of one.
    private static void DrawView(string name, bool dark, Func<MadeUpPC, Task<FrameworkElement>> make, double width = 1280, double height = 800) =>
        Pictures.OnAThreadOfItsOwn(async () =>
        {
            using var pc = new MadeUpPC();
            var view = await make(pc);
            Look.Wear(view, dark);

            var path = Pictures.Draw(view, width, height, name);

            var picture = Pictures.Read(path);
            Assert.Equal(((int)width * Pictures.Scale, (int)height * Pictures.Scale), (picture.Width, picture.Height));
            // Grounds of several greys, words, and the shades between that smooth the
            // letters' edges: some hundreds of colours. A view that drew nothing would
            // be one.
            Assert.True(picture.Colours > 100, $"{path} has only {picture.Colours} colours in it: was anything drawn?");
        });

    // The same, of the window's own view, of a model that a test has brought to some state.
    private static void Draw(string name, bool dark, Func<MadeUpPC, LibraryModel, Task> bring, double width = 1280, double height = 800) =>
        DrawView(
            name,
            dark,
            async pc =>
            {
                var model = pc.Model();
                var view = new LibraryView(model);
                await bring(pc, model);
                await model.MarksAreWorkedOutAsync();
                return view;
            },
            width,
            height);

    private static string In(bool dark) => dark ? "dark" : "light";

    private static async Task ASongOpen(MadeUpPC pc, LibraryModel model, string song)
    {
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        model.Open(pc.Song(song));
    }

    // ---- The lists, and a song (M3)

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheWindowAsItOpens(bool dark) => Draw(
        "01 the window as it opens, " + In(dark),
        dark,
        async (pc, model) =>
        {
            await model.StartAsync();
            model.ChooseLibrary(pc.Library("Songs"));
        });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ASongOpenToBeLookedAt(bool dark) => Draw("02 a song open, " + In(dark), dark, (pc, model) => ASongOpen(pc, model, "Amazing Grace"));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheBandWhileProPresenterIsRunning(bool dark) => Draw(
        "03 ProPresenter running, " + In(dark),
        dark,
        async (pc, model) =>
        {
            pc.ProPresenterIsRunning = true;
            await ASongOpen(pc, model, "Plain Song");
        });

    [Fact]
    public void AFileThatCannotBeRead() => Draw("04 a file that cannot be read", true, (pc, model) => ASongOpen(pc, model, "Nonsense"));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NoPlaceFound(bool dark) => Draw(
        "05 no place found, " + In(dark),
        dark,
        async (pc, model) =>
        {
            pc.HasAProPresenterFolder = false;
            await model.StartAsync();
        });

    [Fact]
    public void TheListNarrowedByWhatIsTypedAndByMark() => Draw(
        "06 the list narrowed",
        true,
        async (pc, model) =>
        {
            await ASongOpen(pc, model, "Holy Holy Holy");
            await model.MarksAreWorkedOutAsync();
            model.Typed = "o";
            model.Filter = MarkFilter.WithChords;
        });

    [Fact]
    public void TheOldEditorsCopiesAndAChosenFolder() => Draw(
        "07 the old editor's copies, in a chosen folder",
        false,
        async (pc, model) =>
        {
            await model.StartAsync();
            model.Add(Path.Combine(pc.Place, "Libraries"));
            model.ChooseLibrary(model.Libraries.Single(library => library.Name == "Old copies").Id);
            model.Open(model.Songs.Single(song => song.Name == "It Is Well (Chords)").Id);
        });

    [Fact]
    public void ASongWithBlankSlidesAndChordsWithNoWords() => Draw(
        "08 blank slides and chords with no words",
        true,
        (pc, model) => ASongOpen(pc, model, "Chords With No Words"));

    [Fact]
    public void TheWindowAtItsSmallest() => Draw(
        "09 the window at its smallest",
        true,
        (pc, model) => ASongOpen(pc, model, "Amazing Grace"),
        width: 640,
        height: 440);

    [Fact]
    public void TheBarDraggedSoThatTheLibrariesHaveMostOfTheHeight() => Draw(
        "10 the bar dragged down",
        false,
        async (pc, model) =>
        {
            await ASongOpen(pc, model, "Blank Slides");
            model.SetListsSplit(0.7);
        });

    [Fact]
    public void APlaceWithNoLibrariesInIt() => Draw(
        "11 a place with no libraries",
        true,
        async (pc, model) =>
        {
            await model.StartAsync();
            model.Add(Directory.CreateDirectory(Path.Combine(pc.Folder, "Nothing in it")).FullName);
        });

    // ---- Earlier versions (M4)

    private static async Task TheSongWithAHistoryOpen(MadeUpPC pc, LibraryModel model)
    {
        pc.GivePlainSongAHistory();
        await ASongOpen(pc, model, "Plain Song");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ASongThatHasEarlierVersions(bool dark) => Draw("12 a song that has earlier versions, " + In(dark), dark, TheSongWithAHistoryOpen);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheEarlierVersionsOfASong(bool dark) => Draw(
        "13 earlier versions, " + In(dark),
        dark,
        async (pc, model) =>
        {
            await TheSongWithAHistoryOpen(pc, model);
            model.ShowEarlierVersions();
            model.PickCopy(model.Earlier!.Copies[^1].Id);
        });

    [Fact]
    public void ACopyOfAnotherSongIsAskedAboutFirst() => Draw(
        "14 a copy of another song",
        true,
        async (pc, model) =>
        {
            pc.GivePlainSongAHistory();
            var elsewhere = Path.Combine(pc.Folder, "Was there once.pro");
            File.WriteAllBytes(elsewhere, pc.BytesOf("Amazing Grace"));
            pc.AppData.Copies().Keep(elsewhere, new KeptSong(MadeUpPC.PlaceName, "Songs", "Plain Song"), CopyReason.BeforeAChange, DateTime.UtcNow.AddDays(-40));
            await ASongOpen(pc, model, "Plain Song");
            model.ShowEarlierVersions();
            model.PickCopy(model.Earlier!.Copies.Single(copy => copy.SongName == "Amazing Grace").Id);
            model.PutBackPicked();
        });

    [Fact]
    public void AVersionJustPutBackWithTheOfferToUndo() => Draw(
        "15 a version just put back",
        false,
        async (pc, model) =>
        {
            await TheSongWithAHistoryOpen(pc, model);
            model.ShowEarlierVersions();
            model.PickCopy(model.Earlier!.Copies[^1].Id);
            model.PutBackPicked();
        });

    [Fact]
    public void NothingPutBackWhileProPresenterIsRunning() => Draw(
        "16 not put back, ProPresenter running",
        true,
        async (pc, model) =>
        {
            await TheSongWithAHistoryOpen(pc, model);
            model.ShowEarlierVersions();
            pc.ProPresenterIsRunning = true;
            model.PutBackPicked();
        });

    [Fact]
    public void AFileChangedBySomethingElseAndTheOfferToOpenAgain() => Draw(
        "17 changed by something else",
        true,
        async (pc, model) =>
        {
            await TheSongWithAHistoryOpen(pc, model);
            model.ShowEarlierVersions();
            File.WriteAllBytes(pc.Song("Plain Song"), pc.BytesOf("It Is Well"));
            model.PutBackPicked();
            model.CloseEarlierVersions();
        },
        width: 900,
        height: 560);

    // ---- All Copies, in the settings (M4)

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AllCopies(bool dark) => DrawView(
        "18 all copies, " + In(dark),
        dark,
        pc =>
        {
            pc.GivePlainSongAHistory();
            pc.Change("Holy Holy Holy", 1, song => song.SetKey("D"));
            pc.Change("Amazing Grace (Chords)", 40, song => song.SetKey("A"), "Old copies");
            File.Delete(pc.Song("Holy Holy Holy"));
            var model = new AllCopiesModel(pc.AppData.Copies());
            var view = new AllCopiesView(model);
            model.PickSong("Documents/Songs/Plain Song");
            return Task.FromResult<FrameworkElement>(view);
        },
        width: 960,
        height: 520);

    [Fact]
    public void AllCopiesWithNoneKept() => DrawView(
        "19 all copies, none kept",
        true,
        pc => Task.FromResult<FrameworkElement>(new AllCopiesView(new AllCopiesModel(pc.AppData.Copies()))),
        width: 960,
        height: 520);
}
