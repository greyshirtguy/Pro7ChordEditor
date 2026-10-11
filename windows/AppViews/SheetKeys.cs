// Which key of the chord sheet's a key press is, with no window in it
// (spec/03-chord-sheet.md, rules 14b, 19 to 28, 47, 50 and 53; spec/10-platforms.md for
// which keys they are on Windows).
//
// It is apart from the view so that a test can press any key with any others held,
// which a test cannot do to a real keyboard. The Mac's is `SheetCanvas.key(of:)`.
//
// The keys, on Windows:
//
//   the arrows            the spot: Right and Left a word, with Shift a character;
//                         Down and Up a line
//   1 to 7                the key's own chord for that note, on the spot. The keys of
//                         the top row are taken by where they are and not by what they
//                         type, with Shift or without: on a keyboard that has its
//                         numbers on Shift, 1 to 7 are still one key each
//   A to G                opens the typing bubble with that letter
//   Enter, F2             opens it on what is at the spot
//   Delete, Backspace     takes off the chord at the spot
//   Ctrl+C, Ctrl+Shift+C  copies the spot's line, or its group
//   Ctrl+V                puts them on the lines from the spot's on
//   Ctrl+Z                undo; Ctrl+Y and Ctrl+Shift+Z, redo
//   Ctrl with plus (or equals), minus and 0: the words larger, smaller, their usual size
//
// Nothing with Alt or the Windows key held is the sheet's.

using System.Windows.Input;
using ChordSheet = Pro7ChordEditor.Song.ChordSheet;

namespace Pro7ChordEditor.AppViews;

public enum SheetKeyKind
{
    Left,
    Right,
    Up,
    Down,
    Delete,
    Open,
    Number,
    Letter,
    Copy,
    Paste,
    Undo,
    Redo,
    Zoom,
}

/// <summary>A key the sheet answers while no chord is being typed.</summary>
/// <param name="Number">For 1 to 7, which; for a zoom, a step larger (1) or smaller (-1), or the usual size (0).</param>
/// <param name="Letter">For A to G, which, as a capital.</param>
/// <param name="WholeGroup">For a copy, whether it is of the spot's whole group.</param>
public readonly record struct SheetKey(SheetKeyKind Kind, int Number = 0, string Letter = "", bool WholeGroup = false)
{
    public static SheetKey Zoom(int direction) => new(SheetKeyKind.Zoom, direction);
}

public static class SheetKeys
{
    /// <summary>Which key of the sheet's a key press is, if any, and whether Shift was held with it.</summary>
    public static (SheetKey Key, bool Shift)? Of(Key key, ModifierKeys held)
    {
        var shift = held.HasFlag(ModifierKeys.Shift);
        if ((held & (ModifierKeys.Alt | ModifierKeys.Windows)) != ModifierKeys.None)
        {
            return null;
        }
        if (held.HasFlag(ModifierKeys.Control))
        {
            return key switch
            {
                Key.C => (new SheetKey(SheetKeyKind.Copy, WholeGroup: shift), shift),
                Key.V when !shift => (new SheetKey(SheetKeyKind.Paste), false),
                Key.Z => (new SheetKey(shift ? SheetKeyKind.Redo : SheetKeyKind.Undo), shift),
                Key.Y when !shift => (new SheetKey(SheetKeyKind.Redo), false),
                // (Plus is Shift with the equals key on most keyboards: both are taken.)
                Key.OemPlus or Key.Add => (SheetKey.Zoom(1), false),
                Key.OemMinus or Key.Subtract when !shift => (SheetKey.Zoom(-1), false),
                Key.D0 or Key.NumPad0 when !shift => (SheetKey.Zoom(0), false),
                _ => null,
            };
        }
        return key switch
        {
            Key.Left => (new SheetKey(SheetKeyKind.Left), shift),
            Key.Right => (new SheetKey(SheetKeyKind.Right), shift),
            Key.Up => (new SheetKey(SheetKeyKind.Up), shift),
            Key.Down => (new SheetKey(SheetKeyKind.Down), shift),
            Key.Delete or Key.Back => (new SheetKey(SheetKeyKind.Delete), shift),
            Key.Enter or Key.F2 => (new SheetKey(SheetKeyKind.Open), shift),
            >= Key.D1 and <= Key.D7 => (new SheetKey(SheetKeyKind.Number, key - Key.D1 + 1), shift),
            >= Key.NumPad1 and <= Key.NumPad7 => (new SheetKey(SheetKeyKind.Number, key - Key.NumPad1 + 1), shift),
            >= Key.A and <= Key.G => (new SheetKey(SheetKeyKind.Letter, Letter: ((char)('A' + (key - Key.A))).ToString()), shift),
            _ => null,
        };
    }

    /// <summary>
    /// Which key of the box a chord is typed in is the sheet's to answer, if any: with
    /// nothing held but, for some, Shift.
    /// </summary>
    public static ChordSheet.EntryKey? InTheBox(Key key, ModifierKeys held)
    {
        if ((held & ~ModifierKeys.Shift) != ModifierKeys.None)
        {
            return null;
        }
        return key switch
        {
            Key.Escape => ChordSheet.EntryKey.Escape,
            Key.Enter => ChordSheet.EntryKey.Enter,
            Key.Tab when !held.HasFlag(ModifierKeys.Shift) => ChordSheet.EntryKey.Tab,
            Key.Down => ChordSheet.EntryKey.Down,
            Key.Up => ChordSheet.EntryKey.Up,
            Key.Right when !held.HasFlag(ModifierKeys.Shift) => ChordSheet.EntryKey.Right,
            Key.Back or Key.Delete => ChordSheet.EntryKey.Delete,
            _ => null,
        };
    }
}
