// Pictures of the window, to be looked at (Pictures.cs says where they go).
//
// A test here cannot tell whether a picture looks right: only eyes can, and whoever
// changes how something is drawn is to open the pictures and say what was seen
// (spec/10-platforms.md). What a test CAN tell is that a picture was drawn at all, at
// the size asked for, and has something in it; a picture that failed to draw would
// otherwise be a blank one that nobody happened to open.
//
// Each is a whole view, on a made-up PC (MadeUpPC.cs), in the dark look or the light
// one as the test says, whatever this PC is set to: the lists, a song open on the chord
// sheet (the cases spec/10 lists, and more) and as ChordPro text and typed in, its two
// keys, Revert and its question, its earlier versions, the settings, and All Copies.

using System.IO;
using System.Windows;
using System.Windows.Input;
using Pro7ChordEditor.Song;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class PictureTests
{
    // Draws a view that a test makes on a made-up PC, and holds the picture to what a
    // test can tell of one.
    private static void DrawView(string name, bool dark, Func<MadeUpPC, Task<FrameworkElement>> make, double width = 1280, double height = 800) =>
        Pictures.OnAThreadOfItsOwn(async () =>
        {
            using var pc = new MadeUpPC();
            var view = await make(pc);
            Look.Wear(view, dark);

            var path = Pictures.Draw(view, width, height, name);

            var picture = Pictures.Read(path);
            Assert.Equal(((int)width * Pictures.Scale, (int)height * Pictures.Scale), (picture.Width, picture.Height));
            // Grounds of several greys, words, and the shades between that smooth the
            // letters' edges: some hundreds of colours. A view that drew nothing would
            // be one.
            Assert.True(picture.Colours > 100, $"{path} has only {picture.Colours} colours in it: was anything drawn?");
        });

    // The same, of the window's own view, of a model that a test has brought to some state.
    private static void Draw(string name, bool dark, Func<MadeUpPC, LibraryModel, Task> bring, double width = 1280, double height = 800) =>
        DrawView(
            name,
            dark,
            async pc =>
            {
                var model = pc.Model();
                var view = new LibraryView(model);
                await bring(pc, model);
                await model.MarksAreWorkedOutAsync();
                return view;
            },
            width,
            height);

    private static string In(bool dark) => dark ? "dark" : "light";

    private static async Task ASongOpen(MadeUpPC pc, LibraryModel model, string song)
    {
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        model.Open(pc.Song(song));
    }

    // ---- The lists, and a song (M3)

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheWindowAsItOpens(bool dark) => Draw(
        "01 the window as it opens, " + In(dark),
        dark,
        async (pc, model) =>
        {
            await model.StartAsync();
            model.ChooseLibrary(pc.Library("Songs"));
        });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ASongOpenToBeLookedAt(bool dark) => Draw("02 a song open, " + In(dark), dark, (pc, model) => ASongOpen(pc, model, "Amazing Grace"));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheBandWhileProPresenterIsRunning(bool dark) => Draw(
        "03 ProPresenter running, " + In(dark),
        dark,
        async (pc, model) =>
        {
            pc.ProPresenterIsRunning = true;
            await ASongOpen(pc, model, "Plain Song");
        });

    [Fact]
    public void AFileThatCannotBeRead() => Draw("04 a file that cannot be read", true, (pc, model) => ASongOpen(pc, model, "Nonsense"));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NoPlaceFound(bool dark) => Draw(
        "05 no place found, " + In(dark),
        dark,
        async (pc, model) =>
        {
            pc.HasAProPresenterFolder = false;
            await model.StartAsync();
        });

    [Fact]
    public void TheListNarrowedByWhatIsTypedAndByMark() => Draw(
        "06 the list narrowed",
        true,
        async (pc, model) =>
        {
            await ASongOpen(pc, model, "Holy Holy Holy");
            await model.MarksAreWorkedOutAsync();
            model.Typed = "o";
            model.Filter = MarkFilter.WithChords;
        });

    [Fact]
    public void TheOldEditorsCopiesAndAChosenFolder() => Draw(
        "07 the old editor's copies, in a chosen folder",
        false,
        async (pc, model) =>
        {
            await model.StartAsync();
            model.Add(Path.Combine(pc.Place, "Libraries"));
            model.ChooseLibrary(model.Libraries.Single(library => library.Name == "Old copies").Id);
            model.Open(model.Songs.Single(song => song.Name == "It Is Well (Chords)").Id);
        });

    [Fact]
    public void ASongWithBlankSlidesAndChordsWithNoWords() => Draw(
        "08 blank slides and chords with no words",
        true,
        (pc, model) => ASongOpen(pc, model, "Chords With No Words"));

    [Fact]
    public void TheWindowAtItsSmallest() => Draw(
        "09 the window at its smallest",
        true,
        (pc, model) => ASongOpen(pc, model, "Amazing Grace"),
        width: 640,
        height: 440);

    [Fact]
    public void TheBarDraggedSoThatTheLibrariesHaveMostOfTheHeight() => Draw(
        "10 the bar dragged down",
        false,
        async (pc, model) =>
        {
            await ASongOpen(pc, model, "Blank Slides");
            model.SetListsSplit(0.7);
        });

    [Fact]
    public void APlaceWithNoLibrariesInIt() => Draw(
        "11 a place with no libraries",
        true,
        async (pc, model) =>
        {
            await model.StartAsync();
            model.Add(Directory.CreateDirectory(Path.Combine(pc.Folder, "Nothing in it")).FullName);
        });

    // ---- Earlier versions (M4)

    private static async Task TheSongWithAHistoryOpen(MadeUpPC pc, LibraryModel model)
    {
        pc.GivePlainSongAHistory();
        await ASongOpen(pc, model, "Plain Song");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ASongThatHasEarlierVersions(bool dark) => Draw("12 a song that has earlier versions, " + In(dark), dark, TheSongWithAHistoryOpen);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheEarlierVersionsOfASong(bool dark) => Draw(
        "13 earlier versions, " + In(dark),
        dark,
        async (pc, model) =>
        {
            await TheSongWithAHistoryOpen(pc, model);
            model.ShowEarlierVersions();
            model.PickCopy(model.Earlier!.Copies[^1].Id);
        });

    [Fact]
    public void ACopyOfAnotherSongIsAskedAboutFirst() => Draw(
        "14 a copy of another song",
        true,
        async (pc, model) =>
        {
            pc.GivePlainSongAHistory();
            var elsewhere = Path.Combine(pc.Folder, "Was there once.pro");
            File.WriteAllBytes(elsewhere, pc.BytesOf("Amazing Grace"));
            pc.AppData.Copies().Keep(elsewhere, new KeptSong(MadeUpPC.PlaceName, "Songs", "Plain Song"), CopyReason.BeforeAChange, DateTime.UtcNow.AddDays(-40));
            await ASongOpen(pc, model, "Plain Song");
            model.ShowEarlierVersions();
            model.PickCopy(model.Earlier!.Copies.Single(copy => copy.SongName == "Amazing Grace").Id);
            model.PutBackPicked();
        });

    [Fact]
    public void AVersionJustPutBackWithTheOfferToUndo() => Draw(
        "15 a version just put back",
        false,
        async (pc, model) =>
        {
            await TheSongWithAHistoryOpen(pc, model);
            model.ShowEarlierVersions();
            model.PickCopy(model.Earlier!.Copies[^1].Id);
            model.PutBackPicked();
        });

    [Fact]
    public void NothingPutBackWhileProPresenterIsRunning() => Draw(
        "16 not put back, ProPresenter running",
        true,
        async (pc, model) =>
        {
            await TheSongWithAHistoryOpen(pc, model);
            model.ShowEarlierVersions();
            pc.ProPresenterIsRunning = true;
            model.PutBackPicked();
        });

    [Fact]
    public void AFileChangedBySomethingElseAndTheOfferToOpenAgain() => Draw(
        "17 changed by something else",
        true,
        async (pc, model) =>
        {
            await TheSongWithAHistoryOpen(pc, model);
            model.ShowEarlierVersions();
            File.WriteAllBytes(pc.Song("Plain Song"), pc.BytesOf("It Is Well"));
            model.PutBackPicked();
            model.CloseEarlierVersions();
        },
        width: 900,
        height: 560);

    // ---- All Copies, in the settings (M4)

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AllCopies(bool dark) => DrawView(
        "18 all copies, " + In(dark),
        dark,
        pc =>
        {
            pc.GivePlainSongAHistory();
            pc.Change("Holy Holy Holy", 1, song => song.SetKey("D"));
            pc.Change("Amazing Grace (Chords)", 40, song => song.SetKey("A"), "Old copies");
            File.Delete(pc.Song("Holy Holy Holy"));
            var model = new AllCopiesModel(pc.AppData.Copies());
            var view = new AllCopiesView(model);
            model.PickSong("Documents/Songs/Plain Song");
            return Task.FromResult<FrameworkElement>(view);
        },
        width: 960,
        height: 520);

    [Fact]
    public void AllCopiesWithNoneKept() => DrawView(
        "19 all copies, none kept",
        true,
        pc => Task.FromResult<FrameworkElement>(new AllCopiesView(new AllCopiesModel(pc.AppData.Copies()))),
        width: 960,
        height: 520);

    // ---- The ChordPro editor (M5)

    // The window's view with a song open in it, and someone at its text.
    private static void DrawTyped(string name, bool dark, string song, Func<MadeUpPC, LibraryModel, Typist, Task> bring, double width = 1280, double height = 800, Action<MadeUpPC>? first = null) =>
        DrawView(
            name,
            dark,
            async pc =>
            {
                first?.Invoke(pc);
                var model = pc.Model();
                model.PauseBeforeSaving = TimeSpan.FromMilliseconds(1);
                var view = new LibraryView(model);
                await ASongOpen(pc, model, song);
                await model.MarksAreWorkedOutAsync();
                // (A song opens on the chord sheet.)
                model.Show(LibraryModel.Editor.ChordPro);
                var typist = new Typist(view.SongPane.ChordPro, model);
                view.SongPane.ChordPro.SaysNo = () => { };
                await bring(pc, model, typist);
                return view;
            },
            width,
            height);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AChordBeingTyped(bool dark) => DrawTyped(
        "20 a chord being typed, " + In(dark),
        dark,
        "Plain Song",
        async (_, model, typist) =>
        {
            typist.CaretAt(typist.PlaceOf("Thirteen"));
            typist.Type("[Am7");
            await model.WhatWasTypedIsSavedAsync();
            // (And one still being typed: its brackets are empty yet.)
            typist.CaretAt(typist.PlaceOf("Sing it loud"));
            typist.Type("[");
        });

    [Fact]
    public void ASongThatCannotBeChangedAsChordProText() => DrawTyped(
        "21 words with brackets of their own",
        true,
        "Plain Song",
        (_, _, _) => Task.CompletedTask,
        first: pc =>
        {
            var bytes = pc.BytesOf("Plain Song");
            "Thir[een"u8.CopyTo(bytes.AsSpan(bytes.AsSpan().IndexOf("Thirteen"u8)));
            File.WriteAllBytes(pc.Song("Plain Song"), bytes);
        });

    [Fact]
    public void TheKeysUnderTheNameWhereThePaneIsNarrow() => DrawTyped(
        "22 the keys under the name",
        true,
        "A Song - In A Minor Key",
        (pc, model, _) =>
        {
            // (With an earlier version, so that its button is there too.)
            model.SetShownKey("Bm");
            return Task.CompletedTask;
        },
        width: 620,
        height: 440);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RevertAsksFirst(bool dark) => DrawTyped(
        "23 Revert asks first, " + In(dark),
        dark,
        "Amazing Grace",
        async (_, model, typist) =>
        {
            typist.CaretAt(typist.PlaceOf("wretch"));
            typist.Type("[Em");
            await model.WhatWasTypedIsSavedAsync();
            model.AskToRevert();
        },
        width: 1000,
        height: 620);

    [Fact]
    public void RevertedWithTheOfferToUndo() => DrawTyped(
        "24 reverted, with the offer to undo",
        true,
        "Amazing Grace",
        async (_, model, typist) =>
        {
            typist.CaretAt(typist.PlaceOf("wretch"));
            typist.Type("[Em");
            await model.WhatWasTypedIsSavedAsync();
            model.Revert();
        },
        width: 1000,
        height: 620);

    [Theory]
    [InlineData(1.5, "largest")]
    [InlineData(0.5, "smallest")]
    public void TheTextAtItsLargestAndItsSmallest(double zoom, string said) => DrawTyped(
        "25 the text at its " + said,
        true,
        "Amazing Grace",
        (_, model, _) =>
        {
            for (var times = 0; times < 9; times++)
            {
                model.ZoomText(zoom > 1 ? 1 : -1);
            }
            Assert.Equal(zoom, model.TextZoom);
            return Task.CompletedTask;
        },
        width: 1000,
        height: 620);

    [Fact]
    public void ASongThatStatesNoKey() => DrawTyped(
        "26 a song that states no key",
        false,
        "Holy Holy Holy",
        (_, _, _) => Task.CompletedTask,
        width: 1000,
        height: 620);

    [Fact]
    public void ASaveRefusedBecauseProPresenterWasStartedUnseen() => DrawTyped(
        "27 a save refused, ProPresenter started unseen",
        true,
        "Plain Song",
        async (pc, model, typist) =>
        {
            typist.CaretAt(typist.PlaceOf("Thirteen"));
            typist.Type("[Am7");
            pc.ProPresenterIsRunning = true;
            await model.WhatWasTypedIsSavedAsync();
        },
        width: 1000,
        height: 620);

    [Fact]
    public void WhatTheKeyThatWouldSaveSays() => DrawTyped(
        "28 Ctrl+S, pressed out of habit",
        false,
        "Plain Song",
        (_, model, typist) =>
        {
            typist.CaretAt(typist.PlaceOf("Thirteen"));
            typist.Type("[Am7");
            model.SaveNow();
            return Task.CompletedTask;
        },
        width: 1000,
        height: 620);

    // ---- The chord sheet (M6)

    // The window's view with a song open on its chord sheet, and someone at it with a
    // mouse and a keyboard.
    private static void DrawSheet(string name, bool dark, string song, Func<MadeUpPC, LibraryModel, Hand, Task> bring, double width = 1280, double height = 800, Action<MadeUpPC>? first = null) =>
        DrawView(
            name,
            dark,
            async pc =>
            {
                first?.Invoke(pc);
                var model = pc.Model();
                var view = new LibraryView(model);
                await ASongOpen(pc, model, song);
                await model.MarksAreWorkedOutAsync();
                // Laid out as it will be drawn, so that there are places to point at.
                Look.Wear(view, dark);
                Pictures.LayOut(view, width, height);
                await bring(pc, model, new Hand(view.SongPane.ChordSheet.Canvas));
                return view;
            },
            width,
            height);

    private static readonly Func<MadeUpPC, LibraryModel, Hand, Task> AsItIs = (_, _, _) => Task.CompletedTask;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheChordSheetAsASongOpens(bool dark) => DrawSheet("30 the chord sheet as a song opens, " + In(dark), dark, "Amazing Grace", AsItIs);

    [Fact]
    public void TheChordSheetInAWideWindow() => DrawSheet("31 the chord sheet in a wide window", true, "Amazing Grace", AsItIs, width: 1900, height: 1000);

    [Theory]
    [InlineData(-9, "smallest")]
    [InlineData(9, "largest")]
    public void TheChordSheetWithItsWordsSmallAndLarge(int steps, string said) => DrawSheet(
        "32 the chord sheet with its words at their " + said,
        true,
        "Amazing Grace",
        (_, model, _) =>
        {
            for (var step = 0; step < Math.Abs(steps); step++)
            {
                model.ZoomSheet(Math.Sign(steps));
            }
            return Task.CompletedTask;
        },
        width: 1900,
        height: 1000);

    [Fact]
    public void TheSpotOnAWord() => DrawSheet(
        "33 the spot on a word",
        true,
        "Amazing Grace",
        (_, _, hand) =>
        {
            hand.PointAt("wretch");
            return Task.CompletedTask;
        });

    [Fact]
    public void TheSpotOnAChord() => DrawSheet(
        "34 the spot on a chord",
        true,
        "Amazing Grace",
        (_, _, hand) =>
        {
            hand.PointAt(hand.PlaceOf("sweet"), onItsChord: true);
            return Task.CompletedTask;
        });

    [Fact]
    public void TheBubbleAChordIsTypedIn() => DrawSheet(
        "35 a chord being typed on the sheet",
        true,
        "Amazing Grace",
        (_, _, hand) =>
        {
            hand.Click("saved");
            hand.Type("D/");
            // (And the pointer has gone on to somewhere else, where the spot follows it.)
            hand.PointAt("lost");
            return Task.CompletedTask;
        });

    [Fact]
    public void TheBubbleOpenedOnAChordThatIsThere() => DrawSheet(
        "36 the bubble opened on a chord that is there",
        true,
        "Amazing Grace",
        (_, _, hand) =>
        {
            hand.Click(hand.PlaceOf("blind"), onItsChord: true);
            hand.Press(Key.Down);
            return Task.CompletedTask;
        });

    [Fact]
    public void AChordBeingDragged() => DrawSheet(
        "37 a chord being dragged",
        true,
        "Amazing Grace",
        (_, _, hand) =>
        {
            hand.Drag(hand.PlaceOf("me"), hand.PlaceOf("wretch"), letGo: false);
            return Task.CompletedTask;
        });

    // ---- The line of hints, and the highlighted line (spec/03, rules 53a to 53h)
    //
    // (The pictures of the spot above have the line as it is on a word and on a chord,
    // with that line's words in the highlighted line's colour.)

    // With Ctrl held at a line: how its chords are copied and pasted, and that a paste
    // there would replace. In a wide window the two sentences are side by side.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheHintsWithCtrlHeldAtALine(bool dark) => DrawSheet(
        "72 the hints with Ctrl held at a line, " + In(dark),
        dark,
        "Amazing Grace",
        (_, _, hand) =>
        {
            hand.PointAt("wretch");
            hand.Canvas.KeysHeld(ctrl: true);
            return Task.CompletedTask;
        },
        width: 1700,
        height: 900);

    // In a window of the usual size they do not fit side by side: one under the other,
    // the line two lines tall. Here after a whole verse was copied.
    [Fact]
    public void TheHintsOnCopyingOneUnderTheOther() => DrawSheet(
        "73 the hints on copying, one under the other",
        true,
        "Amazing Grace",
        (_, _, hand) =>
        {
            hand.PointAt("Amazing");
            hand.Press(Key.C, ModifierKeys.Control | ModifierKeys.Shift);
            hand.PointAt("I once was lost");
            hand.Canvas.KeysHeld(ctrl: true);
            return Task.CompletedTask;
        });

    // Lit, as it is for a moment when what it says has just changed.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheHintsLitWhenWhatTheySayHasJustChanged(bool dark) => DrawSheet(
        "74 the hints lit, as when what they say has just changed, " + In(dark),
        dark,
        "Amazing Grace",
        (_, _, hand) =>
        {
            hand.PointAt(hand.PlaceOf("sweet"), onItsChord: true);
            System.Windows.DependencyObject? up = hand.Canvas;
            while (up is not null and not ChordSheetView)
            {
                up = System.Windows.Media.VisualTreeHelper.GetParent(up) ?? System.Windows.LogicalTreeHelper.GetParent(up);
            }
            ((ChordSheetView)up!).HintLine.ShowLit(1);
            return Task.CompletedTask;
        });

    // While a chord is typed, with its name cleared: that Enter takes it off (rule 35a).
    [Fact]
    public void TheHintsWhileAChordsNameIsCleared() => DrawSheet(
        "75 the hints while a chord" + (char)0x2019 + "s name is cleared",
        true,
        "Amazing Grace",
        (_, _, hand) =>
        {
            hand.Click(hand.PlaceOf("sweet"), onItsChord: true);
            hand.Press(Key.Back);
            return Task.CompletedTask;
        });

    // A line of chords alone, highlighted: its rule in the colour, twice as thick.
    [Fact]
    public void ALineOfChordsAloneHighlighted() => DrawSheet(
        "76 a line of chords alone, highlighted",
        true,
        "Chords With No Words",
        (_, _, hand) =>
        {
            var alone = Enumerable.Range(0, hand.Sheet.Rows.Count).First(row => hand.Sheet.Usable(row) && hand.Sheet.Rows[row].Alone);
            hand.PointAt(new Spot(alone, 0));
            return Task.CompletedTask;
        });

    // ---- The guide (spec/13-guide.md: "The window, drawn into pictures and looked at:
    // as it opens, in the light and in the dark; made narrow; and the whole page")

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheGuideAsItOpens(bool dark) => DrawView(
        "77 the guide as it opens, " + In(dark),
        dark,
        _ => Task.FromResult<FrameworkElement>(new GuideView()),
        width: 780,
        height: 820);

    [Fact]
    public void TheGuideMadeNarrow() => DrawView(
        "78 the guide made narrow",
        false,
        _ => Task.FromResult<FrameworkElement>(new GuideView()),
        width: 460,
        height: 700);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheWholeOfTheGuidesPage(bool dark) => DrawView(
        "79 the whole of the guide" + (char)0x2019 + "s page, " + In(dark),
        dark,
        _ => Task.FromResult<FrameworkElement>(new GuideView()),
        width: 780,
        height: 3050);

    [Fact]
    public void TheGuideInAWideWindow() => DrawView(
        "80 the guide in a wide window",
        true,
        _ => Task.FromResult<FrameworkElement>(new GuideView()),
        width: 1300,
        height: 820);

    // (spec/03, rule 46b: the lit bubble with the chord's name, where it would land.
    // What the system itself shows meanwhile, its pointer, no picture here can have.)
    [Fact]
    public void OneOfTheKeysChordsCarriedFromTheStripOverAWord() => DrawSheet(
        "71 one of the key" + (char)0x2019 + "s chords carried from the strip over a word",
        true,
        "Amazing Grace",
        (_, _, hand) =>
        {
            Assert.True(hand.Canvas.CarriedOfKey("Em", hand.PointOf(hand.PlaceOf("wretch")), shift: false));
            return Task.CompletedTask;
        });

    [Fact]
    public void ASongWithALineOfChordsAlone() => DrawSheet(
        "38 a line of chords alone, and the spot after its last",
        true,
        "Plain Song",
        (_, _, hand) =>
        {
            var alone = Enumerable.Range(0, hand.Sheet.Rows.Count).Last(row => hand.Sheet.Rows[row].Alone);
            hand.PointAt(new Spot(alone, hand.Sheet.Stops(alone)[^1]));
            return Task.CompletedTask;
        });

    [Fact]
    public void BlankSlidesOnTheSheet() => DrawSheet("39 blank slides on the sheet", true, "Blank Slides", AsItIs);

    [Fact]
    public void TheChordSheetAtTheWindowsSmallest() => DrawSheet("40 the chord sheet at the window" + (char)0x2019 + "s smallest", true, "Amazing Grace", AsItIs, width: 640, height: 480);

    [Fact]
    public void ASongThatStatesNoKeyOnTheSheet() => DrawSheet("41 a song that states no key, on the sheet", false, "Holy Holy Holy", AsItIs);

    [Fact]
    public void AChordRefusedOnTheSheetWhileProPresenterIsRunning() => DrawSheet(
        "42 a chord refused on the sheet while ProPresenter is running",
        true,
        "Amazing Grace",
        (pc, _, hand) =>
        {
            // (Started since the app last looked: the song itself finds it running.)
            pc.ProPresenterIsRunning = true;
            hand.PointAt("wretch");
            hand.Press(Key.D4);
            return Task.CompletedTask;
        });

    [Fact]
    public void WhatAPasteSays() => DrawSheet(
        "43 a line" + (char)0x2019 + "s chords pasted onto another, and what is said",
        true,
        "Amazing Grace",
        (_, _, hand) =>
        {
            hand.PointAt("Amazing");
            hand.Press(Key.C, ModifierKeys.Control);
            hand.PointAt("Twas");
            hand.Press(Key.V, ModifierKeys.Control);
            return Task.CompletedTask;
        });

    [Fact]
    public void ALongSongOnTheSheet() => DrawSheet("44 a longer song on the sheet", true, "It Is Well", AsItIs);

    [Fact]
    public void ASongThatProPresenterMadeOnTheSheet() => DrawSheet("45 a song that ProPresenter made, on the sheet", true, "Rock Of Ages (Toplady)", AsItIs);

    // ---- The settings, and the colours of a chord

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheSettingsChords(bool dark) => DrawView(
        "46 the settings, chords, " + In(dark),
        dark,
        async pc =>
        {
            var model = pc.Model();
            await model.StartAsync();
            // (One as it comes, and one chosen: its button to put it back is then lit.)
            model.SetTextChordColour("#4f9dff");
            return new SettingsView(model, new AllCopiesModel(pc.AppData.Copies()));
        },
        width: 960,
        height: 600);

    [Fact]
    public void TheSettingsBackups() => DrawView(
        "47 the settings, backups",
        true,
        async pc =>
        {
            pc.GivePlainSongAHistory();
            var model = pc.Model();
            await model.StartAsync();
            var settings = new SettingsView(model, new AllCopiesModel(pc.AppData.Copies()));
            settings.Show("Backups");
            return settings;
        },
        width: 960,
        height: 600);

    [Fact]
    public void TheSheetsChordsInAColourChosen() => DrawSheet(
        "48 the chords of the sheet in a colour chosen",
        true,
        "Amazing Grace",
        (_, model, hand) =>
        {
            model.SetSheetChordColour("#7fd6ff");
            hand.Click("saved");
            hand.Type("Em");
            hand.PointAt(hand.PlaceOf("sweet"), onItsChord: true);
            return Task.CompletedTask;
        });

    [Fact]
    public void TheTextsChordsInAColourChosen() => DrawTyped(
        "49 the chords of the ChordPro text in a colour chosen",
        false,
        "Amazing Grace",
        (_, model, _) =>
        {
            model.SetTextChordColour("#1f7a3d");
            return Task.CompletedTask;
        });

    // ---- Mending (M8)

    // One of the files as the old editor left them, open, in the library of them.
    private static async Task AnOldCopyOpen(MadeUpPC pc, LibraryModel model, string song)
    {
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Old copies"));
        await model.MarksAreWorkedOutAsync();
        model.Open(pc.Song(song, "Old copies"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ASongWhoseChordsAreShownTwice(bool dark) => Draw(
        "50 a song whose chords ProPresenter shows twice, " + In(dark),
        dark,
        (pc, model) => AnOldCopyOpen(pc, model, "Plain Song (Chords)"));

    [Fact]
    public void ASongWhoseKeyDoesNotFitItsChords() => Draw(
        "51 a song whose key does not fit its chords",
        true,
        (pc, model) => AnOldCopyOpen(pc, model, "A Song - In A Minor Key (Chords)"));

    [Fact]
    public void TheListNarrowedToTheSongsThatWantMending() => Draw(
        "52 the list narrowed to the songs that want mending",
        true,
        async (pc, model) =>
        {
            await AnOldCopyOpen(pc, model, "It Is Well (Chords)");
            model.Filter = MarkFilter.WantingMending;
        });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MendAllAsksFirst(bool dark) => Draw(
        "53 Mend All asks first, " + In(dark),
        dark,
        async (pc, model) =>
        {
            await AnOldCopyOpen(pc, model, "It Is Well (Chords)");
            model.Filter = MarkFilter.WantingMending;
            model.AskToMendAll();
        });

    [Fact]
    public void WhatMendAllDid() => Draw(
        "54 what Mend All did",
        true,
        async (pc, model) =>
        {
            await AnOldCopyOpen(pc, model, "It Is Well (Chords)");
            model.Filter = MarkFilter.WantingMending;
            model.AskToMendAll();
            // (ProPresenter is started when three are done, so that a song that was not
            // mended is to be seen too.)
            pc.IsStartedOnceAsked = pc.TimesAsked + 3;
            await model.MendAllAsync();
        });

    [Fact]
    public void ASongOfferedToHaveItsChordsMoved() => Draw(
        "55 a song offered to have its chords moved",
        true,
        async (pc, model) =>
        {
            await AnOldCopyOpen(pc, model, "Amazing Grace (Chords)");
            model.SetOffersToMoveBack(true);
        });

    [Fact]
    public void ASongShownAsItWouldBeWithItsChordsMoved() => Draw(
        "56 a song shown as it would be with its chords moved",
        true,
        async (pc, model) =>
        {
            await AnOldCopyOpen(pc, model, "Amazing Grace (Chords)");
            model.SetOffersToMoveBack(true);
            model.Take(LibraryModel.OfferAction.ShowMoved);
        });

    // One of the old editor's copies open, with the song it was saved beside in the
    // library too, and no chords on that.
    private static Task ACopyBesideItsOriginal(MadeUpPC pc, LibraryModel model, string song)
    {
        pc.GiveTheOldCopiesTheirOriginals();
        return AnOldCopyOpen(pc, model, song);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ACopyThatOffersToPutItsChordsIntoItsOriginal(bool dark) => Draw(
        "58 a copy that offers to put its chords into its original, " + In(dark),
        dark,
        (pc, model) => ACopyBesideItsOriginal(pc, model, "It Is Well (Chords)"));

    [Fact]
    public void WhatPuttingACopysChordsInWillDoIsSaidFirst() => Draw(
        "59 what putting a copy" + (char)0x2019 + "s chords in will do is said first",
        true,
        async (pc, model) =>
        {
            await ACopyBesideItsOriginal(pc, model, "A Song - In A Minor Key (Chords)");
            model.Take(LibraryModel.OfferAction.Fold);
        });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheOriginalWithTheChordsInItAndWhatOfTheCopy(bool dark) => Draw(
        "60 the original with the chords in it, and what of the copy, " + In(dark),
        dark,
        async (pc, model) =>
        {
            await ACopyBesideItsOriginal(pc, model, "It Is Well (Chords)");
            model.Take(LibraryModel.OfferAction.Fold);
            model.Answer(true);
        });

    [Fact]
    public void TheCopyMovedOutOfTheLibrary() => Draw(
        "61 the copy moved out of the library",
        true,
        async (pc, model) =>
        {
            await ACopyBesideItsOriginal(pc, model, "It Is Well (Chords)");
            model.Take(LibraryModel.OfferAction.Fold);
            model.Answer(true);
            model.Answer(true);
        });

    [Fact]
    public void ACopyWhoseOriginalHasItsChordsAlready() => Draw(
        "62 a copy whose original has its chords already",
        true,
        async (pc, model) =>
        {
            await ACopyBesideItsOriginal(pc, model, "Plain Song (Chords)");
            model.Take(LibraryModel.OfferAction.Fold);
            model.Answer(true);
            model.Answer(false);
            model.Open(pc.Song("Plain Song (Chords)", "Old copies"));
        });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheSettingsMending(bool dark) => DrawView(
        "57 the settings, mending, " + In(dark),
        dark,
        async pc =>
        {
            var model = pc.Model();
            await model.StartAsync();
            var settings = new SettingsView(model, new AllCopiesModel(pc.AppData.Copies()));
            settings.Show("Mending");
            return settings;
        },
        width: 960,
        height: 600);

    // ---- ChordPro files (M9)

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ANewSongFromAChordProFileIsAskedAbout(bool dark) => Draw(
        "63 a new song from a ChordPro file is asked about, " + In(dark),
        dark,
        async (pc, model) =>
        {
            await ASongOpen(pc, model, "Plain Song");
            model.BringIn(ChordProFilesModelTests.ASong("amazing-grace.cho"));
        });

    [Fact]
    public void AFileThatIsNoSong() => Draw(
        "64 a file that is no song",
        true,
        async (pc, model) =>
        {
            await ASongOpen(pc, model, "Plain Song");
            model.BringIn(ChordProFilesModelTests.AFile(pc, "Only a heading.cho", "{title: Nothing}\n"));
        });

    [Fact]
    public void ANewSongIsNotMadeWhileProPresenterIsRunning() => Draw(
        "65 a new song is not made while ProPresenter is running",
        true,
        async (pc, model) =>
        {
            await ASongOpen(pc, model, "Plain Song");
            pc.ProPresenterIsRunning = true;
            model.ProPresenterMayHaveChanged();
            model.BringIn(ChordProFilesModelTests.ASong("minor-key.cho"));
            model.MakeItLookLike(null);
            model.SetLinesToASlide(1);
        });

    [Fact]
    public void WhyASongWasNotMadeIsSaidInThePanel() => Draw(
        "66 why a song was not made is said in the panel",
        false,
        async (pc, model) =>
        {
            await ASongOpen(pc, model, "Plain Song");
            model.BringIn(ChordProFilesModelTests.ASong("holy-holy-holy.txt"));
            model.MakeItLookLike(pc.Song("Nonsense"));
            model.MakeTheSong();
        });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheSongThatWasMade(bool dark) => Draw(
        "67 the song that was made, " + In(dark),
        dark,
        async (pc, model) =>
        {
            await ASongOpen(pc, model, "Plain Song");
            model.BringIn(ChordProFilesModelTests.ASong("amazing-grace.cho"));
            model.MakeTheSong();
        });

    [Fact]
    public void ANewSongAskedAboutAtTheWindowsSmallest() => Draw(
        "68 a new song asked about at the window" + (char)0x2019 + "s smallest",
        true,
        async (pc, model) =>
        {
            await ASongOpen(pc, model, "Plain Song");
            model.BringIn(ChordProFilesModelTests.ASong("it-is-well.chordpro"));
            model.NameTheNewSong("It Is Well / with my soul");
        },
        width: 640,
        height: 480);

    // ---- About (M10)

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheSettingsAbout(bool dark) => DrawView(
        "70 the settings, about, " + In(dark),
        dark,
        async pc =>
        {
            var model = pc.Model();
            await model.StartAsync();
            var settings = new SettingsView(model, new AllCopiesModel(pc.AppData.Copies()));
            settings.Show("About");
            return settings;
        },
        width: 960,
        height: 600);

    [Fact]
    public void TheSongWrittenAsAChordProFile() => Draw(
        "69 the song written as a ChordPro file",
        true,
        async (pc, model) =>
        {
            await ASongOpen(pc, model, "Plain Song");
            model.WriteAsChordPro(Path.Combine(pc.Documents, "Plain Song.cho"));
        });
}
