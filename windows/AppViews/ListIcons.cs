// The two small pictures that stand before names in the lists at the left of the
// window: an orange one before a library, a grey one before a song (spec/06-library.md,
// rule 6c).
//
// WHOSE THEY ARE. Both are ProPresenter's own, not this app's, and are here at the
// maintainer's asking (of the Mac app, 2026-10-11), so that what is a ProPresenter
// library and what is a presentation look here as they do there:
//
//   - the outline is the one ProPresenter draws before a presentation in its own
//     lists ("PresentationItem", from a file the maintainer supplied);
//   - the orange square with that outline on it is ProPresenter's mark. Its three
//     colours were read off a picture of the mark; nothing of the picture itself is
//     kept.
//
// They are Renewed Vision's, and nothing in this repository's licence makes them
// anyone else's. This app is not ProPresenter and has nothing to do with Renewed
// Vision (AGENTS.md). If either is ever to be taken out, this is the one file.
//
// HOW THEY ARE DRAWN. From the outline's path, in code, and not from picture files: a
// list has one for every row, at whatever size the row's words are, on a screen of any
// sharpness, and a path is sharp at all of them and costs nothing to keep. The path is
// the Mac app's, to the digit (ListIcons.swift), and is read by WPF's own reader of
// such paths.

using System.Windows;
using System.Windows.Media;

namespace Pro7ChordEditor.AppViews;

/// <summary>
/// The outline that stands for a presentation: a bar, and beside it a screen seen from
/// its side.
/// </summary>
public static class PresentationGlyph
{
    /// <summary>The side of the square the outline is drawn in.</summary>
    public const double Square = 200;

    // The outline's path as its file has it.
    private const string Drawing =
        "M52.7658902,159.785959 L11.3005931,171.295039 C5.62059887,172.866106 0,168.595351 0,162.693146 "
        + "L0,37.199155 C0,31.2967302 5.62059887,27.025646 11.3005931,28.5977009 L52.7658902,40.1059032 "
        + "C55.8061407,40.9512711 57.9119851,43.7096957 57.9119851,46.8685173 L57.9119851,153.024113 "
        + "C57.9119851,156.182715 55.8061407,158.940591 52.7658902,159.785959 Z "
        + "M79.8014154,33.3284677 C73.0981966,34.8114846 73.7210242,44.5549539 80.5429238,45.1778912 "
        + "L133.116026,58.5103316 C139.092887,60.3789241 143.156143,65.9105947 143.156143,72.1689523 "
        + "L143.156143,126.967019 C143.156143,133.358879 138.89966,138.994299 132.744943,140.72895 "
        + "L79.4900565,153.749812 C72.6829782,154.373408 72.0452195,164.072084 78.7039741,165.584963 "
        + "L188.002363,193.747792 C194.142808,195.156373 200,190.484892 200,184.18196 L200,15.8288026 "
        + "C200,9.55551379 194.202093,4.88402157 188.061648,6.23356914 L79.786594,33.3284677 "
        + "L79.8014154,33.3284677 Z";

    /// <summary>The outline, in its square of 200, made once.</summary>
    public static Geometry Outline { get; } = Made();

    private static Geometry Made()
    {
        var outline = Geometry.Parse(Drawing);
        outline.Freeze();
        return outline;
    }

    /// <summary>The outline as it is drawn in a rectangle: in the middle of it, as big as fits, kept square.</summary>
    public static Geometry FittedIn(Rect room)
    {
        var scale = Math.Min(room.Width, room.Height) / Square;
        var fitted = new TransformGroup();
        fitted.Children.Add(new ScaleTransform(scale, scale));
        fitted.Children.Add(new TranslateTransform(room.X + ((room.Width - (Square * scale)) / 2), room.Y + ((room.Height - (Square * scale)) / 2)));
        var drawn = Outline.Clone();
        drawn.Transform = fitted;
        drawn.Freeze();
        return drawn;
    }
}

/// <summary>What stands before a song's name: the outline, in the colour of the words about it but quieter.</summary>
public sealed class PresentationIcon : FrameworkElement
{
    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(PresentationIcon), new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    protected override void OnRender(DrawingContext drawing) =>
        drawing.DrawGeometry(Fill, null, PresentationGlyph.FittedIn(new Rect(RenderSize)));
}

/// <summary>
/// What stands before a library's name: the outline in white on an orange square with
/// rounded corners, lighter at its top left than at its bottom right.
/// </summary>
public sealed class LibraryIcon : FrameworkElement
{
    /// <summary>The mark's three colours, from its top left corner, its middle and its bottom right.</summary>
    public static readonly IReadOnlyList<Color> Colours = [Color.FromRgb(255, 201, 0), Color.FromRgb(255, 143, 0), Color.FromRgb(255, 64, 0)];

    private static readonly Brush Orange = MadeOrange();

    private static LinearGradientBrush MadeOrange()
    {
        var orange = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
        orange.GradientStops.Add(new GradientStop(Colours[0], 0));
        orange.GradientStops.Add(new GradientStop(Colours[1], 0.5));
        orange.GradientStops.Add(new GradientStop(Colours[2], 1));
        orange.Freeze();
        return orange;
    }

    protected override void OnRender(DrawingContext drawing)
    {
        var side = Math.Min(RenderSize.Width, RenderSize.Height);
        var square = new Rect((RenderSize.Width - side) / 2, (RenderSize.Height - side) / 2, side, side);
        drawing.DrawRoundedRectangle(Orange, null, square, side * 0.23, side * 0.23);
        // (On the mark the outline is two thirds as wide as the square.)
        var inner = side * 0.66;
        drawing.DrawGeometry(Brushes.White, null, PresentationGlyph.FittedIn(new Rect(square.X + ((side - inner) / 2), square.Y + ((side - inner) / 2), inner, inner)));
    }
}
