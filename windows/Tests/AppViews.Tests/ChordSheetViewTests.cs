// The chord sheet in its view: what the pointer and the keys do there, and what is then
// drawn where (spec/03-chord-sheet.md).
//
// What a click or a key does to a song is Song/ChordSheet.cs's, and is held to the
// vectors with no window (Song.Tests/ChordSheetTests.cs). These hold what is the
// view's: that the real things are laid out where the grid says, that a point of the
// sheet is the place the tail then stands on, that the bubble opens where the chord is
// for and takes what is typed, that a chord is carried only after the pointer has gone
// a little way, and that each thing done reaches the song's file.
//
// Each is done as a person does it, by a hand at the sheet (Hand.cs), on a made-up PC
// (MadeUpPC.cs), in the window's own view laid out at a size and never shown.

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Pro7ChordEditor.Song;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class ChordSheetViewTests
{
    private static void OnAMadeUpPC(Func<MadeUpPC, Task> what) => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        await what(pc);
    });

    // The window's view with a song open on its sheet, laid out, and a hand at it.
    private static async Task<(LibraryModel Model, LibraryView View, Hand Hand)> Open(MadeUpPC pc, string name, double width = 1280, double height = 800)
    {
        var model = pc.Model();
        var view = new LibraryView(model);
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        model.Open(pc.Song(name));
        Look.Wear(view, dark: true);
        Pictures.LayOut(view, width, height);
        return (model, view, new Hand(view.SongPane.ChordSheet.Canvas));
    }

    // A box of a song's file as "place:name" for each chord.
    private static string InTheFile(int box, string name, MadeUpPC pc) =>
        string.Join(" ", new OpenSong(pc.Song(name)).TextBoxes[box].Chords.Select(chord => chord.At + ":" + chord.Name));

    private static string Said(FormattableString what) => what.ToString(CultureInfo.InvariantCulture);

    // ---- The bubble a chord is typed in

    [Fact]
    public void TheBubbleIsAsTallAsWhatIsOfferedNeedsAndGrowsUpwardsFromWhereTheChordWillStand() => OnAMadeUpPC(async pc =>
    {
        var (_, view, hand) = await Open(pc, "Amazing Grace");
        var canvas = hand.Canvas;
        Assert.False(hand.BubbleIsOpen);

        // A click on a place of a line opens it there.
        var place = hand.PlaceOf("saved");
        hand.Click(place);
        Pictures.LayOut(view, 1280, 800);
        Assert.True(hand.BubbleIsOpen);
        Assert.True(hand.Sheet.Typing);
        Assert.Equal(place, hand.Sheet.TypingAt);

        // "D/" typed: six are offered, on one row. The bubble is 330 wide, and as tall
        // as the box (24, and 14 round it) and one row of what is offered (26, and 8).
        hand.Type("D/");
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal("D/", hand.Box.Text);
        Assert.Equal(["D/F#", "D/A", "D/G", "D/B", "D/C", "D/E"], canvas.Entry.Shown);
        var frame = canvas.EntryFrame;
        var wrap = (FrameworkElement)canvas.Entry.FindName("Offered");
        Assert.True(
            Math.Abs(frame.Height - 72) <= 2 && frame.Width == SheetLook.EntryWidth,
            Said($"the bubble is {frame.Width} by {frame.Height}; what is offered is {wrap.ActualWidth} by {wrap.ActualHeight} (asked {wrap.DesiredSize}); the box is {hand.Box.ActualHeight} tall; the bubble itself asked {canvas.Entry.DesiredSize} and has {canvas.Entry.ActualWidth} by {canvas.Entry.ActualHeight}"));
        // Its box stands where the chord will: on the row of chords of its line.
        var line = hand.Grid.Lines[place.Row];
        Assert.Equal(line.ChordTop + canvas.Metrics.BubbleHeight + 3, frame.Bottom, 0.01);
        Assert.Equal(hand.Grid.XOf(place), frame.Left, 0.01);

        // What could not be on the way to a chord is not taken: the box is as it was,
        // and its caret where it was.
        hand.Type("!");
        Assert.Equal("D/", hand.Box.Text);
        Assert.Equal((2, 0), (hand.Box.SelectionStart, hand.Box.SelectionLength));
        Assert.Equal("D/", hand.Sheet.Entry.Text);
        // With nothing offered the bubble is the box alone: 24, and 14 round it.
        hand.Type("#");
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal("D/#", hand.Box.Text);
        Assert.Empty(canvas.Entry.Shown);
        Assert.Equal(38, canvas.EntryFrame.Height, 2.0);
        Assert.Equal(line.ChordTop + canvas.Metrics.BubbleHeight + 3, canvas.EntryFrame.Bottom, 0.01);
        // Esc throws the typing away, and the bubble is gone.
        hand.Press(Key.Escape);
        Pictures.LayOut(view, 1280, 800);
        Assert.False(hand.BubbleIsOpen);
        Assert.False(hand.Sheet.Typing);
    });
}
