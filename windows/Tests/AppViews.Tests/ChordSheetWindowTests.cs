// The chord sheet in a REAL window: the one thing about it that only a window shows.
//
// Every other test of the sheet lays its view out with no window, and hands it what the
// pointer does by the methods the real handlers call (Hand.cs). That holds everything
// but what a window itself does to those handlers, and the sheet's first fault in a
// real window was exactly there: the sheet keeps the mouse while the button is down,
// and gave it back BEFORE it was told the button was let go. Giving it back says "the
// mouse was lost", which forgot the press; so in the window a click opened no bubble,
// and a chord that was carried went back where it had been. With no window there is no
// mouse to keep, nothing is ever lost, and every test passed. The maintainer found it
// the first time the sheet was clicked (2026-10-12).
//
// So here the view is put in a window that is really there, and that nobody sees: off
// every screen, never brought to the front, with nothing in the taskbar. It is shown
// only so that the sheet can keep the mouse, which nothing can that is not really
// shown. No key is pressed and no pointer moved: the button's pressing and letting go
// are handed to the sheet by `ButtonDown` and `ButtonUp`, which are all its real
// handlers call. Each test first makes sure the sheet DID keep the mouse, or it would
// prove nothing.

using System.Windows;
using Pro7ChordEditor.Song;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class ChordSheetWindowTests
{
    private static void InARealWindow(string song, Action<MadeUpPC, LibraryModel, Hand> what) => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        var model = pc.Model();
        var view = new LibraryView(model);
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        model.Open(pc.Song(song));
        Look.Wear(view, dark: true);
        var window = new Window
        {
            Content = view,
            Width = 1280,
            Height = 800,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -30000,
            Top = -30000,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            ShowActivated = false,
        };
        window.Show();
        try
        {
            Pictures.Settle();
            what(pc, model, new Hand(view.SongPane.ChordSheet.Canvas));
        }
        finally
        {
            window.Close();
        }
    });

    // Presses and lets go at a point, as the real button's handlers do, and holds that
    // the sheet had the mouse between the two and has not after.
    private static void Click(Hand hand, Point point)
    {
        hand.Canvas.PointerMoved(point, shift: false);
        hand.Canvas.ButtonDown(point, shift: false);
        Assert.True(hand.Canvas.IsMouseCaptured, "the sheet did not keep the mouse while the button was down: this test would prove nothing");
        hand.Canvas.ButtonUp(point, shift: false, copy: false);
        Assert.False(hand.Canvas.IsMouseCaptured);
    }

    [Fact]
    public void InARealWindowAClickOnAPlaceOpensTheBubbleThereAndAClickElsewhereKeepsWhatWasTyped() => InARealWindow("Amazing Grace", (pc, model, hand) =>
    {
        Assert.False(hand.BubbleIsOpen);
        var saved = hand.PlaceOf("saved");

        // A click on a place of a line (rule 27).
        Click(hand, hand.PointOf(saved));
        Assert.True(hand.Sheet.Typing);
        Assert.Equal(saved, hand.Sheet.TypingAt);
        Assert.True(hand.BubbleIsOpen);

        // A chord typed, and a click on another place: what was typed is kept, and the
        // bubble opens there (rule 39).
        hand.Type("Em");
        var wretch = hand.PlaceOf("wretch");
        Click(hand, hand.PointOf(wretch));
        Assert.Contains(saved.At + ":Em", hand.ChordsOf(saved.Row), StringComparison.Ordinal);
        Assert.True(hand.Sheet.Typing);
        Assert.Equal(wretch, hand.Sheet.TypingAt);
        Assert.Contains("Em", new OpenSong(pc.Song("Amazing Grace")).UsedChords);

        // A click on the bare sheet, with nothing typed: the bubble closes, and nothing
        // more is put on.
        var before = new OpenSong(pc.Song("Amazing Grace")).UsedChords.Count;
        Click(hand, Hand.BareSheet);
        Assert.False(hand.Sheet.Typing);
        Assert.False(hand.BubbleIsOpen);
        Assert.Equal(before, new OpenSong(pc.Song("Amazing Grace")).UsedChords.Count);

        // A click on a chord's own bubble opens it on that chord, all of it to be typed
        // over.
        var chord = new Spot(saved.Row, saved.At);
        Click(hand, hand.PointOf(chord, onItsChord: true));
        Assert.True(hand.Sheet.Typing);
        Assert.Equal(chord, hand.Sheet.TypingAt);
        Assert.Equal("Em", hand.Box.Text);
    });

    [Fact]
    public void InARealWindowAChordCarriedIsPutDownWhereTheButtonIsLetGo() => InARealWindow("Amazing Grace", (pc, model, hand) =>
    {
        // "That saved a wretch like me" has one chord, on "me".
        var line = hand.PlaceOf("That saved").Row;
        var chord = hand.Sheet.Rows[line].Chords.Single();
        Assert.Equal(("D", hand.PlaceOf("me").At), (chord.Name, chord.At));
        var from = new Spot(line, chord.At);
        var to = hand.PlaceOf("wretch");
        Assert.Equal(line, to.Row);

        var start = hand.PointOf(from, onItsChord: true);
        var end = hand.PointOf(to);
        hand.Canvas.PointerMoved(start, shift: false);
        hand.Canvas.ButtonDown(start, shift: false);
        Assert.True(hand.Canvas.IsMouseCaptured, "the sheet did not keep the mouse while the button was down: this test would prove nothing");
        for (var step = 1; step <= 4; step++)
        {
            hand.Canvas.Dragged(new Point(start.X + ((end.X - start.X) * step / 4), start.Y + ((end.Y - start.Y) * step / 4)), shift: false);
        }
        Assert.NotNull(hand.Sheet.Dragged);
        hand.Canvas.ButtonUp(end, shift: false, copy: false);

        // It is where it was let go, in the song and in its file; and nothing is being
        // carried, nor has the sheet the mouse.
        Assert.False(hand.Canvas.IsMouseCaptured);
        Assert.Null(hand.Sheet.Dragged);
        Assert.Equal(to.At + ":D", hand.ChordsOf(line));
        Assert.Contains(new OpenSong(pc.Song("Amazing Grace")).TextBoxes, box => box.Chords.Any(one => one.Name == "D" && box.Words[one.At..].StartsWith("wretch", StringComparison.Ordinal)));
    });

    // One of the key's chords dragged out of the strip (spec/03, rule 46a). A button
    // keeps the mouse while it is pressed, as the sheet does, which is WPF's own doing
    // and happens only in a window. Before its chord is handed to the system to carry,
    // the button gives the mouse back: so that it is a button no longer held down, of
    // which no click comes when the chord is let go. (What the system then does with
    // the chord is not run here, nor anywhere: this test's own is in its place.)
    [Fact]
    public void InARealWindowAButtonOfTheStripGivesTheMouseBackBeforeItsChordIsHandedOver() => InARealWindow("Amazing Grace", (pc, model, hand) =>
    {
        var strip = ((LibraryView)Window.GetWindow(hand.Canvas)!.Content).SongPane.ChordSheet;
        var fourth = strip.ChordsOfTheKey[3];
        var before = pc.BytesOf("Amazing Grace");
        var handed = new List<bool>();
        strip.Carry = (button, _) => handed.Add(button.IsMouseCaptured);

        hand.PointAt("wretch");
        Assert.True(fourth.CaptureMouse(), "the button could not keep the mouse: this test would prove nothing");
        Assert.True(fourth.IsMouseCaptured);
        strip.ChordPressed(fourth, new Point(5, 5));
        Assert.True(strip.ChordPulled(fourth, "C", new Point(5, 12), buttonIsDown: true));
        Assert.Equal([false], handed);
        Assert.False(fourth.IsMouseCaptured);
        Assert.False(fourth.IsPressed);
        // No click came of it: nothing was put on the spot.
        Assert.Equal(before, pc.BytesOf("Amazing Grace"));
        Assert.False(model.CanUndo);
    });
}
