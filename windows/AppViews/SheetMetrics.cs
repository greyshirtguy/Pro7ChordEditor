// How big things are on the chord sheet at one zoom, measured by WPF: the two fonts, the
// heights that go with them, each line's words as one line of text, and the widths of
// chords' names (spec/03-chord-sheet.md, rules 10 and 11; spec/10-platforms.md: "one
// custom-drawn element ... measuring with FormattedText or GlyphRun").
//
// THE SAME NUMBERS, FOR DRAWING AND FOR POINTING. Each line's words are one line of
// text as WPF's own layer for text lays one out (`TextLine`). That line is what is
// drawn, and how far along it a character is, is asked of that same line; so a tail
// stands on the character the pointer chose. A place is a UTF-16 unit, which is what
// that layer counts in too.
//
// A place along a line never goes backwards. The halving that finds the character under
// the pointer (ChordLayout.CharacterAt) counts on that. So where the layer says a unit
// starts left of one before it, it is not believed, and the unit is put where the next
// one that is believed starts. (SEEN on the Mac, of a zero-width space that ends a
// line, which real songs have. Not seen here; kept, since nothing is lost by it.)
//
// The Mac's is `SheetMetrics`, in ChordSheetView.swift, which asks Core Text.

using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Pro7ChordEditor.Song;

namespace Pro7ChordEditor.AppViews;

public sealed class SheetMetrics : SheetSizes
{
    private static readonly CultureInfo Language = CultureInfo.GetCultureInfo("en-US");
    private static readonly Brush WordsBrush = Frozen(SheetLook.Words);

    // What lays the lines out. It is kept as long as the lines are: a line that has
    // outlived what laid it out can be measured still, and cannot be drawn (FOUND: the
    // first drawing said so, and nothing else had).
    private readonly TextFormatter formatter = TextFormatter.Create(TextFormattingMode.Ideal);
    private readonly double dots;
    private readonly Typeface wordFace = new(SheetLook.Font, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
    private readonly Typeface boldFace = new(SheetLook.Font, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
    private readonly Dictionary<string, FormattedText> names = new(StringComparer.Ordinal);
    private readonly Dictionary<string, double> labels = new(StringComparer.Ordinal);
    private TextLine?[] lines = [];
    private double[]?[] edges = [];
    private Brush chordBrush = Frozen(SheetLook.ChordName);

    /// <param name="scale">How many times its usual size everything is.</param>
    /// <param name="dotsToAUnit">How many of the screen's dots there are to one of WPF's units: 1 on a plain screen.</param>
    public SheetMetrics(double scale, double dotsToAUnit = 1)
    {
        Scale = scale;
        dots = dotsToAUnit;
        WordHeight = HeightOf(SheetLook.WordSize * scale);
        ChordHeight = HeightOf(SheetLook.ChordSize * scale);
    }

    public override double Scale { get; }

    public override double WordHeight { get; }

    public override double ChordHeight { get; }

    /// <summary>How tall a line of the system's font is at a size: what its letters need over and under.</summary>
    public static double HeightOf(double size) => Math.Ceiling(SheetLook.Font.LineSpacing * size);

    /// <summary>The colour chords' names are drawn in.</summary>
    public Brush ChordBrush
    {
        get => chordBrush;
        set
        {
            chordBrush = value;
            names.Clear();
        }
    }

    /// <summary>
    /// The sheet's rows are these now: each line's words are laid out as one line of
    /// text when the row is first asked about.
    /// </summary>
    public void Hold(IReadOnlyList<SheetRow> rows)
    {
        foreach (var line in lines)
        {
            line?.Dispose();
        }
        lines = new TextLine?[rows.Count];
        edges = new double[]?[rows.Count];
        held = rows;
    }

    private IReadOnlyList<SheetRow> held = [];

    /// <summary>A row's words as a line of text, to draw; nothing for a row that is a group's name.</summary>
    public TextLine? Line(int row)
    {
        if (row < 0 || row >= held.Count || held[row].Kind != SheetRowKind.Line)
        {
            return null;
        }
        return lines[row] ??= LineOf(held[row].Text, wordFace, SheetLook.WordSize * Scale, WordsBrush);
    }

    public override double Advance(int row, int units)
    {
        if (Line(row) is not { } line)
        {
            return 0;
        }
        var known = edges[row] ??= EdgesOf(line, held[row].Text.Length);
        return known[Math.Min(Math.Max(units, 0), known.Length - 1)];
    }

    /// <summary>
    /// Where each unit of a line starts, and at the end where the line ends: never less
    /// than the one before.
    /// </summary>
    internal static double[] EdgesOf(TextLine line, int length)
    {
        var said = new double[length + 1];
        for (var index = 0; index < length; index++)
        {
            said[index] = line.GetDistanceFromCharacterHit(new CharacterHit(index, 0));
        }
        said[length] = line.WidthIncludingTrailingWhitespace;
        return Believed(said, line.WidthIncludingTrailingWhitespace);
    }

    /// <summary>
    /// Places along a line as they were said, with each that is left of one before it
    /// put where the next one that is believed starts; and the last, at the line's end.
    /// </summary>
    internal static double[] Believed(double[] said, double width)
    {
        var edges = (double[])said.Clone();
        var believed = new bool[edges.Length];
        var furthest = 0.0;
        for (var index = 0; index < edges.Length; index++)
        {
            if (edges[index] < furthest)
            {
                believed[index] = false;
            }
            else
            {
                believed[index] = true;
                furthest = edges[index];
            }
        }
        var next = Math.Max(furthest, width);
        for (var index = edges.Length - 1; index >= 0; index--)
        {
            if (believed[index])
            {
                next = edges[index];
            }
            else
            {
                edges[index] = next;
            }
        }
        return edges;
    }

    /// <summary>A chord's name, to draw, in the colour chords are drawn in.</summary>
    public FormattedText Name(string name)
    {
        if (!names.TryGetValue(name, out var made))
        {
            made = Text(name, boldFace, SheetLook.ChordSize * Scale, chordBrush);
            names[name] = made;
        }
        return made;
    }

    public override double NameWidth(string name) => Name(name).WidthIncludingTrailingWhitespace;

    public override double LabelWidth(string label)
    {
        if (!labels.TryGetValue(label, out var width))
        {
            width = Text(label, wordFace, SheetLook.LabelSize, Brushes.White).WidthIncludingTrailingWhitespace;
            labels[label] = width;
        }
        return width;
    }

    /// <summary>A few words to draw, in the sheet's font: plain, or bold.</summary>
    public FormattedText Text(string text, bool bold, double size, Brush brush) => Text(text, bold ? boldFace : wordFace, size, brush);

    private FormattedText Text(string text, Typeface face, double size, Brush brush) =>
        new(text, Language, FlowDirection.LeftToRight, face, size, brush, dots);

    // ---- A line of text, as WPF's layer for text lays one out

    private TextLine LineOf(string text, Typeface face, double size, Brush brush)
    {
        var run = new Run(face, size, brush, dots);
        // (A width of nought is no width to keep to: the line is as long as its words.)
        return formatter.FormatLine(new Source(text, run), 0, 0, new Paragraph(run), null);
    }

    private static SolidColorBrush Frozen(Color colour)
    {
        var brush = new SolidColorBrush(colour);
        brush.Freeze();
        return brush;
    }

    // The words of one line, handed to the layer as it asks for them.
    private sealed class Source(string text, TextRunProperties run) : TextSource
    {
        public override TextRun GetTextRun(int textSourceCharacterIndex) =>
            textSourceCharacterIndex < text.Length
                ? new TextCharacters(text, textSourceCharacterIndex, text.Length - textSourceCharacterIndex, run)
                : new TextEndOfParagraph(1);

        public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit) =>
            new(0, new CultureSpecificCharacterBufferRange(Language, CharacterBufferRange.Empty));

        public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex) => textSourceCharacterIndex;
    }

    // How the words of a line are written: one font, one size, one colour.
    private sealed class Run : TextRunProperties
    {
        private readonly Typeface face;
        private readonly double size;
        private readonly Brush brush;

        public Run(Typeface face, double size, Brush brush, double dots)
        {
            this.face = face;
            this.size = size;
            this.brush = brush;
            PixelsPerDip = dots;
        }

        public override Typeface Typeface => face;

        public override double FontRenderingEmSize => size;

        public override double FontHintingEmSize => size;

        public override TextDecorationCollection? TextDecorations => null;

        public override Brush ForegroundBrush => brush;

        public override Brush? BackgroundBrush => null;

        public override CultureInfo CultureInfo => Language;

        public override TextEffectCollection? TextEffects => null;
    }

    // How a line is set: from the left, never wrapped.
    private sealed class Paragraph(TextRunProperties run) : TextParagraphProperties
    {
        public override FlowDirection FlowDirection => FlowDirection.LeftToRight;

        public override TextAlignment TextAlignment => TextAlignment.Left;

        public override double LineHeight => 0;

        public override bool FirstLineInParagraph => true;

        public override TextRunProperties DefaultTextRunProperties => run;

        public override TextWrapping TextWrapping => TextWrapping.NoWrap;

        public override TextMarkerProperties? TextMarkerProperties => null;

        public override double Indent => 0;
    }
}
