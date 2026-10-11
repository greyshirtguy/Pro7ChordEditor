// The chord sheet's slides as a grid, number for number (SheetGrid.cs;
// spec/03-chord-sheet.md, rules 7, 8, 16, 18 and 20).
//
// No window, and nothing measured by the system: every character here is 10 wide, a
// chord's name is 8 a letter, a line of words is 28 tall and a chord's name 18. So a
// line with its row of chords is 64 tall (the name's 18 and 6, a tail of 6, the words'
// 28 and 6), a bubble is its name and 14, and every number below can be worked out on
// paper. The song is a stand-in that only holds its text boxes.
//
// Where a line's chords stand along it is the sheet's model's, held to layout.json
// (Song.Tests). What is held here is the grid's own: how wide and tall the cards are,
// how many go to a row, where each line is in its card, and what is under a point.

using System.Windows;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Song;
using Xunit;
using TextBox = Pro7ChordEditor.Song.TextBox;

namespace Pro7ChordEditor.AppViews.Tests;

public class SheetGridTests
{
    // Every character 10 wide, at the usual size.
    private sealed class Sizes(double scale = 1) : SheetSizes
    {
        public override double Scale => scale;

        public override double WordHeight => 28 * scale;

        public override double ChordHeight => 18 * scale;

        public override double Advance(int row, int units) => units * 10 * scale;

        public override double NameWidth(string name) => name.Length * 8 * scale;

        // (The words on a frame's foot do not zoom.)
        public override double LabelWidth(string label) => label.Length * 7;
    }

    // A song that is its text boxes and nothing else.
    private sealed class Song(params TextBox[] boxes) : ISheetSong
    {
        private readonly List<TextBox> boxes = [.. boxes];

        public IReadOnlyList<TextBox> TextBoxes => [.. boxes];

        public IReadOnlyList<string> UsedChords => [.. boxes.SelectMany(box => box.Chords.Select(chord => chord.Name))];

        public string Key => "G";

        public void SetChords(int slide, string element, IReadOnlyList<Chord> chords, bool joined)
        {
            var index = boxes.FindIndex(box => box.Slide == slide && box.Element == element);
            boxes[index] = boxes[index] with { Chords = [.. chords] };
        }

        public void SetChordsAlone(int slide, string element, int line, IReadOnlyList<string> names, bool joined) =>
            throw new NotSupportedException("no test here changes a line of chords alone");
    }

    private static readonly Colour Blue = new(0.1, 0.4, 0.9);

    // What a line of two chords alone has for words: a stand-in for each chord, which
    // has no width, and a wide space between them (spec/01, "Chords with no words").
    private static readonly string StandIns = new([(char)0x200B, (char)0x2001, (char)0x200B]);

    // A slide's one text box: its words, and its chords as places and names.
    private static TextBox Slide(int slide, string words, string group = "", bool starts = false, Colour? colour = null, params (int At, string Name)[] chords) =>
        new(slide, group, colour, starts, "box" + slide, words, [.. chords.Select(chord => new Chord(chord.At, chord.Name))], words.Length == 0 && chords.Length == 0);

    // Five slides of two lines, the widest of them 13 characters; in two groups.
    private static ChordSheet FiveSlides() => new(new Song(
        Slide(0, "One two three\nFour five", "Verse 1", starts: true, Blue, (0, "G"), (8, "D")),
        Slide(1, "Six seven\nEight nine", "Verse 1", chords: (4, "Em")),
        Slide(2, "Ten eleven\nTwelve", "Chorus", starts: true),
        Slide(3, "Thirteen\nFourteen", "Chorus"),
        Slide(4, "Fifteen\nSixteen")));

    // The sheet's rows that are lines, in order: a group's name is a row of the sheet
    // too, over its first slide, and is in no card.
    private static int[] LinesOf(ChordSheet sheet) =>
        [.. Enumerable.Range(0, sheet.Rows.Count).Where(row => sheet.Rows[row].Kind == SheetRowKind.Line)];

    private static void Close(double expected, double actual) => Assert.Equal(expected, actual, 0.0001);

    private static void Close(Rect expected, Rect actual)
    {
        Assert.True(
            Math.Abs(expected.X - actual.X) < 0.0001 && Math.Abs(expected.Y - actual.Y) < 0.0001 && Math.Abs(expected.Width - actual.Width) < 0.0001 && Math.Abs(expected.Height - actual.Height) < 0.0001,
            "expected " + expected + " and it is " + actual);
    }

    // ---- How it is laid out

    [Fact]
    public void TheSlidesAreAGridOfCardsOfOneWidthInTheOrderOfTheSongAsManyToARowAsFit()
    {
        var sheet = FiveSlides();
        var sizes = new Sizes();
        Assert.Equal(64, sizes.LineHeight);
        Assert.Equal((24.0, 6.0, 10.0, 7.0), (sizes.BubbleHeight, sizes.Tail, sizes.CardPad, sizes.BubblePad));
        Assert.Equal(22, sizes.Bubble("G"));

        // The widest line is 130. A card is that and 10 each side: 150. Its frame is 4
        // each side more: 158. And a frame keeps 6 clear all round it: a cell of 170.
        // Seen 600 wide, less the sheet's margin of 6 each side, three go to a row.
        var grid = SheetGrid.LayOut(sheet, sizes, 600);
        Assert.Equal(3, grid.Columns);
        Assert.Equal(5, grid.Cells.Count);
        Assert.Equal([0, 1, 2, 3, 4], grid.Cells.Select(cell => cell.Slide));
        Assert.Equal([(0, 0), (0, 1), (0, 2), (1, 0), (1, 1)], grid.Cells.Select(cell => (cell.GridRow, cell.Column)));
        Assert.All(grid.Cells, cell => Close(158, cell.Frame.Width));
        Assert.All(grid.Cells, cell => Close(150, cell.Card.Width));

        // Two lines need 128, and 10 over and under: 148. The shape of a screen would be
        // 150 by 84, which is less: the card is as tall as its lines need. A frame is
        // its card, 4 over it, and a foot of 22 under.
        Assert.All(grid.Cells, cell => Close(148, cell.Card.Height));
        Close(new Rect(12, 36, 158, 174), grid.Cells[0].Frame);
        Close(new Rect(16, 40, 150, 148), grid.Cells[0].Card);
        Close(new Rect(182, 36, 158, 174), grid.Cells[1].Frame);
        Close(new Rect(352, 36, 158, 174), grid.Cells[2].Frame);
        // The second row of slides is under the first: its frame, and 6 clear each way.
        Close(new Rect(12, 222, 158, 174), grid.Cells[3].Frame);
        Close(new Rect(182, 222, 158, 174), grid.Cells[4].Frame);
        // The whole sheet: three cells and its margins across; and down, the two rows
        // from 30, its margin, and 40 to spare under the last.
        Close(522, grid.Content.Width);
        Close(30 + 186 + 186 + 6 + 40, grid.Content.Height);
        Assert.Equal(600, grid.SeenWidth);

        // The sheet has a row for each line, and one for each group's name: two groups
        // are named here. A group's name is laid out in no card.
        var lines = LinesOf(sheet);
        Assert.Equal([1, 2, 3, 4, 6, 7, 8, 9, 10, 11], lines);
        Assert.Equal(12, grid.Lines.Count);
        Assert.Equal([-1, 0, 0, 1, 1, -1, 2, 2, 3, 3, 4, 4], grid.Lines.Select(line => line.Cell));
        Assert.All(lines, row => Assert.True(grid.IsLaid(row)));
        Assert.False(grid.IsLaid(0));
        Assert.False(grid.IsLaid(5));
        Assert.False(grid.IsLaid(12));
        Assert.False(grid.IsLaid(-1));
        Assert.Equal([(1, 2), (3, 2), (6, 2), (8, 2), (10, 2)], grid.Cells.Select(cell => (cell.FirstRow, cell.Count)));
        // Each line is in its slide's card: 10 in from the card's edge, and the lines
        // in the middle of the card up and down (here they fill it, less the 10).
        Close(50, grid.Cells[0].LinesTop);
        Assert.Equal((26.0, 52.0), (grid.Lines[lines[0]].Left, grid.Lines[lines[0]].ChordTop));
        Assert.Equal((26.0, 116.0), (grid.Lines[lines[1]].Left, grid.Lines[lines[1]].ChordTop));
        Assert.Equal((196.0, 52.0), (grid.Lines[lines[2]].Left, grid.Lines[lines[2]].ChordTop));
        Assert.Equal((26.0, 238.0), (grid.Lines[lines[6]].Left, grid.Lines[lines[6]].ChordTop));
        // And its chords stand where the sheet's model puts them along it.
        Assert.Equal(sheet.Placed(lines[0], sizes).Xs, grid.Lines[lines[0]].Placed!.Xs);
        Assert.Equal([0.0, 80.0], grid.Lines[lines[0]].Placed!.Xs);
    }

    [Fact]
    public void FewerGoToARowWhereTheSheetIsNarrowerAndMoreWhereTheWordsAreSmaller()
    {
        var sheet = FiveSlides();
        // A cell is 170: 182 holds one and its margins, 352 two, 522 three, and so on.
        Assert.Equal(1, SheetGrid.LayOut(sheet, new Sizes(), 100).Columns);
        Assert.Equal(1, SheetGrid.LayOut(sheet, new Sizes(), 351).Columns);
        Assert.Equal(2, SheetGrid.LayOut(sheet, new Sizes(), 352).Columns);
        Assert.Equal(2, SheetGrid.LayOut(sheet, new Sizes(), 521).Columns);
        Assert.Equal(3, SheetGrid.LayOut(sheet, new Sizes(), 522).Columns);
        Assert.Equal(4, SheetGrid.LayOut(sheet, new Sizes(), 692).Columns);
        // Never more to a row than there are slides.
        Assert.Equal(5, SheetGrid.LayOut(sheet, new Sizes(), 5000).Columns);
        // One to a row: the slides one under another, and the sheet as wide as one.
        var one = SheetGrid.LayOut(sheet, new Sizes(), 300);
        Assert.Equal([0, 1, 2, 3, 4], one.Cells.Select(cell => cell.GridRow));
        Assert.All(one.Cells, cell => Assert.Equal(0, cell.Column));
        Close(182, one.Content.Width);

        // At half the size a line is 65 wide and a card would be 75, in a frame of 83
        // (the frame does not zoom). But the words on the first frame's foot, "1  Verse
        // 1", are 70 and 8 each side at any size: every frame is 86, and its cell 98.
        // Seen 522 wide, five go to a row where three did.
        var small = SheetGrid.LayOut(sheet, new Sizes(0.5), 522);
        Assert.Equal(5, small.Columns);
        Assert.All(small.Cells, cell => Close(86, cell.Frame.Width));
        Assert.All(small.Cells, cell => Close(78, cell.Card.Width));
        // Two lines of 32 and 5 over and under: 74. The foot is 22 at any size.
        Assert.All(small.Cells, cell => Close(74, cell.Card.Height));
        Assert.All(small.Cells, cell => Close(4 + 74 + 22, cell.Frame.Height));
        // At half as large again a cell is 245, and two go to a row.
        var large = SheetGrid.LayOut(sheet, new Sizes(1.5), 522);
        Assert.Equal(2, large.Columns);
        Assert.All(large.Cells, cell => Close(195 + 30 + 8, cell.Frame.Width));
    }

    [Fact]
    public void ACardIsTheShapeOfAScreenButNeverTallerForThatThanFourLinesNeed()
    {
        // One line of 40 characters: 400 wide, a card of 420. The shape of a screen,
        // 9 to 16, is 236.25 tall; one line needs only 84. The card is the shape.
        var wide = SheetGrid.LayOut(new ChordSheet(new Song(Slide(0, new string('w', 40)))), new Sizes(), 2000);
        Close(420, wide.Cells[0].Card.Width);
        Close(236.25, wide.Cells[0].Card.Height);
        // Its one line is in the middle of the card, up and down.
        Close(wide.Cells[0].Card.Y + ((236.25 - 64) / 2), wide.Cells[0].LinesTop);
        Close(wide.Cells[0].LinesTop + 2, wide.Lines[0].ChordTop);

        // One line of 80 characters: a card of 820, whose shape would be 461.25 tall.
        // Four lines need 276, and it is no taller than that.
        var wider = SheetGrid.LayOut(new ChordSheet(new Song(Slide(0, new string('w', 80)))), new Sizes(), 2000);
        Close(820, wider.Cells[0].Card.Width);
        Close(276, wider.Cells[0].Card.Height);
    }

    [Fact]
    public void ARowOfSlidesWithOneOfMoreLinesThanTheShapeHoldsIsAsTallAsThatSlideNeeds()
    {
        // Cards of 150, which two lines fill. The second slide has five: 320, and 20.
        var sheet = new ChordSheet(new Song(
            Slide(0, "One two three\nFour"),
            Slide(1, "a\nb\nc\nd\ne"),
            Slide(2, "Five"),
            Slide(3, "Six")));
        var grid = SheetGrid.LayOut(sheet, new Sizes(), 522);
        Assert.Equal(3, grid.Columns);
        // The whole of its row of slides is that tall, and each slide's lines are in
        // the middle of its card.
        Assert.All(grid.Cells.Take(3), cell => Close(340, cell.Card.Height));
        Close(40 + ((340 - 128) / 2.0), grid.Cells[0].LinesTop);
        Close(50, grid.Cells[1].LinesTop);
        Close(40 + ((340 - 64) / 2.0), grid.Cells[2].LinesTop);
        // The next row is under it, and is its own height: the shape of a screen,
        // 84.375, which is more than its one line's 84.
        Close(36 + 4 + 340 + 22 + 12, grid.Cells[3].Frame.Y);
        Close(84.375, grid.Cells[3].Card.Height);
    }

    [Fact]
    public void WhatASlideNeedsAcross()
    {
        var sizes = new Sizes();
        // Its widest line, 10 each side inside the card, and the frame's 4 each side.
        Close(158, SheetGrid.WidthNeeded(130, "1", sizes));
        // Never less than 40 of line: a slide of one short word, and a blank one.
        Close(68, SheetGrid.WidthNeeded(20, "1", sizes));
        Close(68, SheetGrid.WidthNeeded(0, "1", sizes));
        // What its foot says, and 8 each side of that, if that is more.
        Close((7 * 30) + 16, SheetGrid.WidthNeeded(130, new string('n', 30), sizes));
        // At half the size the line and the card's room are halved, the frame not.
        Close(65 + 10 + 8, SheetGrid.WidthNeeded(65, "1", new Sizes(0.5)));
        Close(20 + 10 + 8, SheetGrid.WidthNeeded(5, "1", new Sizes(0.5)));

        // A line of chords alone has a place past its last chord, for the next: 44 more
        // than its chords take, so that the place can be pointed at.
        var alone = new ChordSheet(new Song(new TextBox(0, "Intro", null, true, "box0", StandIns, [new Chord(0, "G"), new Chord(2, "D")], false)));
        Assert.True(alone.Rows.Single(row => row.Kind == SheetRowKind.Line).Alone);
        var chords = alone.Placed(LinesOf(alone)[0], sizes).Width;
        Assert.True(chords >= 44, "two chords side by side take " + chords);
        Close(chords + 44 + 20 + 8, SheetGrid.LayOut(alone, sizes, 2000).Cells[0].Frame.Width);
        // At half the size that room is 22, and the card's 5 each side; the frame's 4 is
        // as it was.
        // (Of a slide in no group, whose foot says only "1": the words on a foot do not
        // zoom, and "1  Intro" would be the wider.)
        var small = new Sizes(0.5);
        var inNoGroup = new ChordSheet(new Song(new TextBox(0, "", null, false, "box0", StandIns, [new Chord(0, "G"), new Chord(2, "D")], false)));
        Close(inNoGroup.Placed(0, small).Width + 22 + 10 + 8, SheetGrid.LayOut(inNoGroup, small, 2000).Cells[0].Frame.Width);
        // And a blank slide, which has none yet: the 44, and the card's and the frame's.
        var blank = new ChordSheet(new Song(Slide(0, "")));
        Assert.True(blank.Rows.Single(row => row.Kind == SheetRowKind.Line).Blank);
        Close(44 + 20 + 8, SheetGrid.LayOut(blank, sizes, 2000).Cells[0].Frame.Width);
        // A line that comes to need more, for a chord at its end, is given it then: a
        // long name on the last word of the widest line.
        var sheet = FiveSlides();
        Close(158, SheetGrid.LayOut(sheet, sizes, 600).Cells[0].Frame.Width);
        Assert.True(sheet.Put(LinesOf(sheet)[0], 8, "F#m7b5/C#"));
        // The bubble is 72 and 14, from 80 along: 6 past the words' end.
        Close(158 + 36, SheetGrid.LayOut(sheet, sizes, 600).Cells[0].Frame.Width);
        Assert.All(SheetGrid.LayOut(sheet, sizes, 600).Cells, cell => Close(194, cell.Frame.Width));
    }

    [Fact]
    public void AFrameSaysItsSlidesNumberAndOnTheFirstSlideOfAGroupTheGroupsNameAndIsTheGroupsColour()
    {
        var grid = SheetGrid.LayOut(FiveSlides(), new Sizes(), 600);
        Assert.Equal(["1  Verse 1", "2", "3  Chorus", "4", "5"], grid.Cells.Select(cell => cell.Label));
        Assert.Equal(Blue, grid.Cells[0].Colour);
        Assert.Null(grid.Cells[1].Colour);
        Assert.Null(grid.Cells[4].Colour);
        // A group with no name has no name to say, though it starts.
        var box = Slide(6, "Words", "", starts: true);
        Assert.Equal("7", SheetGrid.LabelOf(new SheetRow(SheetRowKind.Line, 6), box));
        Assert.Equal("7  Bridge", SheetGrid.LabelOf(new SheetRow(SheetRowKind.Line, 6), box with { Group = "Bridge" }));
        Assert.Equal("7", SheetGrid.LabelOf(new SheetRow(SheetRowKind.Line, 6), box with { Group = "Bridge", StartsItsGroup = false }));
    }

    // ---- What is where

    [Fact]
    public void EveryPartOfACardIsOneOfItsLinesAndOffTheCardsThereIsNone()
    {
        var sheet = FiveSlides();
        var grid = SheetGrid.LayOut(sheet, new Sizes(), 600);
        var card = grid.Cells[0].Card;
        // The card's lines start at 50 and are 64 each. Over the first of them is the
        // first, and under the last the last.
        var lines = LinesOf(sheet);
        Assert.Equal(lines[0], grid.LineAt(new Point(card.X + 1, card.Y + 1)));
        Assert.Equal(lines[0], grid.LineAt(new Point(card.X + 60, 50 + 63)));
        Assert.Equal(lines[1], grid.LineAt(new Point(card.X + 60, 50 + 64)));
        Assert.Equal(lines[1], grid.LineAt(new Point(card.Right - 1, card.Bottom - 1)));
        // The second slide's lines are the song's third and fourth.
        Assert.Equal(lines[2], grid.LineAt(new Point(grid.Cells[1].Card.X + 5, 60)));
        Assert.Equal(lines[7], grid.LineAt(new Point(grid.Cells[3].Card.X + 5, grid.Cells[3].Card.Bottom - 2)));
        // A frame and its foot are bare sheet, and so is what is between the slides.
        Assert.Null(grid.LineAt(new Point(card.X - 2, card.Y + 20)));
        Assert.Null(grid.LineAt(new Point(card.X + 20, card.Bottom + 10)));
        Assert.Null(grid.LineAt(new Point(card.Right + 8, card.Y + 20)));
        Assert.Null(grid.LineAt(new Point(2, 2)));
        Assert.Null(grid.LineAt(new Point(card.X + 20, 5000)));
    }

    [Fact]
    public void APointOfACardIsTheCharacterThereOrWithShiftTheWordOrTheChordWhoseBubbleItIsOn()
    {
        var sheet = FiveSlides();
        var grid = SheetGrid.LayOut(sheet, new Sizes(), 600);
        // The first line is "One two three", from 26 along the sheet, each character 10.
        var first = LinesOf(sheet)[0];
        var words = 52 + 24 + 6 + 14.0;
        Assert.Equal((first, 0, false), grid.PlaceAt(new Point(26 + 3, words), shift: false));
        Assert.Equal((first, 5, false), grid.PlaceAt(new Point(26 + 53, words), shift: false));
        Assert.Equal((first, 12, false), grid.PlaceAt(new Point(26 + 127, words), shift: false));
        // With Shift, the start of the word it is in.
        Assert.Equal((first, 4, false), grid.PlaceAt(new Point(26 + 53, words), shift: true));
        Assert.Equal((first, 8, false), grid.PlaceAt(new Point(26 + 127, words), shift: true));
        // Past the end of the words, its last character; and before them, its first.
        Assert.Equal((first, 12, false), grid.PlaceAt(new Point(26 + 139, words), shift: false));
        Assert.Equal((first, 0, false), grid.PlaceAt(new Point(26 - 9, words), shift: false));

        // On a chord's bubble, that chord's place, whatever character is under it: the D
        // on "three" is 22 wide from 80 along, over the characters 8 and 9 and part of 10.
        Assert.Equal((first, 8, true), grid.PlaceAt(new Point(26 + 80 + 15, 52 + 5), shift: false));
        Assert.Equal((first, 8, true), grid.PlaceAt(new Point(26 + 80 + 21, 52 + 29), shift: false));
        Assert.Equal((first, 0, true), grid.PlaceAt(new Point(26 + 3, 52 + 5), shift: true));
        // Beside the bubbles, on the row of chords, it is the character under the point.
        Assert.Equal((first, 4, false), grid.PlaceAt(new Point(26 + 45, 52 + 5), shift: false));
        // Under the bubble's tail, among the words, it is the character.
        Assert.Equal((first, 9, false), grid.PlaceAt(new Point(26 + 95, 52 + 30), shift: false));

        // Off the cards there is no place.
        Assert.Null(grid.PlaceAt(new Point(2, 2), shift: false));
        Assert.Null(grid.PlaceAt(new Point(26 + 3, grid.Cells[0].Card.Bottom + 8), shift: false));

        // A chord that is being carried is never taken to be over another's bubble: it
        // goes to the character there.
        Assert.Equal((first, 9), grid.PlaceCarriedTo(new Point(26 + 95, 52 + 5), shift: false));
        Assert.Equal((first, 8), grid.PlaceCarriedTo(new Point(26 + 95, 52 + 5), shift: true));
        Assert.Null(grid.PlaceCarriedTo(new Point(2, 2), shift: false));
    }

    [Fact]
    public void APlaceAlongALineNeverGoesBackwardsAndAPointOverAPlaceIsThatPlace()
    {
        var sheet = FiveSlides();
        var grid = SheetGrid.LayOut(sheet, new Sizes(), 600);
        foreach (var row in LinesOf(sheet))
        {
            var item = grid.Lines[row];
            var words = item.ChordTop + 24 + 6 + 14;
            var last = -1;
            for (var x = item.Left - 12; x < item.Left + 150; x += 0.5)
            {
                if (grid.PlaceAt(new Point(x, words), shift: false) is not { } place)
                {
                    continue;
                }
                Assert.True(place.At >= last, "row " + row + " goes back from " + last + " to " + place.At + " at " + x);
                last = place.At;
            }
            Assert.Equal(sheet.Rows[row].Text.Length - 1, last);
            // A point that is over a place, among the words, is that place; and over
            // the chord that stands there, that chord.
            for (var at = 0; at < sheet.Rows[row].Text.Length; at++)
            {
                var point = grid.PointOver(new Spot(row, at));
                Assert.NotNull(point);
                // (Over the space between two words it is the word after: rule 16.)
                var there = sheet.Rows[row].Text[at] == ' ' ? at + 1 : at;
                Assert.Equal((row, there, false), grid.PlaceAt(point.Value, shift: false));
                Close(item.Left + (at * 10), grid.XOf(new Spot(row, at)));
            }
            foreach (var chord in sheet.Rows[row].Chords)
            {
                var point = grid.PointOver(new Spot(row, chord.At), onItsChord: true);
                Assert.NotNull(point);
                Assert.Equal((row, chord.At, true), grid.PlaceAt(point.Value, shift: false));
            }
        }
        // There is no point over a chord where there is none, nor over a group's name,
        // nor over a row there is not.
        Assert.Null(grid.PointOver(new Spot(LinesOf(sheet)[0], 5), onItsChord: true));
        Assert.Null(grid.PointOver(new Spot(0, 0)));
        Assert.Null(grid.PointOver(new Spot(99, 0)));
    }

    [Fact]
    public void ALineWithNoWordsCanTakeNoSpotAndHasNoPlaceUnderAPoint()
    {
        // A slide whose second line is empty, between two of words.
        var sheet = new ChordSheet(new Song(Slide(0, "One\n\nThree"), Slide(1, "Four")));
        var grid = SheetGrid.LayOut(sheet, new Sizes(), 600);
        var empty = Enumerable.Range(0, sheet.Rows.Count).Single(row => sheet.Rows[row].Kind == SheetRowKind.Line && sheet.Rows[row].Text.Length == 0);
        Assert.False(sheet.Usable(empty));
        var item = grid.Lines[empty];
        // The point is on that line of the card, and the line has no place.
        Assert.Equal(empty, grid.LineAt(new Point(item.Left + 5, item.ChordTop + 40)));
        Assert.Null(grid.PlaceAt(new Point(item.Left + 5, item.ChordTop + 40), shift: false));
        Assert.Null(grid.PlaceCarriedTo(new Point(item.Left + 5, item.ChordTop + 40), shift: false));
        Assert.Null(grid.PointOver(new Spot(empty, 0)));
        // Down from the line over it goes past it, and Up from the one under it.
        Assert.Equal(empty + 1, grid.LineBeside(empty - 1, 1));
        Assert.Equal(empty - 1, grid.LineBeside(empty + 1, -1));
    }

    // ---- Down and Up, by the screen

    [Fact]
    public void DownAndUpGoToTheLineUnderOrOverOnTheScreen()
    {
        var sheet = FiveSlides();
        var grid = SheetGrid.LayOut(sheet, new Sizes(), 600);
        // Three to a row: slides 1, 2, 3 over slides 4 and 5. Within a slide, its next
        // line. (The song's lines are counted here from nought, two to a slide.)
        var lines = LinesOf(sheet);
        int? Beside(int line, int delta) => grid.LineBeside(lines[line], delta) is { } row ? Array.IndexOf(lines, row) : null;
        Assert.Equal(1, Beside(0, 1));
        Assert.Equal(0, Beside(1, -1));
        // From a slide's last line, the first line of the slide under it; and from its
        // first, the last line of the slide over it.
        Assert.Equal(6, Beside(1, 1));
        Assert.Equal(1, Beside(6, -1));
        Assert.Equal(8, Beside(3, 1));
        Assert.Equal(3, Beside(8, -1));
        // The third slide has none under it: the row under is a short one, and its
        // last slide is the one gone to.
        Assert.Equal(8, Beside(5, 1));
        // Over the first row and under the last there is nowhere to go.
        Assert.Null(Beside(0, -1));
        Assert.Null(Beside(2, -1));
        Assert.Null(Beside(7, 1));
        Assert.Null(Beside(9, 1));
        // And from a group's name, or a row there is not, nowhere.
        Assert.Null(grid.LineBeside(0, 1));
        Assert.Null(grid.LineBeside(99, 1));

        // One to a row, the slides are one under another, and it is the order of the song.
        var one = SheetGrid.LayOut(sheet, new Sizes(), 300);
        Assert.Equal(lines.Skip(1), lines.Take(9).Select(row => one.LineBeside(row, 1)!.Value));
    }

    [Fact]
    public void DownAndUpPutTheSpotOnTheStopNearestUnderOrOverWhereItWas()
    {
        var sheet = FiveSlides();
        var grid = SheetGrid.LayOut(sheet, new Sizes(), 600);
        var revealed = 0;
        sheet.Reveal = () => revealed++;
        // With no spot yet, the first arrow key puts it on the first word of the song.
        var lines = LinesOf(sheet);
        Assert.False(sheet.Usable(sheet.Spot.Row));
        grid.StepLine(1);
        Assert.Equal(new Spot(lines[0], 0), sheet.Spot);

        // From "three" (80 along) down to "Four five": its stops are 0 and 50, and 50
        // is the nearer.
        sheet.SetSpot(lines[0], 8);
        revealed = 0;
        grid.StepLine(1);
        Assert.Equal(new Spot(lines[1], 5), sheet.Spot);
        Assert.Equal(1, revealed);
        // Down again, to the slide under it: "Thirteen" has one stop.
        grid.StepLine(1);
        Assert.Equal(new Spot(lines[6], 0), sheet.Spot);
        // And up, to the last line of the slide over it, at the stop nearest over it.
        grid.StepLine(-1);
        Assert.Equal(new Spot(lines[1], 0), sheet.Spot);
        grid.StepLine(-1);
        Assert.Equal(new Spot(lines[0], 0), sheet.Spot);
        // Up from the top row changes nothing, and reveals nothing.
        revealed = 0;
        grid.StepLine(-1);
        Assert.Equal(new Spot(lines[0], 0), sheet.Spot);
        Assert.Equal(0, revealed);

        // The slides are side by side: from the second slide's last line, at "nine" (50
        // along its line, 220 along the sheet), down to the slide under it, whose
        // "Fifteen" has one stop.
        sheet.SetSpot(lines[3], 6);
        grid.StepLine(1);
        Assert.Equal(new Spot(lines[8], 0), sheet.Spot);

        // Of two stops as near, the one further left: from 50 along "abcde fgh", the
        // line under it has its stops at 0 and at 100.
        var two = new ChordSheet(new Song(Slide(0, "abcde fgh\nabcdefghi jkl")));
        var under = SheetGrid.LayOut(two, new Sizes(), 600);
        Assert.Equal([0, 10], two.Stops(1));
        two.SetSpot(0, 5);
        under.StepLine(1);
        Assert.Equal(new Spot(1, 0), two.Spot);
    }
}
