// The letters of the ChordPro text, drawn in their colours behind the text box that is
// typed in: chords in orange and bold, brackets and all, and the lines in curly brackets
// greyed (spec/04-chordpro-editor.md, rule 4).
//
// WHY IT IS DRAWN APART. WPF's plain text box has one colour for all its text. The page
// offers two ways to more: a rich text box, which is what the first Windows editor was
// and where its trouble with places came from; or AvalonEdit, which is a dependency to
// be agreed first (spec/10-platforms.md). This is a third, with neither: the text box
// keeps its text, its caret and its selection, and draws its own letters in no colour at
// all; and this draws the same letters, in colour, exactly where the box has them. So
// the colouring cannot change the text, having no text of its own: it is asked of the
// box's every time.
//
// "Exactly where" is the box's own answer: each line is drawn at the place the box says
// its first character is (`GetRectFromCharacterIndex`), in the box's own font and size,
// laid out by the same means the box lays its lines out. A test draws the two and holds
// them to be dot for dot the same (ChordProTypingTests).
//
// It draws when the box has finished laying itself out, and not before: the places are
// only true then. So whatever changes what is to be drawn (the text, the size, the
// colours, the scrolling) only marks this as stale, and the next laying out draws it.
//
// Nothing here is a rule. Which stretches of a line are chords is
// Song/ChordProEditor.cs's (`Colouring`). The Mac colours its text view's own text
// (ChordProView.swift), which a Mac's can have.

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ChordProEditor = Pro7ChordEditor.Song.ChordProEditor;

namespace Pro7ChordEditor.AppViews;

/// <summary>The ChordPro text's letters, in colour, behind the box they are typed in.</summary>
public sealed class ChordProUnderlay : FrameworkElement
{
    public static readonly DependencyProperty WordsBrushProperty = Brush(nameof(WordsBrush));
    public static readonly DependencyProperty ChordBrushProperty = Brush(nameof(ChordBrush));
    public static readonly DependencyProperty QuietBrushProperty = Brush(nameof(QuietBrush));

    // How many lines' letters are kept made ready, before all are let go.
    private const int MostKept = 4000;

    private readonly DrawingVisual drawing = new();
    private readonly Dictionary<string, FormattedText> kept = new(StringComparer.Ordinal);
    private TextBox? box;
    private string[] lines = [""];
    private int[] starts = [0];
    private bool stale = true;

    public ChordProUnderlay()
    {
        AddVisualChild(drawing);
        IsHitTestVisible = false;
        LayoutUpdated += (_, _) =>
        {
            if (stale)
            {
                Draw();
            }
        };
        IsVisibleChanged += (_, _) => Stale();
        SizeChanged += (_, _) => Stale();
    }

    /// <summary>The colour of the song's words.</summary>
    public Brush? WordsBrush
    {
        get => (Brush?)GetValue(WordsBrushProperty);
        set => SetValue(WordsBrushProperty, value);
    }

    /// <summary>The colour of a chord, brackets and all.</summary>
    public Brush? ChordBrush
    {
        get => (Brush?)GetValue(ChordBrushProperty);
        set => SetValue(ChordBrushProperty, value);
    }

    /// <summary>The colour of a line in curly brackets.</summary>
    public Brush? QuietBrush
    {
        get => (Brush?)GetValue(QuietBrushProperty);
        set => SetValue(QuietBrushProperty, value);
    }

    protected override int VisualChildrenCount => 1;

    protected override Visual GetVisualChild(int index) => drawing;

    private static DependencyProperty Brush(string name) => DependencyProperty.Register(
        name,
        typeof(Brush),
        typeof(ChordProUnderlay),
        new PropertyMetadata(null, (made, _) => ((ChordProUnderlay)made).Remake()));

    /// <summary>The box whose letters these are. Asked once.</summary>
    public void Follow(TextBox followed)
    {
        box = followed;
        followed.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler((_, _) => Stale()));
        followed.SizeChanged += (_, _) => Stale();
        Stale();
    }

    /// <summary>The text is this now.</summary>
    public void Show(string text)
    {
        lines = text.Split('\n');
        starts = new int[lines.Length];
        for (var line = 1; line < lines.Length; line++)
        {
            starts[line] = starts[line - 1] + lines[line - 1].Length + 1;
        }
        Stale();
    }

    /// <summary>The letters are to be made again: their size or their colours have changed.</summary>
    public void Remake()
    {
        kept.Clear();
        Stale();
    }

    /// <summary>What is drawn is no longer what is to be seen. It is drawn at the next laying out.</summary>
    public void Stale()
    {
        stale = true;
        InvalidateArrange();
    }

    /// <summary>
    /// Which stretches of a line are drawn as what: for a test to ask, since the
    /// colours are nowhere in the text.
    /// </summary>
    internal static (IReadOnlyList<ChordProEditor.Stretch> Chords, bool Greyed) ColoursOf(string line)
    {
        var found = ChordProEditor.Colouring(line);
        return (found.Chords, found.Greyed.Count > 0);
    }

    /// <summary>
    /// How wide a line's letters are as they are drawn here, chords bold: for a test to
    /// hold against how wide the box has the same line.
    /// </summary>
    internal double WidthOf(string line) => Letters(line).WidthIncludingTrailingWhitespace;

    private int LineAt(int place)
    {
        var found = Array.BinarySearch(starts, place);
        return found >= 0 ? found : Math.Max(~found - 1, 0);
    }

    // Where the box shows its text: inside its padding and its scroll bars.
    private Rect? Viewport(TextBox of)
    {
        if (Found<ScrollContentPresenter>(of) is not { } presenter || presenter.ActualWidth <= 0 || presenter.ActualHeight <= 0)
        {
            return null;
        }
        var corner = presenter.TranslatePoint(default, this);
        return new Rect(corner, new Size(presenter.ActualWidth, presenter.ActualHeight));
    }

    private static T? Found<T>(DependencyObject under)
        where T : DependencyObject
    {
        for (var child = 0; child < VisualTreeHelper.GetChildrenCount(under); child++)
        {
            var one = VisualTreeHelper.GetChild(under, child);
            if (one is T found)
            {
                return found;
            }
            if (Found<T>(one) is { } deeper)
            {
                return deeper;
            }
        }
        return null;
    }

    private void Draw()
    {
        stale = false;
        using var context = drawing.RenderOpen();
        // (Not by asking whether this is to be seen: what is drawn into a picture with
        // no window is, to WPF, never seen. A pane that is put away has no size.)
        if (box is null || Viewport(box) is not { } view)
        {
            return;
        }
        // The lines in view, by the box's own account of what is at its top and its
        // foot; and one more each way, for a line half seen.
        var inBox = TranslatePoint(view.TopLeft, box);
        var top = box.GetCharacterIndexFromPoint(new Point(inBox.X + 1, inBox.Y + 1), snapToText: true);
        var foot = box.GetCharacterIndexFromPoint(new Point(inBox.X + 1, inBox.Y + view.Height - 1), snapToText: true);
        if (top < 0 || foot < 0)
        {
            return;
        }
        var first = Math.Max(LineAt(Math.Min(top, foot)) - 1, 0);
        var last = Math.Min(LineAt(Math.Max(top, foot)) + 1, lines.Length - 1);

        context.PushClip(new RectangleGeometry(view));
        for (var line = first; line <= last; line++)
        {
            if (lines[line].Length == 0)
            {
                continue;
            }
            // (Words that have in them a character at which a text box starts a new
            // line, though it ends no line of the song's, are drawn on two lines here as
            // they are there: the letters are laid out by the same means, and what
            // follows such a character starts at the left again in both.)
            var at = box.GetRectFromCharacterIndex(starts[line]);
            if (!at.IsEmpty)
            {
                context.DrawText(Letters(lines[line]), box.TranslatePoint(at.TopLeft, this));
            }
        }
        context.Pop();
    }

    // A line's letters, made ready to draw: in the box's font and size, the chords in
    // their colour and bold. Kept, since most lines are the same from one drawing to
    // the next.
    private FormattedText Letters(string line)
    {
        if (kept.TryGetValue(line, out var ready))
        {
            return ready;
        }
        // (Which stretches are chords, and whether the line is a greyed one, in which
        // nothing is a chord, is the editor's to say.)
        var colours = ChordProEditor.Colouring(line);
        var greyed = colours.Greyed.Count > 0;
        if (kept.Count >= MostKept)
        {
            kept.Clear();
        }
        var of = box!;
        var words = WordsBrush ?? Brushes.Black;
        var made = new FormattedText(
            line,
            Culture(of),
            FlowDirection.LeftToRight,
            new Typeface(of.FontFamily, of.FontStyle, FontWeights.Normal, of.FontStretch),
            of.FontSize,
            greyed ? QuietBrush ?? words : words,
            null,
            TextOptions.GetTextFormattingMode(of),
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        foreach (var chord in colours.Chords)
        {
            made.SetForegroundBrush(ChordBrush ?? words, chord.Start, chord.End - chord.Start);
            made.SetFontWeight(FontWeights.Bold, chord.Start, chord.End - chord.Start);
        }
        kept[line] = made;
        return made;
    }

    // The language the box lays its text out in, which says what stands in for a
    // character its font has not.
    private static CultureInfo Culture(TextBox of)
    {
        try
        {
            return of.Language.GetSpecificCulture();
        }
        catch (InvalidOperationException)
        {
            return CultureInfo.InvariantCulture;
        }
    }
}
