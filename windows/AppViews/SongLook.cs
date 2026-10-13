// The song's header, as far as it is numbers: which of the ways it can be laid out has
// room for the song's name, the switch between its two editors, its two keys and
// Earlier Versions. spec/06-library.md, rule 12: the switch "is in the middle of the
// song's header", "the name is at the header's left and the keys at its right", and
// "where the pane is too narrow for the name to be read beside them, the keys go on a
// row of their own under it."
//
// It is apart from the view so that a test can hold it without drawing anything, as the
// two lists' arithmetic is (ListsLook.cs). These are the ways the Mac's header is laid
// out, in the same order (SongHeader.swift). Until 2026-10-13 the Mac had the system
// try each way and take the first that fits, and the system built all three, their
// pickers with them, whenever anything in the song's part of the window changed: which
// was what kept its pointer waiting (spec/06, rule 6e). It is arithmetic there too
// now, as it has been here from the first. Whatever a platform offers that chooses
// among arrangements by building them, a header with pickers in it is not to be left
// to it.

namespace Pro7ChordEditor.AppViews;

/// <summary>The colours of the song's header that are this app's own.</summary>
public static class HeaderLook
{
    /// <summary>
    /// The Earlier Versions button: a blue, the colour furthest from the sheet's orange,
    /// which in this header says which editor is shown (spec/07, rule 15: #2978db).
    /// </summary>
    public static readonly System.Windows.Media.Color EarlierVersions = System.Windows.Media.Color.FromRgb(0x29, 0x78, 0xDB);
}

/// <summary>The ways the song's header is laid out, the widest first.</summary>
public enum HeaderRows
{
    /// <summary>One row: the name at the left, read whole; the switch; Earlier Versions and the keys at the right.</summary>
    One,

    /// <summary>The name, read whole, with the switch at its right; under them the keys, with Earlier Versions at the right of their row.</summary>
    KeysUnder,

    /// <summary>A row each: the name, with Earlier Versions at its right; the switch, in the middle; the keys.</summary>
    EachARow,
}

public static class SongLook
{
    /// <summary>How far apart the things in the header stand.</summary>
    public const double Gap = 14;

    /// <summary>
    /// How the header is laid out in the room it has: the first of its ways that fits.
    /// Nothing is ever cut short to fit but the name, and the name only where even a row
    /// to itself and Earlier Versions is too narrow for it.
    /// </summary>
    /// <param name="name">How wide the song's name is when none of it is cut short.</param>
    /// <param name="editors">How wide the switch between the two editors is; nothing if it is not there.</param>
    /// <param name="earlier">How wide Earlier Versions is; nothing if it is not there.</param>
    /// <param name="keys">How wide the two keys are together; nothing if they are not there.</param>
    /// <param name="room">How wide the header is.</param>
    public static HeaderRows Rows(double name, double editors, double earlier, double keys, double room)
    {
        var atTheRight = AtTheRight(earlier, keys);
        if (name + Beside(editors) + Beside(atTheRight) <= room)
        {
            return HeaderRows.One;
        }
        return name + Beside(editors) <= room && atTheRight <= room ? HeaderRows.KeysUnder : HeaderRows.EachARow;
    }

    /// <summary>How wide Earlier Versions and the keys are, side by side.</summary>
    public static double AtTheRight(double earlier, double keys) => earlier + (earlier > 0 && keys > 0 ? Gap : 0) + keys;

    /// <summary>
    /// On the one row, how much of it the name's side has, so that the switch stands in
    /// the middle of the pane and not of what is left over: half of what the switch
    /// leaves, where the name can be read in one half and what is at the right in the
    /// other. Where one of them needs more than its half, it has it, and the switch
    /// stands where the two leave it.
    /// </summary>
    public static double NameSide(double name, double editors, double atTheRight, double room)
    {
        var both = room - editors - (2 * Gap);
        return Math.Max(name, Math.Min(both / 2, both - atTheRight));
    }

    // (What stands beside something else is a gap from it; what is not there is nothing.)
    private static double Beside(double width) => width > 0 ? Gap + width : 0;
}
