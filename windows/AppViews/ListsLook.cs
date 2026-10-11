// The two lists at the left of the window, as far as they are numbers and words: how
// the bar between them shares out their height, and what the bar says. spec/06-library.md,
// rules 6a and 6b.
//
// It is apart from the views so that a test can hold it without drawing anything. The
// greys of the two lists are in Look.cs, with the app's other colours. The Mac's is
// `ListsLook`, in LibraryWindow.swift.

using System.Globalization;

namespace Pro7ChordEditor.AppViews;

public static class ListsLook
{
    /// <summary>How tall the bar between the two lists is.</summary>
    public const double BarHeight = 22;

    /// <summary>
    /// The least height the libraries' part is left with: room for what is over them
    /// and three of them.
    /// </summary>
    public const double LeastLibraries = 170;

    /// <summary>
    /// The least height the songs' part is left with: room for the search box and the
    /// choice over them, and a few of them.
    /// </summary>
    public const double LeastSongs = 150;

    /// <summary>How wide the lists are until the edge between them and the song is dragged.</summary>
    public const double UsualWidth = 270;

    /// <summary>The least and the most width the lists can be dragged to.</summary>
    public const double LeastWidth = 210;

    public const double MostWidth = 480;

    /// <summary>The least width the song is left with.</summary>
    public const double LeastSong = 380;

    /// <summary>How wide the edge between the lists and the song is.</summary>
    public const double EdgeWidth = 5;

    /// <summary>
    /// How wide the lists are in a window this wide, when the edge was last left where
    /// they were so wide: that, but never so wide that the song is left with less than
    /// its least, and never less than their own least. (In a window too narrow for
    /// both, the lists have theirs.) So a window made narrow takes the width from the
    /// lists, and made wide again gives it back.
    /// </summary>
    public static double Width(double leftAt, double window) =>
        Math.Max(LeastWidth, Math.Min(Math.Min(leftAt, MostWidth), window - EdgeWidth - LeastSong));

    /// <summary>
    /// What the bar says of the list under it: how many songs the library has, and how
    /// many of them are listed where the list is narrowed.
    /// </summary>
    public static string Count(int shown, int all)
    {
        var whole = all.ToString(CultureInfo.InvariantCulture);
        if (shown != all)
        {
            return shown.ToString(CultureInfo.InvariantCulture) + " OF " + whole + " SONGS";
        }
        return all == 1 ? "1 SONG" : whole + " SONGS";
    }

    /// <summary>
    /// How tall the libraries' part is, given this share of a height that also holds the
    /// bar and the songs' part: the share of it, but never so much or so little that
    /// either is left with less than its least. (In a window too short for both, the
    /// libraries have theirs.)
    /// </summary>
    public static double Top(double share, double height)
    {
        var most = Math.Max(LeastLibraries, height - BarHeight - LeastSongs);
        return Math.Min(Math.Max(share * height, LeastLibraries), most);
    }
}
