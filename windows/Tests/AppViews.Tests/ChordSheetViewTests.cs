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
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
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

    private static void Click(ButtonBase button) => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

    private static T Named<T>(FrameworkElement within, string name)
        where T : class =>
        within.FindName(name) as T ?? throw new InvalidOperationException("there is no " + name + " there");

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
        // And it stays there, wherever along the line the pointer goes meanwhile, and
        // the spot with the pointer.
        hand.PointAt("wretch");
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal(hand.PlaceOf("wretch"), hand.Sheet.Spot);
        Assert.Equal(place, hand.Sheet.TypingAt);
        Assert.Equal(frame, canvas.EntryFrame);

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
        // (Placed again for its new height, it is still where its chord will stand, and
        // not where the pointer went.)
        Assert.Equal(hand.Grid.XOf(place), canvas.EntryFrame.Left, 0.01);
        Assert.NotEqual(hand.Grid.XOf(hand.Sheet.Spot), hand.Grid.XOf(place));
        // Esc throws the typing away, and the bubble is gone.
        hand.Press(Key.Escape);
        Pictures.LayOut(view, 1280, 800);
        Assert.False(hand.BubbleIsOpen);
        Assert.False(hand.Sheet.Typing);
    });

    // The sheet is at least as big as what is seen of it, so that a click beside a short
    // song, or under it, is a click on the sheet still.
    [Fact]
    public void TheSheetFillsWhatIsSeenOfItHoweverShortTheSong() => OnAMadeUpPC(async pc =>
    {
        var (_, view, hand) = await Open(pc, "Plain Song");
        var scroll = Named<ScrollViewer>(view.SongPane.ChordSheet, "Scroll");
        // (Four slides in one row: the song is far less tall than what is seen, and
        // less wide.)
        Assert.True(hand.Grid.Content.Height < scroll.ViewportHeight - 100);
        Assert.True(hand.Grid.Content.Width < scroll.ViewportWidth - 50);
        Assert.True(hand.Canvas.ActualHeight >= scroll.ViewportHeight - 0.5, "the sheet is " + hand.Canvas.ActualHeight + " tall, and " + scroll.ViewportHeight + " is seen");
        Assert.True(hand.Canvas.ActualWidth >= scroll.ViewportWidth - 0.5, "the sheet is " + hand.Canvas.ActualWidth + " wide, and " + scroll.ViewportWidth + " is seen");
    });

    // ---- The spot, and the pointer

    // Amazing Grace's first slide, as the file has it: its two lines are one text
    // box's words, and a chord's place is counted along the whole box. The second line
    // starts at 34.
    private const string FirstSlide = "1:G 8:G7 18:C 28:G 59:D";

    [Fact]
    public void TheSpotFollowsThePointerToTheCharacterUnderItAndWithShiftToTheWordAndOverABubbleToItsChord() => OnAMadeUpPC(async pc =>
    {
        var (_, _, hand) = await Open(pc, "Amazing Grace");
        var sheet = hand.Sheet;
        // As a song opens there is no spot yet.
        Assert.False(sheet.Usable(sheet.Spot.Row));

        // Over the third letter of a word: that character.
        var wretch = hand.PlaceOf("wretch");
        hand.PointAt(new Spot(wretch.Row, wretch.At + 2));
        Assert.Equal(new Spot(wretch.Row, wretch.At + 2), sheet.Spot);
        Assert.Same(Cursors.Arrow, hand.Canvas.Cursor);
        // With Shift: the start of its word.
        hand.PointAt(new Spot(wretch.Row, wretch.At + 2), shift: true);
        Assert.Equal(wretch, sheet.Spot);
        // Over the space between two words: the word after.
        hand.PointAt(new Spot(wretch.Row, wretch.At - 1));
        Assert.Equal(wretch, sheet.Spot);

        // Over a chord's bubble: that chord, wherever in the bubble; and the pointer
        // shows that there is something to take hold of.
        var sweet = hand.PlaceOf("sweet");
        Assert.Equal("C", sheet.ChordAt(sweet.Row, sweet.At));
        var bubble = hand.PointOf(sweet, onItsChord: true);
        hand.Canvas.PointerMoved(bubble, shift: false);
        Assert.Equal(sweet, sheet.Spot);
        Assert.Same(Cursors.Hand, hand.Canvas.Cursor);
        hand.Canvas.PointerMoved(new Point(bubble.X + 6, bubble.Y - 4), shift: true);
        Assert.Equal(sweet, sheet.Spot);
        hand.PointAt(wretch);
        Assert.Same(Cursors.Arrow, hand.Canvas.Cursor);

        // Off the cards the spot stays where it was: on a frame's foot, and on the bare
        // sheet between the slides.
        var frame = hand.Grid.Cells[0].Frame;
        hand.Canvas.PointerMoved(new Point(frame.Left + 20, frame.Bottom - 8), shift: false);
        Assert.Equal(wretch, sheet.Spot);
        hand.Canvas.PointerMoved(Hand.BareSheet, shift: false);
        Assert.Equal(wretch, sheet.Spot);
        // And the pointer, gone there from over a chord, shows nothing to take hold of.
        hand.Canvas.PointerMoved(bubble, shift: false);
        Assert.Same(Cursors.Hand, hand.Canvas.Cursor);
        hand.Canvas.PointerMoved(Hand.BareSheet, shift: false);
        Assert.Same(Cursors.Arrow, hand.Canvas.Cursor);
        Assert.Equal(sweet, sheet.Spot);

        // Over the room above a card's first line it is that line's, and under its
        // last, the last's.
        var card = hand.Grid.Cells[0].Card;
        hand.Canvas.PointerMoved(new Point(hand.PointOf(sweet).X, card.Top + 2), shift: true);
        Assert.Equal(sweet, sheet.Spot);
        hand.Canvas.PointerMoved(new Point(hand.PointOf(wretch).X, card.Bottom - 2), shift: true);
        Assert.Equal(wretch, sheet.Spot);
    });

    [Fact]
    public void APlaceAlongARealLineNeverGoesBackwardsAndThePointOverAPlaceIsThatPlace() => OnAMadeUpPC(async pc =>
    {
        // With the letters of a real font, of every song there is, at three sizes.
        foreach (var name in MadeUpPC.SongsWithChords)
        {
            var (model, view, hand) = await Open(pc, name);
            foreach (var steps in new[] { 0, -3, 4 })
            {
                model.ZoomSheet(0);
                for (var step = 0; step < Math.Abs(steps); step++)
                {
                    model.ZoomSheet(Math.Sign(steps));
                }
                Pictures.LayOut(view, 1280, 800);
                var sheet = hand.Sheet;
                var grid = hand.Grid;
                var metrics = hand.Canvas.Metrics;
                for (var row = 0; row < sheet.Rows.Count; row++)
                {
                    if (!sheet.Usable(row) || sheet.Rows[row].Alone)
                    {
                        continue;
                    }
                    var item = grid.Lines[row];
                    var text = sheet.Rows[row].Text;
                    var among = item.ChordTop + metrics.BubbleHeight + metrics.Tail + (metrics.WordHeight / 2);
                    var last = -1;
                    for (var x = item.Left - 3; x <= item.Left + metrics.Advance(row, text.Length) + 3; x += 0.75)
                    {
                        var place = grid.PlaceAt(new Point(x, among), shift: false);
                        Assert.True(place is not null && place.Value.Row == row, Said($"{name}: no place of row {row} at {x}"));
                        Assert.True(place!.Value.At >= last, Said($"{name}, row {row}: back from {last} to {place.Value.At} at {x}"));
                        last = place.Value.At;
                    }
                    // Every character that is not a space can be pointed at. The spot
                    // then stands where that character starts; or right of there, where
                    // the bubble of a chord before it on the line is in its way (rule
                    // 11), which on a line with no chords is never.
                    for (var at = 0; at < text.Length; at++)
                    {
                        if (char.IsWhiteSpace(text[at]) || char.IsLowSurrogate(text[at]) || metrics.Advance(row, at + 1) - metrics.Advance(row, at) < 0.5)
                        {
                            continue;
                        }
                        hand.PointAt(new Spot(row, at));
                        Assert.True(sheet.Spot == new Spot(row, at), Said($"{name}, row {row}: pointed at {at} and the spot is at {sheet.Spot.At}"));
                        var starts = item.Left + metrics.Advance(row, at);
                        var stands = grid.XOf(sheet.Spot);
                        Assert.True(
                            sheet.Rows[row].Chords.Count == 0 ? Math.Abs(stands - starts) < 0.001 : stands >= starts - 0.001 && stands < starts + 120,
                            Said($"{name}, row {row} at {at}: the character starts at {starts} and the spot stands at {stands}"));
                    }
                }
            }
        }
    });

    // ---- 1 to 7, Delete, and undo

    [Fact]
    public void PointAndPressFourPutsTheKeysFourthChordThereAndDeleteTakesItOffAndBothCanBeUndone() => OnAMadeUpPC(async pc =>
    {
        var (model, view, hand) = await Open(pc, "Amazing Grace");
        Assert.Equal(FirstSlide, InTheFile(0, "Amazing Grace", pc));
        Assert.False(model.CanUndo);
        Assert.Equal(LibraryModel.SheetHelp, model.Notice);

        // No click first: the pointer over a word, and a key.
        hand.PointAt("wretch");
        Assert.True(hand.Press(Key.D4));
        Assert.Equal("1:G 8:G7 18:C 28:G 47:C 59:D", InTheFile(0, "Amazing Grace", pc));
        Assert.Equal("13:C 25:D", hand.ChordsOf("wretch"));
        Assert.False(hand.BubbleIsOpen);
        Assert.True(model.CanUndo);
        Assert.True(model.ChangedSinceOpened);
        // In place of whatever is there: 5 on the same word.
        Assert.True(hand.Press(Key.NumPad5));
        Assert.Equal("1:G 8:G7 18:C 28:G 47:D 59:D", InTheFile(0, "Amazing Grace", pc));
        // 8, 9 and 0 are no chord of a key.
        Assert.False(hand.Press(Key.D8));
        Assert.False(hand.Press(Key.D0));

        // Delete takes off the chord at the spot, and so does Backspace.
        Assert.True(hand.Press(Key.Delete));
        Assert.Equal(FirstSlide, InTheFile(0, "Amazing Grace", pc));
        hand.PointAt(hand.PlaceOf("sweet"), onItsChord: true);
        Assert.True(hand.Press(Key.Back));
        Assert.Equal("1:G 8:G7 28:G 59:D", InTheFile(0, "Amazing Grace", pc));
        // Where there is none, nothing is taken, and nothing is a step of undo.
        hand.PointAt("saved");
        hand.Press(Key.Delete);
        Assert.Equal("1:G 8:G7 28:G 59:D", InTheFile(0, "Amazing Grace", pc));

        // Ctrl+Z takes back the last change, one for each thing done; Ctrl+Y and
        // Ctrl+Shift+Z put it back.
        Assert.True(hand.Press(Key.Z, ModifierKeys.Control));
        Assert.Equal(FirstSlide, InTheFile(0, "Amazing Grace", pc));
        Assert.True(hand.Press(Key.Z, ModifierKeys.Control));
        Assert.Equal("1:G 8:G7 18:C 28:G 47:D 59:D", InTheFile(0, "Amazing Grace", pc));
        Assert.True(hand.Press(Key.Z, ModifierKeys.Control));
        Assert.Equal("1:G 8:G7 18:C 28:G 47:C 59:D", InTheFile(0, "Amazing Grace", pc));
        Assert.True(hand.Press(Key.Y, ModifierKeys.Control));
        Assert.Equal("1:G 8:G7 18:C 28:G 47:D 59:D", InTheFile(0, "Amazing Grace", pc));
        Assert.True(hand.Press(Key.Z, ModifierKeys.Control));
        Assert.True(hand.Press(Key.Z, ModifierKeys.Control));
        Assert.Equal(FirstSlide, InTheFile(0, "Amazing Grace", pc));
        Assert.False(model.CanUndo);
        Assert.True(model.CanRedo);
        // With nothing left to take back the key does nothing.
        hand.Press(Key.Z, ModifierKeys.Control);
        Assert.Equal(FirstSlide, InTheFile(0, "Amazing Grace", pc));
        Assert.True(hand.Press(Key.Z, ModifierKeys.Control | ModifierKeys.Shift));
        Assert.Equal("1:G 8:G7 18:C 28:G 47:C 59:D", InTheFile(0, "Amazing Grace", pc));
        // And the sheet shows what the file has, each time.
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal("13:C 25:D", hand.ChordsOf("wretch"));
        Assert.Same(hand.Sheet.Rows, hand.Grid.Rows);
    });

    // ---- The arrow keys

    [Fact]
    public void TheArrowKeysMoveTheSpotAWordAtATimeAndWithShiftACharacterAndDownAndUpByTheScreen() => OnAMadeUpPC(async pc =>
    {
        var (_, _, hand) = await Open(pc, "Amazing Grace");
        var sheet = hand.Sheet;
        // With no spot yet, the first arrow key puts it on the first word of the song.
        Assert.True(hand.Press(Key.Right));
        var amazing = hand.PlaceOf("Amazing");
        Assert.Equal(amazing, sheet.Spot);
        // Right: the next start of a word; Left: the one before.
        hand.Press(Key.Right);
        Assert.Equal(hand.PlaceOf("grace"), sheet.Spot);
        hand.Press(Key.Right);
        Assert.Equal(hand.PlaceOf("how"), sheet.Spot);
        hand.Press(Key.Left);
        Assert.Equal(hand.PlaceOf("grace"), sheet.Spot);
        // With Shift, a character at a time, spaces passed over.
        hand.Press(Key.Right, ModifierKeys.Shift);
        Assert.Equal(new Spot(amazing.Row, 9), sheet.Spot);
        hand.Press(Key.Left, ModifierKeys.Shift);
        hand.Press(Key.Left, ModifierKeys.Shift);
        Assert.Equal(new Spot(amazing.Row, 6), sheet.Spot);
        // From the end of a line Right goes to the first word of the next line.
        hand.PointAt("sound");
        hand.Press(Key.Right);
        Assert.Equal(hand.PlaceOf("That saved"), sheet.Spot);
        hand.Press(Key.Left);
        Assert.Equal(hand.PlaceOf("sound"), sheet.Spot);

        // Down and Up go by the screen: three slides to a row here, so that under the
        // first slide is the fourth, not the second.
        Assert.Equal(3, hand.Grid.Columns);
        hand.PointAt("That saved");
        hand.Press(Key.Down);
        Assert.Equal(hand.PlaceOf("How precious").Row, sheet.Spot.Row);
        Assert.Equal(3, sheet.Rows[sheet.Spot.Row].Slide);
        Assert.Equal(3, sheet.Picked);
        hand.Press(Key.Up);
        Assert.Equal(hand.PlaceOf("That saved").Row, sheet.Spot.Row);
        Assert.Equal(0, sheet.Picked);
        hand.Press(Key.Up);
        Assert.Equal(amazing.Row, sheet.Spot.Row);
        // From the first row of slides Up goes nowhere.
        var before = sheet.Spot;
        hand.Press(Key.Up);
        Assert.Equal(before, sheet.Spot);
        // Right from a slide's last word goes on to the next slide of the song, which
        // is beside it.
        hand.PointAt("me");
        hand.Press(Key.Right);
        Assert.Equal(hand.PlaceOf("I once"), sheet.Spot);
        Assert.Equal(1, sheet.Picked);
    });

    // ---- Typing a chord

    [Fact]
    public void ALetterOpensTheBubbleWithThatLetterAndEnterKeepsTheChord() => OnAMadeUpPC(async pc =>
    {
        var (model, view, hand) = await Open(pc, "Amazing Grace");
        var wretch = hand.PlaceOf("wretch");
        hand.PointAt(wretch);
        // A small letter, as a keyboard gives one: it is a capital in the box, with the
        // caret after it and nothing selected.
        hand.Type("e");
        Assert.True(hand.BubbleIsOpen);
        Assert.Equal(wretch, hand.Sheet.TypingAt);
        Assert.Equal("E", hand.Box.Text);
        Assert.Equal((1, 0), (hand.Box.SelectionStart, hand.Box.SelectionLength));
        // What it might be going to be is offered, the first fourteen at the most.
        Assert.NotEmpty(hand.Canvas.Entry.Shown);
        Assert.True(hand.Canvas.Entry.Shown.Count <= 14);
        Assert.All(hand.Canvas.Entry.Shown, name => Assert.StartsWith("E", name, StringComparison.Ordinal));
        Assert.Equal(-1, hand.Canvas.Entry.Picked);
        // What is typed is tidied as it is typed.
        hand.Type("M7");
        Assert.Equal("EM7", hand.Box.Text);
        hand.Press(Key.Back);
        hand.Press(Key.Back);
        hand.Type("m");
        Assert.Equal("Em", hand.Box.Text);
        // Nothing is in the file until it is kept.
        Assert.Equal(FirstSlide, InTheFile(0, "Amazing Grace", pc));
        Assert.False(model.CanUndo);

        // Enter keeps it, and the bubble is gone.
        Assert.True(hand.Press(Key.Enter));
        Assert.False(hand.BubbleIsOpen);
        Assert.False(hand.Sheet.Typing);
        Assert.Equal("1:G 8:G7 18:C 28:G 47:Em 59:D", InTheFile(0, "Amazing Grace", pc));
        Assert.Equal(wretch, hand.Sheet.Spot);

        // Enter, or F2, opens it on what is at the spot: the chord there, all of it
        // selected to be typed over.
        Assert.True(hand.Press(Key.Enter));
        Assert.True(hand.BubbleIsOpen);
        Assert.Equal("Em", hand.Box.Text);
        Assert.Equal((0, 2), (hand.Box.SelectionStart, hand.Box.SelectionLength));
        // Esc throws the typing away and changes nothing.
        hand.Type("A");
        Assert.Equal("A", hand.Box.Text);
        Assert.True(hand.Press(Key.Escape));
        Assert.False(hand.BubbleIsOpen);
        Assert.Equal("1:G 8:G7 18:C 28:G 47:Em 59:D", InTheFile(0, "Amazing Grace", pc));
        // Where there is none it opens empty.
        hand.PointAt("saved");
        Assert.True(hand.Press(Key.F2));
        Assert.True(hand.BubbleIsOpen);
        Assert.Equal("", hand.Box.Text);
        // What is kept must be a chord: "H" is not taken into the box at all, and
        // Enter on an empty box puts nothing on.
        hand.Type("H");
        Assert.Equal("", hand.Box.Text);
        hand.Press(Key.Enter);
        Assert.False(hand.BubbleIsOpen);
        Assert.Equal("1:G 8:G7 18:C 28:G 47:Em 59:D", InTheFile(0, "Amazing Grace", pc));
        // A slash typed on the way to a bass note that never came is left off.
        hand.Type("g");
        hand.Type("/");
        Assert.Equal("G/", hand.Box.Text);
        hand.Press(Key.Enter);
        Assert.Equal("1:G 8:G7 18:C 28:G 39:G 47:Em 59:D", InTheFile(0, "Amazing Grace", pc));
        // At most sixteen characters are taken.
        hand.PointAt("That saved");
        hand.Type("A");
        hand.Type("m7b5/C#m7b5m7b5m7b5");
        Assert.Equal(16, hand.Box.Text.Length);
        hand.Press(Key.Escape);
        Pictures.LayOut(view, 1280, 800);
        Assert.False(hand.BubbleIsOpen);
    });

    [Fact]
    public void AClickOnAnotherPlaceKeepsWhatWasTypedAndOpensTheBubbleThereAndTabKeepsItAndMovesTheSpotOn() => OnAMadeUpPC(async pc =>
    {
        var (_, _, hand) = await Open(pc, "Amazing Grace");
        var sheet = hand.Sheet;
        // Click, type, click, type: each click keeps the chord before it.
        hand.Click("saved");
        Assert.True(hand.BubbleIsOpen);
        Assert.Equal(hand.PlaceOf("saved"), sheet.TypingAt);
        Assert.Equal("", hand.Box.Text);
        hand.Type("Am");
        var begun = sheet.TypingsBegun;
        hand.Click("wretch");
        Assert.Equal("1:G 8:G7 18:C 28:G 39:Am 59:D", InTheFile(0, "Amazing Grace", pc));
        Assert.True(hand.BubbleIsOpen);
        Assert.Equal(hand.PlaceOf("wretch"), sheet.TypingAt);
        Assert.True(sheet.TypingsBegun > begun);
        Assert.Equal("", hand.Box.Text);
        hand.Type("C");
        // While a chord is being typed the spot goes on following the pointer, and the
        // place being typed for is remembered apart from it.
        hand.PointAt("lost");
        Assert.Equal(hand.PlaceOf("lost"), sheet.Spot);
        Assert.Equal(hand.PlaceOf("wretch"), sheet.TypingAt);
        Assert.Equal("C", hand.Box.Text);
        // A click on another chord's bubble keeps it too, and opens the bubble on that
        // chord, all of it selected.
        hand.Click(hand.PlaceOf("me"), onItsChord: true);
        Assert.Equal("1:G 8:G7 18:C 28:G 39:Am 47:C 59:D", InTheFile(0, "Amazing Grace", pc));
        Assert.Equal(hand.PlaceOf("me"), sheet.TypingAt);
        Assert.Equal("D", hand.Box.Text);
        Assert.Equal((0, 1), (hand.Box.SelectionStart, hand.Box.SelectionLength));
        // Typed over, and a click on the bare sheet keeps it and closes the bubble.
        hand.Type("D7");
        Assert.Equal("D7", hand.Box.Text);
        hand.ClickAt(Hand.BareSheet);
        Assert.False(hand.BubbleIsOpen);
        Assert.Equal("1:G 8:G7 18:C 28:G 39:Am 47:C 59:D7", InTheFile(0, "Amazing Grace", pc));

        // Tab keeps the chord and moves the spot on to the next word, wherever the
        // pointer has been meanwhile. The bubble is gone: a letter opens it there.
        hand.Click("That saved");
        hand.Type("G");
        hand.PointAt("found");
        Assert.True(hand.Press(Key.Tab));
        Assert.Equal("1:G 8:G7 18:C 28:G 34:G 39:Am 47:C 59:D7", InTheFile(0, "Amazing Grace", pc));
        Assert.False(hand.BubbleIsOpen);
        Assert.Equal(hand.PlaceOf("saved"), sheet.Spot);
        // A slash on the end, typed on the way to a bass note that never came, is left
        // off what Tab keeps, as it is off what Enter keeps.
        hand.Press(Key.Right);
        Assert.Equal(new Spot(hand.PlaceOf("saved").Row, 11), sheet.Spot);
        hand.Type("Bb/");
        Assert.Equal(new Spot(hand.PlaceOf("saved").Row, 11), sheet.TypingAt);
        Assert.Equal("Bb/", hand.Box.Text);
        // Shift with Tab would take the keyboard out of the box by the back door: it is
        // swallowed, and nothing is kept.
        Assert.True(hand.Press(Key.Tab, ModifierKeys.Shift));
        Assert.Equal("Bb/", hand.Box.Text);
        Assert.True(sheet.Typing);
        Assert.True(hand.Press(Key.Tab));
        Assert.Equal("1:G 8:G7 18:C 28:G 34:G 39:Am 45:Bb 47:C 59:D7", InTheFile(0, "Amazing Grace", pc));
        Assert.Equal(hand.PlaceOf("wretch"), sheet.Spot);
        Assert.False(hand.BubbleIsOpen);

        // If nothing is picked and what is typed is not yet a chord, Tab takes the
        // first that is offered. (Only nothing is not yet a chord: whatever the box
        // takes that is something, is one. NOTES.md, "At M5".)
        hand.Click("like");
        Assert.Equal("", hand.Box.Text);
        var first = hand.Canvas.Entry.Shown[0];
        Assert.True(hand.Press(Key.Tab));
        Assert.Equal("1:G 8:G7 18:C 28:G 34:G 39:Am 45:Bb 47:C 54:" + first + " 59:D7", InTheFile(0, "Amazing Grace", pc));
        Assert.Equal(hand.PlaceOf("me"), sheet.Spot);
        // And Esc is the one thing that throws the typing away.
        hand.Click("like");
        Assert.Equal(first, hand.Box.Text);
        hand.Type("F#m");
        Assert.True(hand.Press(Key.Escape));
        Assert.False(hand.BubbleIsOpen);
        Assert.Equal("1:G 8:G7 18:C 28:G 34:G 39:Am 45:Bb 47:C 54:" + first + " 59:D7", InTheFile(0, "Amazing Grace", pc));
    });

    [Fact]
    public void DownAndUpGoThroughWhatIsOfferedAndRightPutsTheOnePickedInTheBoxToBeGoneOnWith() => OnAMadeUpPC(async pc =>
    {
        var (_, _, hand) = await Open(pc, "Amazing Grace");
        var entry = hand.Canvas.Entry;
        hand.Click("wretch");
        hand.Type("D");
        var offered = entry.Shown.ToList();
        Assert.True(offered.Count > 2);
        Assert.Equal(-1, entry.Picked);
        // Each is a button; none is lit until one is picked.
        List<Border> Buttons() => [.. Named<WrapPanel>(entry, "Offered").Children.OfType<Border>()];
        static Color Of(Border button) => ((SolidColorBrush)button.Background).Color;
        Assert.Equal(offered, Buttons().Select(button => ((TextBlock)button.Child).Text));
        Assert.All(Buttons(), button => Assert.Equal(SheetLook.Chip, Of(button)));
        // Down picks the first, and the next; Up goes back; and round and round. The
        // one picked is lit in the sheet's orange, and no other.
        Assert.True(hand.Press(Key.Down));
        Assert.Equal(0, entry.Picked);
        Assert.Equal(SheetLook.Accent, Of(Buttons()[0]));
        Assert.All(Buttons().Skip(1), button => Assert.Equal(SheetLook.Chip, Of(button)));
        hand.Press(Key.Down);
        Assert.Equal(1, entry.Picked);
        hand.Press(Key.Up);
        Assert.Equal(0, entry.Picked);
        hand.Press(Key.Up);
        Assert.Equal(offered.Count - 1, entry.Picked);
        hand.Press(Key.Down);
        Assert.Equal(0, entry.Picked);
        // What is typed is still what was typed.
        Assert.Equal("D", hand.Box.Text);
        // Right, at the end of the box with one picked, puts that one in the box to be
        // gone on with.
        hand.Press(Key.Down);
        var picked = offered[1];
        // Not at the end of the box, Right is the box's own key, which moves the caret:
        // nothing is taken, and the one picked stays picked.
        hand.Box.Select(0, 0);
        Assert.False(hand.Canvas.KeyInTheBox(Key.Right, ModifierKeys.None));
        Assert.Equal("D", hand.Box.Text);
        Assert.Equal(1, entry.Picked);
        // Nor with what is in the box selected.
        hand.Box.Select(0, 1);
        Assert.False(hand.Canvas.KeyInTheBox(Key.Right, ModifierKeys.None));
        Assert.Equal("D", hand.Box.Text);
        // At the end of the box, it takes the one picked.
        hand.Box.Select(1, 0);
        Assert.True(hand.Press(Key.Right));
        Assert.Equal(picked, hand.Box.Text);
        Assert.Equal((picked.Length, 0), (hand.Box.SelectionStart, hand.Box.SelectionLength));
        Assert.True(hand.Sheet.Typing);
        Assert.Equal(-1, entry.Picked);

        // Enter keeps the one picked, and not what is typed.
        hand.Press(Key.Escape);
        hand.Click("wretch");
        hand.Type("D");
        hand.Press(Key.Down);
        hand.Press(Key.Down);
        hand.Press(Key.Enter);
        Assert.Equal("13:" + offered[1] + " 25:D", hand.ChordsOf("wretch"));
        // A click on one that is offered keeps that one.
        hand.Click("saved");
        hand.Type("E");
        var wanted = entry.Shown[2];
        Buttons()[2].RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonDownEvent });
        Assert.False(hand.Sheet.Typing);
        Assert.Equal("5:" + wanted + " 13:" + offered[1] + " 25:D", hand.ChordsOf("wretch"));
        // Backspace in an empty box takes off the chord at that place.
        hand.Click(hand.PlaceOf("saved"), onItsChord: true);
        Assert.Equal(wanted, hand.Box.Text);
        hand.Press(Key.Back);
        Assert.Equal("", hand.Box.Text);
        Assert.True(hand.Sheet.Typing);
        Assert.True(hand.Press(Key.Back));
        Assert.False(hand.Sheet.Typing);
        Assert.Equal("13:" + offered[1] + " 25:D", hand.ChordsOf("wretch"));
    });

    // ---- Dragging a chord

    // The song's next three slides, as the file has them.
    private const string SecondSlide = "2:G 11:G7 20:C 27:G 37:Em 47:D 53:G";
    private const string ThirdSlide = "5:G 16:G7 26:C 35:G 61:D";
    private const string FourthSlide = "4:G 13:G7 22:C 30:G 39:Em 46:D 54:G";

    [Fact]
    public void AChordIsCarriedOnceThePointerHasGoneSixWithTheButtonDownAndLetGoItMovesThere() => OnAMadeUpPC(async pc =>
    {
        var (model, _, hand) = await Open(pc, "Amazing Grace");
        var sheet = hand.Sheet;
        var canvas = hand.Canvas;
        Assert.Equal((FirstSlide, SecondSlide), (InTheFile(0, "Amazing Grace", pc), InTheFile(1, "Amazing Grace", pc)));
        var me = hand.PlaceOf("me");
        var wretch = hand.PlaceOf("wretch");

        // Pressed on a bubble and moved 5: nothing is carried yet. Let go, it was a
        // click on the chord, and the bubble opens on it.
        var on = hand.PointOf(me, onItsChord: true);
        canvas.PointerMoved(on, shift: false);
        canvas.Pressed(on, shift: false);
        canvas.Dragged(new Point(on.X + 3, on.Y + 2), shift: false);
        Assert.Null(sheet.Dragged);
        canvas.Released(new Point(on.X + 3, on.Y + 2), shift: false, copy: false);
        Assert.True(hand.BubbleIsOpen);
        Assert.Equal("D", hand.Box.Text);
        hand.Press(Key.Escape);
        Assert.Equal(FirstSlide, InTheFile(0, "Amazing Grace", pc));

        // Moved 6, it is carried: the one left behind is remembered, the spot follows
        // the pointer, and nothing is changed until it is let go.
        canvas.Pressed(on, shift: false);
        canvas.Dragged(new Point(on.X + 4, on.Y + 2), shift: false);
        Assert.Equal((me, "D"), sheet.Dragged);
        Assert.Same(Cursors.SizeAll, canvas.Cursor);
        canvas.Dragged(hand.PointOf(wretch), shift: false);
        Assert.Equal(wretch, sheet.Spot);
        Assert.Equal(FirstSlide, InTheFile(0, "Amazing Grace", pc));
        Assert.False(model.CanUndo);
        // Over another chord's bubble it is the character there, not that chord.
        var sweet = hand.PlaceOf("sweet");
        canvas.Dragged(new Point(hand.PointOf(sweet, onItsChord: true).X + 9, hand.PointOf(sweet, onItsChord: true).Y), shift: false);
        Assert.Equal(sweet.Row, sheet.Spot.Row);
        Assert.NotEqual(sweet.At, sheet.Spot.At);
        // Let go on "wretch": the chord moves there, as one change.
        canvas.Dragged(hand.PointOf(wretch), shift: false);
        canvas.Released(hand.PointOf(wretch), shift: false, copy: false);
        Assert.Null(sheet.Dragged);
        Assert.Same(Cursors.Arrow, canvas.Cursor);
        Assert.Equal("1:G 8:G7 18:C 28:G 47:D", InTheFile(0, "Amazing Grace", pc));
        Assert.False(hand.BubbleIsOpen);
        hand.Press(Key.Z, ModifierKeys.Control);
        Assert.Equal(FirstSlide, InTheFile(0, "Amazing Grace", pc));
        Assert.False(model.CanUndo);

        // With Ctrl held as it is let go, it is copied and the first stays.
        hand.Drag(sweet, hand.PlaceOf("saved"), copy: true);
        Assert.Equal("1:G 8:G7 18:C 28:G 39:C 59:D", InTheFile(0, "Amazing Grace", pc));
        // Whatever chord was at the place it lands on gives way to it.
        hand.Drag(hand.PlaceOf("grace"), sweet);
        Assert.Equal("1:G 18:G7 28:G 39:C 59:D", InTheFile(0, "Amazing Grace", pc));
        // To another line it is put there and taken from here: two changes that undo as one.
        hand.Drag(hand.PlaceOf("sound"), hand.PlaceOf("like"));
        Assert.Equal("1:G 18:G7 39:C 54:G 59:D", InTheFile(0, "Amazing Grace", pc));
        hand.Press(Key.Z, ModifierKeys.Control);
        Assert.Equal("1:G 18:G7 28:G 39:C 59:D", InTheFile(0, "Amazing Grace", pc));
        // And to another slide, which is another text box of the file.
        hand.Drag(hand.PlaceOf("sound"), hand.PlaceOf("was lost"));
        Assert.Equal(("1:G 18:G7 39:C 59:D", "2:G 7:G 11:G7 20:C 27:G 37:Em 47:D 53:G"), (InTheFile(0, "Amazing Grace", pc), InTheFile(1, "Amazing Grace", pc)));
        hand.Press(Key.Z, ModifierKeys.Control);
        Assert.Equal(("1:G 18:G7 28:G 39:C 59:D", SecondSlide), (InTheFile(0, "Amazing Grace", pc), InTheFile(1, "Amazing Grace", pc)));

        // Dropped where it started, nothing happens, and there is nothing more to undo.
        hand.Press(Key.Z, ModifierKeys.Control);
        hand.Press(Key.Z, ModifierKeys.Control);
        Assert.Equal(FirstSlide, InTheFile(0, "Amazing Grace", pc));
        Assert.False(model.CanUndo);
        hand.Drag(me, hand.PlaceOf("like"), letGo: false);
        canvas.Dragged(hand.PointOf(me), shift: false);
        canvas.Released(hand.PointOf(me), shift: false, copy: false);
        Assert.Null(sheet.Dragged);
        Assert.Equal(FirstSlide, InTheFile(0, "Amazing Grace", pc));
        Assert.False(model.CanUndo);
        // Let go off the cards, it goes where the spot last was.
        hand.Drag(me, hand.PlaceOf("like"), letGo: false);
        canvas.Dragged(Hand.BareSheet, shift: false);
        canvas.Released(Hand.BareSheet, shift: false, copy: false);
        Assert.Equal("1:G 8:G7 18:C 28:G 54:D", InTheFile(0, "Amazing Grace", pc));
    });

    [Fact]
    public void AChordCannotBeDraggedWhileAnotherIsBeingTyped() => OnAMadeUpPC(async pc =>
    {
        var (_, _, hand) = await Open(pc, "Amazing Grace");
        var sheet = hand.Sheet;
        hand.Click("saved");
        hand.Type("Am");
        // Pressed on a chord and moved a long way: nothing is carried.
        var me = hand.PlaceOf("me");
        hand.Drag(me, hand.PlaceOf("wretch"), letGo: false);
        Assert.Null(sheet.Dragged);
        Assert.True(sheet.Typing);
        Assert.Equal(hand.PlaceOf("saved"), sheet.TypingAt);
        // Let go, it is a click on that chord: what was typed is kept, and the bubble
        // opens on the chord that was pressed.
        hand.Canvas.Released(hand.PointOf(hand.PlaceOf("wretch")), shift: false, copy: false);
        Assert.Equal("1:G 8:G7 18:C 28:G 39:Am 59:D", InTheFile(0, "Amazing Grace", pc));
        Assert.True(hand.BubbleIsOpen);
        Assert.Equal(me, sheet.TypingAt);
        Assert.Equal("D", hand.Box.Text);
    });

    // ---- A line's chords onto another line

    private const string CopiedSays = ": Ctrl+V puts them on the lines from the spot's on, word for word";

    [Fact]
    public void ALinesChordsAreCopiedAndPutOnAnotherLineWordForWordAndTheQuietLineSaysWhatCameOfIt() => OnAMadeUpPC(async pc =>
    {
        var (model, view, hand) = await Open(pc, "Amazing Grace");
        var quiet = (TextBlock)view.SongPane.FindName("Quiet");
        Assert.Equal((ThirdSlide, FourthSlide), (InTheFile(2, "Amazing Grace", pc), InTheFile(3, "Amazing Grace", pc)));
        // With nothing copied yet, Ctrl+V says so.
        hand.PointAt("Twas");
        hand.Press(Key.V, ModifierKeys.Control);
        Assert.Equal("Nothing has been copied yet. Ctrl+C copies the chords of the line the spot is on.", model.Notice);
        Assert.Equal(SongPane.WithKeysWhole(model.Notice), quiet.Text);

        // Ctrl+C copies the chords of the spot's line, and says what was copied.
        hand.PointAt("Amazing");
        hand.Press(Key.C, ModifierKeys.Control);
        Assert.Equal("4 chords copied from the line" + CopiedSays, model.Notice);
        Assert.Equal(SongPane.WithKeysWhole(model.Notice), quiet.Text);
        Assert.Equal(FirstSlide, InTheFile(0, "Amazing Grace", pc));
        // Ctrl+V puts them on the spot's line: each on the same numbered word, as far
        // into it as it was. The line's own chords are replaced.
        hand.PointAt("heart");
        hand.Press(Key.V, ModifierKeys.Control);
        Assert.Equal("1:G 5:G7 16:C 26:G 61:D", InTheFile(2, "Amazing Grace", pc));
        Assert.Equal("4 chords put on the line.", model.Notice);
        Assert.Equal("1:G 5:G7 16:C 26:G", hand.ChordsOf("Twas"));
        // On a line with fewer words, those for words it has not got are left out.
        hand.PointAt("And grace");
        hand.Press(Key.V, ModifierKeys.Control);
        Assert.Equal("1:G 5:G7 16:C 26:G 41:G 44:G7 53:C", InTheFile(2, "Amazing Grace", pc));
        Assert.Equal("3 chords put on the line. 1 had no word to go on, and was left out.", model.Notice);
        // A line of chords alone takes none of a line of words, and that is said.
        var alone = Enumerable.Range(0, hand.Sheet.Rows.Count).Last(row => hand.Sheet.Rows[row].Alone);
        hand.Canvas.KeyPressed(new SheetKey(SheetKeyKind.Undo));
        hand.Sheet.SetSpot(alone, 0);
        hand.Press(Key.V, ModifierKeys.Control);
        Assert.Equal(
            "Nothing was put here: chords copied from a line of words go only onto a line of words, and chords with no words only onto a line with none.",
            model.Notice);
        // What is said goes after a few seconds, and the line says what the keys do.
        model.SayingLasts = TimeSpan.FromMilliseconds(1);
        hand.PointAt("That saved");
        hand.Press(Key.C, ModifierKeys.Control);
        Assert.Equal("1 chord copied from the line" + CopiedSays, model.Notice);
        await model.WhatWasSaidIsGoneAsync();
        Assert.Equal(LibraryModel.SheetHelp, model.Notice);
        Assert.Equal(SongPane.WithKeysWhole(LibraryModel.SheetHelp), quiet.Text);
    });

    [Fact]
    public void AWholeGroupsChordsAreCopiedAndPastedLineForLineAndThePasteUndoesAsOne() => OnAMadeUpPC(async pc =>
    {
        var (model, _, hand) = await Open(pc, "Amazing Grace");
        // Ctrl+Shift+C: every line from the group's name down to the next group's. The
        // first verse is two slides of two lines.
        hand.PointAt("wretch");
        hand.Press(Key.C, ModifierKeys.Control | ModifierKeys.Shift);
        Assert.Equal("12 chords copied from 4 lines" + CopiedSays, model.Notice);
        // Ctrl+V puts them on the lines from the spot's line on, one copied line to
        // each: the second verse's four, in two text boxes.
        hand.PointAt("Twas");
        hand.Press(Key.V, ModifierKeys.Control);
        Assert.Equal("9 chords put on 4 lines. 3 had no word to go on, and were left out.", model.Notice);
        Assert.Equal(("1:G 5:G7 16:C 26:G", "4:G 17:G7 28:C 39:Em 46:D"), (InTheFile(2, "Amazing Grace", pc), InTheFile(3, "Amazing Grace", pc)));
        // However many lines it changed, one undo takes it all back.
        hand.Press(Key.Z, ModifierKeys.Control);
        Assert.Equal((ThirdSlide, FourthSlide), (InTheFile(2, "Amazing Grace", pc), InTheFile(3, "Amazing Grace", pc)));
        Assert.False(model.CanUndo);
        // And one redo puts it all back.
        hand.Press(Key.Y, ModifierKeys.Control);
        Assert.Equal(("1:G 5:G7 16:C 26:G", "4:G 17:G7 28:C 39:Em 46:D"), (InTheFile(2, "Amazing Grace", pc), InTheFile(3, "Amazing Grace", pc)));
    });

    [Fact]
    public void AClickOnALineAndThenCtrlVPastesThereWhileNothingHasBeenTypedInTheBox() => OnAMadeUpPC(async pc =>
    {
        var (model, _, hand) = await Open(pc, "Amazing Grace");
        var canvas = hand.Canvas;
        // What was last copied on this PC, by any program: a number that goes up.
        var copiedOnThisPC = 7u;
        canvas.WhatWasCopiedLast = () => copiedOnThisPC;
        hand.PointAt("Amazing");
        hand.Press(Key.C, ModifierKeys.Control);

        // A click on a line opens the bubble there; Ctrl+V then closes it, changing
        // nothing of its own, and pastes from that line on.
        hand.Click("heart");
        Assert.True(hand.BubbleIsOpen);
        Assert.True(hand.Press(Key.V, ModifierKeys.Control));
        Assert.False(hand.BubbleIsOpen);
        Assert.False(hand.Sheet.Typing);
        Assert.Equal("1:G 5:G7 16:C 26:G 61:D", InTheFile(2, "Amazing Grace", pc));
        Assert.Equal("4 chords put on the line.", model.Notice);
        // So it does after a click on a chord: nothing has been typed over it.
        hand.Click(new Spot(hand.PlaceOf("And grace").Row, 21), onItsChord: true);
        Assert.Equal("D", hand.Box.Text);
        Assert.True(hand.Press(Key.V, ModifierKeys.Control));
        Assert.Equal("1:G 5:G7 16:C 26:G 41:G 44:G7 53:C", InTheFile(2, "Amazing Grace", pc));

        // With anything typed in the box, the key is the box's own: nothing is pasted,
        // and the typing goes on.
        hand.Click("How precious");
        hand.Type("C");
        Assert.False(hand.Press(Key.V, ModifierKeys.Control));
        Assert.True(hand.Sheet.Typing);
        Assert.Equal(FourthSlide, InTheFile(3, "Amazing Grace", pc));
        hand.Press(Key.Escape);
        // With anything copied elsewhere since, it is the box's own too: what Ctrl+V
        // would paste is no longer a line's chords.
        copiedOnThisPC = 8;
        hand.Click("How precious");
        Assert.False(hand.Press(Key.V, ModifierKeys.Control));
        Assert.True(hand.Sheet.Typing);
        Assert.Equal(FourthSlide, InTheFile(3, "Amazing Grace", pc));

        // Ctrl+C in a box that is empty, which has nothing of its own to copy, copies
        // the chords of the bubble's line, and the bubble stays open.
        Assert.Equal("", hand.Box.Text);
        Assert.True(hand.Press(Key.C, ModifierKeys.Control));
        Assert.Equal("4 chords copied from the line" + CopiedSays, model.Notice);
        Assert.True(hand.BubbleIsOpen);
        Assert.Equal(hand.PlaceOf("How precious"), hand.Sheet.TypingAt);
        // And Ctrl+Shift+C those of its group: the second verse's four lines.
        Assert.True(hand.Press(Key.C, ModifierKeys.Control | ModifierKeys.Shift));
        Assert.StartsWith("14 chords copied from 4 lines", model.Notice, StringComparison.Ordinal);
        Assert.True(hand.BubbleIsOpen);
        // Copied here just now, a line's chords are again the last thing copied.
        Assert.True(hand.Press(Key.V, ModifierKeys.Control));
        Assert.False(hand.BubbleIsOpen);
        // In a box with a chord in it, Ctrl+C is the box's own: it copies the name.
        hand.Click(hand.PlaceOf("precious"), onItsChord: true);
        Assert.NotEqual("", hand.Box.Text);
        Assert.False(hand.Press(Key.C, ModifierKeys.Control));
        Assert.True(hand.BubbleIsOpen);

        // The keys that zoom are the sheet's whether or not the bubble is open.
        var zoom = model.SheetZoom;
        Assert.True(hand.Press(Key.OemPlus, ModifierKeys.Control));
        Assert.True(model.SheetZoom > zoom);
        Assert.True(hand.Press(Key.D0, ModifierKeys.Control));
        Assert.Equal(LibraryModel.UsualSheetZoom, model.SheetZoom);
        Assert.True(hand.BubbleIsOpen);
        Assert.True(hand.Sheet.Typing);
    });

    // More of rule 52b, written after its parts were broken in turn.
    [Fact]
    public void CtrlCInTheBoxCopiesTheBubblesLineAndCtrlVChangesNothingOfItsOwnAndOnlyPastesWhatWasCopiedInThisSong() => OnAMadeUpPC(async pc =>
    {
        var (model, view, hand) = await Open(pc, "Amazing Grace");
        hand.Canvas.WhatWasCopiedLast = () => 7u;
        var third = InTheFile(2, "Amazing Grace", pc);

        // Ctrl+C in an empty box copies the bubble's line, wherever the pointer has gone
        // meanwhile: "That saved a wretch like me" has one chord, and "Amazing grace how
        // sweet the sound", which the pointer is then over, has four.
        hand.Click("saved");
        Assert.Equal("", hand.Box.Text);
        hand.PointAt("Amazing");
        Assert.NotEqual(hand.Sheet.TypingAt.Row, hand.Sheet.Spot.Row);
        Assert.True(hand.Press(Key.C, ModifierKeys.Control));
        Assert.Equal("1 chord copied from the line" + CopiedSays, model.Notice);
        Assert.True(hand.BubbleIsOpen);
        hand.Press(Key.Escape);

        // Ctrl+V after a click changes nothing of its own: not even where one of the
        // chords offered was picked with Down first. One undo takes the paste back, and
        // there is nothing else to take back.
        hand.PointAt("Amazing");
        hand.Press(Key.C, ModifierKeys.Control);
        hand.Click("heart");
        hand.Press(Key.Down);
        Assert.Equal(0, hand.Canvas.Entry.Picked);
        Assert.True(hand.Press(Key.V, ModifierKeys.Control));
        Assert.False(hand.BubbleIsOpen);
        Assert.Equal("1:G 5:G7 16:C 26:G 61:D", InTheFile(2, "Amazing Grace", pc));
        hand.Press(Key.Z, ModifierKeys.Control);
        Assert.Equal(third, InTheFile(2, "Amazing Grace", pc));
        Assert.False(model.CanUndo);

        // What was copied is the sheet's own, of the song it was copied in. In another
        // song nothing has been copied yet, and Ctrl+V in the box is the box's own,
        // though nothing has been copied anywhere else on the PC since.
        model.Open(pc.Song("It Is Well"));
        Pictures.LayOut(view, 1280, 800);
        Assert.Null(hand.Sheet.Copied);
        hand.Click("peace");
        Assert.True(hand.BubbleIsOpen);
        Assert.False(hand.Press(Key.V, ModifierKeys.Control));
        Assert.True(hand.BubbleIsOpen);
        Assert.True(hand.Sheet.Typing);
    });

    // ---- Leaving the sheet

    [Fact]
    public void LeavingTheSheetForAnythingElseKeepsTheChordThatWasBeingTyped() => OnAMadeUpPC(async pc =>
    {
        var (model, view, hand) = await Open(pc, "Amazing Grace");
        // The keyboard goes from the box to something else in the window.
        hand.Click("saved");
        hand.Type("Am");
        hand.Box.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, Environment.TickCount, hand.Box, null) { RoutedEvent = Keyboard.LostKeyboardFocusEvent });
        Assert.False(hand.Sheet.Typing);
        Assert.False(hand.BubbleIsOpen);
        Assert.Equal("1:G 8:G7 18:C 28:G 39:Am 59:D", InTheFile(0, "Amazing Grace", pc));

        // The window is left for another program: its model is told to settle.
        hand.Click("wretch");
        hand.Type("C");
        Assert.True(model.Settle());
        Pictures.LayOut(view, 1280, 800);
        Assert.False(hand.BubbleIsOpen);
        Assert.Equal("1:G 8:G7 18:C 28:G 39:Am 47:C 59:D", InTheFile(0, "Amazing Grace", pc));

        // The other editor is shown.
        hand.Click("like");
        hand.Type("Em");
        Assert.True(view.KeyPressed(Key.D2, ModifierKeys.Control));
        Assert.Equal(LibraryModel.Editor.ChordPro, model.EditorShown);
        Assert.Equal("1:G 8:G7 18:C 28:G 39:Am 47:C 54:Em 59:D", InTheFile(0, "Amazing Grace", pc));
        Assert.Contains("That [Am]saved a [C]wretch [Em]like [D]me", model.ChordPro!.Text, StringComparison.Ordinal);
        Assert.True(view.KeyPressed(Key.D1, ModifierKeys.Control));
        Pictures.LayOut(view, 1280, 800);
        Assert.False(hand.BubbleIsOpen);
        Assert.Equal("5:Am 13:C 20:Em 25:D", hand.ChordsOf("wretch"));

        // Another song is opened.
        hand.Click("That saved");
        hand.Type("G");
        model.Open(pc.Song("Plain Song"));
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal("1:G 8:G7 18:C 28:G 34:G 39:Am 47:C 54:Em 59:D", InTheFile(0, "Amazing Grace", pc));
        Assert.False(hand.BubbleIsOpen);
        Assert.Equal("Plain Song", model.Shown!.Name);
        Assert.Same(model.Sheet, hand.Canvas.Sheet);
        Assert.False(hand.Sheet.Typing);
        Assert.Contains(hand.Sheet.Rows, row => row.Text == "Thirteen");
    });

    [Fact]
    public void TheTwoEditorsKeepToOneSong() => OnAMadeUpPC(async pc =>
    {
        var (model, view, hand) = await Open(pc, "Amazing Grace");
        model.PauseBeforeSaving = TimeSpan.FromMilliseconds(1);
        var pane = view.SongPane;
        // A chord put on, on the sheet; then the song as ChordPro text has it.
        hand.PointAt("wretch");
        hand.Press(Key.D4);
        pane.Switch.Choose(LibraryModel.Editor.ChordPro);
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal(LibraryModel.Editor.ChordPro, model.EditorShown);
        Assert.Equal(LibraryModel.Editor.ChordPro, pane.Switch.Filled);
        Assert.Equal(Visibility.Collapsed, pane.ChordSheet.Visibility);
        Assert.Equal(Visibility.Visible, ((FrameworkElement)pane.FindName("Editor")).Visibility);
        Assert.Contains("That saved a [C]wretch like [D]me", pane.ChordPro.TextBox.Text, StringComparison.Ordinal);
        Assert.Equal("7 slides, 41 chords. A change is saved a moment after it is typed.", model.Notice);

        // A chord typed in, in the text; then the sheet has it.
        var typist = new Typist(pane.ChordPro, model);
        typist.CaretAt(typist.PlaceOf("saved"));
        typist.Type("[Em");
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal("1:G 8:G7 18:C 28:G 39:Em 47:C 59:D", InTheFile(0, "Amazing Grace", pc));
        pane.Switch.Choose(LibraryModel.Editor.Sheet);
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal(LibraryModel.Editor.Sheet, pane.Switch.Filled);
        Assert.Equal(Visibility.Visible, pane.ChordSheet.Visibility);
        Assert.Equal(Visibility.Collapsed, ((FrameworkElement)pane.FindName("Editor")).Visibility);
        Assert.Equal("5:Em 13:C 25:D", hand.ChordsOf("wretch"));
        Assert.Same(hand.Sheet.Rows, hand.Grid.Rows);
        Assert.Equal(LibraryModel.SheetHelp, model.Notice);

        // Undone on the sheet, where undo is the song's: the text typed is taken back
        // too, and the text shows it when it is next looked at.
        hand.Press(Key.Z, ModifierKeys.Control);
        Assert.Equal("1:G 8:G7 18:C 28:G 47:C 59:D", InTheFile(0, "Amazing Grace", pc));
        pane.Switch.Choose(LibraryModel.Editor.ChordPro);
        Assert.Contains("That saved a [C]wretch like [D]me", pane.ChordPro.TextBox.Text, StringComparison.Ordinal);
        // The text's own undo has nothing of before it was made again from the song.
        Assert.False(pane.ChordPro.CanUndo);
    });

    // ---- How the real thing is laid out

    [Fact]
    public void EverySlideOfASongIsACardThatSaysItsNumberAndItsGroupsNameAndAsManyGoToARowAsFit() => OnAMadeUpPC(async pc =>
    {
        var (_, view, hand) = await Open(pc, "Amazing Grace");
        var grid = hand.Grid;
        var sheet = hand.Sheet;
        Assert.Equal(["1  Verse 1", "2", "3  Verse 2", "4", "5  Verse 3", "6", "7  Ending"], grid.Cells.Select(cell => cell.Label));
        Assert.Equal([0, 1, 2, 3, 4, 5, 6], grid.Cells.Select(cell => cell.Slide));
        // Every card is one size, and every line of the song is in its slide's card.
        Assert.Single(grid.Cells.Select(cell => Math.Round(cell.Frame.Width, 3)).Distinct());
        Assert.Single(grid.Cells.Select(cell => Math.Round(cell.Card.Height, 3)).Distinct());
        for (var row = 0; row < sheet.Rows.Count; row++)
        {
            if (sheet.Rows[row].Kind != SheetRowKind.Line)
            {
                Assert.False(grid.IsLaid(row));
                continue;
            }
            var cell = grid.Cells[grid.Lines[row].Cell];
            Assert.Equal(sheet.Rows[row].Slide, cell.Slide);
            var top = grid.Lines[row].ChordTop;
            Assert.True(top >= cell.Card.Top && top + hand.Canvas.Metrics.LineHeight <= cell.Card.Bottom + 0.001, Said($"row {row} is not within its card"));
            // No line is wider than its card has room for.
            Assert.True(grid.Lines[row].Left + grid.Lines[row].Placed!.Width <= cell.Card.Right - hand.Canvas.Metrics.CardPad + 0.001, Said($"row {row} runs out of its card"));
        }
        // Three to a row in a window of 1280, and the sheet no wider than it is seen.
        var scroll = Named<ScrollViewer>(view.SongPane.ChordSheet, "Scroll");
        Assert.Equal(3, grid.Columns);
        Assert.Equal(scroll.ViewportWidth, grid.SeenWidth, 0.5);
        Assert.True(grid.Content.Width <= scroll.ViewportWidth);
        Assert.Equal(0, scroll.ScrollableWidth);
        Assert.True(scroll.ScrollableHeight > 0);
        // The element is as big as what it is seen through, so that a click anywhere in
        // what is seen is a click on the sheet.
        Assert.True(hand.Canvas.ActualWidth >= scroll.ViewportWidth - 0.5);
        Assert.True(hand.Canvas.ActualHeight >= grid.Content.Height - 0.5);

        // Wider, more go to a row; narrower, fewer; and at the window's least, one.
        Pictures.LayOut(view, 1900, 1000);
        Assert.True(hand.Grid.Columns > 3, "only " + hand.Grid.Columns + " to a row in a window of 1900");
        Assert.Equal(scroll.ViewportWidth, hand.Grid.SeenWidth, 0.5);
        Pictures.LayOut(view, 1000, 800);
        Assert.Equal(2, hand.Grid.Columns);
        Pictures.LayOut(view, 640, 480);
        Assert.Equal(1, hand.Grid.Columns);
        Assert.Equal([0, 1, 2, 3, 4, 5, 6], hand.Grid.Cells.Select(cell => cell.GridRow));
        // The sheet's rows are the song's still: only where they are has changed.
        Assert.Same(sheet.Rows, hand.Grid.Rows);
    });

    // ---- Zooming

    [Fact]
    public void TheSheetZoomsByItsKeysAndItsButtonsAndTheSizeIsRememberedAndTheSlideAtTheTopStaysThere() => OnAMadeUpPC(async pc =>
    {
        var (model, view, hand) = await Open(pc, "Amazing Grace", height: 520);
        var strip = view.SongPane.ChordSheet;
        var smaller = Named<Button>(strip, "Smaller");
        var larger = Named<Button>(strip, "Larger");
        var scroll = Named<ScrollViewer>(strip, "Scroll");
        // How wide the song's first line of words would be at the full size: what the
        // sizes below are held against.
        var atTheFullSize = hand.Grid.Lines[hand.PlaceOf("Amazing").Row].Placed!.WordsWidth / 0.8;

        void Size(double zoom, int? columns = null)
        {
            Pictures.LayOut(view, 1280, 520);
            Assert.Equal(zoom, model.SheetZoom);
            Assert.Equal(zoom, hand.Canvas.Metrics.Scale);
            Assert.Equal(zoom, hand.Grid.Lines[hand.PlaceOf("Amazing").Row].Placed!.WordsWidth / atTheFullSize, 2);
            if (columns is { } wanted)
            {
                Assert.Equal(wanted, hand.Grid.Columns);
            }
        }

        // The usual size is 80 in the hundred.
        Assert.Equal(0.8, LibraryModel.UsualSheetZoom);
        Size(0.8, 3);
        Assert.True(smaller.IsEnabled && larger.IsEnabled);

        // Ctrl with minus: a step down, to 50 and no further. The words are smaller,
        // and more slides go to a row.
        Assert.True(hand.Press(Key.OemMinus, ModifierKeys.Control));
        Size(0.7);
        hand.Press(Key.Subtract, ModifierKeys.Control);
        Size(0.6);
        hand.Press(Key.OemMinus, ModifierKeys.Control);
        Size(0.5);
        Assert.True(hand.Grid.Columns > 3);
        Assert.False(smaller.IsEnabled);
        Assert.True(larger.IsEnabled);
        hand.Press(Key.OemMinus, ModifierKeys.Control);
        Size(0.5);
        // Ctrl with 0: the usual size again.
        Assert.True(hand.Press(Key.D0, ModifierKeys.Control));
        Size(0.8, 3);
        // Ctrl with plus, which is Shift and the equals key, or with the equals key
        // alone, or with the plus of the number keys: a step up, to 150.
        hand.Press(Key.OemPlus, ModifierKeys.Control | ModifierKeys.Shift);
        Size(0.9);
        hand.Press(Key.OemPlus, ModifierKeys.Control);
        Size(1.0);
        hand.Press(Key.Add, ModifierKeys.Control);
        Size(1.1);
        // The two buttons of the strip do the same, and are greyed at the ends.
        Click(larger);
        Size(1.25);
        Click(larger);
        Size(1.5);
        Assert.False(larger.IsEnabled);
        Assert.True(smaller.IsEnabled);
        Assert.True(hand.Grid.Columns < 3);
        Click(larger);
        Size(1.5);
        Click(smaller);
        Size(1.25);
        // Neither button takes the keyboard when it is clicked.
        Assert.False(smaller.Focusable);
        Assert.False(larger.Focusable);

        // The slide that was at the top of what is seen is still there after the
        // slides are laid out again at another size.
        hand.Press(Key.D0, ModifierKeys.Control);
        Size(0.8, 3);
        var fifth = hand.Grid.Cells[4];
        // (A frame keeps 6 clear over it: its top is 6 under its cell's.)
        scroll.ScrollToVerticalOffset(fifth.Frame.Top - 6 + 30);
        Pictures.LayOut(view, 1280, 520);
        Assert.Equal(fifth.Frame.Top - 6 + 30, scroll.VerticalOffset, 0.5);

        // The slide at the top of what is seen: the first, in the order of the song,
        // that has any of itself in sight. And how far down from the top of what is
        // seen a slide starts.
        int AtTheTop() => hand.Grid.Cells.First(cell => cell.Frame.Bottom > scroll.VerticalOffset + 1).Slide;
        double FromTheTop(int slide) => hand.Grid.Cells.Single(cell => cell.Slide == slide).Frame.Top - scroll.VerticalOffset;

        // The first slide of the second row of slides is at the top now, 24 of its
        // frame out of sight.
        Assert.Equal(3, AtTheTop());
        Assert.Equal(-24, FromTheTop(3), 0.5);
        hand.Press(Key.OemPlus, ModifierKeys.Control);
        Size(0.9);
        Assert.Equal(-24, FromTheTop(3), 1.0);
        // Two steps more, one straight after the other.
        hand.Press(Key.OemPlus, ModifierKeys.Control);
        hand.Press(Key.OemPlus, ModifierKeys.Control);
        Size(1.1, 2);
        Assert.Equal(-24, FromTheTop(3), 1.0);
        // Two go to a row now, and the slide before it is beside it: that one is the
        // first in sight. After the window is made narrower, with one slide to a row,
        // it is that one that is still at the top.
        Assert.Equal(2, AtTheTop());
        Assert.Equal(-24, FromTheTop(2), 1.0);
        Pictures.LayOut(view, 900, 520);
        Assert.Equal(1, hand.Grid.Columns);
        Assert.Equal(2, AtTheTop());
        Assert.Equal(-24, FromTheTop(2), 1.0);
        // And wider again, with more to a row than before.
        Pictures.LayOut(view, 1900, 520);
        Assert.True(hand.Grid.Columns >= 3);
        Assert.Equal(-24, FromTheTop(2), 1.0);

        // The size is remembered from one start of the app to the next, and is the
        // app's: nothing in the song's file has changed.
        Assert.Equal(1.1, pc.AppData.ReadSettings().SheetZoom);
        var again = pc.Model();
        await again.StartAsync();
        Assert.Equal(1.1, again.SheetZoom);
        Assert.False(model.ChangedSinceOpened);
        Assert.Equal(FirstSlide, InTheFile(0, "Amazing Grace", pc));
    });

    [Fact]
    public void MovingTheSpotWithTheKeysScrollsTheSheetToKeepItInView() => OnAMadeUpPC(async pc =>
    {
        var (_, view, hand) = await Open(pc, "Amazing Grace", height: 520);
        var scroll = Named<ScrollViewer>(view.SongPane.ChordSheet, "Scroll");
        Assert.Equal(0, scroll.VerticalOffset);
        Assert.True(scroll.ScrollableHeight > 200);

        bool InView()
        {
            Pictures.LayOut(view, 1280, 520);
            var line = hand.Grid.Lines[hand.Sheet.Spot.Row];
            return line.ChordTop >= scroll.VerticalOffset - 0.5 && line.ChordTop + hand.Canvas.Metrics.LineHeight <= scroll.VerticalOffset + scroll.ViewportHeight + 0.5;
        }

        // Down, slide by slide, to the foot of the song: the spot's line is in sight
        // each time, and the sheet has scrolled to keep it so.
        hand.Press(Key.Right);
        Assert.True(InView());
        for (var times = 0; times < 6; times++)
        {
            hand.Press(Key.Down);
            Assert.True(InView(), "after Down " + (times + 1) + " the spot is out of sight");
        }
        Assert.Equal(6, hand.Sheet.Rows[hand.Sheet.Spot.Row].Slide);
        Assert.True(scroll.VerticalOffset > 100);
        // And back up to its head.
        for (var times = 0; times < 6; times++)
        {
            hand.Press(Key.Up);
            Assert.True(InView(), "after Up " + (times + 1) + " the spot is out of sight");
        }
        Assert.Equal(0, hand.Sheet.Rows[hand.Sheet.Spot.Row].Slide);
        // The pointer moving does not scroll the sheet: only the keys do.
        var offset = scroll.VerticalOffset;
        hand.PointAt("grace");
        Pictures.LayOut(view, 1280, 520);
        Assert.Equal(offset, scroll.VerticalOffset);
        // Another song starts from the top.
        hand.Press(Key.Down);
        hand.Press(Key.Down);
        hand.Press(Key.Down);
        Pictures.LayOut(view, 1280, 520);
        Assert.True(scroll.VerticalOffset > 0);
        view.Model.Open(pc.Song("It Is Well"));
        Pictures.LayOut(view, 1280, 520);
        Assert.Equal(0, scroll.VerticalOffset);
    });

    // ---- The strip

    [Fact]
    public void TheStripHasTheKeysSevenChordsAndAClickOnOnePutsItOnTheSpotOrInPlaceOfWhatIsBeingTyped() => OnAMadeUpPC(async pc =>
    {
        var (model, view, hand) = await Open(pc, "Amazing Grace");
        var strip = view.SongPane.ChordSheet;
        Assert.Equal(["1 G", "2 Am", "3 Bm", "4 C", "5 D", "6 Em", "7 F#dim"], strip.ChordsOfTheKey.Select(AutomationProperties.GetName));
        Assert.Equal("Puts C on the spot. The key 4 does the same.", strip.ChordsOfTheKey[3].ToolTip);
        Assert.All(strip.ChordsOfTheKey, button => Assert.False(button.Focusable));
        Assert.Equal(Visibility.Collapsed, Named<TextBlock>(strip, "NoKey").Visibility);

        // A click on one puts it on the spot, as its key does.
        hand.PointAt("wretch");
        Click(strip.ChordsOfTheKey[3]);
        Assert.Equal("1:G 8:G7 18:C 28:G 47:C 59:D", InTheFile(0, "Amazing Grace", pc));
        // While a chord is being typed, it goes where the typing was, in place of what
        // was typed, wherever the pointer has been since.
        hand.Click("saved");
        hand.Type("Am");
        hand.PointAt("lost");
        Click(strip.ChordsOfTheKey[5]);
        Assert.Equal("1:G 8:G7 18:C 28:G 39:Em 47:C 59:D", InTheFile(0, "Amazing Grace", pc));
        Assert.Equal(SecondSlide, InTheFile(1, "Amazing Grace", pc));
        Pictures.LayOut(view, 1280, 800);
        Assert.False(hand.Sheet.Typing);
        Assert.False(hand.BubbleIsOpen);

        // The key is set in the song's header, and the strip's chords go by it. No
        // chord of the song is changed by that.
        var key = Named<ComboBox>(view.SongPane, "Key");
        key.SelectedValue = "D";
        Assert.Equal("D", model.Shown!.Key);
        Assert.Equal(["1 D", "2 Em", "3 F#m", "4 G", "5 A", "6 Bm", "7 C#dim"], strip.ChordsOfTheKey.Select(AutomationProperties.GetName));
        Assert.Equal("1:G 8:G7 18:C 28:G 39:Em 47:C 59:D", InTheFile(0, "Amazing Grace", pc));
        hand.PointAt("like");
        hand.Press(Key.D5);
        Assert.Equal("1:G 8:G7 18:C 28:G 39:Em 47:C 54:A 59:D", InTheFile(0, "Amazing Grace", pc));
        // And what is offered while typing goes by it too.
        hand.Type("f");
        Assert.Contains("F#m", hand.Canvas.Entry.Shown);
        hand.Press(Key.Escape);
    });

    [Fact]
    public void ASongThatStatesNoKeyHasTheChordsOfCAndTheStripSaysSoWhereThereIsRoom() => OnAMadeUpPC(async pc =>
    {
        var (model, view, hand) = await Open(pc, "Holy Holy Holy");
        var strip = view.SongPane.ChordSheet;
        var says = Named<TextBlock>(strip, "NoKey");
        Assert.Equal("", model.Shown!.Key);
        Assert.Equal(["1 C", "2 Dm", "3 Em", "4 F", "5 G", "6 Am", "7 Bdim"], strip.ChordsOfTheKey.Select(AutomationProperties.GetName));
        Assert.Equal(Visibility.Visible, says.Visibility);
        // (With which key its chords look like, which the same sum as doubts a key can
        // say: spec/08-repair.md, check 3.)
        Assert.Equal("The song names no key: pick the one its chords are in. They look like D.", says.Text);
        // 1 to 7 go by C until a key is set.
        hand.PointAt("Almighty");
        hand.Press(Key.D2);
        Assert.Contains(hand.Sheet.Rows[hand.Sheet.Spot.Row].Chords, chord => chord.Name == "Dm");
        // Where the pane is too narrow for the chords and what is said, that is left
        // out; and nothing of the strip is squeezed: the chords are as wide as they were.
        var chords = Named<FrameworkElement>(strip, "Chords");
        var wide = chords.ActualWidth;
        // As the pane is made narrower: first which key the chords look like goes, and
        // the page's words stand alone; then they go too.
        var said = new List<string>();
        for (var width = 1280; width >= 760; width -= 10)
        {
            Pictures.LayOut(view, width, 600);
            var now = says.Visibility == Visibility.Visible ? says.Text : "";
            if (said.Count == 0 || said[^1] != now)
            {
                said.Add(now);
            }
        }
        Assert.Equal(
            ["The song names no key: pick the one its chords are in. They look like D.", "The song names no key: pick the one its chords are in", ""],
            said);
        Pictures.LayOut(view, 760, 600);
        Assert.Equal(Visibility.Collapsed, says.Visibility);
        Assert.Equal(wide, chords.ActualWidth, 0.5);
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal(Visibility.Visible, says.Visibility);
        // With a key picked, there is nothing to say.
        model.SetKey("D");
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal(Visibility.Collapsed, says.Visibility);
        Assert.Equal("1 D", AutomationProperties.GetName(strip.ChordsOfTheKey[0]));

        // The sum: the chords, 12, what is said, 8, and the two buttons.
        Assert.True(ChordSheetView.HasRoomToSay(chords: 400, said: 300, zoom: 54, room: 774));
        Assert.False(ChordSheetView.HasRoomToSay(chords: 400, said: 300, zoom: 54, room: 773));
    });

    [Fact]
    public void AScreenReaderIsToldTheSheetIsThereAndWhatTheKeysDoOnIt() => OnAMadeUpPC(async pc =>
    {
        var (_, view, hand) = await Open(pc, "Amazing Grace");
        var told = System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(hand.Canvas);
        Assert.NotNull(told);
        Assert.Equal("The song's chord sheet", told.GetName());
        Assert.Equal(System.Windows.Automation.Peers.AutomationControlType.Custom, told.GetAutomationControlType());
        Assert.Equal("chord sheet", told.GetLocalizedControlType());
        Assert.Equal(LibraryModel.SheetHelp, told.GetHelpText());
        Assert.True(told.IsContentElement());
        Assert.True(hand.Canvas.Focusable);
        // The strip's buttons are called by their number and their chord, and the two
        // that zoom by what they do.
        var strip = view.SongPane.ChordSheet;
        Assert.Equal("4 C", System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(strip.ChordsOfTheKey[3]).GetName());
        Assert.Equal("Smaller words", System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(Named<Button>(strip, "Smaller")).GetName());
        Assert.Equal("Larger words", System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(Named<Button>(strip, "Larger")).GetName());
        // And the box a chord is typed in.
        hand.Click("saved");
        Assert.Equal("The chord being typed", System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(hand.Box).GetName());
    });

    // ---- The switch between the two editors

    [Fact]
    public void TheSwitchShowsTheOtherEditorAndSoDoCtrlWithOneAndWithTwo() => OnAMadeUpPC(async pc =>
    {
        var (model, view, _) = await Open(pc, "Amazing Grace");
        var pane = view.SongPane;
        var chords = Named<RadioButton>(pane.Switch, "Chords");
        var chordPro = Named<RadioButton>(pane.Switch, "ChordPro");
        // A song opens on the chord sheet, and the switch says so.
        Assert.Equal(LibraryModel.Editor.Sheet, model.EditorShown);
        Assert.Equal((true, false), (chords.IsChecked, chordPro.IsChecked));
        Assert.Equal(Visibility.Visible, pane.Switch.Visibility);
        Assert.Equal(("Chords", "ChordPro"), (AutomationProperties.GetName(chords), AutomationProperties.GetName(chordPro)));
        Assert.Equal("The chord sheet: point at a word and press a key, or click and type. Ctrl+1", chords.ToolTip);
        Assert.Equal("The song as ChordPro text: type chords in square brackets among the words. Ctrl+2", chordPro.ToolTip);
        // Each is some 30 tall, and the two are side by side.
        Assert.Equal(30, chords.ActualHeight, 0.5);
        Assert.True(chordPro.TranslatePoint(default, pane).X >= chords.TranslatePoint(default, pane).X + chords.ActualWidth);

        // A click on the other shows the other.
        chordPro.IsChecked = true;
        Assert.Equal(LibraryModel.Editor.ChordPro, model.EditorShown);
        Assert.Equal((false, true), (chords.IsChecked, chordPro.IsChecked));
        Assert.Equal(Visibility.Collapsed, pane.ChordSheet.Visibility);
        chords.IsChecked = true;
        Assert.Equal(LibraryModel.Editor.Sheet, model.EditorShown);
        Assert.Equal(Visibility.Visible, pane.ChordSheet.Visibility);

        // Ctrl with 2 and with 1, whatever in the window has the keyboard; and the keys
        // of the number pad.
        Assert.True(view.KeyPressed(Key.D2, ModifierKeys.Control));
        Assert.Equal(LibraryModel.Editor.ChordPro, model.EditorShown);
        Assert.Equal((false, true), (chords.IsChecked, chordPro.IsChecked));
        Assert.True(view.KeyPressed(Key.NumPad1, ModifierKeys.Control));
        Assert.Equal(LibraryModel.Editor.Sheet, model.EditorShown);
        Assert.True(view.KeyPressed(Key.NumPad2, ModifierKeys.Control));
        Assert.True(view.KeyPressed(Key.D1, ModifierKeys.Control));
        Assert.Equal(LibraryModel.Editor.Sheet, model.EditorShown);
        // Not 1 and 2 by themselves, which on the sheet are chords; nor with more held.
        Assert.False(view.KeyPressed(Key.D2, ModifierKeys.None));
        Assert.False(view.KeyPressed(Key.D2, ModifierKeys.Control | ModifierKeys.Shift));
        Assert.False(view.KeyPressed(Key.D2, ModifierKeys.Control | ModifierKeys.Alt));
        Assert.False(view.KeyPressed(Key.D3, ModifierKeys.Control));
        Assert.Equal(LibraryModel.Editor.Sheet, model.EditorShown);

        // While something is asked over the window the keys change nothing.
        model.SetKey("A");
        model.AskToRevert();
        Assert.NotNull(model.RevertQuestion);
        Assert.True(view.KeyPressed(Key.D2, ModifierKeys.Control));
        Assert.Equal(LibraryModel.Editor.Sheet, model.EditorShown);
        model.DontRevert();
        model.ShowEarlierVersions();
        Assert.NotNull(model.Earlier);
        Assert.True(view.KeyPressed(Key.D2, ModifierKeys.Control));
        Assert.Equal(LibraryModel.Editor.Sheet, model.EditorShown);
        model.CloseEarlierVersions();
        Assert.True(view.KeyPressed(Key.D2, ModifierKeys.Control));
        Assert.Equal(LibraryModel.Editor.ChordPro, model.EditorShown);
        view.KeyPressed(Key.D1, ModifierKeys.Control);

        // A song that could not be read has no editor to show, and no switch.
        model.Open(pc.Song("Nonsense"));
        Assert.Equal(Visibility.Collapsed, pane.Switch.Visibility);
        Assert.Equal(Visibility.Collapsed, pane.ChordSheet.Visibility);
        Assert.True(view.KeyPressed(Key.D2, ModifierKeys.Control));
        Assert.Equal(LibraryModel.Editor.Sheet, model.EditorShown);
        // The next song opens in the editor that was shown last.
        model.Open(pc.Song("Plain Song"));
        view.KeyPressed(Key.D2, ModifierKeys.Control);
        model.Open(pc.Song("Amazing Grace"));
        Assert.Equal(LibraryModel.Editor.ChordPro, model.EditorShown);
        Assert.Equal(Visibility.Collapsed, pane.ChordSheet.Visibility);
        Assert.Contains("A[G]mazing", pane.ChordPro.TextBox.Text, StringComparison.Ordinal);
    });

    // ---- What the song refuses

    [Fact]
    public void WhileProPresenterIsRunningNothingDoneOnTheSheetChangesTheSongAndTheQuietLineSaysWhy() => OnAMadeUpPC(async pc =>
    {
        const string sentence = "ProPresenter is running. Quit it to change chords here. Nothing was changed.";
        var (model, view, hand) = await Open(pc, "Amazing Grace");
        var before = pc.BytesOf("Amazing Grace");
        // Started since the app last looked: the song itself finds it running.
        pc.ProPresenterIsRunning = true;
        hand.PointAt("wretch");
        hand.Press(Key.D4);
        Assert.Equal(sentence, model.Notice);
        Assert.Equal(sentence, Named<TextBlock>(view.SongPane, "Quiet").Text);
        Assert.True(model.ProPresenterIsRunning);
        Assert.Equal(Visibility.Visible, Named<FrameworkElement>(view.SongPane, "Band").Visibility);
        Assert.Equal(before, pc.BytesOf("Amazing Grace"));
        Assert.Equal("25:D", hand.ChordsOf("wretch"));
        // A chord typed and kept; one taken off; one dragged; a line's chords pasted.
        hand.Click("saved");
        hand.Type("Am");
        hand.Press(Key.Enter);
        Assert.False(hand.BubbleIsOpen);
        Assert.Equal(sentence, model.Notice);
        hand.PointAt(hand.PlaceOf("me"), onItsChord: true);
        hand.Press(Key.Delete);
        hand.Drag(hand.PlaceOf("sweet"), hand.PlaceOf("saved"));
        hand.PointAt("Amazing");
        hand.Press(Key.C, ModifierKeys.Control);
        // (Copying changes no song, and says what it copied in place of what went wrong.)
        Assert.Equal("4 chords copied from the line" + CopiedSays, model.Notice);
        hand.PointAt("Twas");
        hand.Press(Key.V, ModifierKeys.Control);
        // What the song refused has its own sentence, and nothing more is said.
        Assert.Equal(sentence, model.Notice);
        // A chord still being typed when another song is asked for is refused too, and
        // then the song is not left: the sentence would be swept away with it.
        hand.Click("wretch");
        hand.Type("C");
        model.Open(pc.Song("Plain Song"));
        Assert.Equal("Amazing Grace", model.Shown!.Name);
        Assert.Equal(sentence, model.Notice);
        Assert.False(hand.Sheet.Typing);
        Assert.Equal(before, pc.BytesOf("Amazing Grace"));
        Assert.False(model.CanUndo);
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal("1:G 8:G7 18:C 28:G", hand.ChordsOf("Amazing"));
        Assert.Equal("25:D", hand.ChordsOf("wretch"));

        // ProPresenter quit, and the app told of it: the same key now does it.
        pc.ProPresenterIsRunning = false;
        await model.LookForProPresenterAsync();
        Assert.False(model.ProPresenterIsRunning);
        Assert.NotEqual(sentence, model.Notice);
        hand.PointAt("wretch");
        hand.Press(Key.D4);
        Assert.Equal("1:G 8:G7 18:C 28:G 47:C 59:D", InTheFile(0, "Amazing Grace", pc));
        Assert.Equal(LibraryModel.SheetHelp, model.Notice);
    });

    [Fact]
    public void AChordGoesOnABlankSlideAndTakenOffAgainTheFileIsWhatItWas() => OnAMadeUpPC(async pc =>
    {
        var (model, view, hand) = await Open(pc, "Blank Slides");
        var before = pc.BytesOf("Blank Slides");
        var sheet = hand.Sheet;
        var blank = Enumerable.Range(0, sheet.Rows.Count).First(row => sheet.Rows[row].Blank);
        Assert.True(sheet.Usable(blank));
        Assert.Empty(sheet.Rows[blank].Chords);
        // It has one place: for its first chord.
        Assert.Equal([0], sheet.Stops(blank));
        hand.PointAt(new Spot(blank, 0));
        Assert.Equal(new Spot(blank, 0), sheet.Spot);
        hand.Press(Key.D1);
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal("0:G", hand.ChordsOf(blank));
        Assert.NotEqual(before, pc.BytesOf("Blank Slides"));
        // And another after it, typed.
        hand.PointAt(new Spot(blank, sheet.Stops(blank)[^1]));
        hand.Type("d");
        hand.Press(Key.Enter);
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal("0:G 2:D", hand.ChordsOf(blank));
        // Taken off again in the same sitting, the file is byte for byte what it was.
        hand.PointAt(new Spot(blank, 2), onItsChord: true);
        hand.Press(Key.Delete);
        hand.PointAt(new Spot(blank, 0), onItsChord: true);
        hand.Press(Key.Delete);
        Pictures.LayOut(view, 1280, 800);
        Assert.Empty(sheet.Rows[blank].Chords);
        Assert.Equal(before, pc.BytesOf("Blank Slides"));
        Assert.True(model.CanUndo);
    });

    // ---- What is drawn

    [Fact]
    public void TheSpotIsDrawnAtItsPlaceAndMovingItDrawsNothingElseAgain() => OnAMadeUpPC(async pc =>
    {
        // (In a window tall enough for two rows of slides to be seen whole, so that
        // nothing scrolls.)
        var (_, view, hand) = await Open(pc, "Amazing Grace", height: 900);
        var canvas = hand.Canvas;
        var none = Pictures.Drawn(view, 1280, 900);

        // The dots of the window that differ between two drawings, as the rectangle
        // of the sheet they are all within.
        Rect? Differ((int Width, int Height, int[] Dots) one, (int Width, int Height, int[] Dots) other)
        {
            Rect? within = null;
            for (var dot = 0; dot < one.Dots.Length; dot++)
            {
                if (one.Dots[dot] == other.Dots[dot])
                {
                    continue;
                }
                var at = view.TranslatePoint(new Point((dot % one.Width) / (double)Pictures.Scale, (dot / one.Width) / (double)Pictures.Scale), canvas);
                within = within is { } has ? Rect.Union(has, at) : new Rect(at, new Size(0, 0));
            }
            return within;
        }

        // Pointed at a word with no chord: a bubble in outline over the place, with a
        // tail down through the words. Nothing else of the window is drawn otherwise.
        var wretch = hand.PlaceOf("wretch");
        hand.PointAt(wretch);
        var spot = Pictures.Drawn(view, 1280, 900);
        var changed = Differ(none, spot);
        Assert.NotNull(changed);
        var metrics = canvas.Metrics;
        var line = hand.Grid.Lines[wretch.Row];
        var x = hand.Grid.XOf(wretch);
        var allowed = new Rect(x - 1.5, line.ChordTop - 1.5, (26 * metrics.Scale) + 3, metrics.BubbleHeight + metrics.Tail + metrics.WordHeight + (3 * metrics.Scale) + 3);
        Assert.True(allowed.Contains(changed.Value), Said($"what changed is {changed.Value}, and the spot is within {allowed}"));
        Assert.True(changed.Value.Width > 15 * metrics.Scale && changed.Value.Height > metrics.BubbleHeight + metrics.Tail, Said($"what changed is only {changed.Value}"));
        // It is the accent's orange.
        var orange = spot.Dots.Where((dot, n) => dot != none.Dots[n]).Count(dot => ((dot >> 16) & 0xFF) > 150 && ((dot >> 8) & 0xFF) is > 70 and < 150 && (dot & 0xFF) < 90);
        Assert.True(orange > 60, "only " + orange + " dots of the spot are orange");

        // Pointed at a chord, that chord's own bubble is lit instead, and its tail.
        var sweet = hand.PlaceOf("sweet");
        hand.PointAt(sweet, onItsChord: true);
        var lit = Pictures.Drawn(view, 1280, 900);
        var bubble = Differ(none, lit);
        Assert.NotNull(bubble);
        var placed = hand.Grid.Lines[sweet.Row].Placed!;
        var which = hand.Sheet.Rows[sweet.Row].Chords.ToList().FindIndex(chord => chord.At == sweet.At);
        var its = new Rect(hand.Grid.Lines[sweet.Row].Left + placed.Xs[which] - 1.5, hand.Grid.Lines[sweet.Row].ChordTop - 1.5, placed.Widths[which] + 3, metrics.BubbleHeight + metrics.Tail + (3 * metrics.Scale) + 3);
        Assert.True(its.Contains(bubble.Value), Said($"what changed is {bubble.Value}, and the chord is within {its}"));

        // The keys pick the spot's slide: the ring goes from the first slide's frame to
        // the fourth's, which is under it.
        hand.PointAt("That saved");
        var first = Pictures.Drawn(view, 1280, 900);
        hand.Press(Key.Down);
        var fourth = Pictures.Drawn(view, 1280, 900);
        var rings = Differ(first, fourth);
        Assert.NotNull(rings);
        var both = Rect.Union(hand.Grid.Cells[0].Frame, hand.Grid.Cells[3].Frame);
        both.Inflate(6, 6);
        Assert.True(both.Contains(rings.Value), Said($"what changed is {rings.Value}, and the two slides are within {both}"));
        Assert.True(rings.Value.Height > hand.Grid.Cells[0].Frame.Height * 1.9);
    });

    [Fact]
    public void TheNamesOfKeysAreKeptWholeWhereTheQuietLineIsBrokenInTwo() => Pictures.OnAThreadOfItsOwn(() =>
    {
        // A character with no width, that joins what is either side of it, each side
        // of every plus; and nothing else is changed.
        var joiner = ((char)0x2060).ToString();
        Assert.Equal("Ctrl" + joiner + "+" + joiner + "C and Ctrl" + joiner + "+" + joiner + "V", SongPane.WithKeysWhole("Ctrl+C and Ctrl+V"));
        Assert.Equal("Nothing was changed.", SongPane.WithKeysWhole("Nothing was changed."));
        Assert.Equal(LibraryModel.SheetHelp, SongPane.WithKeysWhole(LibraryModel.SheetHelp).Replace(joiner, "", StringComparison.Ordinal));
        Assert.Equal(LibraryModel.SheetHelp.Count(unit => unit == '+') * 2, SongPane.WithKeysWhole(LibraryModel.SheetHelp).Count(unit => unit == (char)0x2060));

        // And WPF keeps to it. Laid out narrower and narrower, a line left to itself is
        // broken at the plus of "Ctrl+C"; with the two joined, the name is whole on the
        // next line. They add nothing to how wide the words are.
        static System.Windows.Media.FormattedText Laid(string text, double width) => new(
            text,
            CultureInfo.GetCultureInfo("en-US"),
            FlowDirection.LeftToRight,
            new System.Windows.Media.Typeface(SheetLook.Font, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
            12.5,
            System.Windows.Media.Brushes.Black,
            1.0)
        {
            MaxTextWidth = width,
        };

        const string before = "Delete removes  -  Ctrl";
        const string plain = before + "+C and Ctrl+V: a line";
        var whole = SongPane.WithKeysWhole(plain);
        Assert.Equal(Laid(plain, 2000).WidthIncludingTrailingWhitespace, Laid(whole, 2000).WidthIncludingTrailingWhitespace, 0.001);
        var line = Laid("Ctrl", 2000).Height;
        var upTo = Laid(before, 2000).WidthIncludingTrailingWhitespace;
        var brokenOnce = false;
        foreach (var width in new[] { upTo + 2, upTo + 6, upTo + 10, upTo + 14 })
        {
            double TallOf(string text)
            {
                var from = text.IndexOf("Ctrl", StringComparison.Ordinal);
                var to = text.IndexOf(" and", StringComparison.Ordinal);
                return Laid(text, width).BuildHighlightGeometry(new Point(0, 0), from, to - from)!.Bounds.Height;
            }

            brokenOnce |= TallOf(plain) > line * 1.5;
            Assert.True(TallOf(whole) < line * 1.5, Said($"at {width} the name of the key is on two lines"));
        }
        Assert.True(brokenOnce, "left to itself the line was never broken inside the key's name: this test holds nothing");
    });

    [Fact]
    public void TheQuietLineIsAsTallWhateverItSaysWhileTheSheetIsShown() => OnAMadeUpPC(async pc =>
    {
        var (model, view, hand) = await Open(pc, "Amazing Grace");
        var quiet = Named<TextBlock>(view.SongPane, "Quiet");
        var foot = Named<FrameworkElement>(view.SongPane, "Foot");
        // What the keys do is on two lines here, and is all there.
        Assert.Equal(SongPane.WithKeysWhole(LibraryModel.SheetHelp), quiet.Text);
        var tall = foot.ActualHeight;
        Assert.True(quiet.ActualHeight > 30, "the help is " + quiet.ActualHeight + " tall");
        // Something short is said for a few seconds: the line is as tall as it was, so
        // that the sheet over it does not grow and shrink.
        hand.PointAt("Amazing");
        hand.Press(Key.C, ModifierKeys.Control);
        Pictures.LayOut(view, 1280, 800);
        Assert.StartsWith("4 chords copied", quiet.Text, StringComparison.Ordinal);
        Assert.Equal(tall, foot.ActualHeight, 0.5);
        // Over the ChordPro text one line is all it ever needs.
        model.Show(LibraryModel.Editor.ChordPro);
        Pictures.LayOut(view, 1280, 800);
        Assert.True(foot.ActualHeight < tall - 10);
        model.Show(LibraryModel.Editor.Sheet);
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal(tall, foot.ActualHeight, 0.5);
        // At the window's least it has four lines at the most, cut short with dots.
        Pictures.LayOut(view, 640, 480);
        Assert.InRange(quiet.ActualHeight, 3 * 16.0, (4 * 17.0) + 1);
        // On the sheet its words go on to the next line where one is not enough; over
        // the text they are one line.
        Assert.Equal(TextWrapping.Wrap, quiet.TextWrapping);
        model.Show(LibraryModel.Editor.ChordPro);
        Pictures.LayOut(view, 640, 480);
        Assert.Equal(TextWrapping.NoWrap, quiet.TextWrapping);
        Assert.True(quiet.ActualHeight < 2 * 17.0);

        // Where four lines are not enough it has four and no more: in a pane by itself,
        // narrower than the window ever makes it, where what the keys do would run to a
        // dozen.
        // (Of a song newly opened, with nothing just said of it.)
        var (another, _, _) = await Open(pc, "Plain Song");
        var alone = new SongPane { DataContext = another };
        Look.Wear(alone, dark: true);
        Pictures.LayOut(alone, 330, 600);
        var narrow = Named<TextBlock>(alone, "Quiet");
        Assert.Equal(SongPane.WithKeysWhole(LibraryModel.SheetHelp), narrow.Text);
        var line = Math.Ceiling(narrow.FontFamily.LineSpacing * narrow.FontSize);
        Assert.InRange(narrow.ActualHeight, (3 * line) + 1, (4 * line) + 0.5);
        Assert.Equal(TextTrimming.CharacterEllipsis, narrow.TextTrimming);
    });

    // ---- Where the bubble may stand, and what else is the box's

    [Fact]
    public void TheBubbleIsKeptWithinWhatIsSeen() => OnAMadeUpPC(async pc =>
    {
        // (In a window low enough for the sheet to scroll a good way.)
        var (_, view, hand) = await Open(pc, "Amazing Grace", height: 520);
        var scroll = Named<ScrollViewer>(view.SongPane.ChordSheet, "Scroll");
        // The last word of a slide's second line, in the last card of a row: the
        // bubble, 330 wide, would run out of sight at the right. It is kept 12 in from
        // the edge of what is seen, and still grows upwards from the row of chords of
        // its line, where there is room over it.
        var relieved = hand.PlaceOf("relieved");
        Assert.True(hand.Grid.XOf(relieved) + SheetLook.EntryWidth > scroll.ViewportWidth);
        hand.Click(relieved);
        Pictures.LayOut(view, 1280, 520);
        var frame = hand.Canvas.EntryFrame;
        Assert.Equal(scroll.ViewportWidth - 12, frame.Right, 0.5);
        Assert.True(frame.Left < hand.Grid.XOf(relieved));
        Assert.Equal(hand.Grid.Lines[relieved.Row].ChordTop + hand.Canvas.Metrics.BubbleHeight + 3, frame.Bottom, 0.01);
        Assert.True(frame.Top > 4);
        hand.Press(Key.Escape);

        // A chord on the first line of the first row of slides, with so many offered
        // that the bubble is taller than the room over the line: it is kept in sight, 4
        // from the top, and covers what it must.
        var fear = hand.PlaceOf("fear");
        hand.Click(fear, onItsChord: true);
        hand.Type("G");
        Pictures.LayOut(view, 1280, 520);
        frame = hand.Canvas.EntryFrame;
        var chords = hand.Grid.Lines[fear.Row].ChordTop + hand.Canvas.Metrics.BubbleHeight + 3;
        Assert.True(frame.Height > chords - 4, Said($"the bubble is only {frame.Height} tall, and there is {chords - 4} over the line"));
        Assert.Equal(4, frame.Top, 0.01);
        Assert.Equal(scroll.ViewportWidth - 12, frame.Right, 0.5);
        hand.Press(Key.Escape);

        // A line at the very top of what is seen: there is no room over it, and the
        // bubble is kept 4 in from the top, covering what it must.
        var precious = hand.PlaceOf("How precious");
        scroll.ScrollToVerticalOffset(hand.Grid.Lines[precious.Row].ChordTop + 6);
        Pictures.LayOut(view, 1280, 520);
        Assert.True(scroll.VerticalOffset > 200);
        hand.Click(precious);
        Pictures.LayOut(view, 1280, 520);
        Assert.Equal(scroll.VerticalOffset + 4, hand.Canvas.EntryFrame.Top, 0.5);
        // (In the first card of its row there is room at the right: it stands at its place.)
        Assert.Equal(hand.Grid.XOf(precious), hand.Canvas.EntryFrame.Left, 0.5);
        hand.Press(Key.Escape);
    });

    [Fact]
    public void ACharacterRefusedInTheMiddleOfTheBoxLeavesTheCaretWhereItWas() => OnAMadeUpPC(async pc =>
    {
        var (_, _, hand) = await Open(pc, "Amazing Grace");
        hand.Click("saved");
        hand.Type("Am7");
        // The caret between the A and the m, and a key that could not be on the way to
        // a chord: the box is as it was, and the caret is still between them.
        hand.Box.Select(1, 0);
        hand.Type("!");
        Assert.Equal("Am7", hand.Box.Text);
        Assert.Equal((1, 0), (hand.Box.SelectionStart, hand.Box.SelectionLength));
        // One that is taken goes in there, and the caret after it.
        hand.Type("b");
        Assert.Equal("Abm7", hand.Box.Text);
        Assert.Equal((2, 0), (hand.Box.SelectionStart, hand.Box.SelectionLength));
        // A small letter that is a note is made a capital where it stands, the caret
        // staying after it.
        hand.Box.Select(0, 4);
        hand.Type("c");
        Assert.Equal("C", hand.Box.Text);
        Assert.Equal((1, 0), (hand.Box.SelectionStart, hand.Box.SelectionLength));
        hand.Press(Key.Escape);
    });

    [Fact]
    public void CtrlVAfterAClickPastesOnTheLineThatWasClickedWhereverThePointerHasGoneSince() => OnAMadeUpPC(async pc =>
    {
        var (model, _, hand) = await Open(pc, "Amazing Grace");
        var copiedOnThisPC = 3u;
        hand.Canvas.WhatWasCopiedLast = () => copiedOnThisPC;
        // With nothing copied yet, Ctrl+V in the box is the box's own.
        hand.Click("heart");
        Assert.False(hand.Press(Key.V, ModifierKeys.Control));
        Assert.True(hand.Sheet.Typing);
        hand.Press(Key.Escape);

        hand.PointAt("Amazing");
        hand.Press(Key.C, ModifierKeys.Control);
        hand.Click("heart");
        // The pointer has moved on to another slide, and the spot with it.
        hand.PointAt("lost");
        Assert.Equal(hand.PlaceOf("lost"), hand.Sheet.Spot);
        Assert.True(hand.Press(Key.V, ModifierKeys.Control));
        // It is the line that was clicked that has the chords, and not the pointer's.
        Assert.Equal("1:G 5:G7 16:C 26:G 61:D", InTheFile(2, "Amazing Grace", pc));
        Assert.Equal(SecondSlide, InTheFile(1, "Amazing Grace", pc));
        Assert.Equal("4 chords put on the line.", model.Notice);
        // The chord the bubble had opened on is as it was: the bubble closed, changing
        // nothing of its own. One undo takes the paste back, and there is no more.
        hand.Press(Key.Z, ModifierKeys.Control);
        Assert.Equal(ThirdSlide, InTheFile(2, "Amazing Grace", pc));
        Assert.False(model.CanUndo);
    });

    // ---- What is drawn where

    [Fact]
    public void TheWordsAndTheChordsOfEveryLineAreDrawnWhereTheGridSaysTheyAre() => OnAMadeUpPC(async pc =>
    {
        var (_, view, hand) = await Open(pc, "Amazing Grace", height: 900);
        var canvas = hand.Canvas;
        var metrics = canvas.Metrics;
        var grid = hand.Grid;
        var sheet = hand.Sheet;
        var scroll = Named<ScrollViewer>(view.SongPane.ChordSheet, "Scroll");
        var drawn = Pictures.Drawn(view, 1280, 900);

        // How many dots of a part of the sheet are of some kind.
        int Dots(Rect ofTheSheet, Func<int, int, int, bool> are)
        {
            var corner = canvas.TranslatePoint(ofTheSheet.TopLeft, view);
            var count = 0;
            for (var y = (int)Math.Ceiling(corner.Y * Pictures.Scale); y < (int)Math.Floor((corner.Y + ofTheSheet.Height) * Pictures.Scale); y++)
            {
                for (var x = (int)Math.Ceiling(corner.X * Pictures.Scale); x < (int)Math.Floor((corner.X + ofTheSheet.Width) * Pictures.Scale); x++)
                {
                    var dot = drawn.Dots[(y * drawn.Width) + x];
                    if (are((dot >> 16) & 0xFF, (dot >> 8) & 0xFF, dot & 0xFF))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        // The words are nearly white, and a chord's name the colour of sand.
        static bool Words(int red, int green, int blue) => red > 190 && green > 190 && blue > 190;
        static bool Name(int red, int green, int blue) => red > 215 && green is > 170 and < 240 && blue is > 120 and < 200 && red - blue > 40;

        var lines = 0;
        for (var row = 0; row < sheet.Rows.Count; row++)
        {
            if (sheet.Rows[row].Kind != SheetRowKind.Line || sheet.Rows[row].Alone)
            {
                continue;
            }
            var item = grid.Lines[row];
            if (item.ChordTop + metrics.LineHeight > scroll.ViewportHeight)
            {
                continue;
            }
            lines++;
            var placed = item.Placed!;
            var card = grid.Cells[item.Cell].Card;
            var wordsTop = item.ChordTop + metrics.BubbleHeight + metrics.Tail;
            // The words: in their own room, from the line's left to where they end; and
            // nothing of their colour beside them, on to the card's edge.
            var words = Dots(new Rect(item.Left, wordsTop, placed.WordsWidth, metrics.WordHeight), Words);
            Assert.True(words > placed.WordsWidth * 3, Said($"row {row}: only {words} dots of words in {placed.WordsWidth} of line"));
            Assert.Equal(0, Dots(new Rect(item.Left + placed.WordsWidth + 1.5, wordsTop, card.Right - item.Left - placed.WordsWidth - 2.5, metrics.WordHeight), Words));
            Assert.Equal(0, Dots(new Rect(card.Left + 1, wordsTop, item.Left - card.Left - 2, metrics.WordHeight), Words));
            // No words on the row of chords, and no chord's name among the words.
            Assert.Equal(0, Dots(new Rect(card.Left + 1, item.ChordTop, card.Width - 2, metrics.BubbleHeight), Words));
            Assert.Equal(0, Dots(new Rect(card.Left + 1, wordsTop, card.Width - 2, metrics.WordHeight), Name));
            // Each chord's name: in its bubble, and nothing of its colour between the
            // bubbles nor before the first.
            var chords = sheet.Rows[row].Chords;
            var free = card.Left + 1;
            for (var n = 0; n < chords.Count; n++)
            {
                var bubble = new Rect(item.Left + placed.Xs[n], item.ChordTop, placed.Widths[n], metrics.BubbleHeight);
                Assert.True(Dots(bubble, Name) > 8, Said($"row {row}: the name of {chords[n].Name} is not in its bubble at {bubble}"));
                Assert.Equal(0, Dots(new Rect(free, item.ChordTop, Math.Max(0, bubble.Left - 0.5 - free), metrics.BubbleHeight), Name));
                free = bubble.Right + 0.5;
            }
            Assert.Equal(0, Dots(new Rect(free, item.ChordTop, Math.Max(0, card.Right - 1 - free), metrics.BubbleHeight), Name));
        }
        // (Every line of the two rows of slides that are in sight: twelve.)
        Assert.Equal(12, lines);

        // A frame's foot says its slide's number, in white; and the bare sheet between
        // two frames has nothing on it at all.
        var first = grid.Cells[0];
        Assert.True(Dots(new Rect(first.Frame.Left + 6, first.Card.Bottom + 1, 80, SheetLook.Foot - 2), Words) > 30);
        // (Between the second and the third: the first is picked, and its ring is in
        // the room beside it.)
        var between = new Rect(grid.Cells[1].Frame.Right + 1, first.Frame.Top, grid.Cells[2].Frame.Left - grid.Cells[1].Frame.Right - 2, first.Frame.Height);
        Assert.True(between.Width > 8);
        Assert.Equal(0, Dots(between, (red, green, blue) => red != 0x1B || green != 0x1C || blue != 0x1F));
        // The slide that is picked has its ring, in the sheet's orange, just outside its
        // frame; and the others have none.
        static bool Orange(int red, int green, int blue) => red > 230 && green is > 110 and < 170 && blue < 80;
        Assert.True(Dots(new Rect(first.Frame.Left - 6, first.Frame.Top - 6, first.Frame.Width + 12, 5), Orange) > first.Frame.Width);
        Assert.Equal(0, Dots(new Rect(grid.Cells[1].Frame.Left - 5, grid.Cells[1].Frame.Top - 6, grid.Cells[1].Frame.Width + 10, 5), Orange));
    });

    [Fact]
    public void TheKeysBringTheSpotsWholeSlideIntoViewWithAsLittleScrollingAsDoesIt() => OnAMadeUpPC(async pc =>
    {
        var (_, view, hand) = await Open(pc, "Amazing Grace", height: 520);
        var scroll = Named<ScrollViewer>(view.SongPane.ChordSheet, "Scroll");
        hand.PointAt("That saved");
        Assert.Equal(0, scroll.VerticalOffset);
        // Down, to the slide under: more of it is out of sight than in. The sheet scrolls
        // until the whole of its frame, and the 6 that is kept clear round it, is in
        // sight, and no further.
        hand.Press(Key.Down);
        Pictures.LayOut(view, 1280, 520);
        var frame = hand.Grid.Cells[3].Frame;
        Assert.Equal(3, hand.Sheet.Rows[hand.Sheet.Spot.Row].Slide);
        Assert.True(frame.Height + 12 <= scroll.ViewportHeight);
        Assert.Equal(frame.Bottom + 6 - scroll.ViewportHeight, scroll.VerticalOffset, 0.5);
        // The next line of the same slide is in sight already: nothing scrolls.
        var offset = scroll.VerticalOffset;
        hand.Press(Key.Down);
        Pictures.LayOut(view, 1280, 520);
        Assert.Equal(offset, scroll.VerticalOffset);
        // Up, to the slide over: back until its frame's top, and the 6 over it, is in sight.
        hand.Press(Key.Up);
        hand.Press(Key.Up);
        Pictures.LayOut(view, 1280, 520);
        Assert.Equal(0, hand.Sheet.Rows[hand.Sheet.Spot.Row].Slide);
        Assert.Equal(hand.Grid.Cells[0].Frame.Top - 6, scroll.VerticalOffset, 0.5);
        // Right and Left bring it into view as well: on from the first slide's last
        // word, slide by slide, to the fourth, which is under the first.
        hand.PointAt("me");
        for (var words = 0; words < 30 && hand.Sheet.Rows[hand.Sheet.Spot.Row].Slide < 3; words++)
        {
            hand.Press(Key.Right);
        }
        Pictures.LayOut(view, 1280, 520);
        Assert.Equal(3, hand.Sheet.Rows[hand.Sheet.Spot.Row].Slide);
        Assert.Equal(frame.Bottom + 6 - scroll.ViewportHeight, scroll.VerticalOffset, 0.5);
    });

    // ---- The window's menu

    [Fact]
    public void TheViewMenuHasTheTwoEditorsWithTheOneShownTickedAndTheSizeOfTheWordsOfThatOne() => OnAMadeUpPC(async pc =>
    {
        var (model, view, _) = await Open(pc, "Amazing Grace");
        var chords = Named<MenuItem>(view, "ShowChords");
        var chordPro = Named<MenuItem>(view, "ShowChordPro");
        var larger = Named<MenuItem>(view, "LargerWords");
        var smaller = Named<MenuItem>(view, "SmallerWords");
        var usual = Named<MenuItem>(view, "UsualWords");
        void Opened() => ((MenuItem)chords.Parent).RaiseEvent(new RoutedEventArgs(MenuItem.SubmenuOpenedEvent));
        static void Chosen(MenuItem item) => item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

        Assert.Equal(("Ctrl+1", "Ctrl+2", "Ctrl+Plus", "Ctrl+Minus", "Ctrl+0"), (chords.InputGestureText, chordPro.InputGestureText, larger.InputGestureText, smaller.InputGestureText, usual.InputGestureText));
        // The sheet is shown: its name is ticked, and the sizes are the sheet's.
        Opened();
        Assert.Equal((true, false), (chords.IsChecked, chordPro.IsChecked));
        Assert.True(chords.IsEnabled && chordPro.IsEnabled && larger.IsEnabled && smaller.IsEnabled && usual.IsEnabled);
        Chosen(larger);
        Assert.Equal((0.9, 1.0), (model.SheetZoom, model.TextZoom));
        Chosen(smaller);
        Chosen(smaller);
        Assert.Equal((0.7, 1.0), (model.SheetZoom, model.TextZoom));
        Chosen(usual);
        Assert.Equal((0.8, 1.0), (model.SheetZoom, model.TextZoom));
        // The other editor, chosen in the menu: ticked, and the sizes are the text's.
        Chosen(chordPro);
        Assert.Equal(LibraryModel.Editor.ChordPro, model.EditorShown);
        Opened();
        Assert.Equal((false, true), (chords.IsChecked, chordPro.IsChecked));
        Chosen(larger);
        Chosen(larger);
        Assert.Equal((0.8, 1.25), (model.SheetZoom, model.TextZoom));
        Chosen(usual);
        Assert.Equal((0.8, 1.0), (model.SheetZoom, model.TextZoom));
        // At the end of its sizes, the one that would go further is greyed.
        for (var times = 0; times < 9; times++)
        {
            Chosen(smaller);
        }
        Opened();
        Assert.Equal(0.5, model.TextZoom);
        Assert.False(smaller.IsEnabled);
        Assert.True(larger.IsEnabled);
        Chosen(usual);
        // (And at the text's largest, though the sheet could be larger still.)
        for (var times = 0; times < 9; times++)
        {
            Chosen(larger);
        }
        Opened();
        Assert.Equal((0.8, 1.5), (model.SheetZoom, model.TextZoom));
        Assert.False(larger.IsEnabled);
        Assert.True(smaller.IsEnabled);
        Chosen(usual);
        Chosen(chords);
        Assert.Equal(LibraryModel.Editor.Sheet, model.EditorShown);
        for (var times = 0; times < 9; times++)
        {
            Chosen(larger);
        }
        Opened();
        Assert.Equal((1.5, 1.0), (model.SheetZoom, model.TextZoom));
        Assert.False(larger.IsEnabled);
        Assert.True(smaller.IsEnabled);
        // With a song that could not be read, or none, there is nothing to show or to size.
        model.Open(pc.Song("Nonsense"));
        Opened();
        Assert.False(chords.IsEnabled || chordPro.IsEnabled || larger.IsEnabled || smaller.IsEnabled || usual.IsEnabled);
        Assert.Equal((false, false), (chords.IsChecked, chordPro.IsChecked));
    });
}
