// One of the key's chords dragged out of the strip onto a word (spec/03-chord-sheet.md,
// "A chord dragged out of the strip", rules 46a to 46h).
//
// MINE, NOT A VECTOR, each of them: the page asks each app for tests of its own, one for
// each of the rules that a test can reach. They expect what the Mac's expect
// (ChordFromTheStripTests.swift), case for case where the two can be alike.
//
// What a chord let go on a place does to a song is Song/ChordSheet.cs's, and is held
// with no window (Song.Tests, `OneOfTheKeysChordsCarriedInFromTheStrip...`). Here is
// what is the window's: its two ends.
//
//   - THE SHEET'S END. Windows tells a view four things as something is dragged over
//     it. The four handlers read the name and the place out of what they are handed
//     and call `CarriedIn`, `CarriedOver`, `CarriedAway` and `LetGo`, and these tests
//     call the same four, with a real `DataObject` such as the strip hands over.
//   - THE STRIP'S END. A press on a button and a pull of 6 picks the chord up. The
//     handlers of the real mouse call `ChordPressed` and `ChordPulled`, and so do
//     these. What then carries the chord is the system's (`DragDrop.DoDragDrop`), and
//     every test here puts its own in the place of that FIRST, as `Carry`.
//
// **The system's own dragging is run by no test.** It reads the real mouse, on the
// real screen, and does not come back until a real button is let go. So nothing here
// shows that Windows hands the sheet what the strip gave it, shows its two pointers,
// gives up at Esc, or goes on telling the sheet of a chord that is held still:
// windows/NOTES.md lists those for a hand.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Song;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class ChordFromTheStripTests
{
    private const string Running = "ProPresenter is running. Quit it to change chords here. Nothing was changed.";

    // The first slide's chords in the file, as the song comes: its second line is
    // "That saved a wretch like me", which starts 34 along.
    private const string FirstSlide = "1:G 8:G7 18:C 28:G 59:D";

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

    // Two points of the sheet that are over no place: beside the cards, and a frame's
    // foot, where the slide's number is.
    private static Point[] OffThePlaces(Hand hand)
    {
        var cell = hand.Grid.Cells[0];
        var beside = new Point(hand.Canvas.ActualWidth - 3, 200);
        var foot = new Point(cell.Frame.Left + (cell.Frame.Width / 2), cell.Card.Bottom + 8);
        Assert.True(hand.Grid.Cells.All(each => !each.Frame.Contains(beside)), "the point beside the cards is on one");
        Assert.True(cell.Frame.Contains(foot) && !cell.Card.Contains(foot), "the point is not on the frame's foot");
        return [beside, foot];
    }

    // ---- The sheet's end

    [Fact]
    public void OverAPlaceTheChordShowsThereAndLetGoItIsInTheFileAndOffThePlacesItShowsNowhereAndLetGoChangesNothing() => OnAMadeUpPC(async pc =>
    {
        var (model, _, hand) = await Open(pc, "Amazing Grace");
        var canvas = hand.Canvas;
        var sheet = hand.Sheet;
        var before = pc.BytesOf("Amazing Grace");
        var line = hand.PlaceOf("wretch").Row;
        var name = sheet.InKey[3];
        var over = hand.PointOf(new Spot(line, 7));

        Assert.True(canvas.CarriedOfKey(name, over, shift: false));
        Assert.Equal((new Spot(-1, 0), name), sheet.Dragged);
        Assert.Equal(new Spot(line, 7), sheet.Spot);
        // With Shift it goes to the start of the word.
        Assert.True(canvas.CarriedOfKey(name, over, shift: true));
        Assert.Equal(new Spot(line, 5), sheet.Spot);

        // Beside the cards is bare sheet, and so is a frame's foot: it shows nowhere,
        // and let go there nothing is asked of the song.
        foreach (var bare in OffThePlaces(hand))
        {
            Assert.True(canvas.CarriedOfKey(name, over, shift: false));
            Assert.False(canvas.CarriedOfKey(name, bare, shift: false));
            Assert.Null(sheet.Dragged);
            Assert.False(canvas.DroppedOfKey(name, bare, shift: false));
        }
        // Off the sheet altogether.
        Assert.True(canvas.CarriedOfKey(name, over, shift: false));
        Assert.False(canvas.CarriedOfKey(name, null, shift: false));
        Assert.Null(sheet.Dragged);
        Assert.Equal(before, pc.BytesOf("Amazing Grace"));
        Assert.False(model.CanUndo);

        // Let go over the place.
        Assert.True(canvas.DroppedOfKey(name, over, shift: false));
        Assert.Equal("1:G 8:G7 18:C 28:G 41:C 59:D", InTheFile(0, "Amazing Grace", pc));
        Assert.Null(sheet.Dragged);
        Assert.False(sheet.Typing);
        Assert.Equal(new Spot(line, 7), sheet.Spot);
        Assert.Null(model.Refused);
        // In place of the chord that is there.
        var other = sheet.InKey[5];
        Assert.True(canvas.DroppedOfKey(other, hand.PointOf(new Spot(line, 7)), shift: false));
        Assert.Equal("1:G 8:G7 18:C 28:G 41:Em 59:D", InTheFile(0, "Amazing Grace", pc));
        // Each was one step.
        Assert.True(hand.Press(Key.Z, ModifierKeys.Control));
        Assert.Equal("1:G 8:G7 18:C 28:G 41:C 59:D", InTheFile(0, "Amazing Grace", pc));
        Assert.True(hand.Press(Key.Z, ModifierKeys.Control));
        Assert.Equal(before, pc.BytesOf("Amazing Grace"));
        Assert.False(model.CanUndo);
    });

    [Fact]
    public void OnALineOfChordsAloneItTakesTheChordsPlaceOrPastTheLastIsOneMoreAndOnABlankSlideItIsTheFirst() => OnAMadeUpPC(async pc =>
    {
        const string song = "Chords With No Words";
        var (model, _, hand) = await Open(pc, song, width: 1500, height: 900);
        var canvas = hand.Canvas;
        var sheet = hand.Sheet;
        var before = pc.BytesOf(song);
        var name = sheet.InKey[4];

        var blank = Enumerable.Range(0, sheet.Rows.Count).First(row => sheet.Rows[row].Blank);
        Assert.True(canvas.DroppedOfKey(name, hand.PointOf(new Spot(blank, 0)), shift: false));
        Assert.Equal([new Chord(0, name)], sheet.Rows[blank].Chords);
        Assert.False(sheet.Rows[blank].Blank);
        Assert.NotEqual(before, pc.BytesOf(song));
        Assert.Null(model.Refused);
        // Delete then leaves the file as it was.
        sheet.SetSpot(blank, 0);
        Assert.True(hand.Press(Key.Delete));
        Assert.True(sheet.Rows[blank].Blank);
        Assert.Equal(before, pc.BytesOf(song));

        var alone = Enumerable.Range(0, sheet.Rows.Count).First(row => sheet.Rows[row].Alone && !sheet.Rows[row].Blank && sheet.Rows[row].Chords.Count >= 2);
        var names = sheet.Rows[alone].Chords.Select(chord => chord.Name).ToList();
        Assert.True(canvas.DroppedOfKey(name, hand.PointOf(new Spot(alone, 2)), shift: false));
        var wanted = names.ToList();
        wanted[1] = name;
        Assert.Equal(wanted, sheet.Rows[alone].Chords.Select(chord => chord.Name));
        Assert.True(canvas.DroppedOfKey(name, hand.PointOf(new Spot(alone, names.Count * 2)), shift: false));
        Assert.Equal([.. wanted, name], sheet.Rows[alone].Chords.Select(chord => chord.Name));
        // The sheet is laid out again for the line as it now is: a bubble for each of
        // its chords, the new one among them.
        Assert.Equal(names.Count + 1, hand.Grid.Lines[alone].Placed!.Xs.Count);
        var read = new ChordSheet(new OpenSong(pc.Song(song)));
        Assert.Equal(sheet.Rows[alone].Chords, read.Rows[alone].Chords);
        Assert.Null(model.Refused);
    });

    [Fact]
    public void WhileAChordIsTypedPickingOneUpKeepsWhatWasTypedAndClosesTheBubbleAndTheOneCarriedGoesWhereItIsLetGo() => OnAMadeUpPC(async pc =>
    {
        var (model, view, hand) = await Open(pc, "Amazing Grace");
        var canvas = hand.Canvas;
        var sheet = hand.Sheet;
        var strip = view.SongPane.ChordSheet;
        var line = hand.PlaceOf("wretch").Row;
        var fourth = strip.ChordsOfTheKey[3];
        var name = sheet.InKey[3];
        var carried = ChordSheetCanvas.Carrying(name);

        // In place of the system's own dragging: what happens while the chord is out is
        // the test's to say, each time.
        var handed = new List<string>();
        Action meanwhile = () => { };
        strip.Carry = (button, chord) =>
        {
            Assert.Same(fourth, button);
            handed.Add(chord);
            meanwhile();
        };

        hand.Click(new Spot(line, 5));
        hand.Type("Bm7");
        Assert.True(sheet.Typing);
        Assert.Equal("Bm7", sheet.Entry.Text);
        Assert.True(hand.BubbleIsOpen);

        // The button is pressed and pulled: the chord is picked up. By the time the
        // system has it, what was typed is kept, the bubble is closed, and the button
        // is faint. Then the system tells the sheet of it, and it is let go on a word.
        meanwhile = () =>
        {
            Assert.False(sheet.Typing);
            Assert.False(hand.BubbleIsOpen);
            Assert.Equal("1:G 8:G7 18:C 28:G 39:Bm7 59:D", InTheFile(0, "Amazing Grace", pc));
            Assert.Equal(ChordSheetView.FaintWhileCarried, fourth.Opacity);
            canvas.CarriedIn();
            Assert.Equal(DragDropEffects.Copy, canvas.CarriedOver(carried, () => hand.PointOf(new Spot(line, 13)), shift: false));
            Assert.Equal((name, new Spot(line, 13)), (sheet.Dragged?.Name, sheet.Spot));
            Assert.Equal(DragDropEffects.Copy, canvas.LetGo(carried, hand.PointOf(new Spot(line, 13)), shift: false));
        };
        strip.ChordPressed(fourth, new Point(5, 5));
        Assert.True(strip.ChordPulled(fourth, name, new Point(5, 12), buttonIsDown: true));
        Assert.Equal([name], handed);
        Assert.Equal("1:G 8:G7 18:C 28:G 39:Bm7 47:C 59:D", InTheFile(0, "Amazing Grace", pc));
        Assert.Equal(1.0, fourth.Opacity);
        Assert.Null(sheet.Dragged);
        Assert.Null(model.Refused);
        // They were two changes, neither joined to the other.
        Assert.True(hand.Press(Key.Z, ModifierKeys.Control));
        Assert.Equal("1:G 8:G7 18:C 28:G 39:Bm7 59:D", InTheFile(0, "Amazing Grace", pc));
        Assert.True(hand.Press(Key.Z, ModifierKeys.Control));
        Assert.Equal(FirstSlide, InTheFile(0, "Amazing Grace", pc));

        // The system ends it while the chord is still shown over a place, and tells the
        // sheet nothing (at Esc, say): nothing is left looking as if it were carried,
        // and the window is told to draw the sheet again.
        var drawn = 0;
        meanwhile = () =>
        {
            canvas.CarriedIn();
            Assert.Equal(DragDropEffects.Copy, canvas.CarriedOver(carried, () => hand.PointOf(new Spot(line, 13)), shift: false));
            Assert.NotNull(sheet.Dragged);
            drawn = model.SheetVersion;
        };
        strip.ChordPressed(fourth, new Point(5, 5));
        Assert.True(strip.ChordPulled(fourth, name, new Point(5, 12), buttonIsDown: true));
        Assert.Null(sheet.Dragged);
        Assert.True(model.SheetVersion > drawn);
        Assert.Equal(1.0, fourth.Opacity);
        Assert.Equal(FirstSlide, InTheFile(0, "Amazing Grace", pc));

        // So it is if the carrying fails outright.
        meanwhile = () =>
        {
            canvas.CarriedOver(carried, () => hand.PointOf(new Spot(line, 13)), shift: false);
            throw new InvalidOperationException("the system would not carry it");
        };
        strip.ChordPressed(fourth, new Point(5, 5));
        Assert.Throws<InvalidOperationException>(() => strip.ChordPulled(fourth, name, new Point(5, 12), buttonIsDown: true));
        Assert.Null(sheet.Dragged);
        Assert.Equal(1.0, fourth.Opacity);

        // While ProPresenter is running: refused in the song's own sentence, and the
        // system is told that nothing was done.
        var before = pc.BytesOf("Amazing Grace");
        pc.ProPresenterIsRunning = true;
        Assert.Equal(DragDropEffects.Copy, canvas.CarriedOver(carried, () => hand.PointOf(new Spot(line, 0)), shift: false));
        Assert.Equal(DragDropEffects.None, canvas.LetGo(carried, hand.PointOf(new Spot(line, 0)), shift: false));
        Assert.Equal(Running, model.Notice);
        Assert.Equal(before, pc.BytesOf("Amazing Grace"));
        Assert.Null(sheet.Dragged);
    });

    [Fact]
    public void TheSheetTakesOneOfTheKeysChordsFromTheStripAndNothingElseAndSaysItMayBeLetGoOverAPlaceAndNotOffOne() => OnAMadeUpPC(async pc =>
    {
        var (model, _, hand) = await Open(pc, "Amazing Grace");
        var canvas = hand.Canvas;
        var sheet = hand.Sheet;
        var before = pc.BytesOf("Amazing Grace");
        var line = hand.PlaceOf("wretch").Row;
        var name = sheet.InKey[1];
        var place = hand.PointOf(new Spot(line, 7));
        var bare = OffThePlaces(hand)[0];
        Assert.True(canvas.AllowDrop);

        // What the strip hands the system: the chord's name, under this app's own kind
        // and this run of it's, and under no other.
        var carried = ChordSheetCanvas.Carrying(name);
        Assert.Equal([ChordSheetCanvas.ChordOfTheKey], carried.GetFormats());
        Assert.EndsWith("." + Environment.ProcessId, ChordSheetCanvas.ChordOfTheKey, StringComparison.Ordinal);
        Assert.Equal(name, ChordSheetCanvas.ChordOfKey(carried));

        canvas.CarriedIn();
        Assert.Equal(DragDropEffects.None, canvas.CarriedOver(carried, () => bare, shift: false));
        Assert.Null(sheet.Dragged);
        Assert.Equal(DragDropEffects.Copy, canvas.CarriedOver(carried, () => place, shift: false));
        Assert.Equal(new Spot(line, 7), sheet.Spot);
        Assert.Equal(DragDropEffects.Copy, canvas.CarriedOver(carried, () => place, shift: true));
        Assert.Equal(new Spot(line, 5), sheet.Spot);
        Assert.Equal(DragDropEffects.None, canvas.CarriedOver(carried, () => bare, shift: false));
        Assert.Null(sheet.Dragged);
        // Let go off the places, should the system tell of it all the same: nothing.
        Assert.Equal(DragDropEffects.None, canvas.LetGo(carried, bare, shift: false));
        Assert.Equal(DragDropEffects.Copy, canvas.CarriedOver(carried, () => place, shift: false));
        canvas.CarriedAway();
        Assert.Null(sheet.Dragged);
        Assert.Equal(before, pc.BytesOf("Amazing Grace"));

        // Anything else that is dragged over the sheet is not taken: text from another
        // app, a file, this app's kind with nothing under it, the kind of another run
        // of this app, and nothing at all.
        IDataObject?[] others =
        [
            new DataObject(DataFormats.UnicodeText, "C"),
            new DataObject(DataFormats.Text, "C"),
            new DataObject(DataFormats.FileDrop, new[] { pc.Song("Plain Song") }),
            new DataObject(ChordSheetCanvas.ChordOfTheKey, ""),
            new DataObject(ChordSheetCanvas.ChordOfTheKey, 4),
            new DataObject("Pro7ChordEditor.ChordOfTheKey.0", "C"),
            null,
        ];
        foreach (var other in others)
        {
            Assert.Null(ChordSheetCanvas.ChordOfKey(other));
            canvas.CarriedIn();
            Assert.Equal(DragDropEffects.None, canvas.CarriedOver(other, () => place, shift: false));
            Assert.Null(sheet.Dragged);
            Assert.Equal(DragDropEffects.None, canvas.LetGo(other, place, shift: false));
        }
        // And a name that is no chord, under this app's own kind, is put nowhere.
        var nonsense = ChordSheetCanvas.Carrying("Hello");
        Assert.Equal(DragDropEffects.None, canvas.CarriedOver(nonsense, () => place, shift: false));
        Assert.Equal(DragDropEffects.None, canvas.LetGo(nonsense, place, shift: false));
        Assert.Equal(before, pc.BytesOf("Amazing Grace"));
        Assert.False(model.CanUndo);
    });

    [Fact]
    public void LetGoTheChordGoesToTheCharacterUnderThePointerOrWithShiftHeldToTheStartOfItsWord() => OnAMadeUpPC(async pc =>
    {
        var (_, _, hand) = await Open(pc, "Amazing Grace");
        var canvas = hand.Canvas;
        var line = hand.PlaceOf("wretch").Row;
        var name = hand.Sheet.InKey[2];
        var carried = ChordSheetCanvas.Carrying(name);
        Assert.Equal(DragDropEffects.Copy, canvas.LetGo(carried, hand.PointOf(new Spot(line, 7)), shift: false));
        Assert.Equal("1:G 8:G7 18:C 28:G 41:Bm 59:D", InTheFile(0, "Amazing Grace", pc));
        Assert.Equal(DragDropEffects.Copy, canvas.LetGo(carried, hand.PointOf(new Spot(line, 16)), shift: true));
        Assert.Equal("1:G 8:G7 18:C 28:G 41:Bm 47:Bm 59:D", InTheFile(0, "Amazing Grace", pc));
        Assert.Equal(new Spot(line, 13), hand.Sheet.Spot);
    });

    [Fact]
    public void HeldNearTheBottomOfWhatIsSeenTheSheetScrollsDownAndNearTheTopUpByTheTimeThatHasPassedAndNotUpwardsOnItsWayIn() => OnAMadeUpPC(async pc =>
    {
        const double width = 760;
        const double height = 480;
        var (_, view, hand) = await Open(pc, "Amazing Grace", width, height);
        var canvas = hand.Canvas;
        var scroll = (ScrollViewer)view.SongPane.ChordSheet.FindName("Scroll");
        var clock = 100.0;
        canvas.Now = () => clock;
        var carried = ChordSheetCanvas.Carrying(hand.Sheet.InKey[0]);
        const double near = ChordSheetCanvas.CarriedNear;
        const double fastest = ChordSheetCanvas.CarriedFastest;

        // How far down the sheet is scrolled, and how far it can be.
        double At()
        {
            Pictures.LayOut(view, width, height);
            return scroll.VerticalOffset;
        }
        void ScrollTo(double offset)
        {
            scroll.ScrollToVerticalOffset(offset);
            Assert.Equal(offset, At());
        }
        Assert.True(scroll.ScrollableHeight > 400, "the song is not tall enough here to scroll: " + scroll.ScrollableHeight);
        ScrollTo(200);

        // The system tells the sheet that what is carried is so far down from the top
        // of what is seen of it, or so far up from the bottom. (Where that is on the
        // sheet is asked each time it is wanted: the sheet scrolls under the pointer.)
        void Held(double? down = null, double up = 0, bool entering = false, IDataObject? what = null)
        {
            Pictures.LayOut(view, width, height);
            if (entering)
            {
                canvas.CarriedIn();
            }
            canvas.CarriedOver(
                what ?? carried,
                () => new Point(scroll.HorizontalOffset + (scroll.ViewportWidth / 2), down is { } far ? scroll.VerticalOffset + far : scroll.VerticalOffset + scroll.ViewportHeight - up),
                shift: false);
        }

        // In from the strip, over the top: the sheet does not scroll up under it,
        // however long it is held there.
        Held(4, entering: true);
        clock += 0.1;
        Held(4);
        clock += 0.1;
        Held(10);
        Assert.Equal(200, At());
        // In the middle: nothing.
        Held(120);
        clock += 0.1;
        Held(120);
        Assert.Equal(200, At());

        // At the very bottom: the clock is started the first time, and then the sheet
        // goes its fastest for the time that has passed.
        Held(up: 0);
        Assert.Equal(200, At());
        clock += 0.1;
        Held(up: 0);
        Assert.Equal(200 + (fastest * 0.1), At(), 1.0);
        // Half way into the edge: half as fast. And a long wait counts as a fifth of a
        // second, so that the sheet never jumps.
        var from = At();
        clock += 0.1;
        Held(up: near / 2);
        Assert.Equal(from + (fastest * 0.05), At(), 1.0);
        var then = At();
        clock += 30;
        Held(up: 0);
        Assert.Equal(Math.Min(scroll.ScrollableHeight, then + (fastest * 0.2)), At(), 1.0);
        // Asked very often, each time too soon for the sheet to go a whole pixel, it
        // goes all the same as the time adds up: 40 askings a thousandth of a second
        // apart, barely into the edge, are 40 thousandths at a tenth of its fastest,
        // which is two pixels and a little owed.
        ScrollTo(200);
        Held(120);
        Held(up: near * 0.9);
        for (var times = 0; times < 40; times++)
        {
            clock += 0.001;
            Held(up: near * 0.9);
        }
        Assert.Equal(200 + (fastest * 0.1 * 0.04), At(), 0.6);
        // And it goes by whole pixels, so that the words stay sharp.
        Assert.Equal(Math.Round(At()), At());

        // What is not one of the key's chords scrolls nothing, wherever it is held.
        var text = new DataObject(DataFormats.UnicodeText, "C");
        var was = At();
        Held(up: 0, what: text);
        clock += 0.1;
        Held(up: 0, what: text);
        Assert.Equal(was, At());

        // Never past the end.
        Held(120);
        for (var times = 0; times < 40; times++)
        {
            clock += 0.2;
            Held(up: 0);
        }
        Assert.Equal(scroll.ScrollableHeight, At(), 1.0);

        // Now that it has been clear of the top, held at the top the sheet goes up;
        // and never past its start.
        var bottom = At();
        Held(120);
        Held(0);
        clock += 0.1;
        Held(0);
        Assert.Equal(bottom - (fastest * 0.1), At(), 1.0);
        for (var times = 0; times < 60; times++)
        {
            clock += 0.2;
            Held(0);
        }
        Assert.Equal(0, At());
        // Leaving the edge stops the clock: back at it, the time away is not counted.
        Held(120);
        clock += 0.1;
        Held(up: 0);
        clock += 0.1;
        Assert.Equal(0, At());

        // Taken off the sheet and brought over it again, it comes in over the top
        // afresh: the sheet does not scroll up under it this time either.
        canvas.CarriedAway();
        ScrollTo(200);
        Held(4, entering: true);
        clock += 0.1;
        Held(4);
        clock += 0.1;
        Held(4);
        Assert.Equal(200, At());
    });

    // ---- The strip's end

    [Fact]
    public void APressLetGoOnAButtonIsItsClickAndPulledSixAwayTheChordIsPickedUpTheOnce() => OnAMadeUpPC(async pc =>
    {
        var (_, view, hand) = await Open(pc, "Amazing Grace");
        var strip = view.SongPane.ChordSheet;
        var fourth = strip.ChordsOfTheKey[3];
        var fifth = strip.ChordsOfTheKey[4];
        var handed = new List<string>();
        strip.Carry = (_, name) => handed.Add(name);

        // Each button says its three ways when it is pointed at (rule 46h).
        Assert.Equal("Puts C on the spot. The key 4 does the same. Or drag it onto a word.", fourth.ToolTip);
        Assert.Equal("Puts F#dim on the spot. The key 7 does the same. Or drag it onto a word.", strip.ChordsOfTheKey[6].ToolTip);

        // A press that is let go: the pointer may stray a little, and nothing is
        // picked up. (The click is then the button's own, as it always was.)
        strip.ChordPressed(fourth, new Point(10, 10));
        Assert.False(strip.ChordPulled(fourth, "C", new Point(13, 12), buttonIsDown: true));
        Assert.False(strip.ChordPulled(fourth, "C", new Point(13, 12), buttonIsDown: false));
        Assert.Empty(handed);
        // Nothing without a press: the pointer only passing over, with the button down
        // from somewhere else or not down at all.
        Assert.False(strip.ChordPulled(fourth, "C", new Point(30, 30), buttonIsDown: true));
        Assert.False(strip.ChordPulled(fourth, "C", new Point(30, 30), buttonIsDown: false));
        Assert.Empty(handed);

        // Pulled 6: picked up, the once.
        strip.ChordPressed(fourth, new Point(10, 10));
        Assert.False(strip.ChordPulled(fourth, "C", new Point(13, 12), buttonIsDown: true));
        Assert.True(strip.ChordPulled(fourth, "C", new Point(14, 12), buttonIsDown: true));
        Assert.Equal(["C"], handed);
        Assert.False(strip.ChordPulled(fourth, "C", new Point(60, 90), buttonIsDown: true));
        Assert.Equal(["C"], handed);
        // The other way, and straight up.
        strip.ChordPressed(fourth, new Point(10, 10));
        Assert.True(strip.ChordPulled(fourth, "C", new Point(6, 8), buttonIsDown: true));
        strip.ChordPressed(fourth, new Point(10, 10));
        Assert.True(strip.ChordPulled(fourth, "C", new Point(10, 4), buttonIsDown: true));
        Assert.Equal(["C", "C", "C"], handed);

        // A press on one button is not a pull of another.
        strip.ChordPressed(fourth, new Point(10, 10));
        Assert.False(strip.ChordPulled(fifth, "D", new Point(40, 40), buttonIsDown: true));
        Assert.True(strip.ChordPulled(fourth, "C", new Point(40, 40), buttonIsDown: true));
        Assert.Equal(["C", "C", "C", "C"], handed);

        // With nothing being typed, picking one up changes nothing, and neither does
        // its being carried nowhere.
        Assert.Equal(FirstSlide, InTheFile(0, "Amazing Grace", pc));
        Assert.False(view.Model.CanUndo);

        // And a click still puts the chord on the spot.
        hand.PointAt("wretch");
        fourth.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        Assert.Equal("1:G 8:G7 18:C 28:G 47:C 59:D", InTheFile(0, "Amazing Grace", pc));
    });

    [Fact]
    public void TheMousesOwnPressOnAButtonIsWhatIsPulledAndItsLettingGoForgetsThePress() => OnAMadeUpPC(async pc =>
    {
        var (_, view, _) = await Open(pc, "Amazing Grace");
        var strip = view.SongPane.ChordSheet;
        var fourth = strip.ChordsOfTheKey[3];
        var handed = new List<string>();
        // (First of all: nothing here is to reach the system's own dragging.)
        strip.Carry = (_, name) => handed.Add(name);

        // What WPF raises on a button as the mouse's left button goes down over it and
        // comes up. (No button of any mouse is pressed: the view is in no window, and
        // the pointer is nowhere on it, which it says as the point 0, 0.)
        MouseButtonEventArgs Button(RoutedEvent what) => new(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = what };
        fourth.RaiseEvent(Button(Mouse.PreviewMouseDownEvent));
        Assert.True(strip.ChordPulled(fourth, "C", new Point(0, 6), buttonIsDown: true));
        Assert.Equal(["C"], handed);

        fourth.RaiseEvent(Button(Mouse.PreviewMouseDownEvent));
        fourth.RaiseEvent(Button(Mouse.PreviewMouseUpEvent));
        Assert.False(strip.ChordPulled(fourth, "C", new Point(0, 6), buttonIsDown: true));
        Assert.Equal(["C"], handed);
    });
}
