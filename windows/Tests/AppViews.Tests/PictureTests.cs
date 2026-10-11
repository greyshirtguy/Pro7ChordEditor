// Pictures of the window, to be looked at (Pictures.cs says where they go).
//
// A test here cannot tell whether a picture looks right: only eyes can, and whoever
// changes how something is drawn is to open the pictures and say what was seen
// (spec/10-platforms.md). What a test CAN tell is that a picture was drawn at all, at
// the size asked for, and has something in it; a picture that failed to draw would
// otherwise be a blank one that nobody happened to open.
//
// Each is the whole view the window holds, on a made-up PC (MadeUpPC.cs), in the dark
// look or the light one as the test says, whatever this PC is set to. So far
// (milestone M3): the lists, and a song shown to be looked at. From M5 on, the
// editors, and the cases spec/10 lists.

using Pro7ChordEditor.Song;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class PictureTests
{
    // Draws the window's view of a model that has been brought to some state, and
    // holds the picture to what a test can tell of one.
    private static void Draw(string name, bool dark, Func<MadeUpPC, LibraryModel, Task> bring, double width = 1280, double height = 800) =>
        Pictures.OnAThreadOfItsOwn(async () =>
        {
            using var pc = new MadeUpPC();
            var model = pc.Model();
            var view = new LibraryView(model);
            Look.Wear(view, dark);
            await bring(pc, model);
            await model.MarksAreWorkedOutAsync();

            var path = Pictures.Draw(view, width, height, name);

            var picture = Pictures.Read(path);
            Assert.Equal(((int)width * Pictures.Scale, (int)height * Pictures.Scale), (picture.Width, picture.Height));
            // Grounds of several greys, words, and the shades between that smooth the
            // letters' edges: some hundreds of colours. A view that drew nothing would
            // be one.
            Assert.True(picture.Colours > 100, $"{path} has only {picture.Colours} colours in it: was anything drawn?");
        });

    private static async Task ASongOpen(MadeUpPC pc, LibraryModel model, string song)
    {
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        model.Open(pc.Song(song));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheWindowAsItOpens(bool dark) => Draw(
        "01 the window as it opens, " + (dark ? "dark" : "light"),
        dark,
        async (pc, model) =>
        {
            await model.StartAsync();
            model.ChooseLibrary(pc.Library("Songs"));
        });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ASongOpenToBeLookedAt(bool dark) => Draw(
        "02 a song open, " + (dark ? "dark" : "light"),
        dark,
        (pc, model) => ASongOpen(pc, model, "Amazing Grace"));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheBandWhileProPresenterIsRunning(bool dark) => Draw(
        "03 ProPresenter running, " + (dark ? "dark" : "light"),
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
        "05 no place found, " + (dark ? "dark" : "light"),
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
            model.Add(System.IO.Path.Combine(pc.Place, "Libraries"));
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
            model.Add(System.IO.Directory.CreateDirectory(System.IO.Path.Combine(pc.Folder, "Nothing in it")).FullName);
        });
}
