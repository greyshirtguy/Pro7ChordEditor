// The chord sheet's slides laid out as a grid, and what is where in it: with no drawing
// in it, and nothing measured here (spec/03-chord-sheet.md, rules 7, 8, 16, 18 and 20).
//
// How it is laid out. The slides are a grid, as the slides of a presentation are in
// ProPresenter's own window, but each is its words at a size to be read, with room over
// every line for its chords. A slide is a card in a frame of its group's colour, with
// the slide's number on the frame's foot and, on the first slide of each group, the
// group's name. As many go to a row as fit across, so a wider window, or smaller words
// (the sheet zooms), shows more of the song at once. Every card of a song is one width,
// and no wider than the widest of its slides needs: no room is kept for what might be
// put on a line later. That is the maintainer's, on trying the reference's one column
// of slides with real songs.
//
// It is apart from the view (ChordSheetCanvas.cs), which draws what this says and asks
// it what is under the pointer, so that a test can hold every number of it with letters
// of a fixed width and no window. What things measure is handed in (`SheetSizes`): the
// view hands in WPF's (SheetMetrics.cs).
//
// The Mac's is in ChordSheetView.swift: `layOut`, `line(at:)`, `place(at:shift:)`,
// `point(over:)`, `lineBeside`. The numbers are its numbers.

using System.Globalization;
using System.Windows;
using Pro7ChordEditor.Song;
using Colour = Pro7ChordEditor.Song.Colour;

namespace Pro7ChordEditor.AppViews;

/// <summary>
/// How big things are at one zoom: what the sheet's model asks (how wide words and
/// bubbles are), the heights that go with them, and the few sizes that follow from
/// those (spec/03, rule 10).
/// </summary>
public abstract class SheetSizes : ISheetMeasure
{
    /// <summary>How many times its usual size everything is.</summary>
    public abstract double Scale { get; }

    /// <summary>How tall a line of the words is, and a chord's name.</summary>
    public abstract double WordHeight { get; }

    public abstract double ChordHeight { get; }

    public abstract double Advance(int row, int units);

    /// <summary>How wide a chord's name is, with nothing either side of it.</summary>
    public abstract double NameWidth(string name);

    /// <summary>How wide the words on a frame's foot are (they do not zoom).</summary>
    public abstract double LabelWidth(string label);

    /// <summary>Either side of a chord's name in its bubble.</summary>
    public double BubblePad => 7 * Scale;

    /// <summary>The tail that comes down from a bubble to its character.</summary>
    public double Tail => 6 * Scale;

    /// <summary>The room inside a card round its lines.</summary>
    public double CardPad => 10 * Scale;

    public double BubbleHeight => ChordHeight + (6 * Scale);

    /// <summary>A line of words with its row of chords over it.</summary>
    public double LineHeight => BubbleHeight + Tail + WordHeight + (6 * Scale);

    public double Bubble(string name) => NameWidth(name) + (2 * BubblePad);
}

public sealed class SheetGrid
{
    private readonly ChordSheet sheet;
    private readonly SheetSizes sizes;

    private SheetGrid(ChordSheet sheet, SheetSizes sizes, IReadOnlyList<SheetRow> rows, List<Cell> cells, Laid[] lines, int columns, Size content, double seenWidth)
    {
        this.sheet = sheet;
        this.sizes = sizes;
        Rows = rows;
        Cells = cells;
        Lines = lines;
        Columns = columns;
        Content = content;
        SeenWidth = seenWidth;
    }

    /// <summary>
    /// One slide as it is laid out: its frame in its group's colour, the card inside
    /// that where its lines are, what its foot says, and where it is in the grid.
    /// </summary>
    /// <param name="FirstRow">The first of the sheet's rows that are this slide's lines, and how many they are.</param>
    /// <param name="LinesTop">Where the first of its lines starts, down the sheet.</param>
    /// <param name="Colour">Its group's colour; nothing for a group that has none.</param>
    public sealed record Cell(int Slide, int FirstRow, int Count, Rect Frame, Rect Card, double LinesTop, Colour? Colour, string Label, int GridRow, int Column)
    {
        public bool Has(int row) => row >= FirstRow && row < FirstRow + Count;
    }

    /// <summary>
    /// One line as it is laid out: which slide's card it is in, where its words start,
    /// where its row of chords is, and where its chords stand along it. A row that is a
    /// group's name is in no card.
    /// </summary>
    public readonly record struct Laid(int Cell, double Left, double ChordTop, ChordLayout.Placed? Placed);

    /// <summary>The sheet's rows as they were when this was laid out.</summary>
    public IReadOnlyList<SheetRow> Rows { get; }

    public IReadOnlyList<Cell> Cells { get; }

    public IReadOnlyList<Laid> Lines { get; }

    /// <summary>How many slides there are to a row of the grid.</summary>
    public int Columns { get; }

    /// <summary>How big the whole sheet is.</summary>
    public Size Content { get; }

    /// <summary>How wide the sheet was seen to be when it was laid out.</summary>
    public double SeenWidth { get; }

    /// <summary>The slide's number, and on the first slide of each group that has a name, the group's name.</summary>
    public static string LabelOf(SheetRow row, TextBox box) =>
        (row.Slide + 1).ToString(CultureInfo.InvariantCulture) + (box.StartsItsGroup && box.Group.Length > 0 ? "  " + box.Group : "");

    /// <summary>
    /// What one slide needs across, frame and all: the widest of its lines with their
    /// chords and the room inside the card, or what the words on its foot need if that is
    /// more. Nothing is added for what might be put there later: a line that comes to
    /// need more, for a chord at its end, is given it then.
    /// </summary>
    public static double WidthNeeded(double lines, string label, SheetSizes sizes) =>
        Math.Max(Math.Max(lines, 40 * sizes.Scale) + (2 * sizes.CardPad) + (2 * SheetLook.Frame), sizes.LabelWidth(label) + 16);

    /// <summary>
    /// Lays the slides out as a grid: in the order of the song, as many to a row as fit
    /// across. Every card is one width, what the widest slide of the song needs, and the
    /// shape of a screen, 16 by 9 (though never taller for that than four lines need); a
    /// row with a slide of more lines than that holds is as tall as that slide needs. A
    /// slide's lines stand in the middle of its card, up and down.
    /// </summary>
    public static SheetGrid LayOut(ChordSheet sheet, SheetSizes sizes, double seenWidth)
    {
        var rows = sheet.Rows;
        var lines = new Laid[rows.Count];
        Array.Fill(lines, new Laid(-1, 0, 0, null));
        // Each slide: its first line's row, how many lines it has, what its foot says,
        // and how wide a cell of the grid it needs.
        var slides = new List<(int First, int Count, string Label, double Needs)>();
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            if (row.Kind != SheetRowKind.Line || !row.First)
            {
                continue;
            }
            var widest = 0.0;
            for (var line = index; line < index + row.Count; line++)
            {
                var placed = sheet.Placed(line, sizes);
                lines[line] = lines[line] with { Placed = placed };
                // (A line of chords alone has a place past its last chord, for the next:
                // that place needs room to be pointed at.)
                widest = Math.Max(widest, placed.Width + (rows[line].Alone ? 44 * sizes.Scale : 0));
            }
            var label = LabelOf(row, sheet.Blocks[row.Block]);
            slides.Add((index, row.Count, label, WidthNeeded(widest, label, sizes) + (2 * SheetLook.Around)));
        }
        var pad = sizes.CardPad;
        const double around = SheetLook.Around;

        // Every slide is as wide as the widest of them needs, and as many go to a row as
        // fit across at that width. What is left of the sheet is at its right.
        var across = seenWidth - (2 * SheetLook.Margin);
        var width = slides.Count == 0 ? 0 : slides.Max(slide => slide.Needs);
        var columns = Math.Max(1, Math.Min(slides.Count, (int)(across / Math.Max(width, 1))));
        var cardWidth = width - (2 * around) - (2 * SheetLook.Frame);
        var shaped = Math.Min(cardWidth * SheetLook.Shape, (SheetLook.ShapedUpTo * sizes.LineHeight) + (2 * pad));

        var made = new List<Cell>();
        var top = SheetLook.Top;
        var gridRow = 0;
        for (var from = 0; from < slides.Count; from += columns)
        {
            var these = slides.GetRange(from, Math.Min(columns, slides.Count - from));
            // The shape of a screen; or taller, if the row's lines need more than that.
            var needed = (these.Max(slide => slide.Count) * sizes.LineHeight) + (2 * pad);
            var cardHeight = Math.Max(needed, shaped);
            for (var column = 0; column < these.Count; column++)
            {
                var slide = these[column];
                var frame = new Rect(SheetLook.Margin + (column * width) + around, top + around, width - (2 * around), SheetLook.Frame + cardHeight + SheetLook.Foot);
                var card = new Rect(frame.X + SheetLook.Frame, frame.Y + SheetLook.Frame, cardWidth, cardHeight);
                // The slide's lines, in the middle of its card up and down.
                var linesTop = card.Y + ((cardHeight - (slide.Count * sizes.LineHeight)) / 2);
                for (var line = 0; line < slide.Count; line++)
                {
                    lines[slide.First + line] = lines[slide.First + line] with
                    {
                        Cell = made.Count,
                        Left = card.X + pad,
                        ChordTop = linesTop + (line * sizes.LineHeight) + (2 * sizes.Scale),
                    };
                }
                made.Add(new Cell(rows[slide.First].Slide, slide.First, slide.Count, frame, card, linesTop, sheet.Blocks[rows[slide.First].Block].GroupColour, slide.Label, gridRow, column));
            }
            top += (2 * around) + SheetLook.Frame + cardHeight + SheetLook.Foot;
            gridRow++;
        }
        var content = new Size(SheetLook.Margin + (columns * width) + SheetLook.Margin, top + SheetLook.Margin + 40);
        return new SheetGrid(sheet, sizes, rows, made, lines, columns, content, seenWidth);
    }

    /// <summary>Whether a row of the sheet is a line that was laid out in a card.</summary>
    public bool IsLaid(int row) => row >= 0 && row < Lines.Count && Lines[row].Cell >= 0 && Lines[row].Cell < Cells.Count;

    // ---- What is where

    /// <summary>
    /// The line of a card that a point is on. Every part of a card is one of its lines:
    /// over the first of them, the first, and under the last, the last. Off the cards
    /// there is none: a frame and its foot are bare sheet.
    /// </summary>
    public int? LineAt(Point point)
    {
        foreach (var cell in Cells)
        {
            if (cell.Card.Contains(point))
            {
                var down = (int)Math.Floor((point.Y - cell.LinesTop) / sizes.LineHeight);
                return cell.FirstRow + Math.Min(Math.Max(down, 0), cell.Count - 1);
            }
        }
        return null;
    }

    /// <summary>
    /// The place of a line under a point: a chord's, if the point is on its bubble;
    /// otherwise the character there, or with Shift the word. Nothing off the cards, or
    /// on a line that can take no chord.
    /// </summary>
    public (int Row, int At, bool OnChord)? PlaceAt(Point point, bool shift)
    {
        if (LineAt(point) is not { } index || !sheet.Usable(index) || Lines[index].Placed is not { } placed)
        {
            return null;
        }
        var item = Lines[index];
        var along = point.X - item.Left;
        if (point.Y >= item.ChordTop && point.Y < item.ChordTop + sizes.BubbleHeight + sizes.Tail)
        {
            var chords = sheet.Rows[index].Chords;
            for (var n = 0; n < chords.Count && n < placed.Xs.Count; n++)
            {
                if (along >= placed.Xs[n] && along < placed.Xs[n] + placed.Widths[n])
                {
                    return (index, chords[n].At, true);
                }
            }
        }
        return (index, sheet.PlaceAt(index, along, exact: !shift, sizes), false);
    }

    /// <summary>
    /// The same for a chord that is being carried: the line under the pointer, and never
    /// another chord's bubble for itself.
    /// </summary>
    public (int Row, int At)? PlaceCarriedTo(Point point, bool shift) =>
        LineAt(point) is { } index && sheet.Usable(index) ? (index, sheet.PlaceAt(index, point.X - Lines[index].Left, exact: !shift, sizes)) : null;

    /// <summary>
    /// A point of the sheet that is over a place of a line: in the middle of its
    /// character, down among the words; or, <paramref name="onItsChord"/>, in the bubble
    /// of the chord that stands there. (What the tests point at.)
    /// </summary>
    public Point? PointOver(Spot spot, bool onItsChord = false)
    {
        if (!IsLaid(spot.Row) || !sheet.Usable(spot.Row) || Lines[spot.Row].Placed is not { } placed)
        {
            return null;
        }
        var item = Lines[spot.Row];
        var row = sheet.Rows[spot.Row];
        if (onItsChord)
        {
            for (var n = 0; n < row.Chords.Count; n++)
            {
                if (row.Chords[n].At == spot.At)
                {
                    return new Point(item.Left + placed.Xs[n] + (placed.Widths[n] / 2), item.ChordTop + (sizes.BubbleHeight / 2));
                }
            }
            return null;
        }
        var wordsMiddle = item.ChordTop + sizes.BubbleHeight + sizes.Tail + (sizes.WordHeight / 2);
        if (row.Alone)
        {
            return new Point(item.Left + sheet.XOf(spot.Row, spot.At, sizes) + 2, wordsMiddle);
        }
        var from = sizes.Advance(spot.Row, spot.At);
        var to = sizes.Advance(spot.Row, Math.Min(spot.At + 1, row.Text.Length));
        return new Point(item.Left + ((from + to) / 2), wordsMiddle);
    }

    /// <summary>Where a place of a line is along the sheet: its left edge.</summary>
    public double XOf(Spot spot) => Lines[spot.Row].Left + sheet.XOf(spot.Row, spot.At, sizes);

    /// <summary>
    /// The line over or under a line ON THE SCREEN, that can take the spot: the next of
    /// its own slide; or from a slide's last line the first line of the slide under it in
    /// the grid, and from its first the last line of the slide over it. The slide under
    /// or over is the one in the same column of the next row of slides, or that row's
    /// last where it is a short one.
    /// </summary>
    public int? LineBeside(int row, int delta)
    {
        if (!IsLaid(row))
        {
            return null;
        }
        var here = Cells[Lines[row].Cell];
        for (var next = row + delta; here.Has(next); next += delta)
        {
            if (sheet.Usable(next))
            {
                return next;
            }
        }
        for (var gridRow = here.GridRow + delta; ; gridRow += delta)
        {
            var inRow = Cells.Where(cell => cell.GridRow == gridRow).ToList();
            if (inRow.Count == 0)
            {
                return null;
            }
            var there = inRow.FirstOrDefault(cell => cell.Column == here.Column) ?? inRow[^1];
            var lines = Enumerable.Range(there.FirstRow, there.Count);
            foreach (var line in delta > 0 ? lines : lines.Reverse())
            {
                if (sheet.Usable(line))
                {
                    return line;
                }
            }
        }
    }

    /// <summary>
    /// Down and Up: the spot goes to the line under or over it on the screen, to the stop
    /// of it that is nearest under or over where it was. With no spot yet, the first
    /// arrow key puts it on the first word of the song.
    /// </summary>
    public void StepLine(int delta)
    {
        if (!sheet.Usable(sheet.Spot.Row))
        {
            sheet.Step(1, exact: false);
            return;
        }
        if (LineBeside(sheet.Spot.Row, delta) is not { } next)
        {
            return;
        }
        var x = XOf(sheet.Spot);
        var best = 0;
        var distance = double.PositiveInfinity;
        foreach (var stop in sheet.Stops(next))
        {
            var off = Math.Abs(XOf(new Spot(next, stop)) - x);
            if (off < distance)
            {
                distance = off;
                best = stop;
            }
        }
        sheet.SetSpot(next, best);
        sheet.RevealSpot();
    }
}
