// Which key of the chord sheet's a key press is (SheetKeys.cs), and what the sheet's
// letters measure (SheetMetrics.cs): the two parts of the sheet's view that are tables
// and sums, held with no view.
//
// A test cannot press a key. So the view asks a function which key a press is, and the
// function is asked here about every key it answers, with every other key held that
// changes its answer, and about some it must not answer.

using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Song;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class SheetKeysTests
{
    private const ModifierKeys None = ModifierKeys.None;
    private const ModifierKeys Shift = ModifierKeys.Shift;
    private const ModifierKeys Ctrl = ModifierKeys.Control;

    // A key of the sheet's, said in a few words.
    private static string Of(Key key, ModifierKeys held = None)
    {
        if (SheetKeys.Of(key, held) is not { } found)
        {
            return "none";
        }
        var what = found.Key.Kind switch
        {
            SheetKeyKind.Number => "number " + found.Key.Number.ToString(CultureInfo.InvariantCulture),
            SheetKeyKind.Letter => "letter " + found.Key.Letter,
            SheetKeyKind.Zoom => "zoom " + found.Key.Number.ToString(CultureInfo.InvariantCulture),
            SheetKeyKind.Copy => found.Key.WholeGroup ? "copy group" : "copy line",
            var kind => kind.ToString().ToLowerInvariant(),
        };
        return what + (found.Shift ? ", shift" : "");
    }

    [Fact]
    public void TheKeysOfTheSheetWithNoChordBeingTyped()
    {
        // The arrows, and with Shift.
        Assert.Equal(("left", "right", "up", "down"), (Of(Key.Left), Of(Key.Right), Of(Key.Up), Of(Key.Down)));
        Assert.Equal(("left, shift", "right, shift"), (Of(Key.Left, Shift), Of(Key.Right, Shift)));
        // Delete and Backspace; Enter and F2.
        Assert.Equal(("delete", "delete"), (Of(Key.Delete), Of(Key.Back)));
        Assert.Equal(("open", "open", "open"), (Of(Key.Enter), Of(Key.Return), Of(Key.F2)));
        // 1 to 7, of the top row and of the number pad, and no other number. With Shift
        // they are still themselves: on some keyboards the numbers are typed with it.
        Assert.Equal(
            ["number 1", "number 2", "number 3", "number 4", "number 5", "number 6", "number 7"],
            new[] { Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7 }.Select(key => Of(key)));
        Assert.Equal(
            ["number 1", "number 2", "number 3", "number 4", "number 5", "number 6", "number 7"],
            new[] { Key.NumPad1, Key.NumPad2, Key.NumPad3, Key.NumPad4, Key.NumPad5, Key.NumPad6, Key.NumPad7 }.Select(key => Of(key)));
        Assert.Equal("number 4, shift", Of(Key.D4, Shift));
        Assert.All(new[] { Key.D0, Key.D8, Key.D9, Key.NumPad0, Key.NumPad8, Key.NumPad9 }, key => Assert.Equal("none", Of(key)));
        // A to G, as capitals, and no other letter.
        Assert.Equal(
            ["letter A", "letter B", "letter C", "letter D", "letter E", "letter F", "letter G"],
            new[] { Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G }.Select(key => Of(key)));
        Assert.Equal("letter E, shift", Of(Key.E, Shift));
        Assert.All(new[] { Key.H, Key.I, Key.M, Key.S, Key.V, Key.Y, Key.Z, Key.Space, Key.Tab, Key.Escape, Key.OemPlus, Key.OemMinus, Key.Home, Key.PageDown }, key => Assert.Equal("none", Of(key)));

        // With Ctrl: copying a line or, with Shift, its group; pasting; undo and redo.
        Assert.Equal(("copy line", "copy group, shift"), (Of(Key.C, Ctrl), Of(Key.C, Ctrl | Shift)));
        Assert.Equal(("paste", "none"), (Of(Key.V, Ctrl), Of(Key.V, Ctrl | Shift)));
        Assert.Equal(("undo", "redo, shift", "redo", "none"), (Of(Key.Z, Ctrl), Of(Key.Z, Ctrl | Shift), Of(Key.Y, Ctrl), Of(Key.Y, Ctrl | Shift)));
        // The size: plus (Shift and the equals key), the equals key alone, the plus of
        // the number pad; minus; nought.
        Assert.Equal(("zoom 1", "zoom 1", "zoom 1"), (Of(Key.OemPlus, Ctrl), Of(Key.OemPlus, Ctrl | Shift), Of(Key.Add, Ctrl)));
        Assert.Equal(("zoom -1", "zoom -1", "none"), (Of(Key.OemMinus, Ctrl), Of(Key.Subtract, Ctrl), Of(Key.OemMinus, Ctrl | Shift)));
        Assert.Equal(("zoom 0", "zoom 0", "none"), (Of(Key.D0, Ctrl), Of(Key.NumPad0, Ctrl), Of(Key.D0, Ctrl | Shift)));
        // With Ctrl, nothing else is the sheet's: not the arrows, not a number, not a
        // letter that is no command here.
        Assert.All(new[] { Key.Left, Key.Down, Key.D4, Key.A, Key.D, Key.G, Key.Delete, Key.Enter, Key.X, Key.S }, key => Assert.Equal("none", Of(key, Ctrl)));
        // And nothing with Alt or the Windows key held, whatever else is.
        foreach (var held in new[] { ModifierKeys.Alt, ModifierKeys.Windows, ModifierKeys.Alt | Ctrl, ModifierKeys.Windows | Shift })
        {
            Assert.All(new[] { Key.Left, Key.D4, Key.E, Key.Delete, Key.Enter, Key.C, Key.V, Key.Z, Key.OemPlus }, key => Assert.Equal("none", Of(key, held)));
        }
    }

    [Fact]
    public void TheKeysOfTheBoxAChordIsTypedIn()
    {
        Assert.Equal(ChordSheet.EntryKey.Escape, SheetKeys.InTheBox(Key.Escape, None));
        Assert.Equal(ChordSheet.EntryKey.Enter, SheetKeys.InTheBox(Key.Enter, None));
        Assert.Equal(ChordSheet.EntryKey.Tab, SheetKeys.InTheBox(Key.Tab, None));
        Assert.Equal(ChordSheet.EntryKey.Down, SheetKeys.InTheBox(Key.Down, None));
        Assert.Equal(ChordSheet.EntryKey.Up, SheetKeys.InTheBox(Key.Up, None));
        Assert.Equal(ChordSheet.EntryKey.Right, SheetKeys.InTheBox(Key.Right, None));
        Assert.Equal(ChordSheet.EntryKey.Delete, SheetKeys.InTheBox(Key.Back, None));
        Assert.Equal(ChordSheet.EntryKey.Delete, SheetKeys.InTheBox(Key.Delete, None));
        // With Shift: Right selects in the box, and Tab would leave it backwards;
        // neither is the sheet's. The others are, as without.
        Assert.Null(SheetKeys.InTheBox(Key.Right, Shift));
        Assert.Null(SheetKeys.InTheBox(Key.Tab, Shift));
        Assert.Equal(ChordSheet.EntryKey.Enter, SheetKeys.InTheBox(Key.Enter, Shift));
        Assert.Equal(ChordSheet.EntryKey.Down, SheetKeys.InTheBox(Key.Down, Shift));
        // With anything else held, none is: Ctrl with Backspace takes a word out of a box.
        foreach (var held in new[] { Ctrl, ModifierKeys.Alt, ModifierKeys.Windows, Ctrl | Shift })
        {
            Assert.All(new[] { Key.Escape, Key.Enter, Key.Tab, Key.Down, Key.Up, Key.Right, Key.Back, Key.Delete }, key => Assert.Null(SheetKeys.InTheBox(key, held)));
        }
        // And the box's own keys are its own.
        Assert.All(new[] { Key.Left, Key.Home, Key.End, Key.A, Key.D4, Key.Space, Key.OemQuestion }, key => Assert.Null(SheetKeys.InTheBox(key, None)));
    }

    // ---- What the letters measure

    [Fact]
    public void APlaceAlongALineThatIsSaidToGoBackwardsIsNotBelieved()
    {
        // As they come: each no less than the one before, and the line's end last.
        Assert.Equal([0.0, 10, 20, 30], SheetMetrics.Believed([0, 10, 20, 30], 30));
        // One said to start left of the one before it is put where the next that is
        // believed starts.
        Assert.Equal([0.0, 10, 20, 20, 30], SheetMetrics.Believed([0, 10, 5, 20, 30], 30));
        Assert.Equal([0.0, 10, 30, 30, 30], SheetMetrics.Believed([0, 10, 5, 7, 30], 30));
        // The last of a line so said (a character with no width that ends it) is put at
        // the line's end.
        Assert.Equal([0.0, 10, 20, 30, 30], SheetMetrics.Believed([0, 10, 20, 0, 30], 30));
        Assert.Equal([0.0, 10, 20, 26], SheetMetrics.Believed([0, 10, 20, 0], 26));
        // Two at one place are both believed: a character with no width is at the place
        // of the one after it.
        Assert.Equal([0.0, 10, 10, 20], SheetMetrics.Believed([0, 10, 10, 20], 20));
        Assert.Equal([0.0], SheetMetrics.Believed([0], 0));
        // What is handed in is not itself changed.
        var said = new double[] { 0, 10, 5, 20 };
        SheetMetrics.Believed(said, 20);
        Assert.Equal([0.0, 10, 5, 20], said);
    }

    [Fact]
    public void WhatALineAndAChordsNameMeasureGrowsWithTheSizeAndANameIsBold() => Pictures.OnAThreadOfItsOwn(() =>
    {
        var row = new SheetRow(SheetRowKind.Line, 0) { Text = "Amazing grace how sweet the sound", Chords = [new Chord(1, "G")] };
        var group = new SheetRow(SheetRowKind.Group, 0) { Name = "Verse 1" };
        var usual = new SheetMetrics(1);
        usual.Hold([group, row]);
        // A place is a unit of the words: each no less than the one before it, the
        // first at nought, and one past the last at the line's end.
        Assert.Equal(0, usual.Advance(1, 0));
        var last = 0.0;
        for (var units = 0; units <= row.Text.Length; units++)
        {
            Assert.True(usual.Advance(1, units) >= last, "the place " + units + " is left of the one before it");
            last = usual.Advance(1, units);
        }
        Assert.Equal(usual.Line(1)!.WidthIncludingTrailingWhitespace, usual.Advance(1, row.Text.Length), 0.001);
        Assert.InRange(last, 250, 400);
        // Asked for more than the line has, or less than nothing, it is the end or the start.
        Assert.Equal(last, usual.Advance(1, 999));
        Assert.Equal(0, usual.Advance(1, -5));
        // A group's name is no line: it has nothing to draw and no width.
        Assert.Null(usual.Line(0));
        Assert.Equal(0, usual.Advance(0, 4));
        Assert.Null(usual.Line(7));

        // Twice the size, twice as wide and as tall, near enough: the words, a chord's
        // name, and the heights that go with them. What a frame's foot says does not grow.
        var twice = new SheetMetrics(2);
        twice.Hold([group, row]);
        Assert.Equal(2, twice.Advance(1, row.Text.Length) / last, 1);
        Assert.Equal(2, twice.NameWidth("F#m7") / usual.NameWidth("F#m7"), 1);
        Assert.Equal(2, twice.WordHeight / usual.WordHeight, 1);
        Assert.Equal(2, twice.ChordHeight / usual.ChordHeight, 1);
        Assert.Equal(usual.LabelWidth("3  Verse 2"), twice.LabelWidth("3  Verse 2"));
        Assert.True(usual.WordHeight > usual.ChordHeight);
        Assert.Equal(SheetMetrics.HeightOf(21), usual.WordHeight);
        Assert.Equal(SheetMetrics.HeightOf(14), usual.ChordHeight);
        // A bubble is its name and 7 either side of it, at the size.
        Assert.Equal(usual.NameWidth("G") + 14, usual.Bubble("G"), 0.0001);
        Assert.Equal(twice.NameWidth("G") + 28, twice.Bubble("G"), 0.0001);

        // A chord's name is bold: wider than the same letters as the sheet's words
        // would have them at its size.
        var plain = usual.Text("F#m7b5", bold: false, SheetLook.ChordSize, Brushes.White).WidthIncludingTrailingWhitespace;
        Assert.True(usual.NameWidth("F#m7b5") > plain * 1.03, "the name is " + usual.NameWidth("F#m7b5") + " wide and plain letters " + plain);
        // A name is made ready once, and kept; and made again when the colour chords are
        // drawn in is changed.
        var made = usual.Name("Am");
        Assert.Same(made, usual.Name("Am"));
        usual.ChordBrush = Brushes.Lime;
        Assert.NotSame(made, usual.Name("Am"));
        Assert.Equal(made.WidthIncludingTrailingWhitespace, usual.Name("Am").WidthIncludingTrailingWhitespace);
    });
}
