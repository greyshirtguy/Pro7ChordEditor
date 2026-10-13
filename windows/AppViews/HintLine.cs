// The line of hints under the chord sheet, drawn: what SheetHints says just now, its
// keys drawn as keys. spec/03-chord-sheet.md, rules 53a and 53h.
//
// As many of the hints as there is room for, the first ones first. It is one line, but
// for the two sentences on copying and pasting where there is not room for them side by
// side: then it is two, and the sheet over it that much shorter while they are said.
//
// ONE ELEMENT THAT DRAWS ITSELF, and not a row of text blocks and borders, for the sake
// of spec/06, rule 6e ("Nothing in the window keeps the pointer waiting"). What it says
// changes as often as the pointer crosses a chord. So:
//
//   - how tall it is, is worked out (HintFit.cs) and not asked of what is in it: a
//     change of its words measures nothing else in the window again, unless the line
//     itself is to be taller;
//   - it never asks for any width, and is cut off at its right edge: a hint too long
//     for the line cannot make the song's part of the window wider;
//   - a change of its words draws this one element again, and nothing is built. It
//     draws into a drawing of its own, as the sheet does, and does not ask WPF to
//     have it drawn: asked that, WPF arranges the element again first, which is a
//     pass of the window's laying out, if a short one (FOUND by the test that counts
//     them: one for each change of the words).
//
// IT IS BLUE, TO BE SEEN, AND LIGHTS UP WHEN WHAT IT SAYS CHANGES (rule 53h; the
// maintainer's, 2026-10-14, of the Mac's: "Let's make the tips MORE visible! - I'm
// thinking a light blue theme down there! (and maybe make it light up with a residual
// fading glow (1-2 seconds) when the tip text changes). Make the key shapes a bit
// brighter or different color to stand out a little!"). The colours are the page's, a
// light set and a dark one, and which is worn goes by the look the window wears
// (Look.cs). What is animated is how much of the lit colour shows over the ground: the
// opacity of one brush, which WPF changes by itself between frames. Nothing is laid out
// by it and nothing is drawn again by this code while it fades. It does not light up
// where the person has turned animations off in Windows.
//
// The Mac's is `SheetHintBar`, with `HintLook`, `HintGround`, `HintRow` and `KeyCap`,
// in SongView.swift.

using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Pro7ChordEditor.AppViews;

/// <summary>The line of hints under the chord sheet.</summary>
public sealed class HintLine : FrameworkElement
{
    /// <summary>How long the line takes to light up, how long it stays lit, and how long it takes to fade: a little over a second and a half in all.</summary>
    public static readonly TimeSpan Rises = TimeSpan.FromSeconds(0.12);

    public static readonly TimeSpan Stays = TimeSpan.FromSeconds(0.25);

    public static readonly TimeSpan Fades = TimeSpan.FromSeconds(1.3);

    private static readonly DependencyProperty GroundOfTheLookProperty = DependencyProperty.Register(
        "GroundOfTheLook",
        typeof(Color),
        typeof(HintLine),
        new PropertyMetadata(Colors.White, (line, change) => ((HintLine)line).Wear(Look.IsDark((Color)change.NewValue))));

    private static readonly Typeface Plain = new(SheetLook.Font, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
    private static readonly Typeface OnAKey = new(SheetLook.Font, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

    // How wide words are, by what they are and how they are written: measured once.
    private static readonly Dictionary<(string Words, double Size, bool OnAKey), double> Widths = [];

    private readonly Dictionary<(string Words, double Size, bool OnAKey), FormattedText> written = [];
    private readonly DrawingVisual drawn = new();

    // The lit colour, laid over the ground: its opacity is what lights up and fades.
    private SolidColorBrush glow = new(Colors.Transparent) { Opacity = 0 };
    private Colours colours = Colours.Of(dark: false);
    private IReadOnlyList<SheetHints.Hint> hints = [];
    private HintFit.Fit fit = new(0, false, 1);
    private double fittedTo = double.NaN;
    private LibraryModel? model;

    public HintLine()
    {
        AnimationsAreOn = () => SystemParameters.ClientAreaAnimation && PresentationSource.FromVisual(this) is not null;
        AddVisualChild(drawn);
        ClipToBounds = true;
        Focusable = false;
        Wear(dark: false);
        // (Dark or light as the window is: it follows the colour the look gives a
        // window's ground, as the lists do.)
        SetResourceReference(GroundOfTheLookProperty, Look.Ground);
    }

    /// <summary>The colours of the line, a light set and a dark one: the page's own table (rule 53h).</summary>
    /// <param name="Ground">The line's ground.</param>
    /// <param name="Lit">What it is lit to when what it says has just changed.</param>
    /// <param name="Words">Its words.</param>
    /// <param name="Key">A key: what it is filled with.</param>
    /// <param name="KeyEdge">A key's edge.</param>
    /// <param name="KeySign">The sign on a key.</param>
    /// <param name="Top">The line drawn along its top.</param>
    public sealed record Colours(Color Ground, Color Lit, Color Words, Color Key, Color KeyEdge, Color KeySign, Color Top)
    {
        public static Colours Of(bool dark) => dark
            ? new(Hex(0x1d477c), Hex(0x3a82e0), Hex(0xe3efff), Hex(0x4f90e6), Hex(0xb4d4ff), Hex(0xffffff), Hex(0x3565a3))
            : new(Hex(0xdcebff), Hex(0x9ccbff), Hex(0x17375e), Hex(0xffffff), Hex(0x3f86e3), Hex(0x0b4190), Hex(0x9fc4f2));

        private static Color Hex(int hex) => Color.FromRgb((byte)((hex >> 16) & 0xFF), (byte)((hex >> 8) & 0xFF), (byte)(hex & 0xFF));
    }

    /// <summary>
    /// Whether the line is to light up: whether Windows is set to show animations, and
    /// the line is in a window, where there is somebody to see it. (A test answers for
    /// both. A view that a test draws into a picture is in no window, and so its
    /// picture is the same whenever it is drawn.)
    /// </summary>
    internal Func<bool> AnimationsAreOn { get; set; }

    /// <summary>What the line says.</summary>
    public IReadOnlyList<SheetHints.Hint> Hints => hints;

    /// <summary>How its hints are fitted into the width it has.</summary>
    public HintFit.Fit Fitted => fit;

    /// <summary>The colours it wears now.</summary>
    public Colours Worn => colours;

    /// <summary>How many times it has lit up: for a test to count.</summary>
    internal int LitUp { get; private set; }

    /// <summary>How many times it has drawn itself: for a test to count.</summary>
    internal int Drawn { get; private set; }

    /// <summary>How much of the lit colour shows just now, from nought to one.</summary>
    internal double Lit => glow.Opacity;

    protected override int VisualChildrenCount => 1;

    protected override Visual GetVisualChild(int index) => drawn;

    /// <summary>For a picture, which cannot wait for it: lit this much, and still.</summary>
    internal void ShowLit(double much)
    {
        glow.BeginAnimation(Brush.OpacityProperty, null);
        glow.Opacity = much;
    }

    /// <summary>The model whose hints the line says. It listens for them, and for whether anything can be changed at all.</summary>
    public LibraryModel? Model
    {
        get => model;
        set
        {
            if (ReferenceEquals(model, value))
            {
                return;
            }
            if (model is not null)
            {
                model.HintsChanged -= Read;
                model.PropertyChanged -= TheModelChanged;
            }
            model = value;
            if (model is not null)
            {
                model.HintsChanged += Read;
                model.PropertyChanged += TheModelChanged;
            }
            Say(model?.SheetHintsNow ?? [], lightUp: false);
        }
    }

    private void TheModelChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => Read();

    private void Read() => Say(model?.SheetHintsNow ?? [], lightUp: true);

    /// <summary>
    /// Says these hints. If they are what it says already, nothing is done. Otherwise
    /// it is drawn again, lights up, and only if it is now to be taller or shorter is
    /// anything laid out.
    /// </summary>
    public void Say(IReadOnlyList<SheetHints.Hint> now, bool lightUp = true)
    {
        if (hints.SequenceEqual(now))
        {
            return;
        }
        hints = now;
        var was = HintFit.HeightOf(fit);
        Fit(ActualWidth);
        if (HintFit.HeightOf(fit) != was)
        {
            ToBeAnotherHeight();
        }
        Draw();
        if (lightUp)
        {
            LightUp();
        }
    }

    // The line is to be taller or shorter: it is measured again, AND SO IS WHAT HOLDS
    // IT, which is told here and not left to WPF to tell.
    //
    // WPF tells what holds an element that it wants another size only when the element
    // is measured in the ordinary run of laying out. If the element is first reached
    // by an arranging that started above it (a window being made another size just
    // then does that), it is measured there and then, quietly, and what holds it is
    // never told: the line was then two lines tall inside room for one, its second
    // sentence cut off, until something else changed. (FOUND in a picture a test drew,
    // 2026-10-14: the tests arranged from the top, as a window being resized does.)
    private void ToBeAnotherHeight()
    {
        InvalidateMeasure();
        (VisualTreeHelper.GetParent(this) as UIElement)?.InvalidateMeasure();
    }

    // Lights the line: up in about a tenth of a second, lit for a quarter, and back
    // over a little more than a second. A change while it is lit lights it again, from
    // however lit it is.
    private void LightUp()
    {
        if (!AnimationsAreOn())
        {
            return;
        }
        LitUp++;
        var lighting = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.HoldEnd };
        lighting.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(Rises), new QuadraticEase { EasingMode = EasingMode.EaseOut }));
        lighting.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(Rises + Stays)));
        lighting.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(Rises + Stays + Fades), new QuadraticEase { EasingMode = EasingMode.EaseOut }));
        glow.BeginAnimation(Brush.OpacityProperty, lighting, HandoffBehavior.SnapshotAndReplace);
    }

    private void Wear(bool dark)
    {
        colours = Colours.Of(dark);
        var lit = glow.Opacity;
        glow = new SolidColorBrush(colours.Lit) { Opacity = lit };
        written.Clear();
        Draw();
    }

    private double Dots => VisualTreeHelper.GetDpi(this).PixelsPerDip;

    /// <summary>How wide some words are at a size, as the line draws them: measured once and remembered.</summary>
    internal static double WidthOf(string words, double size, bool onAKey)
    {
        var key = (words, size, onAKey);
        if (!Widths.TryGetValue(key, out var width))
        {
            width = new FormattedText(words, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, onAKey ? OnAKey : Plain, size, Brushes.Black, 1).WidthIncludingTrailingWhitespace;
            Widths[key] = width;
        }
        return width;
    }

    private FormattedText Written(string words, double size, bool onAKey)
    {
        var key = (words, size, onAKey);
        if (!written.TryGetValue(key, out var text))
        {
            var brush = new SolidColorBrush(onAKey ? colours.KeySign : colours.Words);
            brush.Freeze();
            text = new FormattedText(words, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, onAKey ? OnAKey : Plain, size, brush, Dots);
            written[key] = text;
        }
        return text;
    }

    private void Fit(double width)
    {
        fittedTo = width;
        fit = HintFit.Of(hints, Math.Max(0, width - (2 * HintFit.Sides)), WidthOf);
    }

    // As tall as was worked out, and asking for no width at all: what does not fit is
    // cut off at the right, and pushes nothing about.
    protected override Size MeasureOverride(Size availableSize)
    {
        if (!double.IsInfinity(availableSize.Width))
        {
            Fit(availableSize.Width);
        }
        return new Size(0, HintFit.HeightOf(fit));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (finalSize.Width != fittedTo)
        {
            var was = HintFit.HeightOf(fit);
            Fit(finalSize.Width);
            if (HintFit.HeightOf(fit) != was)
            {
                ToBeAnotherHeight();
            }
        }
        return finalSize;
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        written.Clear();
        Draw();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        Draw();
    }

    // Draws the line as it now is: its ground, the lit colour over it as far as it is
    // lit, the line along its top, and as many of its hints as were fitted.
    private void Draw()
    {
        Drawn++;
        using var context = drawn.RenderOpen();
        var whole = new Rect(0, 0, ActualWidth, ActualHeight);
        context.DrawRectangle(Frozen(colours.Ground), null, whole);
        context.DrawRectangle(glow, null, whole);
        context.DrawRectangle(Frozen(colours.Top), null, new Rect(0, 0, ActualWidth, 1));
        if (fit.Count == 0)
        {
            return;
        }
        var size = fit.Size;
        var line = HintFit.KeyHeight * size;
        if (fit.Stacked)
        {
            var all = (fit.Count * line) + ((fit.Count - 1) * HintFit.Lines * size);
            var top = Math.Round((ActualHeight - all) / 2);
            for (var n = 0; n < fit.Count; n++)
            {
                Draw(context, hints[n], HintFit.Sides, top + (n * (line + (HintFit.Lines * size))), size);
            }
        }
        else
        {
            var top = Math.Round((ActualHeight - line) / 2);
            var x = HintFit.Sides;
            for (var n = 0; n < fit.Count; n++)
            {
                x = Draw(context, hints[n], x, top, size) + (HintFit.Between * size);
            }
        }
    }

    // One hint, from a point: its words before its keys, its keys, and what it says.
    // Answers with where it ends.
    private double Draw(DrawingContext context, SheetHints.Hint hint, double x, double top, double size)
    {
        var line = HintFit.KeyHeight * size;
        var first = true;
        double Next()
        {
            if (!first)
            {
                x += HintFit.Within * size;
            }
            first = false;
            return x;
        }
        void Words(string words)
        {
            var text = Written(words, HintFit.WordSize * size, onAKey: false);
            context.DrawText(text, new Point(Next(), top + Math.Round((line - text.Height) / 2)));
            x += Math.Ceiling(WidthOf(words, HintFit.WordSize * size, false));
        }
        if (hint.Lead.Length > 0)
        {
            Words(hint.Lead);
        }
        foreach (var key in hint.Keys)
        {
            // A key, drawn as one: its sign in a small box with rounded corners,
            // brighter than the line it is on, so that the keys are what is seen first.
            var width = HintFit.WidthOfKey(key, size, WidthOf);
            var box = new Rect(Next() + 0.5, top + 0.5, width - 1, line - 1);
            context.DrawRoundedRectangle(Frozen(colours.Key), new Pen(Frozen(colours.KeyEdge), 1), box, 4.5 * size, 4.5 * size);
            var sign = Written(key, HintFit.KeySize * size, onAKey: true);
            context.DrawText(sign, new Point(x + ((width - sign.WidthIncludingTrailingWhitespace) / 2), top + Math.Round((line - sign.Height) / 2)));
            x += width;
        }
        if (hint.Says.Length > 0)
        {
            Words(hint.Says);
        }
        return x;
    }

    private static SolidColorBrush Frozen(Color colour)
    {
        var brush = new SolidColorBrush(colour);
        brush.Freeze();
        return brush;
    }

    // What a screen reader is told: what the line says, as it would be read out.
    protected override AutomationPeer OnCreateAutomationPeer() => new Told(this);

    private sealed class Told(HintLine line) : FrameworkElementAutomationPeer(line)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

        protected override string GetClassNameCore() => "HintLine";

        protected override string GetNameCore() => string.Join(". ", line.hints.Select(hint => hint.Read));

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;
    }
}
