// The song's header, as far as it is numbers: which of the ways it can be laid out has
// room for the song's name, its two keys and Earlier Versions. spec/06-library.md, rule
// 12: "Where the pane is too narrow for the name to be read beside them, the keys go on
// a row of their own under it."
//
// It is apart from the view so that a test can hold it without drawing anything, as the
// two lists' arithmetic is (ListsLook.cs). On the Mac the system tries each way the
// header could be laid out and takes the first that fits (SongView.swift, `header`);
// these are the same ways, in the same order, less the switch between the two editors,
// which comes with the chord sheet.

namespace Pro7ChordEditor.AppViews;

/// <summary>The ways the song's header is laid out, the widest first.</summary>
public enum HeaderRows
{
    /// <summary>One row: the name at the left, read whole; Earlier Versions and the keys at the right.</summary>
    One,

    /// <summary>The name over the keys, with Earlier Versions at the right of the keys' row.</summary>
    KeysUnderTheName,

    /// <summary>The name with Earlier Versions at its right, over the keys by themselves.</summary>
    KeysByThemselves,
}

public static class SongLook
{
    /// <summary>How far apart the things in the header stand.</summary>
    public const double Gap = 14;

    /// <summary>
    /// How the header is laid out in the room it has: the first of its ways that fits.
    /// Nothing is ever cut short to fit but the name, and the name not for the sake of
    /// one row: if it would be, the keys go under it.
    /// </summary>
    /// <param name="name">How wide the song's name is when none of it is cut short.</param>
    /// <param name="earlier">How wide Earlier Versions is; nothing if it is not there.</param>
    /// <param name="keys">How wide the two keys are together; nothing if they are not there.</param>
    /// <param name="room">How wide the header is.</param>
    public static HeaderRows Rows(double name, double earlier, double keys, double room)
    {
        var atTheRight = earlier > 0 ? Gap + earlier : 0;
        if (name + atTheRight + (keys > 0 ? Gap + keys : 0) <= room)
        {
            return HeaderRows.One;
        }
        return keys + atTheRight <= room ? HeaderRows.KeysUnderTheName : HeaderRows.KeysByThemselves;
    }
}
