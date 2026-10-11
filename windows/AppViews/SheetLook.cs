// The chord sheet's colours, and the sizes that do not change with the zoom: Simple
// Presenter's (spec/03-chord-sheet.md, "How it looks"). What grows and shrinks with the
// words is `SheetSizes`' (SheetGrid.cs).
//
// The sheet is drawn by the app and looks the same on both platforms: dark, whatever
// the system is (spec/10-platforms.md). So these are plain colours, and not the look's.
//
// The Mac's is `SheetLook`, in ChordSheetView.swift, number for number.

using System.Windows.Media;

namespace Pro7ChordEditor.AppViews;

public static class SheetLook
{
    public static readonly Color Ground = Of(0x1B1C1F);
    public static readonly Color Strip = Of(0x202226);
    public static readonly Color Card = Of(0x222428);
    public static readonly Color Words = Of(0xE6E6E6);

    /// <summary>The orange of the spot, of what is picked, and of the header's switch (#ff8a1f).</summary>
    public static readonly Color Accent = Of(0xFF8A1F);

    /// <summary>The colour of a chord's name in its bubble, unless another is chosen in the settings.</summary>
    public static readonly Color ChordName = Of(0xFFD9A8);

    public static readonly Color Bubble = Of(0x34363C);
    public static readonly Color BubbleEdge = Of(0x5C5F66);

    /// <summary>A bubble that the spot is on.</summary>
    public static readonly Color Lit = Of(0x4A3A26);

    /// <summary>The thin rule that stands where a line of chords alone would have words.</summary>
    public static readonly Color Rule = Of(0x3A3C42);

    /// <summary>The frame of a slide whose group has no colour, or that is in no group.</summary>
    public static readonly Color Group = Of(0x2B2D31);

    /// <summary>The bubble a chord is typed in, and what is offered in it.</summary>
    public static readonly Color Panel = Of(0x2B2D31);

    public static readonly Color Chip = Of(0x3A3C42);
    public static readonly Color Caption = Of(0x8D9097);
    public static readonly Color Quiet = Of(0x9A9DA3);

    /// <summary>The sizes of the words and of a chord's name where the sheet is not zoomed.</summary>
    public const double WordSize = 21;

    public const double ChordSize = 14;

    /// <summary>The sizes of the words on a frame's foot, of what is offered in the typing bubble, and of what is typed in it.</summary>
    public const double LabelSize = 12;

    public const double ChipSize = 13;
    public const double FieldSize = 16;

    /// <summary>
    /// How thick a slide's frame is at its sides and its top, and how tall its foot,
    /// where the slide's number is and its group's name.
    /// </summary>
    public const double Frame = 4;

    public const double Foot = 22;

    /// <summary>
    /// The room each frame keeps clear all round it (so two frames are twice this apart),
    /// and the sheet's own margin outside that.
    /// </summary>
    public const double Around = 6;

    public const double Margin = 6;

    /// <summary>
    /// The sheet's margin at its top, which is more: the bubble a chord is typed in grows
    /// upwards from the line it is for, and over the first row of slides it would
    /// otherwise have nowhere to grow to but down over the words.
    /// </summary>
    public const double Top = 30;

    /// <summary>
    /// The shape of a card: as tall for its width as a screen is, 9 to 16. But a card is
    /// not made taller for its shape's sake than this many lines need, or a song of long
    /// lines would have cards mostly empty.
    /// </summary>
    public const double Shape = 9.0 / 16.0;

    public const double ShapedUpTo = 4;

    /// <summary>How wide the bubble a chord is typed in is.</summary>
    public const double EntryWidth = 330;

    /// <summary>The font of everything on the sheet: the system's own.</summary>
    public static readonly FontFamily Font = new("Segoe UI Variable Text, Segoe UI");

    /// <summary>Whether words on a colour are to be black: it is a light one.</summary>
    public static bool IsLight(Color colour) => ((0.299 * colour.R) + (0.587 * colour.G) + (0.114 * colour.B)) / 255 > 0.6;

    /// <summary>A colour as the file has one, each part from nought to one, as one to draw with.</summary>
    public static Color Of(Song.Colour colour) =>
        Color.FromRgb(Part(colour.Red), Part(colour.Green), Part(colour.Blue));

    /// <summary>
    /// A colour as the settings keep one, "#rrggbb", as one to draw with; nothing if it
    /// is not written so (spec/06, "Settings").
    /// </summary>
    public static Color? Written(string? written)
    {
        if (written is null || written.Length != 7 || written[0] != '#'
            || !int.TryParse(written.AsSpan(1), System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture, out var number))
        {
            return null;
        }
        return Of(number);
    }

    /// <summary>How a colour is written to be kept: its red, green and blue, each as two of 0 to 9 and a to f.</summary>
    public static string AsWritten(Color colour) =>
        "#" + colour.R.ToString("x2", System.Globalization.CultureInfo.InvariantCulture)
            + colour.G.ToString("x2", System.Globalization.CultureInfo.InvariantCulture)
            + colour.B.ToString("x2", System.Globalization.CultureInfo.InvariantCulture);

    private static byte Part(double share) => (byte)Math.Round(Math.Min(Math.Max(share, 0), 1) * 255);

    private static Color Of(int hex) => Color.FromRgb((byte)((hex >> 16) & 0xFF), (byte)((hex >> 8) & 0xFF), (byte)(hex & 0xFF));
}
