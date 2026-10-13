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
// A LINE IS LAID OUT ONCE FOR ITS WORDS, and kept by what it says for as long as the
// song has a line that says it (spec/06, rule 6e). The sheet's rows are made again at
// every change to the song, a chord put on among them; the words of its lines are the
// same before and after, the words not being this app's to change, and are not laid
// out again. (MEASURED, 2026-10-14, on a song of 56 slides: with every line laid out
// again, and drawn afresh, a chord put on took a tenth of a second, two thirds of it
// that. windows/NOTES.md, "What the window costs".)
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

    // Each line of words as it was laid out, and where its units start, by what the
    // line says.
    private readonly Dictionary<string, (TextLine Line, double[]? Edges)> laidOut = new(StringComparer.Ordinal);
    private Brush chordBrush = Frozen(SheetLook.ChordName);

    // The one line that was last asked for in another colour than the words': the
    // highlighted line, of which there is one at a time.
    private (int Row, Brush Brush, TextLine Line)? inColour;

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

    /// <summary>How many times the lines have been thrown away to be laid out again: for a test to count.</summary>
    internal int Held { get; private set; }

    /// <summary>
    /// The sheet's rows are these now: each line's words are laid out as one line of
    /// text when the row is first asked about.
    ///
    /// Told the rows it has already, it does nothing: a line's words are measured once
    /// for a song, and not again each time its slides are put in rows for another width
    /// (spec/06, rule 6e. Until 2026-10-14 every line was measured again at each step
    /// of the window's edge being dragged, which is what was found slow on the Mac.)
    /// </summary>
    public void Hold(IReadOnlyList<SheetRow> rows)
    {
        if (ReferenceEquals(rows, held))
        {
            return;
        }
        Held++;
        // (What a line of the rows that were says is kept if a line of these says it
        // too, and let go otherwise.)
        var said = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            if (row.Kind == SheetRowKind.Line)
            {
                said.Add(row.Text);
            }
        }
        foreach (var gone in laidOut.Keys.Where(text => !said.Contains(text)).ToList())
        {
            laidOut[gone].Line.Dispose();
            laidOut.Remove(gone);
        }
        inColour?.Line.Dispose();
        inColour = null;
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
        return lines[row] ??= LaidOut(held[row].Text).Line;
    }

    /// <summary>How many times a line's words have been laid out: for a test to count.</summary>
    internal int LinesLaidOut { get; private set; }

    private (TextLine Line, double[]? Edges) LaidOut(string text)
    {
        if (!laidOut.TryGetValue(text, out var known))
        {
            LinesLaidOut++;
            known = (LineOf(text, wordFace, SheetLook.WordSize * Scale, WordsBrush), null);
            laidOut[text] = known;
        }
        return known;
    }

    /// <summary>
    /// A row's words as a line of text in another colour than the words' own: for the
    /// highlighted line. The same words in the same font, so every letter is where it
    /// is in the usual colour.
    /// </summary>
    public TextLine? LineIn(int row, Brush brush)
    {
        if (row < 0 || row >= held.Count || held[row].Kind != SheetRowKind.Line)
        {
            return null;
        }
        if (inColour is { } has && has.Row == row && ReferenceEquals(has.Brush, brush))
        {
            return has.Line;
        }
        inColour?.Line.Dispose();
        var line = LineOf(held[row].Text, wordFace, SheetLook.WordSize * Scale, brush);
        inColour = (row, brush, line);
        return line;
    }

    public override double Advance(int row, int units)
    {
        if (Line(row) is not { } line)
        {
            return 0;
        }
        var known = edges[row] ??= EdgesKept(line, held[row].Text);
        return known[Math.Min(Math.Max(units, 0), known.Length - 1)];
    }

    // Where each unit of a line starts, kept with the line by what it says.
    private double[] EdgesKept(TextLine line, string text)
    {
        var known = LaidOut(text);
        if (known.Edges is { } kept)
        {
            return kept;
        }
        var found = EdgesOf(line, text.Length);
        laidOut[text] = (known.Line, found);
        return found;
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
