// The chord sheet's model, held to testdata/vectors/sheet-model.json and layout.json
// (spec/03-chord-sheet.md, "What to hold your build to": every case of both).
//
// Each test of the first part runs EVERY case of its part of the file, and says of a
// failure which case it was and what was got. No case is copied into a test: the file
// is read. Those of the second part, each "MINE, NOT A VECTOR", are for what the vectors
// do not hold: what goes by the screen, the keys in the typing box, and the places this
// departs from the reference on purpose (Song/ChordSheet.cs says which). They expect
// what the Mac's tests of the same expect (ChordSheetTests.swift).

using System.Text.Json;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Tests;
using Xunit;

namespace Pro7ChordEditor.Song.Tests;

public class ChordSheetTests
{
    private readonly SheetVectors vectors = SheetVectors.Read();

    // ---- Saying which case it was

    private static void Same(IEnumerable<string> expected, IEnumerable<string> got, string which)
    {
        var wanted = string.Join("\n    ", expected);
        var had = string.Join("\n    ", got);
        Assert.True(string.Equals(wanted, had, StringComparison.Ordinal), which + "\n  expected:\n    " + wanted + "\n  got:\n    " + had);
    }

    private static void SameOne<T>(T expected, T got, string which) =>
        Assert.True(EqualityComparer<T>.Default.Equals(expected, got), which + "\n  expected: " + expected + "\n  got:      " + got);

    private static string Said(SheetRow row) => row.Kind != SheetRowKind.Line
        ? "group " + Spelt.Of(row.Name) + " slide " + Spelt.N(row.Slide)
        : "line block " + Spelt.N(row.Block) + " line " + Spelt.N(row.Line) + " slide " + Spelt.N(row.Slide) + " box " + Spelt.Of(row.Element) + " " + Spelt.Of(row.Text)
            + " start " + Spelt.N(row.Start) + " " + Spelt.Of(row.Chords) + " alone " + Spelt.B(row.Alone) + " blank " + Spelt.B(row.Blank)
            + " first " + Spelt.B(row.First) + " count " + Spelt.N(row.Count) + " last " + Spelt.B(row.Last);

    private static string Said(SheetVectors.ARow row) => row.Kind != "line"
        ? "group " + Spelt.Of(row.Name ?? "") + " slide " + Spelt.N(row.Row)
        : "line block " + Spelt.N(row.Block ?? -1) + " line " + Spelt.N(row.Line ?? -1) + " slide " + Spelt.N(row.Row) + " box " + Spelt.Of(row.Element ?? "?") + " " + Spelt.Of(row.Text ?? "?")
            + " start " + Spelt.N(row.Start ?? -1) + " " + Spelt.Of((row.Chords ?? []).Select(chord => chord.Chord)) + " alone " + Spelt.B(row.Alone ?? false) + " blank " + Spelt.B(row.Blank ?? false)
            + " first " + Spelt.B(row.First ?? false) + " count " + Spelt.N(row.Count ?? -1) + " last " + Spelt.B(row.Last ?? false);

    private static string Said(ChordSheet.CopiedLine line) => line switch
    {
        ChordSheet.CopiedWords words => "words [" + string.Join(", ", words.Chords.Select(chord => Spelt.Of(chord.Name) + " word " + Spelt.N(chord.Word) + "+" + Spelt.N(chord.Offset))) + "]",
        ChordSheet.CopiedAlone alone => "alone " + Spelt.Of(alone.Names),
        _ => "?",
    };

    private static TextBox Box(int slide, string element, string words, IReadOnlyList<Chord>? chords = null, string group = "", bool startsItsGroup = false) =>
        new(slide, group, null, startsItsGroup, element, words, chords ?? [], Blank: false);

    // ---- sheet-model.json

    [Fact]
    public void RowsARowForAGroupsNameAndOneForEachLineOfEachTextBox()
    {
        Assert.Equal(vectors.Songs.Count, vectors.Rows.Count);
        foreach (var expected in vectors.Rows)
        {
            var (sheet, _) = vectors.Sheet(expected.Song);
            Same(expected.Rows.Select(Said), sheet.Rows.Select(Said), expected.Song);
            SameOne(expected.MostLines, sheet.MostLines, expected.Song);
            Same(expected.Used.Select(Spelt.Of), sheet.Used.Select(Spelt.Of), expected.Song);
        }
    }

    [Fact]
    public void WordStartsWhereTheWordsOfALineStart()
    {
        Assert.NotEmpty(vectors.WordStarts);
        foreach (var expected in vectors.WordStarts)
        {
            Same(expected.Out.Select(Spelt.N), ChordSheet.WordStarts(expected.Text).Select(Spelt.N), Spelt.Of(expected.Text));
        }
    }

    [Fact]
    public void StopsThePlacesOfEachRowThatTheSpotStopsAtAndWhetherTheRowCanTakeIt()
    {
        Assert.NotEmpty(vectors.Stops);
        foreach (var expected in vectors.Stops)
        {
            var (sheet, _) = vectors.Sheet(expected.Song);
            var which = expected.Song + " row " + Spelt.N(expected.Row);
            SameOne(expected.Usable, sheet.Usable(expected.Row), which);
            Same(expected.Out.Select(Spelt.N), (sheet.Usable(expected.Row) ? sheet.Stops(expected.Row) : []).Select(Spelt.N), which);
        }
    }

    [Fact]
    public void StopAtTheStopAtOrBeforeACharacter()
    {
        Assert.NotEmpty(vectors.StopAt);
        foreach (var expected in vectors.StopAt)
        {
            var (sheet, _) = vectors.Sheet(expected.Song);
            SameOne(expected.Out, sheet.StopAt(expected.Row, expected.At), expected.Song + " row " + Spelt.N(expected.Row) + " at " + Spelt.N(expected.At));
        }
    }

    [Fact]
    public void WalksWhereRightAndLeftTakeTheSpotAWordAtATimeAndWithShiftALetterAtATime()
    {
        Assert.NotEmpty(vectors.Walks);
        for (var number = 0; number < vectors.Walks.Count; number++)
        {
            var expected = vectors.Walks[number];
            var (sheet, song) = vectors.Sheet(expected.Song);
            if (expected.Start is { } start)
            {
                sheet.SetSpot(start.Row, start.At);
            }
            var spots = new List<Spot>();
            foreach (var key in expected.Keys)
            {
                sheet.Step(key.Contains("Left", StringComparison.Ordinal) ? -1 : 1, exact: key.StartsWith("Shift", StringComparison.Ordinal));
                spots.Add(sheet.Spot);
            }
            Same(expected.Spots.Select(spot => Spelt.Of(spot.Spot)), spots.Select(Spelt.Of), "walk " + Spelt.N(number) + " on " + expected.Song + ": " + string.Join(" ", expected.Keys));
            Assert.Empty(song.Asked);
        }
    }

    // Does what one case of `edits` says was done, as the script that made it did.
    private static bool? Run(SheetVectors.What what, Spot spot, ChordSheet sheet)
    {
        if (what.Put is { } name)
        {
            if (what.OnRow is { } row)
            {
                return sheet.Put(row, 0, name);
            }
            if (what.Then is not { } then)
            {
                return sheet.Put(sheet.Spot.Row, sheet.Spot.At, name);
            }
            return sheet.Put(spot.Row, spot.At, name) && Follow(then, spot, sheet);
        }
        if (what.Take == true)
        {
            if (what.Then is not { } then)
            {
                return sheet.Take(sheet.Spot.Row, sheet.Spot.At);
            }
            return sheet.Take(spot.Row, spot.At) && Follow(then, spot, sheet);
        }
        if (what.Key is { } number)
        {
            return sheet.Put(sheet.Spot.Row, sheet.Spot.At, sheet.InKey[number - 1]);
        }
        if (what.Move is { } to)
        {
            sheet.Move(spot, to.Spot, copy: false);
            return null;
        }
        var copied = what.Copy ?? throw new InvalidDataException("a case of edits that says nothing this test knows how to do");
        sheet.Move(spot, copied.Spot, copy: true);
        return null;
    }

    // What a case says was done next, and after that: each at the case's own spot, or at
    // the place of its row that the step names; and each only if the one before it was
    // done, as the script that made the case has them (`a && b && c`).
    private static bool Follow(SheetVectors.Then step, Spot spot, ChordSheet sheet)
    {
        var at = step.At ?? spot.At;
        var done = step.Put is { } name ? sheet.Put(spot.Row, at, name) : sheet.Take(spot.Row, at);
        return done && step.Next is { } next ? Follow(next, spot, sheet) : done;
    }

    [Fact]
    public void EditsForEachThingDoneAtTheSpotWhatTheSongIsAskedWhereTheSpotIsLeftAndTheSongAfterwards()
    {
        Assert.NotEmpty(vectors.Edits);
        for (var number = 0; number < vectors.Edits.Count; number++)
        {
            var expected = vectors.Edits[number];
            var which = "edit " + Spelt.N(number) + " on " + expected.Song + ": " + (expected.What.Note ?? "");
            var (sheet, song) = vectors.Sheet(expected.Song);
            Assert.True(sheet.Usable(expected.Spot.Row), which);
            sheet.SetSpot(expected.Spot.Row, expected.Spot.At);
            var answer = Run(expected.What, expected.Spot.Spot, sheet);
            Same(expected.Asked.Select(asked => asked.Spelt), song.Asked, which);
            SameOne(Spelt.Of(expected.SpotAfter.Spot), Spelt.Of(sheet.Spot), which);
            Same(expected.SongAfter.Select(after => after.Spelt), song.After, which);
            SameOne(expected.Answer, answer, which);
        }
    }

    [Fact]
    public void TypingTheBubbleOpenedAndWhatWasTypedKeptOrThrownAway()
    {
        Assert.NotEmpty(vectors.Typing);
        foreach (var expected in vectors.Typing)
        {
            var (sheet, song) = vectors.Sheet(expected.Song);
            for (var number = 0; number < expected.Steps.Count; number++)
            {
                var step = expected.Steps[number];
                var which = expected.Title + ", step " + Spelt.N(number);
                song.Asked.Clear();
                var what = step.Step;
                if (what.Spot is { } spot)
                {
                    Assert.True(sheet.Usable(spot.Row), which);
                    sheet.SetSpot(spot.Row, spot.At);
                }
                if (what.Press is { } press)
                {
                    sheet.StartTyping(press == "Enter" ? null : press);
                }
                if (what.Typed is { } typed)
                {
                    sheet.Type(typed);
                }
                if (what.Finish is { } finish)
                {
                    sheet.FinishTyping(finish == "Esc" ? null : sheet.Taken(), onward: finish == "Tab");
                }
                if (what.Click is { } click)
                {
                    Assert.True(sheet.Usable(click.Row), which);
                    sheet.Pick(click.Row, click.At);
                }
                if (what.Settle == true)
                {
                    sheet.Settle();
                }
                Same(step.Asked.Select(asked => asked.Spelt), song.Asked, which);
                SameOne(step.Typing, sheet.Typing, which);
                SameOne(step.TypingAt is { } at ? Spelt.Of(at.Spot) : "nowhere", sheet.Typing ? Spelt.Of(sheet.TypingAt) : "nowhere", which);
                SameOne(step.BoxHolds is { } holds ? Spelt.Of(holds.Text) + " selected " + Spelt.B(holds.Selected) : "no box",
                    sheet.Typing ? Spelt.Of(sheet.Entry.Begun) + " selected " + Spelt.B(sheet.Entry.Selected) : "no box",
                    which);
                SameOne(Spelt.Of(step.Spot.Spot), Spelt.Of(sheet.Spot), which);
            }
            Same(expected.SongAfter.Select(after => after.Spelt), song.After, expected.Title);
        }
    }

    [Fact]
    public void CopyPasteALinesChordsOrAGroupsCopiedAndPutOnOtherLines()
    {
        Assert.NotEmpty(vectors.CopyPasted);
        foreach (var expected in vectors.CopyPasted)
        {
            var (sheet, song) = vectors.Sheet(expected.Song);
            sheet.SetSpot(expected.CopyAt.Row, expected.CopyAt.At);
            var said = sheet.Copy(expected.WholeGroup);
            Same(expected.Copied.Spelt, (sheet.Copied ?? []).Select(Said), expected.Title);
            Same(expected.Said, said is null ? [] : [said], expected.Title);
            sheet.SetSpot(expected.PasteAt.Row, expected.PasteAt.At);
            song.Asked.Clear();
            var pasted = sheet.Paste();
            // What the paste answers: how many lines, chords, left out, passed over,
            // refused. (What is said of it is the window's model's: SheetModelTests.)
            Assert.True(expected.Pasted.Did.Pasted == pasted, expected.Title + ": answered " + pasted + ", not " + expected.Pasted.Did.Pasted);
            Same(expected.Asked.Select(asked => asked.Spelt), song.Asked, expected.Title);
            Same(expected.SongAfter.Select(after => after.Spelt), song.After, expected.Title);
        }
    }

    // ---- layout.json

    [Fact]
    public void LayoutWhereTheBubblesOfALineStandAndWhichCharacterIsAtADistanceAlongIt()
    {
        using var layout = JsonDocument.Parse(TestData.Vector("layout.json"));
        var metrics = layout.RootElement.GetProperty("metrics");
        var wordLetter = metrics.GetProperty("wordLetter").GetDouble();
        var chordLetter = metrics.GetProperty("chordLetter").GetDouble();
        var pad = metrics.GetProperty("bubblePadEachSide").GetDouble();
        var gap = metrics.GetProperty("gap").GetDouble();
        Assert.Equal(ChordSheet.Gap, gap);
        var places = layout.RootElement.GetProperty("place").EnumerateArray().ToList();
        var characters = layout.RootElement.GetProperty("characterAt").EnumerateArray().ToList();
        Assert.NotEmpty(places);
        Assert.NotEmpty(characters);
        static IEnumerable<double> Numbers(JsonElement list) => list.EnumerateArray().Select(number => number.GetDouble());
        foreach (var expected in places)
        {
            var text = expected.GetProperty("text").GetString()!;
            List<Chord> chords = [.. expected.GetProperty("chords").EnumerateArray().Select(chord => new Chord(chord.GetProperty("at").GetInt32(), chord.GetProperty("name").GetString()!))];
            var placed = ChordLayout.Place(text.Length, chords, units => wordLetter * units, name => (chordLetter * name.Length) + (2 * pad), gap);
            var which = Spelt.Of(text);
            Assert.True(Numbers(expected.GetProperty("xs")).SequenceEqual(placed.Xs), which);
            Assert.True(Numbers(expected.GetProperty("widths")).SequenceEqual(placed.Widths), which);
            SameOne(expected.GetProperty("width").GetDouble(), placed.Width, which);
            SameOne(expected.GetProperty("wordsWidth").GetDouble(), placed.WordsWidth, which);
        }
        foreach (var expected in characters)
        {
            var text = expected.GetProperty("text").GetString()!;
            var x = expected.GetProperty("x").GetDouble();
            SameOne(expected.GetProperty("out").GetInt32(), ChordLayout.CharacterAt(text.Length, x, units => wordLetter * units), Spelt.Of(text) + " at " + x.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    // ---- MINE, NOT A VECTOR

    // Down and Up go to the stop nearest above or below on the screen (rule 20).
    [Fact]
    public void DownAndUpGoToTheStopNearestAboveOrBelowOnTheScreen()
    {
        var (sheet, _) = vectors.Sheet("grace");
        var widths = new FixedWidths();
        // With no spot yet, the first arrow key puts it on the first word of the song.
        sheet.StepLine(1, widths);
        Assert.Equal(new Spot(1, 0), sheet.Spot);
        // "sweet" starts at 18 of "Amazing grace how sweet the sound": 180 along. Under
        // it, of "That saved a wretch like me", "like" at 20 (200) is nearer than
        // "wretch" at 13 (130).
        sheet.SetSpot(1, 18);
        sheet.StepLine(1, widths);
        Assert.Equal(new Spot(2, 20), sheet.Spot);
        // A chord's bubble is where its place is, if it has been pushed along.
        sheet.StepLine(-1, widths);
        Assert.Equal(new Spot(1, 18), sheet.Spot);
        // The name of a group is passed over, and the last line stays where it is.
        sheet.SetSpot(4, 0);
        sheet.StepLine(1, widths);
        Assert.Equal(new Spot(6, 0), sheet.Spot);
        Assert.Equal(2, sheet.Picked);
        sheet.SetSpot(9, 5);
        sheet.StepLine(1, widths);
        Assert.Equal(new Spot(9, 5), sheet.Spot);
        // And the first line stays where it is, going up.
        sheet.SetSpot(1, 8);
        sheet.StepLine(-1, widths);
        Assert.Equal(new Spot(1, 8), sheet.Spot);
    }

    // The place under the pointer: the character, or with Shift the word; over a space,
    // the word after (rule 16).
    [Fact]
    public void ThePlaceUnderThePointerIsTheCharacterOrTheWordAndOverASpaceTheWordAfter()
    {
        var (sheet, _) = vectors.Sheet("grace");
        var widths = new FixedWidths();
        // "Amazing grace how sweet the sound": each letter ten wide.
        Assert.Equal(0, sheet.PlaceAt(1, 0, exact: true, widths));
        Assert.Equal(3, sheet.PlaceAt(1, 34, exact: true, widths));
        Assert.Equal(0, sheet.PlaceAt(1, 34, exact: false, widths));
        // 75 along is the space after "Amazing": the word after is meant.
        Assert.Equal(8, sheet.PlaceAt(1, 75, exact: true, widths));
        Assert.Equal(0, sheet.PlaceAt(1, 75, exact: false, widths));
        Assert.Equal(32, sheet.PlaceAt(1, 5000, exact: true, widths));
        Assert.Equal(0, sheet.PlaceAt(1, -40, exact: true, widths));
        // Two spaces between words ("Halle  lujah"): only the second is the word after.
        var (odd, _) = vectors.Sheet("odd");
        Assert.Equal(5, odd.PlaceAt(0, 55, exact: true, widths));
        Assert.Equal(7, odd.PlaceAt(0, 65, exact: true, widths));

        // A line of chords alone: each chord, and past the last, one more.
        var (intro, _) = vectors.Sheet("intro");
        Assert.Equal([0.0, 26, 52], intro.Placed(1, widths).Xs);
        Assert.Equal(0, intro.PlaceAt(1, 3, exact: true, widths));
        Assert.Equal(2, intro.PlaceAt(1, 30, exact: true, widths));
        // (A bubble is pointed at from a little past its right edge too: 5.)
        Assert.Equal(2, intro.PlaceAt(1, 50, exact: true, widths));
        Assert.Equal(4, intro.PlaceAt(1, 54, exact: true, widths));
        Assert.Equal(4, intro.PlaceAt(1, 60, exact: false, widths));
        Assert.Equal(6, intro.PlaceAt(1, 300, exact: true, widths));
        Assert.Equal(26, intro.XOf(1, 2, widths));
        Assert.Equal(52 + 22 + 10, intro.XOf(1, 6, widths));
        // A blank slide's line has the one place.
        var (blank, _) = vectors.Sheet("blank");
        Assert.Equal(0, blank.PlaceAt(1, 200, exact: true, widths));
        Assert.Equal(0, blank.XOf(1, 0, widths));
    }

    // A chord belongs to the line its place is in, up to but not including its line
    // break; an empty line is one place wide (rule 3).
    [Fact]
    public void AChordBelongsToTheLineItsPlaceIsInAndAnEmptyLineIsOnePlaceWide()
    {
        // "ab", an empty line, "cd": places 0 1 (2 the break) (3 the empty line, and its
        // break) 4 5.
        var song = new StandInSong([Box(0, "A", "ab\n\ncd", [new Chord(1, "G"), new Chord(2, "X"), new Chord(3, "D"), new Chord(4, "C")])], "C");
        var sheet = new ChordSheet(song);
        Assert.Equal(["[\"G\"@1]", "[\"D\"@0]", "[\"C\"@0]"], sheet.Rows.Select(row => Spelt.Of(row.Chords)));
        Assert.Equal([0, 3, 4], sheet.Rows.Select(row => row.Start));
        // The empty line cannot take the spot, and a change to the first line hands the
        // other lines' chords back as they were, the empty line's with them.
        Assert.False(sheet.Usable(1));
        Assert.True(sheet.Put(0, 0, "Am"));
        Assert.Equal("[\"D\"@0]", Spelt.Of(sheet.Rows[1].Chords));
        Assert.Equal("[\"C\"@0]", Spelt.Of(sheet.Rows[2].Chords));
    }

    // The empty lines a text box's words end with get no row, and do not make the song's
    // cards taller; one that a chord stands on is kept.
    [Fact]
    public void TheEmptyLinesATextBoxsWordsEndWithGetNoRowUnlessAChordStandsOnOne()
    {
        // Two lines of words and then three line breaks; and a box of one line.
        var song = new StandInSong(
            [
                Box(0, "A", "ab\ncd\n\n\n", [new Chord(0, "G"), new Chord(3, "D")]),
                Box(1, "B", "ef\n\ngh\n"),
                Box(2, "C", "ij"),
            ],
            "C");
        var sheet = new ChordSheet(song);
        Assert.Equal(["ab", "cd", "ef", "", "gh", "ij"], sheet.Rows.Select(row => row.Text));
        // (An empty line in the middle of a box's words is a row still.)
        Assert.Equal([2, 2, 3, 3, 3, 1], sheet.Rows.Select(row => row.Count));
        Assert.Equal(3, sheet.MostLines);
        Assert.Equal([false, true, false, false, true, true], sheet.Rows.Select(row => row.Last));
        Assert.Equal([true, false, true, false, false, true], sheet.Rows.Select(row => row.First));
        // Nothing of the words is changed by it, and a change to a line still hands
        // every other chord of the box back.
        sheet.Put(1, 1, "Am");
        Assert.Equal(["setChords slide 0 box \"A\" [\"G\"@0, \"D\"@3, \"Am\"@4] joined false"], song.Asked);
        Assert.Equal("ab\ncd\n\n\n", song.TextBoxes[0].Words);

        // A chord on the second of three empty lines at the end: that line and the one
        // before it are rows, the one after it is not.
        var kept = new ChordSheet(new StandInSong([Box(0, "A", "ab\n\n\n", [new Chord(4, "C")])], "C"));
        Assert.Equal(["ab", "", ""], kept.Rows.Select(row => row.Text));
        Assert.Equal(["[]", "[]", "[\"C\"@0]"], kept.Rows.Select(row => Spelt.Of(row.Chords)));
        // And a box always has one line, though its words be one line break.
        Assert.Single(new ChordSheet(new StandInSong([Box(0, "A", "\n")], "C")).Rows);
    }

    // A group with no name has no row; a chord of a line of chords alone can be moved to
    // its front; two pasted chords that land on one place leave the first.
    [Fact]
    public void AGroupWithNoNameHasNoRowAndTwoOtherSmallThings()
    {
        var song = new StandInSong(
            [
                Box(0, "A", Chords.Placeholders(3), [new Chord(0, "D"), new Chord(2, "G"), new Chord(4, "A")], startsItsGroup: true),
                Box(1, "B", "Amazing grace\nI am", [new Chord(0, "G"), new Chord(3, "D"), new Chord(8, "C")]),
            ],
            "D");
        var sheet = new ChordSheet(song);
        Assert.Equal([SheetRowKind.Line, SheetRowKind.Line, SheetRowKind.Line], sheet.Rows.Select(row => row.Kind));
        // The last of three, to the front: it changes places, and the rest close up.
        sheet.Move(new Spot(0, 4), new Spot(0, 0), copy: false);
        Assert.Equal(["A", "D", "G"], sheet.Rows[0].Chords.Select(chord => chord.Name));
        // "Amazing" has a chord at its start and one three letters in; "I" has one
        // letter, so both would land on it: the first is the one that is put.
        song.Asked.Clear();
        sheet.SetSpot(1, 0);
        sheet.Copy(whole: false);
        sheet.SetSpot(2, 0);
        sheet.Paste();
        Assert.Equal(["setChords slide 1 box \"B\" [\"G\"@0, \"D\"@3, \"C\"@8, \"G\"@14, \"C\"@16] joined false"], song.Asked);
    }

    private sealed class Twice : ISheetMeasure
    {
        public double Scale => 2;

        public double Advance(int row, int units) => 20.0 * units;

        public double Bubble(string name) => (16.0 * name.Length) + 28;
    }

    // Drawn twice the size, the gaps the sheet keeps along a line are twice theirs.
    [Fact]
    public void DrawnTwiceTheSizeTheGapsTheSheetKeepsAlongALineAreTwiceTheirs()
    {
        var (intro, _) = vectors.Sheet("intro");
        // Three chords on a line of chords alone, each one letter: 44 wide, and 8 apart
        // where at the usual size they are 22 wide and 4 apart.
        Assert.Equal([0.0, 52, 104], intro.Placed(1, new Twice()).Xs);
        Assert.Equal([0.0, 26, 52], intro.Placed(1, new FixedWidths()).Xs);
        // The place for a new one is 20 past the last, where it was 10; and a bubble is
        // pointed at from 10 past its right edge, where it was 5.
        Assert.Equal(104 + 44 + 20, intro.XOf(1, 6, new Twice()));
        Assert.Equal(2, intro.PlaceAt(1, 105, exact: true, new Twice()));
        Assert.Equal(4, intro.PlaceAt(1, 107, exact: true, new Twice()));
        // (A measure that says nothing of its size is at the usual one.)
        Assert.Equal(1, ((ISheetMeasure)new FixedWidths()).Scale);
    }

    // A place is never the second half of a character that takes two units.
    [Fact]
    public void APlaceIsNeverTheSecondHalfOfACharacterThatTakesTwoUnits()
    {
        // "a", a musical sharp sign from beyond the first 65,536 (two units), "b".
        var words = new string(['a', (char)0xD834, (char)0xDD30, 'b']);
        var sheet = new ChordSheet(new StandInSong([Box(0, "A", words)], "C"));
        sheet.SetSpot(0, 0);
        sheet.Step(1, exact: true);
        Assert.Equal(1, sheet.Spot.At);
        sheet.Step(1, exact: true);
        Assert.Equal(3, sheet.Spot.At);
        sheet.Step(-1, exact: true);
        Assert.Equal(1, sheet.Spot.At);
        // (Every unit ten wide, as these letters are measured: 25 along is the second
        // half.)
        Assert.Equal(1, sheet.PlaceAt(0, 25, exact: true, new FixedWidths()));
    }

    // The box a chord is typed in takes only what could be a chord, tidied as it is
    // typed, sixteen at most (rule 30).
    [Fact]
    public void TheBoxAChordIsTypedInTakesOnlyWhatCouldBeAChordTidiedAsItIsTypedSixteenAtMost()
    {
        var (sheet, song) = vectors.Sheet("grace");
        sheet.SetSpot(1, 13);
        sheet.StartTyping("F");
        Assert.Equal("F", sheet.Entry.Text);
        Assert.False(sheet.Entry.Selected);
        Assert.Equal("F#", sheet.Type("f#"));
        Assert.Equal("F#m7/A", sheet.Type("F#m7/a"));
        // What could not be a chord is undone at once.
        Assert.Equal("F#m7/A", sheet.Type("F#m7/A!"));
        Assert.Equal("F#m7/A", sheet.Type("F#m7/A_"));
        Assert.Equal("F#m7/A", sheet.Type("x"));
        Assert.Equal("F#m7/A", sheet.Type("F#m7b5add9sus4/Ab"));
        Assert.Equal("F#m7b5add9sus/Ab", sheet.Type("F#m7b5add9sus/Ab"));
        Assert.Equal("", sheet.Type(""));
        Assert.Empty(song.Asked);
    }

    // Down and Up go through what is offered, round and round; Right takes the one
    // picked into the box (rule 31).
    [Fact]
    public void DownAndUpGoThroughWhatIsOfferedRoundAndRoundAndRightTakesTheOnePickedIntoTheBox()
    {
        var (sheet, song) = vectors.Sheet("grace");
        // With no chord being typed nothing is offered.
        Assert.Empty(sheet.Offered);
        sheet.SetSpot(1, 13);
        sheet.StartTyping("E");
        var offered = sheet.Offered;
        Assert.Equal(Chords.Completions("E", "G", sheet.Used).Take(14), offered);
        Assert.InRange(offered.Count, 3, 14);
        // Right with none picked is the box's own key.
        Assert.False(sheet.Press(ChordSheet.EntryKey.Right));
        Assert.True(sheet.Press(ChordSheet.EntryKey.Down));
        Assert.Equal(0, sheet.Entry.Pick);
        Assert.True(sheet.Press(ChordSheet.EntryKey.Down));
        Assert.Equal(1, sheet.Entry.Pick);
        Assert.True(sheet.Press(ChordSheet.EntryKey.Up) && sheet.Press(ChordSheet.EntryKey.Up));
        Assert.Equal(offered.Count - 1, sheet.Entry.Pick);
        Assert.True(sheet.Press(ChordSheet.EntryKey.Down));
        Assert.Equal(0, sheet.Entry.Pick);
        Assert.Equal(offered[0], sheet.Taken());
        // Right with the caret not at the end is the box's own key too.
        Assert.False(sheet.Press(ChordSheet.EntryKey.Right, caretAtEnd: false));
        Assert.True(sheet.Press(ChordSheet.EntryKey.Right));
        Assert.Equal((offered[0], -1), (sheet.Entry.Text, sheet.Entry.Pick));
        // Up from none picked is the last.
        Assert.True(sheet.Press(ChordSheet.EntryKey.Up));
        Assert.Equal(sheet.Offered.Count - 1, sheet.Entry.Pick);
        // Typing unpicks.
        sheet.Type(sheet.Entry.Text);
        Assert.Equal(-1, sheet.Entry.Pick);
        Assert.Empty(song.Asked);
        Assert.True(sheet.Typing);
    }

    // Enter keeps the one picked; Tab takes the first offered if what is typed is no
    // chord yet; Esc changes nothing (rules 32 to 34).
    [Fact]
    public void EnterKeepsTheOnePickedTabTakesTheFirstOfferedIfWhatIsTypedIsNoChordYetAndEscChangesNothing()
    {
        var (sheet, song) = vectors.Sheet("grace");
        sheet.SetSpot(1, 13);
        sheet.StartTyping("E");
        sheet.Press(ChordSheet.EntryKey.Down);
        var picked = sheet.Offered[0];
        sheet.Press(ChordSheet.EntryKey.Enter);
        Assert.False(sheet.Typing);
        Assert.Equal(picked, sheet.ChordAt(1, 13));

        // Nothing typed is no chord: Tab takes the first that is offered, and moves on.
        sheet.SetSpot(2, 5);
        sheet.StartTyping();
        Assert.Equal("", sheet.Entry.Text);
        Assert.True(sheet.Entry.Selected);
        var first = sheet.Offered[0];
        sheet.Press(ChordSheet.EntryKey.Tab);
        Assert.Equal(first, sheet.ChordAt(2, 5));
        Assert.Equal(new Spot(2, 11), sheet.Spot);

        // What is typed is a chord: Tab keeps that, not the first offered.
        sheet.StartTyping("A");
        sheet.Press(ChordSheet.EntryKey.Tab);
        Assert.Equal("A", sheet.ChordAt(2, 11));

        song.Asked.Clear();
        sheet.StartTyping("B");
        sheet.Type("Bm7");
        sheet.Press(ChordSheet.EntryKey.Escape);
        Assert.False(sheet.Typing);
        Assert.Empty(song.Asked);
        // And with no chord being typed, a key is nobody's here.
        Assert.False(sheet.Press(ChordSheet.EntryKey.Enter));
        // Each opening of the bubble is counted, to tell one typing from the next.
        Assert.Equal(4, sheet.TypingsBegun);
    }

    // Backspace in an empty box takes off the chord at that place (rule 35).
    [Fact]
    public void BackspaceInAnEmptyBoxTakesOffTheChordAtThatPlace()
    {
        var (sheet, song) = vectors.Sheet("grace");
        sheet.SetSpot(1, 8);
        sheet.StartTyping();
        Assert.Equal("G7", sheet.Entry.Text);
        Assert.True(sheet.Entry.Selected);
        // With something in it, Backspace is the box's own.
        Assert.False(sheet.Press(ChordSheet.EntryKey.Delete));
        sheet.Type("");
        Assert.True(sheet.Press(ChordSheet.EntryKey.Delete));
        Assert.False(sheet.Typing);
        Assert.Equal("", sheet.ChordAt(1, 8));
        Assert.Single(song.Asked);
    }

    // Kept with nothing in the box, the chord that was there goes: by Enter, by a click
    // on another place, by a click on the bare sheet. Esc changes nothing; and where
    // there was no chord, nothing is asked (rule 35a, new on 2026-10-13). MINE, NOT A
    // VECTOR, there being none until Simple Presenter does as the rule says
    // (testdata/README.md, "Still wanted"): the Mac's `keptEmpty`, case for case.
    [Fact]
    public void KeptWithNothingInTheBoxTheChordThatWasThereGoesAndEscChangesNothing()
    {
        // Enter.
        var (sheet, song) = vectors.Sheet("grace");
        sheet.SetSpot(1, 8);
        sheet.StartTyping();
        sheet.Type("");
        Assert.True(sheet.Press(ChordSheet.EntryKey.Enter));
        Assert.False(sheet.Typing);
        Assert.Equal("", sheet.ChordAt(1, 8));
        Assert.Equal("G", sheet.ChordAt(1, 0));
        Assert.Single(song.Asked);

        // A click on another place: the one goes, and the bubble opens on the other.
        (sheet, song) = vectors.Sheet("grace");
        sheet.Pick(1, 8);
        sheet.Type("");
        sheet.Pick(2, 5);
        Assert.True(sheet.Typing);
        Assert.Equal(new Spot(2, 5), sheet.TypingAt);
        Assert.Equal("", sheet.ChordAt(1, 8));
        Assert.Single(song.Asked);

        // A click on the bare sheet.
        (sheet, song) = vectors.Sheet("grace");
        sheet.Pick(1, 8);
        sheet.Type("");
        sheet.Settle();
        Assert.False(sheet.Typing);
        Assert.Equal("", sheet.ChordAt(1, 8));
        Assert.Single(song.Asked);

        // Esc: the chord is as it was, and the song was asked nothing.
        (sheet, song) = vectors.Sheet("grace");
        sheet.Pick(1, 8);
        sheet.Type("");
        Assert.True(sheet.Press(ChordSheet.EntryKey.Escape));
        Assert.False(sheet.Typing);
        Assert.Equal("G7", sheet.ChordAt(1, 8));
        Assert.Empty(song.Asked);

        // Opened and kept with its name untouched: nothing is taken off.
        (sheet, song) = vectors.Sheet("grace");
        sheet.Pick(1, 8);
        Assert.True(sheet.Press(ChordSheet.EntryKey.Enter));
        Assert.Equal("G7", sheet.ChordAt(1, 8));

        // Where there is no chord, an empty box kept asks the song nothing.
        (sheet, song) = vectors.Sheet("grace");
        sheet.Pick(1, 13);
        Assert.Equal("", sheet.Entry.Text);
        Assert.True(sheet.Press(ChordSheet.EntryKey.Enter));
        Assert.False(sheet.Typing);
        Assert.Empty(song.Asked);

        // On a line of chords alone, the one cleared goes and the others close up.
        (sheet, song) = vectors.Sheet("intro");
        var alone = sheet.Rows.ToList().FindIndex(row => row.Kind == SheetRowKind.Line && row.Alone && row.Chords.Count >= 2);
        Assert.True(alone >= 0);
        var names = sheet.Rows[alone].Chords.Select(chord => chord.Name).ToList();
        sheet.Pick(alone, 0);
        sheet.Type("");
        Assert.True(sheet.Press(ChordSheet.EntryKey.Enter));
        Assert.Equal(names.Skip(1), sheet.Rows[alone].Chords.Select(chord => chord.Name));
        Assert.Single(song.Asked);
    }

    // 1 to 7 put the key's chord on the spot; while a chord is typed, where the typing
    // was, in place of what was typed (rules 24 and 39).
    [Fact]
    public void OneToSevenPutTheKeysChordOnTheSpotOrWhereTheTypingWas()
    {
        var (sheet, _) = vectors.Sheet("grace");
        sheet.SetSpot(1, 13);
        sheet.PutOfKey(6);
        Assert.Equal("Em", sheet.ChordAt(1, 13));
        sheet.SetSpot(2, 5);
        sheet.StartTyping("B");
        sheet.Type("Bm7");
        sheet.PointerOver(4, 0);
        sheet.PutOfKey(5);
        Assert.False(sheet.Typing);
        Assert.Equal("D", sheet.ChordAt(2, 5));
        Assert.Equal("", sheet.ChordAt(4, 0));
        // (There is no chord for nought, nor for eight.)
        sheet.PutOfKey(0);
        sheet.PutOfKey(8);
        Assert.Equal("", sheet.ChordAt(4, 0));
        // A song that states no key is taken to be in C.
        var (odd, _) = vectors.Sheet("odd");
        Assert.Equal("C", odd.Key);
        Assert.Equal(["C", "Dm", "Em", "F", "G", "Am", "Bdim"], odd.InKey);
        odd.SetSpot(0, 0);
        odd.PutOfKey(4);
        Assert.Equal("F", odd.ChordAt(0, 0));
        // Each of the seven puts on its own.
        for (var number = 1; number <= 7; number++)
        {
            odd.PutOfKey(number);
            Assert.Equal(odd.InKey[number - 1], odd.ChordAt(0, 0));
        }
        // Delete takes off the chord at the spot.
        odd.TakeAtSpot();
        Assert.Equal("", odd.ChordAt(0, 0));
    }

    // A chord carried: the spot goes with it and not with the pointer; let go, it moves,
    // or is copied; where it started, nothing (rules 40 to 46).
    [Fact]
    public void AChordCarriedTakesTheSpotWithItAndLetGoItMovesOrIsCopied()
    {
        var (sheet, song) = vectors.Sheet("grace");
        sheet.Lift(1, 8);
        Assert.Equal("G7", sheet.Dragged?.Name);
        sheet.PointerOver(4, 0);
        Assert.Equal(-1, sheet.Spot.Row);
        sheet.Carry(6, 5);
        Assert.Equal(new Spot(6, 5), sheet.Spot);
        sheet.Drop(copy: false);
        Assert.Null(sheet.Dragged);
        Assert.Equal(("G7", ""), (sheet.ChordAt(6, 5), sheet.ChordAt(1, 8)));
        Assert.Equal(2, song.Asked.Count);
        Assert.EndsWith("joined true", song.Asked[1], StringComparison.Ordinal);

        song.Asked.Clear();
        sheet.Lift(6, 5);
        sheet.Carry(7, 4);
        sheet.Drop(copy: true);
        Assert.Equal(("G7", "G7"), (sheet.ChordAt(6, 5), sheet.ChordAt(7, 4)));
        Assert.Single(song.Asked);

        song.Asked.Clear();
        sheet.Lift(6, 5);
        sheet.Carry(6, 5);
        sheet.Drop(copy: false);
        Assert.Empty(song.Asked);
        // Not while another is being typed; and not where there is none.
        sheet.SetSpot(1, 13);
        sheet.StartTyping("E");
        sheet.Lift(6, 5);
        Assert.Null(sheet.Dragged);
        sheet.Press(ChordSheet.EntryKey.Escape);
        sheet.Lift(1, 13);
        Assert.Null(sheet.Dragged);
        sheet.Lift(6, 5);
        sheet.DropNowhere();
        Assert.Null(sheet.Dragged);
        // With nothing carried, nothing is dropped, and carrying moves no spot.
        sheet.SetSpot(1, 0);
        sheet.Carry(6, 5);
        sheet.Drop(copy: false);
        Assert.Equal(new Spot(1, 0), sheet.Spot);
        Assert.Empty(song.Asked);
    }

    // MINE, NOT A VECTOR (as the Mac's `carriedInFromTheStrip`, case for case). One of
    // the key's chords carried in from the strip: over a place the spot goes with it;
    // let go there it is put on as its key would put it, in place of what is there; off
    // the places it is put nowhere (rules 46a to 46g).
    [Fact]
    public void OneOfTheKeysChordsCarriedInFromTheStripIsPutWhereItIsLetGoAsItsKeyWouldPutIt()
    {
        // (In G the key's chords are G, Am, Bm, C, D, Em and F#dim.)
        var (sheet, song) = vectors.Sheet("grace");
        sheet.SetSpot(4, 0);
        sheet.Carry("C", 2, 5);
        Assert.Equal((new Spot(-1, 0), "C"), sheet.Dragged);
        Assert.Equal(new Spot(2, 5), sheet.Spot);
        // While it is carried the spot is its own, and not the pointer's.
        sheet.PointerOver(4, 0);
        Assert.Equal(new Spot(2, 5), sheet.Spot);
        Assert.True(sheet.DropOfKey());
        Assert.Null(sheet.Dragged);
        Assert.Equal(("C", new Spot(2, 5)), (sheet.ChordAt(2, 5), sheet.Spot));
        // One change, joined to nothing: and it is what the key 4 asks with the spot there.
        var (keyed, keyedSong) = vectors.Sheet("grace");
        keyed.SetSpot(2, 5);
        keyed.PutOfKey(4);
        Assert.Single(song.Asked);
        Assert.Equal(keyedSong.Asked, song.Asked);
        Assert.EndsWith("joined false", song.Asked[0], StringComparison.Ordinal);

        // In place of the chord that is there (rule 43).
        sheet.Carry("Em", 1, 8);
        Assert.True(sheet.DropOfKey());
        Assert.Equal(["C", "Em", "G", "G"], sheet.Rows[1].Chords.Select(chord => chord.Name).Order(StringComparer.Ordinal));

        // Off the places nothing shows it, and let go it is put nowhere.
        song.Asked.Clear();
        sheet.Carry("D", 7, 4);
        sheet.CarryOff();
        Assert.Null(sheet.Dragged);
        Assert.False(sheet.DropOfKey());
        // A row that is a group's name, a row there is not, and a name that is no chord.
        sheet.Carry("D", 7, 4);
        sheet.Carry("D", 5, 0);
        Assert.Null(sheet.Dragged);
        sheet.Carry("D", 99, 0);
        Assert.Null(sheet.Dragged);
        sheet.Carry("Hello", 7, 4);
        Assert.Null(sheet.Dragged);
        Assert.False(sheet.DropOfKey());
        Assert.Empty(song.Asked);
        Assert.Equal("", sheet.ChordAt(7, 4));

        // The key that copies a chord as it is dragged changes nothing here (rule 46g).
        sheet.Carry("D", 7, 4);
        sheet.Drop(copy: true);
        Assert.Null(sheet.Dragged);
        Assert.Equal("D", sheet.ChordAt(7, 4));
        Assert.Single(song.Asked);

        // A chord lifted from the sheet is not let go of by what is for one from the
        // strip, nor put on by it.
        sheet.Lift(7, 4);
        sheet.CarryOff();
        Assert.Equal(new Spot(7, 4), sheet.Dragged?.From);
        Assert.False(sheet.DropOfKey());
        Assert.NotNull(sheet.Dragged);
        sheet.DropNowhere();

        // While another chord is being typed nothing is carried; but picking one up
        // keeps what was typed and closes the bubble, and then it is (rule 46e). The
        // two are two changes, neither joined to the other.
        song.Asked.Clear();
        sheet.SetSpot(6, 5);
        sheet.StartTyping("B");
        sheet.Type("Bm7");
        sheet.Carry("C", 9, 0);
        Assert.Null(sheet.Dragged);
        Assert.True(sheet.Typing);
        sheet.LiftOfKey();
        Assert.False(sheet.Typing);
        Assert.Equal("Bm7", sheet.ChordAt(6, 5));
        Assert.Single(song.Asked);
        sheet.Carry("C", 9, 0);
        Assert.True(sheet.DropOfKey());
        Assert.Equal("C", sheet.ChordAt(9, 0));
        Assert.Equal(2, song.Asked.Count);
        Assert.EndsWith("joined false", song.Asked[1], StringComparison.Ordinal);
        // Picked up with nothing typed: nothing is kept, and the bubble is closed.
        sheet.SetSpot(8, 0);
        sheet.StartTyping();
        sheet.LiftOfKey();
        Assert.False(sheet.Typing);
        Assert.Equal("", sheet.ChordAt(8, 0));
        Assert.Equal(2, song.Asked.Count);

        // On a line of chords alone it takes the place of the chord whose place it is
        // over, or past the last is one more (rule 22); on a blank slide, and on a
        // slide with no text box, it is the first.
        var (intro, introSong) = vectors.Sheet("intro");
        intro.Carry("Bm", 1, 2);
        Assert.True(intro.DropOfKey());
        Assert.Equal(["D", "Bm", "A"], intro.Rows[1].Chords.Select(chord => chord.Name));
        intro.Carry("G", 1, 6);
        Assert.True(intro.DropOfKey());
        Assert.Equal([new Chord(0, "D"), new Chord(2, "Bm"), new Chord(4, "A"), new Chord(6, "G")], intro.Rows[1].Chords);
        Assert.Equal(2, introSong.Asked.Count);
        Assert.All(introSong.Asked, asked => Assert.True(asked.StartsWith("setChordsAlone", StringComparison.Ordinal) && asked.EndsWith("joined false", StringComparison.Ordinal), asked));
        var (blank, _) = vectors.Sheet("blank");
        blank.Carry("C", 1, 0);
        Assert.True(blank.DropOfKey());
        Assert.Equal([new Chord(0, "C")], blank.Rows[1].Chords);
        Assert.False(blank.Rows[1].Blank);
        var (bare, _) = vectors.Sheet("bare");
        bare.Carry("C", 2, 0);
        Assert.True(bare.DropOfKey());
        Assert.Equal([new Chord(0, "C")], bare.Rows[2].Chords);

        // What the song refuses is said, and is answered as not put on.
        var (refusing, refusingSong) = vectors.Sheet("grace");
        var said = 0;
        refusing.Refused = _ => said++;
        refusingSong.Refusing = Refusal.ProPresenterIsRunning;
        refusing.Carry("C", 2, 5);
        Assert.False(refusing.DropOfKey());
        Assert.Equal((1, "", null), (said, refusing.ChordAt(2, 5), refusing.Dragged));

        // The place it was over goes, the song having been changed under it: it is
        // carried no longer.
        var two = new StandInSong([Box(0, "A", "Holy holy"), Box(1, "B", "Lord God")], "C");
        var small = new ChordSheet(two);
        small.Carry("F", 1, 0);
        Assert.NotNull(small.Dragged);
        // (Read again with its place still there, it is carried still.)
        small.Reload();
        Assert.Equal((new Spot(-1, 0), "F"), small.Dragged);
        Assert.Equal(new Spot(1, 0), small.Spot);
        two.LoseItsLastBox();
        small.Reload();
        Assert.Null(small.Dragged);
        Assert.Equal(-1, small.Spot.Row);
    }

    // A name the song already has that is no chord is moved, copied and pasted as it is;
    // only what is typed must be a chord.
    [Fact]
    public void ANameTheSongAlreadyHasThatIsNoChordIsMovedCopiedAndPastedAsItIs()
    {
        var song = new StandInSong(
            [Box(0, "A", "Holy holy holy\nLord God Almighty", [new Chord(0, "N.C."), new Chord(5, "C /// | Am ///")])],
            "C");
        var sheet = new ChordSheet(song);
        sheet.Move(new Spot(0, 0), new Spot(1, 5), copy: false);
        Assert.Equal(("N.C.", ""), (sheet.ChordAt(1, 5), sheet.ChordAt(0, 0)));
        sheet.Move(new Spot(0, 5), new Spot(0, 10), copy: true);
        Assert.Equal(("C /// | Am ///", "C /// | Am ///"), (sheet.ChordAt(0, 10), sheet.ChordAt(0, 5)));
        sheet.SetSpot(0, 0);
        sheet.Copy(whole: false);
        sheet.SetSpot(1, 0);
        sheet.Paste();
        Assert.Equal("[\"C /// | Am ///\"@5, \"C /// | Am ///\"@9]", Spelt.Of(sheet.Rows[1].Chords));
        // Typed, it is refused, and nothing is asked.
        song.Asked.Clear();
        Assert.False(sheet.Put(0, 0, "N.C."));
        Assert.False(sheet.Put(0, 0, "hello"));
        Assert.Empty(song.Asked);
        // And a slash on the end of what is typed is left off; of a name the song had, not.
        Assert.True(sheet.Put(0, 0, "G/"));
        Assert.Equal("G", sheet.ChordAt(0, 0));
        Assert.True(sheet.Put(1, 0, "G/", typed: false));
        Assert.Equal("G/", sheet.ChordAt(1, 0));
    }

    // A paste answers with what it did: the lines given chords, the chords put, those
    // left out for want of a word, and the lines passed over.
    [Fact]
    public void APasteAnswersWithWhatItDid()
    {
        var (sheet, song) = vectors.Sheet("grace");
        // Nothing copied yet, and no spot: nothing to say.
        Assert.Null(sheet.Paste());
        Assert.Null(sheet.Copy(whole: false));
        sheet.SetSpot(1, 0);
        Assert.Null(sheet.Paste());
        sheet.Copy(whole: false);
        // Four chords, of the first, second, third and fifth words, onto a line of seven
        // words: all four.
        sheet.SetSpot(6, 0);
        Assert.Equal(new ChordSheet.Pasted(Lines: 1, Chords: 4), sheet.Paste());
        // Onto a line of three ("Amen and amen"): the fifth word's has none to go on.
        sheet.SetSpot(9, 0);
        Assert.Equal(new ChordSheet.Pasted(Lines: 1, Chords: 3, LeftOut: 1), sheet.Paste());
        // Onto a line of one word ("Amen"): one is put, three are left out.
        sheet.SetSpot(8, 0);
        Assert.Equal(new ChordSheet.Pasted(Lines: 1, Chords: 1, LeftOut: 3), sheet.Paste());
        // A whole group of four lines from the song's last line but one: it runs out of
        // lines, and what there are take what they have words for.
        sheet.SetSpot(1, 0);
        sheet.Copy(whole: true);
        sheet.SetSpot(8, 0);
        Assert.Equal(new ChordSheet.Pasted(Lines: 2, Chords: 2, LeftOut: 3), sheet.Paste());

        // The other kind of line is passed over, and says so.
        var (intro, _) = vectors.Sheet("intro");
        intro.SetSpot(3, 0);
        intro.Copy(whole: false);
        intro.SetSpot(1, 0);
        Assert.Equal(new ChordSheet.Pasted(PassedOver: 1), intro.Paste());
        intro.SetSpot(1, 0);
        intro.Copy(whole: false);
        intro.SetSpot(3, 0);
        Assert.Equal(new ChordSheet.Pasted(PassedOver: 1), intro.Paste());
        intro.SetSpot(6, 0);
        Assert.Equal(new ChordSheet.Pasted(Lines: 1, Chords: 3), intro.Paste());

        // What the song refuses is counted so.
        song.Refusing = Refusal.ProPresenterIsRunning;
        sheet.SetSpot(1, 0);
        sheet.Copy(whole: false);
        sheet.SetSpot(6, 0);
        Assert.Equal(new ChordSheet.Pasted(Refused: 1), sheet.Paste());
    }

    // A paste is one step of undo whichever of its lines is the first it changes (rule
    // 52).
    [Fact]
    public void APasteIsOneStepOfUndoWhicheverOfItsLinesIsTheFirstItChanges()
    {
        // A group of a line of chords alone and a line of words, onto a group of two
        // lines of words: the first copied line is passed over (the kinds differ), and
        // the one change that is made is not joined to whatever was done before it.
        var song = new StandInSong(
            [
                Box(0, "A", Chords.Placeholders(2), [new Chord(0, "D"), new Chord(2, "G")], group: "Verse 1", startsItsGroup: true),
                Box(1, "B", "Holy holy holy", [new Chord(5, "A")], group: "Verse 1"),
                Box(2, "C", "Lord God Almighty", group: "Verse 2", startsItsGroup: true),
                Box(3, "D", "Early in the morning", group: "Verse 2"),
            ],
            "D");
        var sheet = new ChordSheet(song);
        sheet.SetSpot(2, 0);
        sheet.Copy(whole: true);
        Assert.Equal(["alone [\"D\", \"G\"]", "words [\"A\" word 1+0]"], sheet.Copied!.Select(Said));
        sheet.SetSpot(4, 0);
        sheet.Paste();
        Assert.Equal(["setChords slide 3 box \"D\" [\"A\"@6] joined false"], song.Asked);
        // And where two lines are changed, the second is joined to the first.
        var (grace, asked) = vectors.Sheet("grace");
        grace.SetSpot(1, 0);
        grace.Copy(whole: true);
        grace.SetSpot(6, 0);
        asked.Asked.Clear();
        grace.Paste();
        Assert.True(asked.Asked.Count > 1);
        Assert.EndsWith("joined false", asked.Asked[0], StringComparison.Ordinal);
        Assert.All(asked.Asked.Skip(1), one => Assert.EndsWith("joined true", one, StringComparison.Ordinal));
    }

    // Whether a paste would take chords off: only where a line that what was copied
    // would go onto has chords of its own; a line that would be passed over loses
    // nothing; and nothing is asked of the song to find out (spec/03, rule 53g). MINE,
    // NOT A VECTOR: the Mac's `whetherAPasteWouldReplace`, case for case.
    [Fact]
    public void WhetherAPasteWouldTakeChordsOffTheLinesItWouldGoOnto()
    {
        var song = new StandInSong(
            [
                Box(0, "A", "Holy holy holy", [new Chord(0, "D")], "Verse 1", startsItsGroup: true),
                Box(1, "B", "Lord God Almighty", [new Chord(5, "A")], "Verse 1"),
                Box(2, "C", "Early in the morning", null, "Verse 2", startsItsGroup: true),
                Box(3, "D", "Our song shall rise", [new Chord(4, "G")], "Verse 2"),
                Box(4, "E", Chords.Placeholders(2), [new Chord(0, "D"), new Chord(2, "G")], "Ending", startsItsGroup: true),
                Box(5, "F", Chords.Placeholders(1), null, "Ending"),
            ],
            "D");
        var sheet = new ChordSheet(song);
        // (The rows: 0 the first verse's name, 1 and 2 its lines; 3, and 4 and 5; 6,
        // and 7 and 8, which are lines of chords alone, the second with none on it.)
        SheetRowKind[] kinds = [SheetRowKind.Group, SheetRowKind.Line, SheetRowKind.Line, SheetRowKind.Group, SheetRowKind.Line, SheetRowKind.Line, SheetRowKind.Group, SheetRowKind.Line, SheetRowKind.Line];
        Assert.Equal(kinds, sheet.Rows.Select(row => row.Kind));
        bool Would(int row)
        {
            sheet.SetSpot(row, 0);
            return sheet.PasteWouldReplace;
        }

        // Nothing copied yet: nothing would be put on, and so nothing taken off.
        Assert.False(Would(1));
        Assert.False(Would(4));

        // One line of words copied: onto a line that has chords, yes; onto one that
        // has none, no; onto a line of chords alone it is passed over, and that line
        // keeps its chords.
        sheet.SetSpot(1, 0);
        sheet.Copy(whole: false);
        Assert.True(Would(1));
        Assert.True(Would(2));
        Assert.True(Would(5));
        Assert.False(Would(4));
        Assert.False(Would(7));
        Assert.False(Would(8));

        // A whole verse of two lines: it goes onto the spot's line and the next, so it
        // is yes from a line that has none if the line after it has.
        sheet.SetSpot(1, 0);
        sheet.Copy(whole: true);
        Assert.Equal(2, sheet.Copied?.Count);
        Assert.True(Would(4));
        // From the song's last line of words, its second line would go to a line of
        // chords alone and be passed over: only the first counts.
        Assert.True(Would(5));
        // From the last line of all there is one line to go onto, and it is of the
        // other kind.
        Assert.False(Would(8));

        // A line of chords alone copied: onto one that has chords, yes; onto the one
        // that has none, no; and a line of words keeps its own.
        sheet.SetSpot(7, 0);
        sheet.Copy(whole: false);
        Assert.True(Would(7));
        Assert.False(Would(8));
        Assert.False(Would(1));
        Assert.False(Would(5));

        // No spot: no. (The spot cannot be set on a group's name; a sheet with none
        // has it on no row.)
        var bare = new ChordSheet(new StandInSong([Box(0, "A", "", null, "Verse 1", startsItsGroup: true)], "D"));
        Assert.False(bare.PasteWouldReplace);
        // And the song was asked nothing by any of it.
        Assert.Empty(song.Asked);

        // What it says is what a paste then does: every line it changes that had
        // chords, it took them off.
        sheet.SetSpot(1, 0);
        sheet.Copy(whole: false);
        sheet.SetSpot(5, 0);
        Assert.True(sheet.PasteWouldReplace);
        Assert.Equal(new ChordSheet.Pasted(Lines: 1, Chords: 1), sheet.Paste());
        Assert.Equal([new Chord(0, "D")], sheet.Rows[5].Chords);
    }

    // What the song refuses is said, changes nothing, and is not counted as done.
    [Fact]
    public void WhatTheSongRefusesIsSaidChangesNothingAndIsNotCountedAsDone()
    {
        var (sheet, song) = vectors.Sheet("grace");
        var said = new List<Refusal>();
        var changes = 0;
        sheet.Refused = refusal => said.Add(refusal);
        sheet.Changed = () => changes++;
        song.Refusing = Refusal.ProPresenterIsRunning;
        sheet.SetSpot(1, 13);
        Assert.False(sheet.Put(1, 13, "Em"));
        Assert.Equal((1, 0, ""), (said.Count, changes, sheet.ChordAt(1, 13)));
        Assert.Same(song.Refusing, said[0]);
        // A chord moved to another line that is refused there is not taken from here.
        sheet.Move(new Spot(1, 8), new Spot(6, 0), copy: false);
        Assert.Equal((2, 2, "G7"), (said.Count, song.Asked.Count, sheet.ChordAt(1, 8)));
        song.Refusing = null;
        Assert.True(sheet.Put(1, 13, "Em"));
        Assert.Equal((2, 1), (said.Count, changes));
    }

    // After a copy, the key that pastes is named as the platform names it.
    [Fact]
    public void AfterACopyTheKeyThatPastesIsNamedAsThePlatformNamesIt()
    {
        var (sheet, _) = vectors.Sheet("grace");
        Assert.Equal("Ctrl+V", sheet.PasteKeys);
        sheet.SetSpot(2, 0);
        Assert.Equal("1 chord copied from the line: Ctrl+V puts them on the lines from the spot's on, word for word", sheet.Copy(whole: false));
        sheet.PasteKeys = "Command+V";
        Assert.Equal("8 chords copied from 4 lines: Command+V puts them on the lines from the spot's on, word for word", sheet.Copy(whole: true));
    }

    // When the song is made again from under the sheet (an undo, a version put back),
    // what was being done to a line that has gone is let go.
    [Fact]
    public void WhenTheSongChangesFromElsewhereWhatWasBeingDoneToALineThatHasGoneIsLetGo()
    {
        var boxes = new List<TextBox>
        {
            Box(0, "A", "Holy holy holy", [new Chord(0, "D")]),
            Box(1, "B", "Lord God Almighty", [new Chord(5, "G")]),
        };
        var song = new Shrinking(boxes);
        var sheet = new ChordSheet(song);
        sheet.SetSpot(1, 5);
        sheet.StartTyping("A");
        sheet.Lift(1, 5);
        Assert.True(sheet.Typing);
        // (A chord cannot be carried while another is typed: lift it with none typed.)
        sheet.Press(ChordSheet.EntryKey.Escape);
        sheet.Lift(1, 5);
        sheet.StartTyping("A");
        Assert.NotNull(sheet.Dragged);
        // The second slide goes from the song.
        song.Boxes = [boxes[0]];
        sheet.Reload();
        Assert.Equal(-1, sheet.Spot.Row);
        Assert.False(sheet.Typing);
        Assert.Null(sheet.Dragged);
        // And a chord that is no longer the one that was lifted is not carried on.
        sheet.Lift(0, 0);
        song.Boxes = [boxes[0] with { Chords = [new Chord(0, "Em")] }];
        sheet.Reload();
        Assert.Null(sheet.Dragged);
    }

    // ---- Cases written after each rule was broken in turn (NOTES.md, "At M6")

    // A line that begins with spaces has its first stop at its first word: that is
    // where the first arrow key goes, and what a place before it comes to.
    [Fact]
    public void ALineThatBeginsWithSpacesHasItsFirstStopAtItsFirstWord()
    {
        var song = new StandInSong([Box(0, "A", "  Holy holy", [new Chord(2, "D")]), Box(1, "B", "Lord God")], "D");
        var sheet = new ChordSheet(song);
        Assert.Equal([2, 7], sheet.Stops(0));
        Assert.Equal(2, sheet.StopAt(0, 0));
        Assert.Equal(2, sheet.StopAt(0, 1));
        Assert.Equal(2, sheet.StopAt(0, 5));
        Assert.Equal(7, sheet.StopAt(0, 7));
        sheet.Step(1, exact: false);
        Assert.Equal(new Spot(0, 2), sheet.Spot);
    }

    // The spot is only ever on a row that can take it.
    [Fact]
    public void TheSpotCannotBeSetOnAGroupsNameNorOnAnEmptyLineNorOnARowThereIsNot()
    {
        var (sheet, _) = vectors.Sheet("grace");
        sheet.SetSpot(1, 8);
        sheet.SetSpot(0, 0);
        Assert.Equal(new Spot(1, 8), sheet.Spot);
        sheet.SetSpot(99, 0);
        sheet.SetSpot(-1, 0);
        Assert.Equal(new Spot(1, 8), sheet.Spot);
        var gapped = new ChordSheet(new StandInSong([Box(0, "A", "One\n\nThree")], "D"));
        gapped.SetSpot(0, 0);
        gapped.SetSpot(1, 0);
        Assert.Equal(new Spot(0, 0), gapped.Spot);
        gapped.SetSpot(2, 0);
        Assert.Equal(new Spot(2, 0), gapped.Spot);
    }

    // With Shift the spot goes a character at a time; but on a line of chords alone its
    // places are its chords and one more, with Shift or without: a stand-in is no
    // character to stop on.
    [Fact]
    public void WithShiftTheSpotOnALineOfChordsAloneStillGoesFromChordToChord()
    {
        var song = new StandInSong([Box(0, "A", Chords.Placeholders(3), [new Chord(0, "D"), new Chord(2, "G"), new Chord(4, "A")])], "D");
        var sheet = new ChordSheet(song);
        sheet.SetSpot(0, 0);
        sheet.Step(1, exact: true);
        Assert.Equal(new Spot(0, 2), sheet.Spot);
        sheet.Step(1, exact: true);
        sheet.Step(1, exact: true);
        Assert.Equal(new Spot(0, 6), sheet.Spot);
        sheet.Step(-1, exact: true);
        Assert.Equal(new Spot(0, 4), sheet.Spot);
    }

    // The keys that move the spot pick its slide and ask for it to be brought into
    // view (rule 23). The pointer does neither.
    [Fact]
    public void MovingTheSpotWithTheKeysPicksItsSlideAndAsksForItToBeBroughtIntoView()
    {
        var (sheet, _) = vectors.Sheet("grace");
        var asked = 0;
        sheet.Reveal = () => asked++;
        sheet.PointerOver(6, 0);
        Assert.Equal((0, 0), (sheet.Picked, asked));
        // Right, along a line of the first slide.
        sheet.SetSpot(1, 0);
        sheet.Step(1, exact: false);
        Assert.Equal((0, 1), (sheet.Picked, asked));
        // Right from the slide's last word, on to the next slide.
        sheet.SetSpot(2, sheet.Stops(2)[^1]);
        sheet.Step(1, exact: false);
        Assert.Equal(3, sheet.Spot.Row);
        Assert.Equal((sheet.Rows[3].Slide, 2), (sheet.Picked, asked));
        Assert.NotEqual(sheet.Rows[2].Slide, sheet.Rows[3].Slide);
        // Left, with Shift, back to the slide before.
        sheet.Step(-1, exact: true);
        Assert.Equal(2, sheet.Spot.Row);
        Assert.Equal((sheet.Rows[2].Slide, 3), (sheet.Picked, asked));
        // Down, and Up.
        sheet.StepLine(1, new FixedWidths());
        Assert.Equal((sheet.Rows[3].Slide, 4), (sheet.Picked, asked));
        sheet.StepLine(-1, new FixedWidths());
        Assert.Equal((sheet.Rows[2].Slide, 5), (sheet.Picked, asked));
    }

    // Tab keeps the chord and moves the spot on from the place it was typed for,
    // wherever the pointer has been meanwhile (rules 33 and 38).
    [Fact]
    public void TabMovesTheSpotOnFromWhereTheChordWasTypedNotFromWhereThePointerIs()
    {
        var (sheet, _) = vectors.Sheet("grace");
        sheet.SetSpot(2, 5);
        sheet.StartTyping("A");
        // The pointer goes off to another slide, and the spot with it.
        sheet.PointerOver(6, 0);
        Assert.Equal((new Spot(6, 0), new Spot(2, 5)), (sheet.Spot, sheet.TypingAt));
        Assert.True(sheet.Press(ChordSheet.EntryKey.Tab));
        Assert.Equal("A", sheet.ChordAt(2, 5));
        // "That saved a wretch like me": the word after "saved" is "a", at 11.
        Assert.Equal(new Spot(2, 11), sheet.Spot);
        Assert.False(sheet.Typing);
    }

    // A click on a place picks its slide (rule 7a), as the keys do, and opens the
    // bubble there.
    [Fact]
    public void AClickOnAPlacePicksItsSlide()
    {
        var (sheet, _) = vectors.Sheet("grace");
        Assert.Equal(0, sheet.Picked);
        sheet.Pick(6, 0);
        Assert.Equal(sheet.Rows[6].Slide, sheet.Picked);
        Assert.NotEqual(0, sheet.Picked);
        Assert.True(sheet.Typing);
        Assert.Equal(new Spot(6, 0), sheet.TypingAt);
        // A click on a row that can take nothing picks nothing, and opens nothing.
        sheet.Press(ChordSheet.EntryKey.Escape);
        sheet.Pick(0, 0);
        Assert.Equal(sheet.Rows[6].Slide, sheet.Picked);
        Assert.False(sheet.Typing);
    }

    // Tab in an empty box takes the one that is picked of what is offered, and the
    // first only where none is (rule 33).
    [Fact]
    public void TabInAnEmptyBoxTakesTheOnePickedAndTheFirstOnlyWhereNoneIs()
    {
        var (sheet, _) = vectors.Sheet("grace");
        sheet.SetSpot(2, 5);
        sheet.StartTyping("");
        var offered = sheet.Offered;
        Assert.True(offered.Count > 2);
        sheet.Press(ChordSheet.EntryKey.Down);
        sheet.Press(ChordSheet.EntryKey.Down);
        sheet.Press(ChordSheet.EntryKey.Down);
        Assert.Equal(2, sheet.Entry.Pick);
        sheet.Press(ChordSheet.EntryKey.Tab);
        Assert.Equal(offered[2], sheet.ChordAt(2, 5));
        Assert.NotEqual(offered[0], offered[2]);
        // With none picked, the first.
        sheet.SetSpot(2, 13);
        sheet.StartTyping("");
        sheet.Press(ChordSheet.EntryKey.Tab);
        Assert.Equal(sheet.ChordAt(2, 13), offered[0]);
    }

    // What parts words is what JavaScript calls white space (the reference's sheet
    // parts a line with its \S+), unit for unit: so a space that does not break parts
    // two words as a plain one does, and a zero-width space, which the stand-ins of a
    // line of chords alone are, parts nothing. The Mac app has the same list.
    [Fact]
    public void WhatPartsWordsIsWhatJavaScriptCallsWhiteSpaceUnitForUnit()
    {
        int[] spaces =
        [
            0x0009, 0x000A, 0x000B, 0x000C, 0x000D, 0x0020, 0x00A0, 0x1680,
            0x2000, 0x2001, 0x2002, 0x2003, 0x2004, 0x2005, 0x2006, 0x2007, 0x2008, 0x2009, 0x200A,
            0x2028, 0x2029, 0x202F, 0x205F, 0x3000, 0xFEFF,
        ];
        for (var unit = 0; unit <= 0xFFFF; unit++)
        {
            var parted = ChordSheet.WordStarts(new string(['a', (char)unit, 'b'])).Count == 2;
            Assert.True(parted == spaces.Contains(unit), "U+" + Convert.ToString(unit, 16));
        }
    }

    // Of two stops as near, Down takes the one further left.
    [Fact]
    public void OfTwoStopsAsNearDownTakesTheOneFurtherLeft()
    {
        // Each character is 10 wide. The spot is 50 along; the line under it has its
        // stops at 0 and at 100.
        var sheet = new ChordSheet(new StandInSong([Box(0, "A", "abcde fgh\nabcdefghi jkl")], "D"));
        Assert.Equal([0, 10], sheet.Stops(1));
        sheet.SetSpot(0, 5);
        sheet.StepLine(1, new FixedWidths());
        Assert.Equal(new Spot(1, 0), sheet.Spot);
    }

    // A chord's place along its line is where its bubble stands, which is right of its
    // character where the bubble before it is in the way (rule 11).
    [Fact]
    public void WhereAChordIsAlongItsLineIsWhereItsBubbleWasPushedTo()
    {
        var sheet = new ChordSheet(new StandInSong([Box(0, "A", "Holy holy holy", [new Chord(0, "D"), new Chord(1, "G")])], "D"));
        var widths = new FixedWidths();
        // D's bubble is 22 wide from 0. G's character starts at 10, and its bubble is
        // pushed to 26, to clear D's by 4.
        Assert.Equal([0.0, 26.0], sheet.Placed(0, widths).Xs);
        Assert.Equal(26, sheet.XOf(0, 1, widths));
        Assert.Equal(0, sheet.XOf(0, 0, widths));
        // A place with no chord is where its character is.
        Assert.Equal(20, sheet.XOf(0, 2, widths));
    }

    // A chord past its line's end, or before its start, stands at the end or the start.
    [Fact]
    public void AChordPastItsLinesEndStandsAtTheLinesEnd()
    {
        var past = ChordLayout.Place(5, [new Chord(99, "G")], units => 10.0 * units, _ => 22, 4);
        Assert.Equal([50.0], past.Xs);
        Assert.Equal((72.0, 50.0), (past.Width, past.WordsWidth));
        var before = ChordLayout.Place(5, [new Chord(-3, "G")], units => 10.0 * units, _ => 22, 4);
        Assert.Equal([0.0], before.Xs);
    }

    // Nothing is put on where there is no name to put.
    [Fact]
    public void AChordOfNoNameIsNotPutOnHoweverItIsAsked()
    {
        var (sheet, song) = vectors.Sheet("grace");
        Assert.False(sheet.Put(1, 13, "", typed: false));
        Assert.False(sheet.Put(1, 13, "", typed: true));
        Assert.False(sheet.Put(1, 13, "/", typed: true));
        Assert.Empty(song.Asked);
        Assert.Equal("", sheet.ChordAt(1, 13));
    }

    // An empty line among a slide's words is no line of its group when the group is
    // copied, and a paste goes on past it, and past a group's name, to the next line
    // that can take chords (rules 47 and 50).
    [Fact]
    public void AnEmptyLineAmongTheWordsIsNeitherCopiedNorPastedOntoAndAPasteGoesOnPastAGroupsName()
    {
        var song = new StandInSong(
            [
                Box(0, "A", "Holy holy\n\nLord God", [new Chord(0, "D"), new Chord(11, "G")], group: "Verse 1", startsItsGroup: true),
                Box(1, "B", "Early in\n\nOur song", group: "Verse 2", startsItsGroup: true),
            ],
            "D");
        var sheet = new ChordSheet(song);
        Assert.Equal(["Verse 1", "Holy holy", "", "Lord God", "Verse 2", "Early in", "", "Our song"], sheet.Rows.Select(row => row.Kind == SheetRowKind.Group ? row.Name : row.Text));
        sheet.SetSpot(1, 0);
        Assert.Equal("2 chords copied from 2 lines: Ctrl+V puts them on the lines from the spot's on, word for word", sheet.Copy(whole: true));
        Assert.Equal(["words [\"D\" word 0+0]", "words [\"G\" word 0+0]"], sheet.Copied!.Select(Said));
        // Pasted from the second verse's first line: the second copied line goes past
        // the empty line, to the line after it.
        sheet.SetSpot(5, 0);
        Assert.Equal(new ChordSheet.Pasted(Lines: 2, Chords: 2), sheet.Paste());
        Assert.Equal([new Chord(0, "D")], sheet.Rows[5].Chords.ToList());
        Assert.Empty(sheet.Rows[6].Chords);
        Assert.Equal([new Chord(0, "G")], sheet.Rows[7].Chords.ToList());
        // Pasted from the first verse's last line: the second copied line goes past the
        // second verse's name, to its first line.
        sheet.SetSpot(3, 0);
        Assert.Equal(new ChordSheet.Pasted(Lines: 2, Chords: 2), sheet.Paste());
        Assert.Equal([new Chord(0, "D")], sheet.Rows[3].Chords.ToList());
        Assert.Equal([new Chord(0, "G")], sheet.Rows[5].Chords.ToList());
    }

    // Lines of chords alone pasted together are one step of undo, as lines of words are
    // (rule 52).
    [Fact]
    public void LinesOfChordsAlonePastedTogetherAreOneStepOfUndo()
    {
        var song = new StandInSong(
            [
                Box(0, "A", Chords.Placeholders(2), [new Chord(0, "D"), new Chord(2, "G")], group: "Intro", startsItsGroup: true),
                Box(1, "B", Chords.Placeholders(1), [new Chord(0, "A")], group: "Intro"),
                Box(2, "C", Chords.Placeholders(1), [new Chord(0, "E")], group: "Outro", startsItsGroup: true),
                Box(3, "D", Chords.Placeholders(1), [new Chord(0, "E")], group: "Outro"),
            ],
            "D");
        var sheet = new ChordSheet(song);
        sheet.SetSpot(1, 0);
        sheet.Copy(whole: true);
        Assert.Equal(["alone [\"D\", \"G\"]", "alone [\"A\"]"], sheet.Copied!.Select(Said));
        sheet.SetSpot(4, 0);
        song.Asked.Clear();
        Assert.Equal(new ChordSheet.Pasted(Lines: 2, Chords: 3), sheet.Paste());
        Assert.Equal(
            [
                "setChordsAlone slide 2 box \"C\" line 0 [\"D\", \"G\"] joined false",
                "setChordsAlone slide 3 box \"D\" line 0 [\"A\"] joined true",
            ],
            song.Asked);
    }

    // A song whose text boxes a test changes from under the sheet.
    private sealed class Shrinking(IReadOnlyList<TextBox> boxes) : ISheetSong
    {
        public IReadOnlyList<TextBox> Boxes { get; set; } = boxes;

        public IReadOnlyList<TextBox> TextBoxes => Boxes;

        public IReadOnlyList<string> UsedChords => [.. Boxes.SelectMany(box => box.Chords.Select(chord => chord.Name))];

        public string Key => "D";

        public void SetChords(int slide, string element, IReadOnlyList<Chord> chords, bool joined)
        {
        }

        public void SetChordsAlone(int slide, string element, int line, IReadOnlyList<string> names, bool joined)
        {
        }
    }
}
