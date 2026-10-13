// The line of hints under the chord sheet, and the highlighted line
// (spec/03-chord-sheet.md, rules 53a to 53h; spec/06-library.md, rules 6e, 12 and 20a).
//
//   - what it says, to the letter, for each thing that can be going on (SheetHints.cs):
//     the page's table, with the keys named as Windows names them;
//   - how its hints are fitted into the width there is, by arithmetic (HintFit.cs);
//   - that what is going on is read off the sheet, and which line is the highlighted
//     one;
//   - that the sheet's element says so as a hand moves over it, and the line under it
//     then says the right thing (ChordSheetCanvas.cs, HintLine.cs);
//   - that the highlighted line is drawn in its colour, and only the slides it left and
//     came to are drawn again;
//   - that a change of what the line says tells nothing else in the window, lays out
//     nothing, and lights the line up, unless Windows is set to show no animations.
//
// NEW, and so none of it is a vector's. The words are the page's and the maintainer's;
// the cases are the Mac's (SheetHintsTests.swift) with Windows' keys, and where a case
// is of this app's own way of drawing it is MINE.
//
// What only a hand can tell is not here: how it looks as it lights up and fades, and
// whether Ctrl going down is told at once while the pointer lies still.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Song;
using Xunit;
using Hint = Pro7ChordEditor.AppViews.SheetHints.Hint;
using Situation = Pro7ChordEditor.AppViews.SheetHints.Situation;

namespace Pro7ChordEditor.AppViews.Tests;

public class SheetHintsTests
{
    private static readonly string Apostrophe = ((char)0x2019).ToString();
    private static readonly string Dash = ((char)0x2013).ToString();

    private static void OnAMadeUpPC(Func<MadeUpPC, Task> what) => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        await what(pc);
    });

    private static async Task<(LibraryModel Model, LibraryView View, Hand Hand)> Open(MadeUpPC pc, string name, double width = 1280, double height = 800, bool dark = true)
    {
        var model = pc.Model();
        var view = new LibraryView(model);
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        model.Open(pc.Song(name));
        Look.Wear(view, dark);
        Pictures.LayOut(view, width, height);
        return (model, view, new Hand(view.SongPane.ChordSheet.Canvas));
    }

    // Hints as they would be read out, one to a line.
    private static string Read(IEnumerable<Hint> hints) => string.Join("\n", hints.Select(hint => hint.Read));

    private static string Said(LibraryModel model) => Read(model.SheetHintsNow);

    // ---- What it says (rule 53b), to the letter

    [Fact]
    public void OnAChordAndOnAPlaceWithNone()
    {
        var onChord = SheetHints.Hints(Situation.OnAChord);
        Assert.Equal(
            "Delete Takes this chord off\n"
            + "1 " + Dash + " 7 Change it\n"
            + "You can drag this chord to move it anywhere. (Hold Ctrl to drag a copy)\n"
            + "Click to type over it",
            Read(onChord));
        // The keys are apart from what they do, to be drawn as keys.
        Assert.Equal(["Delete"], onChord[0].Keys);
        Assert.Equal("Takes this chord off", onChord[0].Says);
        Assert.Empty(onChord[2].Keys);

        var onPlace = SheetHints.Hints(Situation.OnAPlace);
        Assert.Equal("1 " + Dash + " 7 Put one of the key" + Apostrophe + "s chords here\nA " + Dash + " G Or a click: type one", Read(onPlace));
        // None of these is one that must be said whatever the room.
        Assert.All(onChord.Concat(onPlace), hint => Assert.False(hint.Needed));
    }

    [Fact]
    public void WhileAChordIsTyped()
    {
        const string usual = "Enter Keeps it\nTab Keeps it and goes on to the next word\nUp Down Choose from what is offered\nEsc Throws it away\nA click on another word keeps it too";
        Assert.Equal(usual, Read(SheetHints.Hints(Situation.Typing(onChord: false, empty: true))));
        Assert.Equal(usual, Read(SheetHints.Hints(Situation.Typing(onChord: false, empty: false))));
        Assert.Equal(usual, Read(SheetHints.Hints(Situation.Typing(onChord: true, empty: false))));
        Assert.Equal(["Up", "Down"], SheetHints.Hints(Situation.Typing(onChord: false, empty: false))[2].Keys);
        // With the box empty on a chord: that Enter takes the chord off (rule 35a).
        Assert.Equal(
            "Enter Takes this chord off\nEsc Leaves it as it was\nOr type the chord that is to be here",
            Read(SheetHints.Hints(Situation.Typing(onChord: true, empty: true))));
        // Whatever is held, and wherever the pointer is.
        Assert.Equal(usual, Read(SheetHints.Hints(Situation.Typing(onChord: false, empty: false), ctrl: true, overStrip: true)));
    }

    [Fact]
    public void WhileAChordIsCarried()
    {
        Assert.Equal(
            "Let go on a word to move it there\nCtrl Held as you let go, leaves a copy behind",
            Read(SheetHints.Hints(Situation.Carrying(fromStrip: false))));
        // With Ctrl held, which is Windows' key that copies: that it will be a copy.
        Assert.Equal("Let go on a word to put a copy there", Read(SheetHints.Hints(Situation.Carrying(fromStrip: false), ctrl: true)));
        // One of the key's chords, out of the strip: whatever is held (rule 46g).
        Assert.Equal("Let go on a word to put this chord there", Read(SheetHints.Hints(Situation.Carrying(fromStrip: true))));
        Assert.Equal("Let go on a word to put this chord there", Read(SheetHints.Hints(Situation.Carrying(fromStrip: true), ctrl: true)));
    }

    [Fact]
    public void WithCtrlHeldAtALineHowItsChordsAreCopiedAndPasted()
    {
        const string copy = "Press Ctrl+C to copy all chords on the highlighted line.";
        const string paste = "Press Ctrl+V to paste previously copied chords to the highlighted line";
        foreach (var at in new[] { Situation.OnAChord, Situation.OnAPlace })
        {
            var said = SheetHints.Hints(at, ctrl: true);
            Assert.Equal(copy + "\n" + paste, Read(said));
            // "Press", then the key as a key, then the rest.
            Assert.Equal(("Press", "Ctrl+C"), (said[0].Lead, said[0].Keys.Single()));
            Assert.Equal(("Press", "Ctrl+V"), (said[1].Lead, said[1].Keys.Single()));
            // Both are said whatever the room.
            Assert.All(said, hint => Assert.True(hint.Needed));
            // Where a paste would take chords off (rule 53g).
            Assert.Equal(copy + "\n" + paste + " (Replaces existing chords)", Read(SheetHints.Hints(at, ctrl: true, paste: new SheetHints.Paste(1, Replaces: true))));
            Assert.Equal(copy + "\n" + paste + " (Replaces existing chords)", Read(SheetHints.Hints(at, ctrl: true, paste: new SheetHints.Paste(null, Replaces: true))));
            // Where a whole verse was copied: onto the highlighted line and those after.
            Assert.Equal(
                copy + "\n" + paste + " and the lines after it",
                Read(SheetHints.Hints(at, ctrl: true, paste: new SheetHints.Paste(4))));
            Assert.Equal(
                copy + "\n" + paste + " and the lines after it (Replaces existing chords)",
                Read(SheetHints.Hints(at, ctrl: true, paste: new SheetHints.Paste(2, Replaces: true))));
        }
        // Ctrl+Shift+C is said nowhere on the line.
        Assert.DoesNotContain("Shift", Read(SheetHints.Hints(Situation.OnAChord, ctrl: true)), StringComparison.Ordinal);
    }

    [Fact]
    public void WithNobodyAtTheSheetOneThingIsSaidWhateverIsHeld()
    {
        const string idle = "Point at a word or a chord, and this line says what the keys do there.";
        Assert.Equal(idle, Read(SheetHints.Hints(Situation.Nothing)));
        // (There is no highlighted line for the copying to be about.)
        Assert.Equal(idle, Read(SheetHints.Hints(Situation.Nothing, ctrl: true, paste: new SheetHints.Paste(3, Replaces: true))));
    }

    [Fact]
    public void OverTheKeysChordsInTheStrip()
    {
        const string strip = "You can drag these down to the text on any slide";
        // Whatever the spot was left on, and whatever is held.
        Assert.Equal(strip, Read(SheetHints.Hints(Situation.Nothing, overStrip: true)));
        Assert.Equal(strip, Read(SheetHints.Hints(Situation.OnAChord, overStrip: true)));
        Assert.Equal(strip, Read(SheetHints.Hints(Situation.OnAPlace, ctrl: true, overStrip: true)));
        // But not while a chord is carried.
        Assert.Equal("Let go on a word to put this chord there", Read(SheetHints.Hints(Situation.Carrying(fromStrip: true), overStrip: true)));
    }

    // "Every hint begins with a capital, as a sentence does, whether or not it is a
    // whole sentence" (rule 53e), and none of them is said by the clock: these are all
    // there are.
    [Fact]
    public void EveryHintBeginsWithACapital()
    {
        IReadOnlyList<Hint>[] all =
        [
            SheetHints.Hints(Situation.Nothing), SheetHints.Hints(Situation.OnAChord), SheetHints.Hints(Situation.OnAPlace),
            SheetHints.Hints(Situation.OnAChord, ctrl: true, paste: new SheetHints.Paste(3, Replaces: true)),
            SheetHints.Hints(Situation.Typing(onChord: true, empty: true)), SheetHints.Hints(Situation.Typing(onChord: false, empty: false)),
            SheetHints.Hints(Situation.Carrying(fromStrip: true)), SheetHints.Hints(Situation.Carrying(fromStrip: false)),
            SheetHints.Hints(Situation.Carrying(fromStrip: false), ctrl: true),
            SheetHints.Strip, SheetHints.Idle, SheetHints.CannotChange,
        ];
        foreach (var hint in all.SelectMany(hints => hints))
        {
            var first = hint.Lead.Length > 0 ? hint.Lead : hint.Says;
            // (A sentence that goes on after its key begins with its "Press".)
            if (hint.Lead.Length > 0)
            {
                Assert.True(char.IsLower(hint.Says[0]), hint.Read);
            }
            Assert.True(char.IsUpper(first[0]), hint.Read);
            // No word of one promises help, or calls the app a product.
            Assert.DoesNotContain("help", hint.Read, StringComparison.OrdinalIgnoreCase);
        }
        Assert.Equal("Nothing can be changed here while ProPresenter is running.", Read(SheetHints.CannotChange));
    }

    // ---- How they are fitted (rule 53a)

    // A measure that is no font's: every letter seven wide. So the sums can be done here.
    private static double Seven(string words, double size, bool onAKey) => words.Length * 7 * size / (onAKey ? HintFit.KeySize : HintFit.WordSize);

    [Fact]
    public void AKeyAndAHintAreAsWideAsTheirPartsAndTheRoomBetweenThem()
    {
        // A key: its sign and six either side; never narrower than a key is.
        Assert.Equal((6 * 7) + 12, HintFit.WidthOfKey("Delete", 1, Seven));
        Assert.Equal(HintFit.KeyLeast, HintFit.WidthOfKey("X", 1, Seven));
        // A hint: its lead, its keys, what it says, and five between each two.
        Assert.Equal((5 * 7) + 5 + ((6 * 7) + 12) + 5 + (4 * 7), HintFit.WidthOf(new Hint("Press", ["Ctrl+C"], "abcd"), 1, Seven));
        Assert.Equal(4 * 7, HintFit.WidthOf(new Hint("abcd"), 1, Seven));
        Assert.Equal(HintFit.KeyLeast + 5 + HintFit.KeyLeast + 5 + 7, HintFit.WidthOf(new Hint(["A", "B"], "x"), 1, Seven));
        // Side by side: eighteen between two hints.
        IReadOnlyList<Hint> two = [new Hint("abcd"), new Hint("ab")];
        Assert.Equal(28 + 18 + 14, HintFit.WidthOf(two, 2, 1, Seven));
        Assert.Equal(28, HintFit.WidthOf(two, 1, 1, Seven));
    }

    [Fact]
    public void WithRoomForAllItsHintsTheLineSaysThemAllAndWithLessTheLastAreLeftOffAndTheFirstIsAlwaysSaid()
    {
        IReadOnlyList<Hint> hints = [new Hint("aaaaaaaaaa"), new Hint("bbbbbbbbbb"), new Hint("cccccccccc")];
        // Each is 70 wide: three are 246, two 158.
        Assert.Equal(new HintFit.Fit(3, false, 1), HintFit.Of(hints, 246, Seven));
        Assert.Equal(new HintFit.Fit(2, false, 1), HintFit.Of(hints, 245, Seven));
        Assert.Equal(new HintFit.Fit(2, false, 1), HintFit.Of(hints, 158, Seven));
        Assert.Equal(new HintFit.Fit(1, false, 1), HintFit.Of(hints, 157, Seven));
        // With no room even for the first, it is said all the same, and cut off.
        Assert.Equal(new HintFit.Fit(1, false, 1), HintFit.Of(hints, 10, Seven));
        Assert.Equal(new HintFit.Fit(1, false, 1), HintFit.Of(hints, 0, Seven));
        Assert.Equal(new HintFit.Fit(0, false, 1), HintFit.Of([], 500, Seven));
        // The line is no taller for any of it.
        Assert.All(new[] { 246.0, 158, 10 }, room => Assert.Equal(HintFit.Height, HintFit.HeightOf(HintFit.Of(hints, room, Seven))));
    }

    [Fact]
    public void TheTwoSentencesOnCopyingAndPastingAreNeverLeftOffSideBySideThenOneUnderTheOtherThenSmallerThenCutOff()
    {
        // 100 and 200 wide as they come.
        IReadOnlyList<Hint> hints = [new Hint("", [], new string('a', 100 / 7 + 1), Needed: true), new Hint("", [], new string('b', 200 / 7 + 1), Needed: true)];
        var first = HintFit.WidthOf(hints[0], 1, Seven);
        var second = HintFit.WidthOf(hints[1], 1, Seven);
        Assert.Equal(new HintFit.Fit(2, false, 1), HintFit.Of(hints, first + 18 + second, Seven));
        // Not side by side: one under the other, as they come, and the line taller.
        var under = HintFit.Of(hints, first + 18 + second - 1, Seven);
        Assert.Equal(new HintFit.Fit(2, true, 1), under);
        Assert.Equal((2 * HintFit.KeyHeight) + HintFit.Lines + (2 * HintFit.OverAndUnder), HintFit.HeightOf(under));
        Assert.True(HintFit.HeightOf(under) > HintFit.Height);
        Assert.Equal(new HintFit.Fit(2, true, 1), HintFit.Of(hints, second, Seven));
        // The longer of them too long as it comes: nine tenths, then eight.
        Assert.Equal(new HintFit.Fit(2, true, 0.9), HintFit.Of(hints, second - 1, Seven));
        Assert.Equal(new HintFit.Fit(2, true, 0.9), HintFit.Of(hints, HintFit.WidthOf(hints[1], 0.9, Seven), Seven));
        Assert.Equal(new HintFit.Fit(2, true, 0.8), HintFit.Of(hints, HintFit.WidthOf(hints[1], 0.9, Seven) - 1, Seven));
        // And at their smallest they are cut off, both said still.
        Assert.Equal(new HintFit.Fit(2, true, 0.8), HintFit.Of(hints, 20, Seven));
        // A hint that is not needed, after two that are, is the one left off.
        IReadOnlyList<Hint> three = [hints[0], hints[1], new Hint("cccc")];
        Assert.Equal(new HintFit.Fit(3, false, 1), HintFit.Of(three, first + 18 + second + 18 + 28, Seven));
        Assert.Equal(new HintFit.Fit(2, false, 1), HintFit.Of(three, first + 18 + second + 18 + 27, Seven));
        Assert.Equal(new HintFit.Fit(2, true, 1), HintFit.Of(three, first + 18 + second - 1, Seven));
    }

    // With the font the line is drawn in: every set of hints there is, in a pane as wide
    // as a window of the usual size gives the song, is said whole, but for the five of
    // a chord being typed, whose last is then left off (Windows names its keys in
    // words, and they are wider than the Mac's signs); and the two on copying and
    // pasting are said whole down to a narrow one.
    [Fact]
    public void AsTheLineMeasuresThemEverySetOfHintsFitsAWindowOfTheUsualSize() => Pictures.OnAThreadOfItsOwn(() =>
    {
        IReadOnlyList<Hint>[] all =
        [
            SheetHints.Hints(Situation.Nothing), SheetHints.Hints(Situation.OnAChord), SheetHints.Hints(Situation.OnAPlace),
            SheetHints.Hints(Situation.Typing(onChord: true, empty: true)), SheetHints.Hints(Situation.Typing(onChord: false, empty: false)),
            SheetHints.Hints(Situation.Carrying(fromStrip: true)), SheetHints.Hints(Situation.Carrying(fromStrip: false)),
            SheetHints.Strip, SheetHints.CannotChange,
        ];
        // (A window 1280 wide with the lists at their usual width leaves the song some 900.)
        Assert.All(all, hints => Assert.Equal(new HintFit.Fit(Math.Min(hints.Count, 4), false, 1), HintFit.Of(hints, 900 - (2 * HintFit.Sides), HintLine.WidthOf)));
        Assert.All(all, hints => Assert.Equal(new HintFit.Fit(hints.Count, false, 1), HintFit.Of(hints, 1100, HintLine.WidthOf)));
        var copying = SheetHints.Copying(new SheetHints.Paste(4, Replaces: true));
        // The longest thing the line ever says: side by side in a wide pane, and one
        // under the other in a narrower, both whole.
        Assert.Equal(new HintFit.Fit(2, false, 1), HintFit.Of(copying, 1500, HintLine.WidthOf));
        var narrow = HintFit.Of(copying, 700, HintLine.WidthOf);
        Assert.True(narrow.Stacked);
        Assert.All(copying, hint => Assert.True(HintFit.WidthOf(hint, narrow.Size, HintLine.WidthOf) <= 700));
    });

    // ---- What is going on, read off the sheet

    [Fact]
    public void WhatIsGoingOnIsReadOffTheSheet() => OnAMadeUpPC(async pc =>
    {
        var (model, _, hand) = await Open(pc, "Amazing Grace");
        var sheet = model.Sheet!;
        var row = hand.PlaceOf("Amazing grace how sweet the sound").Row;
        var chord = sheet.Rows[row].Chords[0];
        var bare = sheet.Stops(row).First(stop => sheet.ChordAt(row, stop).Length == 0);

        sheet.SetSpot(row, chord.At);
        Assert.Equal(Situation.OnAChord, SheetHints.Of(sheet, attending: true));
        Assert.Equal(Situation.Nothing, SheetHints.Of(sheet, attending: false));
        sheet.SetSpot(row, bare);
        Assert.Equal(Situation.OnAPlace, SheetHints.Of(sheet, attending: true));

        sheet.Pick(row, chord.At);
        Assert.Equal(Situation.Typing(onChord: true, empty: false), SheetHints.Of(sheet, attending: false));
        sheet.Type("");
        Assert.Equal(Situation.Typing(onChord: true, empty: true), SheetHints.Of(sheet, attending: false));
        Assert.True(sheet.Press(ChordSheet.EntryKey.Escape));
        sheet.Pick(row, bare);
        Assert.Equal(Situation.Typing(onChord: false, empty: true), SheetHints.Of(sheet, attending: true));
        Assert.True(sheet.Press(ChordSheet.EntryKey.Escape));

        sheet.Lift(row, chord.At);
        Assert.Equal(Situation.Carrying(fromStrip: false), SheetHints.Of(sheet, attending: false));
        sheet.DropNowhere();
        Assert.Equal(Situation.OnAPlace, SheetHints.Of(sheet, attending: true));
        // One of the key's chords, carried in from the strip over a place; and carried
        // off the places again, which is nothing to say of the sheet.
        sheet.Carry("Em", row, bare);
        Assert.Equal(Situation.Carrying(fromStrip: true), SheetHints.Of(sheet, attending: false));
        sheet.CarryOff();
        Assert.Equal(Situation.Nothing, SheetHints.Of(sheet, attending: false));

        // What a paste would do. Nothing copied yet: it would replace where the spot's
        // line has chords, which is all there is to go by.
        // (Every line of this song has a chord: one is taken off its second line.)
        var none = hand.PlaceOf("That saved a wretch like me").Row;
        Assert.True(sheet.Take(none, Assert.Single(sheet.Rows[none].Chords).At));
        sheet.SetSpot(row, 0);
        Assert.Equal(new SheetHints.Paste(null, Replaces: true), SheetHints.PasteIn(sheet));
        sheet.SetSpot(none, 0);
        Assert.Equal(new SheetHints.Paste(null, Replaces: false), SheetHints.PasteIn(sheet));
        // One line copied: as the sheet itself says a paste would do.
        sheet.SetSpot(row, 0);
        Assert.NotNull(sheet.Copy(whole: false));
        Assert.Equal(new SheetHints.Paste(1, Replaces: true), SheetHints.PasteIn(sheet));
        sheet.SetSpot(none, 0);
        Assert.Equal(new SheetHints.Paste(1, Replaces: false), SheetHints.PasteIn(sheet));
        // A whole verse: as many lines as it has.
        sheet.SetSpot(row, 0);
        Assert.NotNull(sheet.Copy(whole: true));
        Assert.True(sheet.Copied!.Count > 1);
        Assert.Equal(sheet.Copied.Count, SheetHints.PasteIn(sheet).Lines);
    });

    [Fact]
    public void TheHighlightedLineIsTheLineThePersonIsAt() => OnAMadeUpPC(async pc =>
    {
        var (model, _, hand) = await Open(pc, "Amazing Grace");
        var sheet = model.Sheet!;
        var row = hand.PlaceOf("Amazing grace how sweet the sound").Row;
        var other = hand.PlaceOf("That saved a wretch like me").Row;
        var chord = sheet.Rows[row].Chords[0];

        sheet.SetSpot(row, 0);
        Assert.Equal(row, SheetHints.Line(sheet, attending: true));
        Assert.Null(SheetHints.Line(sheet, attending: false));
        sheet.SetSpot(other, 5);
        Assert.Equal(other, SheetHints.Line(sheet, attending: true));

        // Typing on one line, the pointer gone on to another: the line of the typing.
        sheet.Pick(row, chord.At);
        sheet.PointerOver(other, 0);
        Assert.Equal(other, sheet.Spot.Row);
        Assert.Equal(row, SheetHints.Line(sheet, attending: true));
        Assert.Equal(row, SheetHints.Line(sheet, attending: false));
        Assert.True(sheet.Press(ChordSheet.EntryKey.Escape));

        // Carrying: where it would land.
        sheet.Lift(row, chord.At);
        sheet.Carry(other, 5);
        Assert.Equal(other, SheetHints.Line(sheet, attending: false));
        sheet.DropNowhere();
        Assert.Null(SheetHints.Line(sheet, attending: false));
    });

    // ---- The sheet's element says what is going on, and the line says it

    [Fact]
    public void TheLineSaysWhatGoesWithWhereThePointerIsAndWithWhatIsHeld() => OnAMadeUpPC(async pc =>
    {
        var (model, view, hand) = await Open(pc, "Amazing Grace");
        var line = view.SongPane.ChordSheet.HintLine;
        var canvas = hand.Canvas;
        var idle = Read(SheetHints.Idle);
        // Opened, with nobody at the sheet: where to point.
        Assert.Equal(idle, Said(model));
        Assert.Equal(idle, Read(line.Hints));
        Assert.False(canvas.Attending);
        Assert.Null(canvas.Highlighted);

        // Over a place with no chord.
        var wretch = hand.PlaceOf("wretch");
        hand.PointAt(wretch);
        Assert.True(canvas.Attending);
        Assert.Equal(Situation.OnAPlace, model.SheetSituation);
        Assert.Equal(Read(SheetHints.Hints(Situation.OnAPlace)), Read(line.Hints));
        Assert.Equal(wretch.Row, canvas.Highlighted);
        // Over a chord.
        var grace = hand.PlaceOf("grace");
        hand.PointAt(grace, onItsChord: true);
        Assert.Equal(Situation.OnAChord, model.SheetSituation);
        Assert.StartsWith("Delete Takes this chord off", Read(line.Hints), StringComparison.Ordinal);
        Assert.Equal(grace.Row, canvas.Highlighted);

        // Ctrl goes down: the copying of that line's chords, and that a paste there
        // would replace, the line having chords and nothing being copied yet.
        canvas.KeysHeld(ctrl: true);
        Assert.True(model.CtrlHeld);
        Assert.Equal(
            "Press Ctrl+C to copy all chords on the highlighted line.\nPress Ctrl+V to paste previously copied chords to the highlighted line (Replaces existing chords)",
            Read(line.Hints));
        // And comes up again.
        canvas.KeysHeld(ctrl: false);
        Assert.StartsWith("Delete", Read(line.Hints), StringComparison.Ordinal);

        // A whole verse copied, and the pointer on a line with no chord whose next
        // line has one: it would replace, and goes onto the lines after it too.
        Assert.True(hand.Press(Key.C, ModifierKeys.Control | ModifierKeys.Shift));
        var copied = hand.Sheet.Copied!.Count;
        Assert.True(copied > 1);
        canvas.KeysHeld(ctrl: true);
        Assert.Equal(
            "Press Ctrl+C to copy all chords on the highlighted line.\nPress Ctrl+V to paste previously copied chords to the highlighted line and the lines after it (Replaces existing chords)",
            Read(line.Hints));
        canvas.KeysHeld(ctrl: false);

        // Off the slides, over the bare sheet: nobody is at the sheet, the spot is only
        // where it was left, and no line is the highlighted one. Whatever is held.
        canvas.PointerMoved(Hand.BareSheet, shift: false);
        Assert.False(canvas.Attending);
        Assert.Equal(idle, Read(line.Hints));
        Assert.Null(canvas.Highlighted);
        canvas.KeysHeld(ctrl: true);
        Assert.Equal(idle, Read(line.Hints));
        canvas.KeysHeld(ctrl: false);

        // A key that moves the spot is the person at the sheet, until the pointer next
        // moves.
        Assert.True(hand.Press(Key.Right));
        Assert.True(canvas.Attending);
        Assert.NotEqual(idle, Read(line.Hints));
        Assert.Equal(hand.Sheet.Spot.Row, canvas.Highlighted);
        canvas.PointerMoved(Hand.BareSheet, shift: false);
        Assert.Equal(idle, Read(line.Hints));
        // The keys that zoom, undo and redo are not: they are not about the spot.
        Assert.True(hand.Press(Key.Z, ModifierKeys.Control));
        Assert.False(canvas.Attending);

        // The pointer over a line, and then out of the sheet altogether.
        hand.PointAt(wretch);
        Assert.Equal(wretch.Row, canvas.Highlighted);
        canvas.PointerLeft();
        Assert.False(canvas.Attending);
        Assert.Null(canvas.Highlighted);
        Assert.Equal(idle, Read(line.Hints));

        // While a chord is typed: what the keys of the box do, wherever the pointer is.
        hand.Click("wretch");
        Assert.True(hand.BubbleIsOpen);
        Assert.StartsWith("Enter Keeps it", Read(line.Hints), StringComparison.Ordinal);
        canvas.PointerLeft();
        Assert.StartsWith("Enter Keeps it", Read(line.Hints), StringComparison.Ordinal);
        Assert.Equal(wretch.Row, canvas.Highlighted);
        Assert.True(hand.Press(Key.Escape));
        // On a chord, with its name cleared: that Enter takes it off.
        hand.Click(grace, onItsChord: true);
        hand.Press(Key.Back);
        Assert.Equal("Enter Takes this chord off\nEsc Leaves it as it was\nOr type the chord that is to be here", Read(line.Hints));
        Assert.True(hand.Press(Key.Escape));

        // While a chord of the sheet's own is carried, and what Ctrl then says.
        hand.Drag(grace, wretch, letGo: false);
        Assert.Equal("Let go on a word to move it there\nCtrl Held as you let go, leaves a copy behind", Read(line.Hints));
        Assert.Equal(wretch.Row, canvas.Highlighted);
        canvas.KeysHeld(ctrl: true);
        Assert.Equal("Let go on a word to put a copy there", Read(line.Hints));
        canvas.KeysHeld(ctrl: false);
        canvas.ButtonUp(hand.PointOf(grace, onItsChord: true), shift: false, copy: false);

        // One of the key's chords carried in from the strip, over a place.
        Assert.True(canvas.CarriedOfKey("Em", hand.PointOf(wretch), shift: false));
        Assert.Equal("Let go on a word to put this chord there", Read(line.Hints));
        canvas.CarriedAway();
        Assert.NotEqual("Let go on a word to put this chord there", Read(line.Hints));
    });

    [Fact]
    public void OverTheKeysChordsInTheStripTheLineSaysTheyCanBeDraggedAndWhileProPresenterIsRunningOnlyThatNothingCanBeChanged() => OnAMadeUpPC(async pc =>
    {
        var (model, view, hand) = await Open(pc, "Amazing Grace");
        var line = view.SongPane.ChordSheet.HintLine;
        const string strip = "You can drag these down to the text on any slide";
        hand.PointAt(hand.PlaceOf("grace"), onItsChord: true);
        // The pointer goes up to the strip: out of the sheet, and over the chords.
        hand.Canvas.PointerLeft();
        model.PointerIsOverTheKeyChords(true);
        Assert.True(model.OverKeyChords);
        Assert.Equal(strip, Read(line.Hints));
        model.PointerIsOverTheKeyChords(false);
        Assert.Equal(Read(SheetHints.Idle), Read(line.Hints));
        // (It is the strip's own mouse that says so: the part of it that holds the
        // chords tells the model as the pointer comes over it and leaves.)
        var chords = (ScrollViewer)view.SongPane.ChordSheet.FindName("ChordsSeen");
        chords.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = UIElement.MouseEnterEvent });
        Assert.True(model.OverKeyChords);
        chords.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = UIElement.MouseLeaveEvent });
        Assert.False(model.OverKeyChords);

        // ProPresenter is started: whatever is under the pointer, only that nothing
        // can be changed.
        hand.PointAt(hand.PlaceOf("grace"), onItsChord: true);
        Assert.StartsWith("Delete", Read(line.Hints), StringComparison.Ordinal);
        pc.ProPresenterIsRunning = true;
        model.ProPresenterMayHaveChanged();
        Assert.True(model.ProPresenterIsRunning);
        Assert.Equal("Nothing can be changed here while ProPresenter is running.", Read(line.Hints));
        model.PointerIsOverTheKeyChords(true);
        Assert.Equal("Nothing can be changed here while ProPresenter is running.", Read(line.Hints));
        model.PointerIsOverTheKeyChords(false);
        pc.ProPresenterIsRunning = false;
        model.ProPresenterMayHaveChanged();
        Assert.StartsWith("Delete", Read(line.Hints), StringComparison.Ordinal);
    });

    // ---- The quiet line under it (spec/06, rule 12)

    [Fact]
    public void TheQuietLineSaysHowMuchThereIsOfTheSongAndNoLongerWhatTheKeysDo() => OnAMadeUpPC(async pc =>
    {
        var (model, view, hand) = await Open(pc, "Amazing Grace");
        var quiet = (TextBlock)view.SongPane.FindName("Quiet");
        Assert.Equal("7 slides, 40 chords.", model.Notice);
        Assert.Equal("7 slides, 40 chords.", quiet.Text);
        Assert.Equal(TextWrapping.NoWrap, quiet.TextWrapping);
        // A chord more is counted at once.
        hand.PointAt("wretch");
        Assert.True(hand.Press(Key.D4));
        Assert.Equal("7 slides, 41 chords.", quiet.Text);
        Assert.True(hand.Press(Key.Delete));
        Assert.Equal("7 slides, 40 chords.", quiet.Text);
        // The line of hints is over it, in the same pane, and is as wide as the pane.
        var line = view.SongPane.ChordSheet.HintLine;
        Assert.Equal(Visibility.Visible, view.SongPane.ChordSheet.Visibility);
        Assert.Equal(view.SongPane.ActualWidth, line.ActualWidth);
        Assert.Equal(HintFit.Height, line.ActualHeight);
        var lineTop = line.TranslatePoint(new Point(0, 0), view.SongPane).Y;
        Assert.True(lineTop + line.ActualHeight <= quiet.TranslatePoint(new Point(0, 0), view.SongPane).Y);
        // In the ChordPro text there is no line of hints: the sheet's is put away.
        model.Show(LibraryModel.Editor.ChordPro);
        Pictures.Settle();
        Assert.Equal(Visibility.Collapsed, view.SongPane.ChordSheet.Visibility);
        Assert.Equal("7 slides, 40 chords. A change is saved a moment after it is typed.", quiet.Text);
    });

    // ---- The line itself, drawn (rules 53a and 53h), and what it costs (spec/06, 6e)

    // The colour of most of the dots of a part of a picture.
    private static Color MostOf((int Width, int Height, int[] Dots) picture, Rect part)
    {
        var counted = new Dictionary<int, int>();
        for (var y = (int)Math.Ceiling(part.Top * Pictures.Scale); y < (int)(part.Bottom * Pictures.Scale); y++)
        {
            for (var x = (int)Math.Ceiling(part.Left * Pictures.Scale); x < (int)(part.Right * Pictures.Scale); x++)
            {
                var dot = picture.Dots[(y * picture.Width) + x] & 0xFFFFFF;
                counted[dot] = counted.GetValueOrDefault(dot) + 1;
            }
        }
        var most = counted.MaxBy(pair => pair.Value).Key;
        return Color.FromRgb((byte)(most >> 16), (byte)(most >> 8), (byte)most);
    }

    private static Rect PartOf(FrameworkElement part, FrameworkElement whole) => new(part.TranslatePoint(new Point(0, 0), whole), new Size(part.ActualWidth, part.ActualHeight));

    [Fact]
    public void TheLineIsBlueLightOrDarkAsTheSystemIsWithItsKeysBrighterThanItsGround() => OnAMadeUpPC(async pc =>
    {
        foreach (var dark in new[] { true, false })
        {
            var (model, view, hand) = await Open(pc, "Amazing Grace", dark: dark);
            var line = view.SongPane.ChordSheet.HintLine;
            var colours = HintLine.Colours.Of(dark);
            Assert.Equal(colours, line.Worn);
            hand.PointAt(hand.PlaceOf("grace"), onItsChord: true);
            var picture = Pictures.Drawn(view, 1280, 800);
            var where = PartOf(line, view);
            // Its ground, which is most of it; and along its top a line of its own.
            Assert.Equal(colours.Ground, MostOf(picture, where));
            Assert.Equal(colours.Top, MostOf(picture, new Rect(where.Left, where.Top, where.Width, 1)));
            // Its first key, which is the first thing on it: filled with the key's colour.
            var key = new Rect(where.Left + HintFit.Sides + 2, where.Top + ((where.Height - HintFit.KeyHeight) / 2) + 2, 5, HintFit.KeyHeight - 4);
            Assert.Equal(colours.Key, MostOf(picture, key));
            // The colours are the page's own (rule 53h).
            Assert.Equal(dark ? "#1d477c" : "#dcebff", SheetLook.AsWritten(colours.Ground));
            Assert.Equal(dark ? "#3a82e0" : "#9ccbff", SheetLook.AsWritten(colours.Lit));
            Assert.Equal(dark ? "#e3efff" : "#17375e", SheetLook.AsWritten(colours.Words));
            Assert.Equal(dark ? "#4f90e6" : "#ffffff", SheetLook.AsWritten(colours.Key));
            Assert.Equal(dark ? "#b4d4ff" : "#3f86e3", SheetLook.AsWritten(colours.KeyEdge));
            Assert.Equal(dark ? "#ffffff" : "#0b4190", SheetLook.AsWritten(colours.KeySign));
            Assert.Equal(dark ? "#3565a3" : "#9fc4f2", SheetLook.AsWritten(colours.Top));
            // Lit, as it is when what it says has just changed: the lit colour.
            line.ShowLit(1);
            Assert.Equal(colours.Lit, MostOf(Pictures.Drawn(view, 1280, 800), where));
            line.ShowLit(0);
            Assert.Equal(colours.Ground, MostOf(Pictures.Drawn(view, 1280, 800), where));
            _ = model;
        }
    });

    [Fact]
    public void AChangeOfWhatTheLineSaysTellsNothingElseLaysOutNothingAndLightsTheLineUp() => OnAMadeUpPC(async pc =>
    {
        var (model, view, hand) = await Open(pc, "Amazing Grace");
        var line = view.SongPane.ChordSheet.HintLine;
        line.AnimationsAreOn = () => true;
        var grace = hand.PlaceOf("grace");
        var wretch = hand.PlaceOf("wretch");
        hand.PointAt(wretch);
        Pictures.Settle();

        // The model's one signal, which has every part of the window read it again, is
        // not said; and nothing in the window is laid out.
        var changes = 0;
        model.PropertyChanged += (_, _) => changes++;
        var laidOut = 0;
        view.LayoutUpdated += (_, _) => laidOut++;
        var (told, drawn, lit) = (model.HintsTold, line.Drawn, line.LitUp);

        hand.PointAt(grace, onItsChord: true);
        Pictures.Settle();
        Assert.Equal(0, changes);
        Assert.Equal(0, laidOut);
        Assert.Equal(told + 1, model.HintsTold);
        // It is drawn again, the once, and lights up.
        Assert.Equal(drawn + 1, line.Drawn);
        Assert.Equal(lit + 1, line.LitUp);
        // The pointer moving on along the same chord says nothing new, and nothing is done.
        hand.PointAt(grace, onItsChord: true);
        hand.PointAt(grace, onItsChord: true);
        Pictures.Settle();
        Assert.Equal((told + 1, drawn + 1, lit + 1), (model.HintsTold, line.Drawn, line.LitUp));
        // Onto a place with none, and back: each a change.
        hand.PointAt(wretch);
        hand.PointAt(grace, onItsChord: true);
        Pictures.Settle();
        Assert.Equal(told + 3, model.HintsTold);
        Assert.Equal(lit + 3, line.LitUp);
        Assert.Equal(0, changes);
        Assert.Equal(0, laidOut);

        // Where Windows is set to show no animations, it does not light up.
        line.AnimationsAreOn = () => false;
        hand.PointAt(wretch);
        Assert.Equal(told + 4, model.HintsTold);
        Assert.Equal(lit + 3, line.LitUp);
        Assert.Equal(0, line.Lit);
    });

    [Fact]
    public void TheLineNeverMakesTheSongsPartWiderAndIsTwoLinesOnlyForTheCopyingWhereThatDoesNotFitOnOne() => OnAMadeUpPC(async pc =>
    {
        // A narrow window: the song's part is too narrow for the copying side by side.
        var (model, view, hand) = await Open(pc, "Amazing Grace", width: 900, height: 700);
        var pane = view.SongPane;
        var line = pane.ChordSheet.HintLine;
        var scroll = (ScrollViewer)pane.ChordSheet.FindName("Scroll");
        var wide = pane.ActualWidth;
        var tall = scroll.ActualHeight;
        hand.PointAt(hand.PlaceOf("grace"), onItsChord: true);
        Pictures.Settle();
        // The hints of a chord do not all fit: the last are left off, the line is one
        // line, and the pane is as wide as it was.
        Assert.True(line.Fitted.Count < line.Hints.Count);
        Assert.False(line.Fitted.Stacked);
        Assert.Equal(HintFit.Height, line.ActualHeight);
        Assert.Equal(wide, pane.ActualWidth);
        Assert.Equal(wide, line.ActualWidth);
        // It asks for no width of its own, whatever it says.
        line.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Assert.Equal(0, line.DesiredSize.Width);
        Pictures.LayOut(view, 900, 700);

        // Ctrl held: the two sentences, one under the other, the line two lines tall
        // and the sheet that much shorter while they are said.
        hand.Canvas.KeysHeld(ctrl: true);
        Pictures.Settle();
        Assert.Equal(2, line.Fitted.Count);
        Assert.True(line.Fitted.Stacked);
        Assert.Equal(HintFit.HeightOf(line.Fitted), line.ActualHeight);
        Assert.True(line.ActualHeight > HintFit.Height);
        Assert.Equal(tall - (line.ActualHeight - HintFit.Height), scroll.ActualHeight);
        Assert.Equal(wide, pane.ActualWidth);
        // Let go, it is one line again and the sheet as tall as it was.
        hand.Canvas.KeysHeld(ctrl: false);
        Pictures.Settle();
        Assert.Equal(HintFit.Height, line.ActualHeight);
        Assert.Equal(tall, scroll.ActualHeight);
        _ = model;
    });

    // MINE, and of WPF's own way with laying out. An element that wants to be measured
    // again, and is first reached by an arranging that began above it (which is what a
    // window being made another size does), is measured there and then, and what
    // holds it is not told that it wants more room. The line of hints tells what holds
    // it itself; so it has its two lines' room whichever way it is reached. (FOUND in
    // a picture: the second sentence was cut off under the first.)
    [Fact]
    public void TheLineHasRoomForItsTwoLinesEvenIfItIsArrangedBeforeItIsMeasured() => OnAMadeUpPC(async pc =>
    {
        var (_, view, hand) = await Open(pc, "Amazing Grace");
        var line = view.SongPane.ChordSheet.HintLine;
        hand.PointAt("Amazing");
        Assert.True(hand.Press(Key.C, ModifierKeys.Control | ModifierKeys.Shift));
        hand.PointAt("I once was lost");
        Pictures.Settle();
        Assert.Equal(HintFit.Height, line.ActualHeight);

        hand.Canvas.KeysHeld(ctrl: true);
        Assert.True(line.Fitted.Stacked);
        Assert.False(line.IsMeasureValid);
        // Arranged where it was, before anything has measured it.
        line.Arrange(System.Windows.Controls.Primitives.LayoutInformation.GetLayoutSlot(line));
        Assert.True(line.IsMeasureValid);
        Pictures.Settle();
        Assert.Equal(HintFit.HeightOf(line.Fitted), line.ActualHeight);
        Assert.True(line.ActualHeight > HintFit.Height);
        // And it is given that much room: nothing of it is cut off.
        Assert.Equal(line.ActualHeight, System.Windows.Controls.Primitives.LayoutInformation.GetLayoutSlot(line).Height);
        Assert.Equal(line.ActualHeight, VisualTreeHelper.GetClip(line)!.Bounds.Height);
    });

    // ---- The highlighted line, drawn (rule 53f)

    // How many dots of a part of a picture are of a colour, or near it.
    private static int DotsOf(Color colour, (int Width, int Height, int[] Dots) picture, Rect part)
    {
        var found = 0;
        for (var y = (int)Math.Ceiling(part.Top * Pictures.Scale); y < (int)(part.Bottom * Pictures.Scale); y++)
        {
            for (var x = (int)Math.Ceiling(part.Left * Pictures.Scale); x < (int)(part.Right * Pictures.Scale); x++)
            {
                var dot = picture.Dots[(y * picture.Width) + x];
                if (Math.Abs(((dot >> 16) & 0xFF) - colour.R) + Math.Abs(((dot >> 8) & 0xFF) - colour.G) + Math.Abs((dot & 0xFF) - colour.B) < 40)
                {
                    found++;
                }
            }
        }
        return found;
    }

    // Where the words of a row are drawn, in the view.
    private static Rect WordsOf(int row, Hand hand, LibraryView view)
    {
        var item = hand.Grid.Lines[row];
        var metrics = hand.Canvas.Metrics;
        var top = item.ChordTop + metrics.BubbleHeight + metrics.Tail + 4;
        var at = hand.Canvas.TranslatePoint(new Point(item.Left, top), view);
        return new Rect(at.X, at.Y, 300, metrics.WordHeight - 8);
    }

    [Fact]
    public void TheWordsOfTheLineThePersonIsAtAreDrawnInItsColourAndOnlyTheSlidesItLeftAndCameToAreDrawnAgain() => OnAMadeUpPC(async pc =>
    {
        var (model, view, hand) = await Open(pc, "Amazing Grace");
        var canvas = hand.Canvas;
        var first = hand.PlaceOf("Amazing grace how sweet the sound");
        var second = hand.PlaceOf("That saved a wretch like me");
        var elsewhere = hand.PlaceOf("I once was lost");
        Assert.NotEqual(hand.Grid.Lines[first.Row].Cell, hand.Grid.Lines[elsewhere.Row].Cell);
        var orange = SheetLook.LineWords;
        Assert.Equal("#ff8a1f", SheetLook.AsWritten(orange));
        Assert.Equal(orange, canvas.LineColour);

        // With nobody at the sheet no line's words are orange. (A chord's name is, but
        // that is over the words, not among them.)
        var plain = Pictures.Drawn(view, 1280, 800);
        Assert.Equal(0, DotsOf(orange, plain, WordsOf(first.Row, hand, view)));
        Assert.True(DotsOf(SheetLook.Words, plain, WordsOf(first.Row, hand, view)) > 100);

        // The pointer on the second line: its words are orange, and the first's are not.
        hand.PointAt(second);
        var lit = Pictures.Drawn(view, 1280, 800);
        Assert.True(DotsOf(orange, lit, WordsOf(second.Row, hand, view)) > 100);
        Assert.Equal(0, DotsOf(SheetLook.Words, lit, WordsOf(second.Row, hand, view)));
        Assert.Equal(0, DotsOf(orange, lit, WordsOf(first.Row, hand, view)));

        // Along the same line nothing is drawn again but what follows the pointer.
        var slides = canvas.SlidesDrawn;
        hand.PointAt(new Spot(second.Row, 5));
        hand.PointAt(new Spot(second.Row, 12));
        Assert.Equal(slides, canvas.SlidesDrawn);
        // To the other line of the same slide: that one slide is drawn again.
        hand.PointAt(first);
        Assert.Equal(slides + 1, canvas.SlidesDrawn);
        // To a line of another slide: the slide it left and the slide it came to.
        hand.PointAt(elsewhere);
        Assert.Equal(slides + 3, canvas.SlidesDrawn);
        var moved = Pictures.Drawn(view, 1280, 800);
        Assert.Equal(0, DotsOf(orange, moved, WordsOf(first.Row, hand, view)));
        Assert.True(DotsOf(orange, moved, WordsOf(elsewhere.Row, hand, view)) > 100);
        // Out of the sheet: the one slide, and no line is orange.
        canvas.PointerLeft();
        Assert.Equal(slides + 4, canvas.SlidesDrawn);
        Assert.Equal(0, DotsOf(orange, Pictures.Drawn(view, 1280, 800), WordsOf(elsewhere.Row, hand, view)));

        // Another colour chosen in the settings: the line is drawn in that.
        model.SetSheetLineColour("#33cc66");
        Assert.Equal(Color.FromRgb(0x33, 0xCC, 0x66), canvas.LineColour);
        hand.PointAt(second);
        var green = Pictures.Drawn(view, 1280, 800);
        Assert.True(DotsOf(Color.FromRgb(0x33, 0xCC, 0x66), green, WordsOf(second.Row, hand, view)) > 100);
        Assert.Equal(0, DotsOf(orange, green, WordsOf(second.Row, hand, view)));
    });

    [Fact]
    public void ALineOfChordsAloneHasItsRuleInTheColourTwiceAsThick() => OnAMadeUpPC(async pc =>
    {
        var (_, view, hand) = await Open(pc, "Chords With No Words");
        var canvas = hand.Canvas;
        var alone = Enumerable.Range(0, hand.Sheet.Rows.Count).First(row => hand.Sheet.Usable(row) && hand.Sheet.Rows[row].Alone && hand.Grid.IsLaid(row));
        var item = hand.Grid.Lines[alone];
        var metrics = canvas.Metrics;
        var middle = item.ChordTop + metrics.BubbleHeight + metrics.Tail + (metrics.WordHeight / 2);
        // Right of its chords' tails, where nothing but the rule is.
        var x = item.Left + (item.Placed?.Width ?? 0) + 30;
        var at = canvas.TranslatePoint(new Point(x, middle - 4), view);
        var band = new Rect(at.X, at.Y, 40, 9);
        var orange = SheetLook.LineWords;
        // How thick a rule of a colour is drawn, in the picture's dots: one column of
        // dots down through it, each counted for as much of the colour as it has over
        // the card's own. (A rule does not always lie on whole dots, and where it does
        // not its first and last are part card.)
        double Thick((int Width, int Height, int[] Dots) picture, Color colour)
        {
            var card = SheetLook.Card;
            var thick = 0.0;
            for (var y = (int)Math.Ceiling(band.Top * Pictures.Scale); y < (int)(band.Bottom * Pictures.Scale); y++)
            {
                var red = (picture.Dots[(y * picture.Width) + (int)((band.Left + 20) * Pictures.Scale)] >> 16) & 0xFF;
                thick += Math.Min(1, Math.Max(0, (red - card.R) / (double)(colour.R - card.R)));
            }
            return thick;
        }

        var plain = Pictures.Drawn(view, 1280, 800);
        Assert.Equal(0, DotsOf(orange, plain, band));
        // One unit thick, which is as many dots as the picture has to a unit.
        Assert.Equal(Pictures.Scale, Thick(plain, SheetLook.Rule), 0.1);

        hand.PointAt(new Spot(alone, 0));
        Assert.Equal(alone, canvas.Highlighted);
        var lit = Pictures.Drawn(view, 1280, 800);
        Assert.Equal(2 * Pictures.Scale, Thick(lit, orange), 0.1);
        Assert.True(DotsOf(orange, lit, band) > 200);
        Assert.Equal(0, DotsOf(SheetLook.Rule, lit, band));
    });

    // ---- A chord's name is orange as it comes (spec/06, rule 20a)

    [Fact]
    public void AChordsNameOnTheSheetIsOrangeAsItComes()
    {
        Assert.Equal("#ff8a1f", SheetLook.AsWritten(SheetLook.ChordName));
        Assert.Equal(SheetLook.Accent, SheetLook.ChordName);
        _ = Chords.IsChord("G");
    }
}
