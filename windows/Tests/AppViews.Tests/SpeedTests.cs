// How long the window keeps its own thread as it is used: as the pointer moves over the
// sheet, as a chord is dragged, and as the edge between the lists and the song is
// moved. spec/06-library.md, rule 6e: "Nothing in the window keeps the pointer
// waiting." windows/NOTES.md, "What the window costs", has what was found with these.
//
// The maintainer felt it on the Mac, of its 1.0.1 (2026-10-13): the spot lagging behind
// the pointer, and the edge slow to drag with a song open. No test had looked at
// either, and no picture could. So the window is put through the same things here, in
// a window that is really there and that nobody sees (off every screen, never brought
// to the front, with nothing in the taskbar: as ChordSheetWindowTests has one), and the
// time each takes is taken: from the thing being done until the window has laid out
// and drawn everything that came of it.
//
// TWO KINDS OF TEST.
//
//   - Two that are always run, and hold the one thing that was badly wrong on the Mac
//     and must not come to be wrong here: a change in what the line of hints says is
//     not to stop the window, and its lighting up is not to keep the window's thread
//     busy. They are held to bounds many times what they take on the PC this was
//     written on, so that a slower machine keeps them with room to spare; the fault
//     they are for did not come near.
//   - One that only measures, on a long song in a large window, and says what it found.
//     It is run when it is asked for (in PowerShell, from windows/):
//
//         $env:PRO7_SPEED = "1"
//         dotnet test --project Tests/AppViews.Tests --filter-class "*SpeedTests"
//
//     Its numbers are written beside the pictures, as "speed.txt", for whoever changes
//     how the window is laid out or drawn, to be set beside the ones in the notes. It
//     fails for nothing: how long is too long on another PC is not a thing a test can
//     know.
//
// The time is time on the clock, the middle one of many goes and the worst of them:
// Windows counts a thread's own time at work in steps of a sixty-fourth of a second,
// which is longer than any of these takes. (But the lighting up is told by that count:
// it is the thread's time at work over two seconds that is wanted there.)
//
// What none of it tells: what the screen itself costs. What a window draws is handed to
// another thread of WPF's own, and from there to the glass, and that is not timed here.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Pro7ChordEditor.Song;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class SpeedTests
{
    private static readonly bool Asked = Environment.GetEnvironmentVariable("PRO7_SPEED") is { Length: > 0 };

    [DllImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern nint GetCurrentThread();

    [DllImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetThreadTimes(nint thread, out long created, out long ended, out long inTheSystem, out long ofItsOwn);

    // How long this thread has been at work, in thousandths of a second.
    private static double AtWork() =>
        GetThreadTimes(GetCurrentThread(), out _, out _, out var inTheSystem, out var ofItsOwn) ? (inTheSystem + ofItsOwn) / 10_000.0 : 0;

    // Lets what was asked for be done: everything the window's thread has waiting that
    // is laying out or drawing, and what came of that.
    private static void Settle() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Input);

    // Lets the window's thread go on for so long, doing whatever comes to it.
    private static void Wait(TimeSpan time)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Send) { Interval = time };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            frame.Continue = false;
        };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }

    // How long a thing takes, with everything the window then has to do: in
    // thousandths of a second.
    private static double Timed(Action what)
    {
        var clock = Stopwatch.StartNew();
        what();
        Settle();
        return clock.Elapsed.TotalMilliseconds;
    }

    private sealed record Times(double Usual, double Worst, int Goes)
    {
        internal static Times Of(IReadOnlyList<double> times)
        {
            var sorted = times.Order().ToList();
            return new Times(sorted[sorted.Count / 2], sorted[^1], sorted.Count);
        }

        public override string ToString() => FormattableString.Invariant($"{Usual,7:0.00} usually, {Worst,7:0.00} at worst, of {Goes}");
    }

    // The window's view in a window that is really there and that nobody sees.
    private static void InARealWindow(double width, double height, Func<MadeUpPC, LibraryModel, Task> bring, Action<MadeUpPC, LibraryModel, LibraryView> what) => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        var model = pc.Model();
        var view = new LibraryView(model);
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        await bring(pc, model);
        Look.Wear(view, dark: true);
        var window = new Window
        {
            Content = view,
            Width = width,
            Height = height,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -30000,
            Top = -30000,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            ShowActivated = false,
        };
        window.Show();
        try
        {
            Pictures.Settle();
            what(pc, model, view);
        }
        finally
        {
            // (Close what a test opens: a sheet left in a window goes on saying what
            // is going on in it.)
            window.Close();
        }
    });

    private static Task AmazingGrace(MadeUpPC pc, LibraryModel model)
    {
        model.Open(pc.Song("Amazing Grace"));
        return Task.CompletedTask;
    }

    // ---- Always run

    // A change in what the line of hints says does not stop the window: what the
    // window then has to do is done in a few thousandths of a second. (On the Mac it
    // stopped the window for a fifth of a second, as often as the pointer crossed a
    // chord: the line's size was asked of what was in it, and the song's header was
    // laid out again for it.)
    [Fact]
    public void AChangeInWhatTheLineOfHintsSaysDoesNotStopTheWindow() => InARealWindow(1280, 800, AmazingGrace, (_, model, view) =>
    {
        var hand = new Hand(view.SongPane.ChordSheet.Canvas);
        var line = view.SongPane.ChordSheet.HintLine;
        var onAChord = hand.PointOf(hand.PlaceOf("grace"), onItsChord: true);
        var onAPlace = hand.PointOf(hand.PlaceOf("wretch"));
        // (Once each first, so that what is made the first time is made.)
        Timed(() => hand.Canvas.PointerMoved(onAChord, shift: false));
        Timed(() => hand.Canvas.PointerMoved(onAPlace, shift: false));
        var told = model.HintsTold;
        var times = new List<double>();
        for (var go = 0; go < 40; go++)
        {
            times.Add(Timed(() => hand.Canvas.PointerMoved(go % 2 == 0 ? onAChord : onAPlace, shift: false)));
        }
        // Each of those was a change of the hints, and of the highlighted line.
        Assert.Equal(told + 40, model.HintsTold);
        Assert.Equal(SheetHints.Hints(SheetHints.Situation.OnAPlace), line.Hints);
        var taken = Times.Of(times);
        Assert.True(taken.Usual < 40, "a change of hints kept the window " + taken + " thousandths of a second");
    });

    // The line of hints lighting up, and fading over a second or two, keeps the
    // window's own thread at work for a small part of that time.
    [Fact]
    public void TheLineOfHintsLightingUpKeepsTheWindowsThreadAtWorkForASmallPartOfTheTime() => InARealWindow(1280, 800, AmazingGrace, (_, _, view) =>
    {
        var hand = new Hand(view.SongPane.ChordSheet.Canvas);
        var line = view.SongPane.ChordSheet.HintLine;
        line.AnimationsAreOn = () => true;
        var onAChord = hand.PointOf(hand.PlaceOf("grace"), onItsChord: true);
        var onAPlace = hand.PointOf(hand.PlaceOf("wretch"));
        hand.Canvas.PointerMoved(onAPlace, shift: false);
        Wait(TimeSpan.FromSeconds(2.2));
        Assert.Equal(0, line.Lit, 0.01);

        // With nothing going on.
        var before = AtWork();
        Wait(TimeSpan.FromSeconds(2));
        var idle = AtWork() - before;

        // Lit, and left to fade: it is lit a moment after, and is not two seconds after.
        var lit = line.LitUp;
        before = AtWork();
        hand.Canvas.PointerMoved(onAChord, shift: false);
        Assert.Equal(lit + 1, line.LitUp);
        // (A fifth of a second after: it is lit by an eighth and stays so until three
        // eighths. Held to half lit and not to wholly, so that a machine that is busy,
        // and comes back late from the wait, does not fail it.)
        Wait(TimeSpan.FromSeconds(0.2));
        Assert.True(line.Lit > 0.5, "a fifth of a second after its words changed the line is lit " + line.Lit);
        Wait(TimeSpan.FromSeconds(1.8));
        var lighting = AtWork() - before;
        Assert.True(line.Lit < 0.02, "two seconds after its words changed the line is still lit " + line.Lit);
        Assert.True(lighting < 800, "the line lighting up kept the window's thread " + lighting + " thousandths of a second at work in two seconds (" + idle + " with nothing going on)");
    });

    // ---- What is kept ready, counted
    //
    // These hold HOW the window is kept quick, by counting and not by the clock: what
    // was found slow on 2026-10-14 was that things were made again that had not
    // changed, and a count says so on any PC.

    private static async Task<(LibraryModel Model, LibraryView View, Hand Hand)> Open(MadeUpPC pc, string name)
    {
        var model = pc.Model();
        var view = new LibraryView(model);
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        model.Open(pc.Song(name));
        Look.Wear(view, dark: true);
        Pictures.LayOut(view, 1280, 800);
        return (model, view, new Hand(view.SongPane.ChordSheet.Canvas));
    }

    [Fact]
    public void AsTheWindowIsMadeWiderOrNarrowerNoLineIsLaidOutOrMadeReadyAgainAndASlideThatIsWhereItWasIsNotDrawnAgain() => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        var (_, view, hand) = await Open(pc, "Amazing Grace");
        var canvas = hand.Canvas;
        var lines = hand.Sheet.Rows.Count(row => row.Kind == SheetRowKind.Line);
        // As the song opens: each line of words laid out once, each line made ready
        // once, each slide drawn once.
        Assert.Equal(lines, canvas.Metrics.LinesLaidOut);
        Assert.Equal(lines, canvas.LinesMadeReady);
        var slides = hand.Grid.Cells.Count;
        Assert.Equal(slides, canvas.SlidesDrawn);
        var columns = hand.Grid.Columns;
        var seen = hand.Grid.SeenWidth;

        // The window a little wider, a step at a time, as when its edge is dragged: the
        // slides are where they were, and nothing is drawn or made again.
        for (var width = 1284.0; width <= 1320; width += 4)
        {
            Pictures.LayOut(view, width, 800);
        }
        Assert.True(hand.Grid.SeenWidth > seen + 30);
        Assert.Equal(columns, hand.Grid.Columns);
        Assert.Equal((lines, lines, slides), (canvas.Metrics.LinesLaidOut, canvas.LinesMadeReady, canvas.SlidesDrawn));

        // So much narrower that fewer slides go to a row: every slide that has moved is
        // drawn again, where it now is. But no line of it is laid out again, nor made
        // ready again: it is laid down again as it was.
        Pictures.LayOut(view, 800, 800);
        Assert.True(hand.Grid.Columns < columns);
        Assert.True(canvas.SlidesDrawn > slides);
        Assert.Equal((lines, lines), (canvas.Metrics.LinesLaidOut, canvas.LinesMadeReady));
        // And drawn so, it is what it would be drawn afresh: the same dots as a sheet
        // that was opened at that width.
        var kept = Pictures.Drawn(view, 800, 800);
        var (_, afresh, _) = await Open(pc, "Amazing Grace");
        var fresh = Pictures.Drawn(afresh, 800, 800);
        Assert.True(kept.Dots.AsSpan().SequenceEqual(fresh.Dots), "a sheet made narrower is not drawn as one opened at that width is");
    });

    [Fact]
    public void AChordPutOnMakesTheOneLineReadyAgainAndLaysOutNoWordsAgain() => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        var (_, _, hand) = await Open(pc, "Amazing Grace");
        var canvas = hand.Canvas;
        var (laidOut, ready, held) = (canvas.Metrics.LinesLaidOut, canvas.LinesMadeReady, canvas.Metrics.Held);
        var slides = canvas.SlidesDrawn;
        // (The pointer away from the line, so that the line is drawn as where nobody is
        // at it: the highlighted line is drawn as it is, each time.)
        var wretch = hand.PlaceOf("wretch");
        hand.Sheet.SetSpot(wretch.Row, wretch.At);
        hand.Sheet.PutOfKey(4);
        canvas.Sync();
        Assert.Equal("13:C 25:D", hand.ChordsOf("wretch"));
        // The rows are others now, and every slide is drawn again with them; but the
        // words are the same, and so are all the lines but one.
        Assert.Equal(held + 1, canvas.Metrics.Held);
        Assert.Equal(laidOut, canvas.Metrics.LinesLaidOut);
        Assert.Equal(ready + 1, canvas.LinesMadeReady);
        Assert.True(canvas.SlidesDrawn > slides);
        // Taken off again, the line is as one that was made ready before: none is made.
        hand.Sheet.TakeAtSpot();
        canvas.Sync();
        Assert.Equal("25:D", hand.ChordsOf("wretch"));
        Assert.Equal((laidOut, ready + 1), (canvas.Metrics.LinesLaidOut, canvas.LinesMadeReady));
        // In another colour of chords, every line is made ready again, the once.
        canvas.ChordColour = System.Windows.Media.Color.FromRgb(0x33, 0xCC, 0x66);
        Assert.Equal(laidOut, canvas.Metrics.LinesLaidOut);
        Assert.Equal(ready + 1 + hand.Sheet.Rows.Count(row => row.Kind == SheetRowKind.Line), canvas.LinesMadeReady);
    });

    [Fact]
    public void TheChordProTextAsksItsBoxWhereALineIsOnceUntilTheTextChanges() => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        var (model, view, _) = await Open(pc, "Amazing Grace");
        model.Show(LibraryModel.Editor.ChordPro);
        Pictures.LayOut(view, 1280, 800);
        var underlay = view.SongPane.ChordPro.Underlay;
        var asked = underlay.PlacesAsked;
        Assert.True(asked > 10, "the box was asked where " + asked + " lines are");
        var drawn = Pictures.Drawn(view, 1280, 800);
        // The window wider and narrower: the lines are where they were, and the box is
        // not asked again.
        for (var width = 1284.0; width <= 1320; width += 4)
        {
            Pictures.LayOut(view, width, 800);
        }
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal(asked, underlay.PlacesAsked);
        Assert.True(drawn.Dots.AsSpan().SequenceEqual(Pictures.Drawn(view, 1280, 800).Dots));
        // Scrolled down and back: only for the lines that had not been in view.
        var box = view.SongPane.ChordPro.TextBox;
        box.ScrollToVerticalOffset(60);
        Pictures.LayOut(view, 1280, 800);
        var afterScrolling = underlay.PlacesAsked;
        box.ScrollToVerticalOffset(0);
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal(afterScrolling, underlay.PlacesAsked);
        Assert.True(drawn.Dots.AsSpan().SequenceEqual(Pictures.Drawn(view, 1280, 800).Dots), "scrolled down and back, the text is not drawn as it was");
        // The text changed, a chord's name typed over: every line in view is asked for
        // again, a line's place hanging on every line above it.
        Assert.Contains("A[G]mazing", box.Text, StringComparison.Ordinal);
        box.Text = box.Text.Replace("A[G]mazing", "A[Em]mazing", StringComparison.Ordinal);
        Pictures.LayOut(view, 1280, 800);
        Assert.Contains("A[Em]mazing", box.Text, StringComparison.Ordinal);
        Assert.True(underlay.PlacesAsked >= afterScrolling + 10, "after the text changed the box was asked where " + (underlay.PlacesAsked - afterScrolling) + " lines are");
    });

    // ---- Measured, when asked for

    // A song of fifty-six slides, made from a ChordPro file as the app makes one.
    private static async Task ALongSong(MadeUpPC pc, LibraryModel model)
    {
        string[] lines =
        [
            "A[G]mazing [G7]grace how [C]sweet the [G]sound", "That saved a wretch like [D]me",
            "I [G]once was [G7]lost but [C]now am [G]found", "Was [Em]blind but [D]now I [G]see",
        ];
        var text = new StringBuilder("{title: A Long Song}\n{key: G}\n\n");
        for (var slide = 0; slide < 56; slide++)
        {
            text.Append("{start_of_verse: Verse ").Append(slide + 1).Append("}\n").Append(lines[(slide * 2) % 4]).Append('\n').Append(lines[((slide * 2) + 1) % 4]).Append("\n{end_of_verse}\n\n");
        }
        var file = Path.Combine(pc.Folder, "a-long-song.cho");
        File.WriteAllText(file, text.ToString());
        model.BringIn(file);
        model.MakeTheSong();
        await model.MarksAreWorkedOutAsync();
        Assert.True(model.Shown is { Slides: >= 50 }, "the long song has " + model.Shown?.Slides + " slides");
    }

    [Fact]
    public void MeasuredWhenAskedForALongSongInALargeWindowThePointerMovedAcrossItAChordDraggedAndTheEdgeMoved()
    {
        if (!Asked)
        {
            return;
        }
        var found = new StringBuilder();
        void Say(string what, Times taken)
        {
            found.AppendLine(FormattableString.Invariant($"{what,-62} {taken}"));
        }

        InARealWindow(1920, 1050, ALongSong, (pc, model, view) =>
        {
            var canvas = view.SongPane.ChordSheet.Canvas;
            var hand = new Hand(canvas);
            var scroll = (ScrollViewer)view.SongPane.ChordSheet.FindName("Scroll");
            found.AppendLine(FormattableString.Invariant($"A song of {model.Shown!.Slides} slides and {model.Shown.Chords} chords, {hand.Grid.Columns} to a row, in a window 1920 by 1050; the sheet is {scroll.ViewportWidth:0} by {scroll.ViewportHeight:0} of {canvas.ActualHeight:0}."));
            found.AppendLine("In thousandths of a second, from the thing done until the window has laid out and drawn what came of it:");

            // The pointer moved across what is seen of the sheet, a few units at a
            // time, down a slant: over words, over chords, and over the bare sheet.
            var moves = new List<double>();
            for (var step = 0; step < 400; step++)
            {
                var at = new Point(20 + ((step * 7.3) % (scroll.ViewportWidth - 40)), 40 + ((step * 2.1) % (scroll.ViewportHeight - 80)));
                moves.Add(Timed(() => canvas.PointerMoved(at, shift: false)));
            }
            Say("the pointer moved over the sheet", Times.Of(moves));
            // Along one line, where nothing but the spot changes.
            var line = hand.PlaceOf("That saved");
            var along = new List<double>();
            for (var step = 0; step < 200; step++)
            {
                var at = hand.PointOf(new Spot(line.Row, step % 26));
                along.Add(Timed(() => canvas.PointerMoved(at, shift: false)));
            }
            Say("the pointer moved along one line", Times.Of(along));
            // From a chord to a place with none and back: each a change of the hints.
            var onAChord = hand.PointOf(hand.PlaceOf("grace"), onItsChord: true);
            var onAPlace = hand.PointOf(hand.PlaceOf("wretch"));
            var hints = new List<double>();
            for (var go = 0; go < 100; go++)
            {
                hints.Add(Timed(() => canvas.PointerMoved(go % 2 == 0 ? onAChord : onAPlace, shift: false)));
            }
            Say("a change of what the hints say (and of the highlighted line)", Times.Of(hints));
            // With Ctrl going down and coming up: the hints of two lines' height, where
            // they do not fit on one, and back.
            var ctrl = new List<double>();
            for (var go = 0; go < 60; go++)
            {
                ctrl.Add(Timed(() => canvas.KeysHeld(ctrl: go % 2 == 0)));
            }
            Say("Ctrl going down, and coming up", Times.Of(ctrl));

            // A chord dragged from one word across the slides and back.
            var from = hand.PlaceOf("grace");
            var start = hand.PointOf(from, onItsChord: true);
            canvas.PointerMoved(start, shift: false);
            canvas.ButtonDown(start, shift: false);
            var dragged = new List<double>();
            for (var step = 0; step < 300; step++)
            {
                var at = new Point(start.X + ((step * 5.7) % (scroll.ViewportWidth - start.X - 30)), start.Y + ((step * 1.9) % (scroll.ViewportHeight - start.Y - 60)));
                dragged.Add(Timed(() => canvas.Dragged(at, shift: false)));
            }
            Say("a chord dragged over the sheet", Times.Of(dragged));
            canvas.MouseLost(buttonIsDown: false);
            Settle();

            // A chord put on, and taken off again: the song's file is written each time.
            hand.PointAt("wretch");
            var put = new List<double>();
            for (var go = 0; go < 20; go++)
            {
                put.Add(Timed(() => canvas.KeyPressed(go % 2 == 0 ? new SheetKey(SheetKeyKind.Number, 4) : new SheetKey(SheetKeyKind.Delete))));
            }
            Say("a chord put on or taken off (the file is saved)", Times.Of(put));

            // The edge between the lists and the song, moved a little at a time, there
            // and back: with the sheet, with the ChordPro text, and with no song.
            List<double> Edge()
            {
                var edge = view.TheEdge;
                var steps = new List<double>();
                edge.RaiseEvent(new DragStartedEventArgs(0, 0));
                var layingOut = new List<double>();
                for (var step = 0; step < 60; step++)
                {
                    var by = step < 30 ? 4 : -4;
                    steps.Add(Timed(() =>
                    {
                        edge.RaiseEvent(new DragDeltaEventArgs(by, 0));
                        var clock = Stopwatch.StartNew();
                        view.UpdateLayout();
                        layingOut.Add(clock.Elapsed.TotalMilliseconds);
                    }));
                }
                Say("    of which, laying out", Times.Of(layingOut));
                edge.RaiseEvent(new DragCompletedEventArgs(0, 0, canceled: false));
                Settle();
                return steps;
            }
            Say("a step of the edge, with the sheet", Times.Of(Edge()));
            model.Show(LibraryModel.Editor.ChordPro);
            Pictures.Settle();
            Say("a step of the edge, with the ChordPro text", Times.Of(Edge()));
            model.Show(LibraryModel.Editor.Sheet);
            Pictures.Settle();
            model.ChooseLibrary(pc.Library("Kids"));
            Pictures.Settle();
            Assert.Null(model.Shown);
            Say("a step of the edge, with no song", Times.Of(Edge()));
        });
        File.WriteAllText(Path.Combine(Pictures.Folder, "speed.txt"), found.ToString());
    }
}
