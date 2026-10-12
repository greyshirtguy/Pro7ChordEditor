// Someone at the chord sheet with a mouse and a keyboard: what they do to it. For the
// tests of the sheet in its view (ChordSheetViewTests.cs), and for the pictures of it.
//
// A test cannot press a button of a mouse nor a key. So the sheet's element takes
// everything the pointer and the keys do by methods that are handed a place and what is
// held (ChordSheetCanvas.cs), and the handlers of the real pointer and keys only call
// those. This calls the same ones: a point of the sheet that is over a place of a line
// is asked of the grid (`SheetGrid.PointOver`), and handed in as the pointer's.
//
// The button is pressed and let go by `ButtonDown` and `ButtonUp`, which are all that
// the real button's two handlers call: they keep the mouse and give it back as well.
// With no window there is no mouse to keep, and that part does nothing; in a real
// window it is real (ChordSheetWindowTests).
//
// Typing in the bubble is typing in a box of text, and is done as the ChordPro editor's
// tests do it: by raising what WPF raises when a character is typed.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Pro7ChordEditor.Song;
using Xunit;
using TextBox = System.Windows.Controls.TextBox;

namespace Pro7ChordEditor.AppViews.Tests;

internal sealed class Hand(ChordSheetCanvas canvas)
{
    public ChordSheetCanvas Canvas { get; } = canvas;

    public ChordSheet Sheet => Canvas.Sheet!;

    public SheetGrid Grid => Canvas.Grid!;

    /// <summary>The box of the bubble a chord is typed in.</summary>
    public TextBox Box => Canvas.Entry.Field;

    /// <summary>Whether the bubble is to be seen.</summary>
    public bool BubbleIsOpen => Canvas.Entry.Visibility == Visibility.Visible;

    /// <summary>The first line that has these words in it, at where they start.</summary>
    public Spot PlaceOf(string words, int which = 1)
    {
        var left = which;
        for (var row = 0; row < Sheet.Rows.Count; row++)
        {
            var at = Sheet.Rows[row].Kind == SheetRowKind.Line ? Sheet.Rows[row].Text.IndexOf(words, StringComparison.Ordinal) : -1;
            if (at >= 0 && --left == 0)
            {
                return new Spot(row, at);
            }
        }
        Assert.Fail("the sheet has no \"" + words + "\" in it" + (which > 1 ? " " + which + " times" : ""));
        return default;
    }

    /// <summary>The names of a line's chords, each with its place: "0:G 9:C".</summary>
    public string ChordsOf(int row) => string.Join(" ", Sheet.Rows[row].Chords.Select(chord => chord.At + ":" + chord.Name));

    public string ChordsOf(string words) => ChordsOf(PlaceOf(words).Row);

    /// <summary>A point of the sheet over a place of a line: among its words, or in the bubble of the chord that stands there.</summary>
    public Point PointOf(Spot spot, bool onItsChord = false)
    {
        var point = Grid.PointOver(spot, onItsChord);
        Assert.True(point is not null, "there is no point over row " + spot.Row + " at " + spot.At + (onItsChord ? ", on a chord" : ""));
        return point!.Value;
    }

    /// <summary>Moves the pointer over a place, with no button down.</summary>
    public void PointAt(Spot spot, bool shift = false, bool onItsChord = false) => Canvas.PointerMoved(PointOf(spot, onItsChord), shift);

    public void PointAt(string words, bool shift = false) => PointAt(PlaceOf(words), shift);

    /// <summary>Clicks on a place of a line, or on the bubble of the chord there.</summary>
    public void Click(Spot spot, bool onItsChord = false, bool shift = false) => ClickAt(PointOf(spot, onItsChord), shift);

    public void Click(string words) => Click(PlaceOf(words));

    /// <summary>Clicks at a point of the sheet, whatever is there.</summary>
    public void ClickAt(Point point, bool shift = false)
    {
        Canvas.PointerMoved(point, shift);
        Canvas.ButtonDown(point, shift);
        Canvas.ButtonUp(point, shift, copy: false);
    }

    /// <summary>A point of the bare sheet: between the slides, at its top left.</summary>
    public static Point BareSheet => new(2, 2);

    /// <summary>
    /// Presses on the chord at one place and carries it to another, in a few steps;
    /// and lets go there, unless told not to. <paramref name="copy"/>: Ctrl is held as
    /// it is let go.
    /// </summary>
    public void Drag(Spot from, Spot to, bool copy = false, bool letGo = true, bool shift = false)
    {
        var start = PointOf(from, onItsChord: true);
        var end = PointOf(to);
        Canvas.PointerMoved(start, shift);
        Canvas.ButtonDown(start, shift);
        for (var step = 1; step <= 4; step++)
        {
            Canvas.Dragged(new Point(start.X + ((end.X - start.X) * step / 4), start.Y + ((end.Y - start.Y) * step / 4)), shift);
        }
        if (letGo)
        {
            Canvas.ButtonUp(end, shift, copy);
        }
    }

    /// <summary>
    /// Presses a key, with these others held: in the box of the bubble while a chord is
    /// being typed, and on the sheet otherwise. Answers whether the key was answered; a
    /// key the box is left to deal with (Backspace in a box with something in it, a
    /// letter) is not.
    /// </summary>
    public bool Press(Key key, ModifierKeys held = ModifierKeys.None)
    {
        if (Sheet.Typing)
        {
            if (Canvas.KeyInTheBox(key, held))
            {
                return true;
            }
            // What a box of text does with the key itself.
            if (key is Key.Back or Key.Delete && held == ModifierKeys.None)
            {
                (key == Key.Back ? EditingCommands.Backspace : EditingCommands.Delete).Execute(null, Box);
            }
            return false;
        }
        if (SheetKeys.Of(key, held) is not { } found)
        {
            return false;
        }
        Canvas.KeyPressed(found.Key, found.Shift);
        return true;
    }

    /// <summary>
    /// Types, a character at a time, as a keyboard does: on the sheet a letter A to G
    /// opens the bubble with that letter in it, and what follows goes into its box.
    /// </summary>
    public void Type(string typed)
    {
        foreach (var character in typed)
        {
            if (!Sheet.Typing)
            {
                var key = char.ToUpperInvariant(character) switch
                {
                    >= 'A' and <= 'G' => Key.A + (char.ToUpperInvariant(character) - 'A'),
                    >= '1' and <= '7' => Key.D1 + (character - '1'),
                    _ => Key.None,
                };
                Assert.True(key != Key.None, "\"" + character + "\" is no key of the sheet's");
                Press(key);
                continue;
            }
            var composition = new TextComposition(InputManager.Current, Box, character.ToString());
            Box.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, composition) { RoutedEvent = TextCompositionManager.TextInputEvent });
        }
    }
}
