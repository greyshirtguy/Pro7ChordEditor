// The chord sheet's part of the window's model (LibraryModel), and the settings' Chords
// section, which is the model's three colours shown and chosen: a chord's in each of
// the two editors, and the highlighted line's.
//
// What is done on the sheet is held by a hand at it (ChordSheetViewTests.cs). Here: the
// sentences a paste is answered with, word for word (spec/03, rule 52a); what the model
// keeps of its own from one start to the next (the sheet's zoom, the colours); and
// that with no song open, or the same editor asked for, nothing happens.

using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Pro7ChordEditor.Song;
using Pro7ChordEditor.Tests;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class SheetModelTests
{
    private static void OnAMadeUpPC(Func<MadeUpPC, Task> what) => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        await what(pc);
    });

    private static async Task<LibraryModel> Open(MadeUpPC pc, string name)
    {
        var model = pc.Model();
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        model.Open(pc.Song(name));
        return model;
    }

    private static void Click(ButtonBase button) => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

    private static T Named<T>(FrameworkElement within, string name)
        where T : class =>
        within.FindName(name) as T ?? throw new InvalidOperationException("there is no " + name + " there");

    private static Color Of(Brush? brush) => ((SolidColorBrush)brush!).Color;

    // ---- What a paste says

    [Fact]
    public void APasteSaysWhatCameOfIt()
    {
        const string kinds = "chords copied from a line of words go only onto a line of words, and chords with no words only onto a line with none";
        static string? Said(int lines = 0, int chords = 0, int leftOut = 0, int passedOver = 0) =>
            LibraryModel.WhatIsSaidOf(new ChordSheet.Pasted(lines, chords, leftOut, passedOver));

        Assert.Equal("4 chords put on the line.", Said(lines: 1, chords: 4));
        Assert.Equal("1 chord put on the line.", Said(lines: 1, chords: 1));
        Assert.Equal("9 chords put on 3 lines.", Said(lines: 3, chords: 9));
        // Where some had no word to go on.
        Assert.Equal("3 chords put on the line. 1 had no word to go on, and was left out.", Said(lines: 1, chords: 3, leftOut: 1));
        Assert.Equal("9 chords put on 4 lines. 3 had no word to go on, and were left out.", Said(lines: 4, chords: 9, leftOut: 3));
        // Where a line was passed over for being of the other kind.
        Assert.Equal("4 chords put on the line. 1 line was passed over: " + kinds + ".", Said(lines: 1, chords: 4, passedOver: 1));
        Assert.Equal(
            "4 chords put on 2 lines. 2 had no word to go on, and were left out. 2 lines were passed over: " + kinds + ".",
            Said(lines: 2, chords: 4, leftOut: 2, passedOver: 2));
        // Where no chord found a word.
        Assert.Equal("None of the copied chords had a word to go on here.", Said(lines: 1, leftOut: 2));
        Assert.Equal("None of the copied chords had a word to go on here.", Said(lines: 2, leftOut: 5, passedOver: 1));
        // Where every line was passed over.
        Assert.Equal("Nothing was put here: " + kinds + ".", Said(passedOver: 1));
        Assert.Equal("Nothing was put here: " + kinds + ".", Said(passedOver: 3));
        // And where there was no line to take any of it, nothing: nothing was tried.
        Assert.Null(Said());
    }

    // The same, held to the vectors: every case of copyPaste in sheet-model.json says
    // what its paste answered (`did`) and the sentence said of that (`said`), since
    // the test data was made again on 2026-10-12. The answer itself is held where the
    // sheet is (Song.Tests, ChordSheetTests); here, that this app says of it what
    // Simple Presenter says, to the letter.
    [Fact]
    public void WhatAPasteSaysIsWhatTheVectorsSayOfEveryPasteTheyHave()
    {
        using var vectors = JsonDocument.Parse(TestData.Vector("sheet-model.json"));
        var cases = vectors.RootElement.GetProperty("copyPaste").EnumerateArray().ToList();
        Assert.True(cases.Count >= 14, "sheet-model.json had fourteen pastes when this was written, and has " + cases.Count);
        var sentences = new HashSet<string>(StringComparer.Ordinal);
        foreach (var expected in cases)
        {
            var title = expected.GetProperty("title").GetString()!;
            var did = expected.GetProperty("pasted").GetProperty("did");
            int Of(string name) => did.GetProperty(name).GetInt32();
            var pasted = new ChordSheet.Pasted(Of("lines"), Of("chords"), Of("leftOut"), Of("passedOver"), Of("refused"));
            var said = expected.GetProperty("pasted").GetProperty("said").EnumerateArray().Select(sentence => sentence.GetString()!).ToList();
            var here = LibraryModel.WhatIsSaidOf(pasted);
            Assert.True(said.SequenceEqual(here is null ? [] : [here], StringComparer.Ordinal), title + ": said " + (here ?? "nothing"));
            sentences.UnionWith(said);
        }
        // (So that it is known what the cases hold between them: a plain paste, one
        // with chords left out, one where nothing was put, one with a line passed over.)
        Assert.Contains(sentences, sentence => sentence.EndsWith("put on the line.", StringComparison.Ordinal));
        Assert.Contains(sentences, sentence => sentence.Contains("left out", StringComparison.Ordinal));
        Assert.Contains(sentences, sentence => sentence.StartsWith("Nothing was put here", StringComparison.Ordinal));
        Assert.Contains(sentences, sentence => sentence.Contains("1 line was passed over", StringComparison.Ordinal));
    }

    // ---- With no song, and with the same editor

    [Fact]
    public void WithNoSongOpenTheSheetsPartDoesNothingAndASongOpenedHasItsOwnSheet() => OnAMadeUpPC(async pc =>
    {
        var model = pc.Model();
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        Assert.Null(model.Sheet);
        Assert.Equal(LibraryModel.Editor.Sheet, model.EditorShown);
        // (The quiet line says how many songs the library has: not what the keys do on
        // a sheet there is not.)
        var said = model.Notice;
        Assert.StartsWith("9 songs", said, StringComparison.Ordinal);
        var changes = 0;
        model.PropertyChanged += (_, _) => changes++;
        model.PutOfKey(1);
        model.CopyChords(wholeGroup: false);
        model.PasteChords();
        model.Undo();
        model.Redo();
        Assert.Equal(0, changes);
        Assert.False(model.CanUndo);
        Assert.False(model.CanRedo);
        Assert.Equal(said, model.Notice);
        // The editor that is shown already, asked for: nothing.
        model.Show(LibraryModel.Editor.Sheet);
        Assert.Equal(0, changes);
        // The other can be asked for before any song is open, and is what the first
        // song then opens in.
        model.Show(LibraryModel.Editor.ChordPro);
        Assert.Equal(1, changes);
        Assert.Equal(LibraryModel.Editor.ChordPro, model.EditorShown);
        model.Show(LibraryModel.Editor.Sheet);

        // A song opened has its sheet: its rows are the song's lines, with a row for
        // each group's name.
        var version = model.SheetVersion;
        model.Open(pc.Song("Plain Song"));
        var sheet = model.Sheet;
        Assert.NotNull(sheet);
        Assert.True(model.SheetVersion > version);
        Assert.Equal(["Verse 1", "Chorus", "Outro"], sheet.Rows.Where(row => row.Kind == SheetRowKind.Group).Select(row => row.Name));
        Assert.Equal(7, sheet.Rows.Count(row => row.Kind == SheetRowKind.Line));
        Assert.Equal("G", sheet.Key);
        Assert.Matches("^[0-9]+ slides?, [0-9]+ chords?[.]$", model.Notice);
        // With no spot there is nothing to copy, and nothing is said.
        model.CopyChords(wholeGroup: false);
        Assert.Matches("^[0-9]+ slides?, [0-9]+ chords?[.]$", model.Notice);
        // Another song has another sheet; one that could not be read has none.
        model.Open(pc.Song("Amazing Grace"));
        Assert.NotSame(sheet, model.Sheet);
        Assert.Contains(model.Sheet!.Rows, row => row.Text == "That saved a wretch like me");
        model.Open(pc.Song("Nonsense"));
        Assert.Null(model.Sheet);
        Assert.NotNull(model.Shown!.Problem);
    });

    // ---- What the app keeps of its own

    [Fact]
    public void TheSheetsZoomGoesByItsStepsAndIsRememberedAndOneThatIsNoStepIsTakenAsTheNearest() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        Assert.Equal([0.5, 0.6, 0.7, 0.8, 0.9, 1.0, 1.1, 1.25, 1.5], LibraryModel.Zooms);
        Assert.Equal(0.8, model.SheetZoom);
        Assert.Null(pc.AppData.ReadSettings().SheetZoom);
        var changes = 0;
        model.PropertyChanged += (_, _) => changes++;
        // A step up answers with the size it then is, and is kept.
        Assert.Equal(0.9, model.ZoomSheet(1));
        Assert.Equal(1, changes);
        Assert.Equal(0.9, pc.AppData.ReadSettings().SheetZoom);
        // However far it is asked to go, it goes a step.
        Assert.Equal(1.0, model.ZoomSheet(7));
        Assert.Equal(0.9, model.ZoomSheet(-3));
        // At either end it stays, and nothing is said to have changed.
        for (var times = 0; times < 12; times++)
        {
            model.ZoomSheet(1);
        }
        Assert.Equal(1.5, model.SheetZoom);
        Assert.False(model.CanZoomSheetIn);
        Assert.True(model.CanZoomSheetOut);
        changes = 0;
        Assert.Equal(1.5, model.ZoomSheet(1));
        Assert.Equal(0, changes);
        for (var times = 0; times < 12; times++)
        {
            model.ZoomSheet(-1);
        }
        Assert.Equal(0.5, model.SheetZoom);
        Assert.False(model.CanZoomSheetOut);
        Assert.True(model.CanZoomSheetIn);
        // The usual size is kept as nothing at all.
        Assert.Equal(0.8, model.ZoomSheet(0));
        Assert.Null(pc.AppData.ReadSettings().SheetZoom);
        // The text's size is its own: neither moves the other.
        Assert.Equal(1.0, model.TextZoom);
        model.ZoomText(1);
        Assert.Equal((1.1, 0.8), (model.TextZoom, model.SheetZoom));
        model.ZoomSheet(-1);
        Assert.Equal((1.1, 0.7), (model.TextZoom, model.SheetZoom));

        // Remembered from one start to the next.
        var again = pc.Model();
        await again.StartAsync();
        Assert.Equal((1.1, 0.7), (again.TextZoom, again.SheetZoom));
        // A size kept that is none of the steps (written by hand, or by a build with
        // other steps) is taken as the nearest that is.
        foreach (var (kept, taken) in new[] { (0.83, 0.8), (0.86, 0.9), (4.0, 1.5), (0.01, 0.5), (1.2, 1.25) })
        {
            pc.AppData.Keep(pc.AppData.ReadSettings() with { SheetZoom = kept });
            var next = pc.Model();
            await next.StartAsync();
            Assert.Equal(taken, next.SheetZoom);
        }
    });

    [Fact]
    public void AColourIsKeptAsItIsWrittenAndWhatIsNoColourIsTakenAsNone()
    {
        Assert.Equal("#ffd9a8", LibraryModel.ColourAsKept("#ffd9a8"));
        Assert.Equal("#ffd9a8", LibraryModel.ColourAsKept("#FFD9A8"));
        Assert.Equal("#00ff7f", LibraryModel.ColourAsKept("#00Ff7F"));
        foreach (var none in new string?[] { null, "", "#", "ffd9a8", "0ffd9a8", "affd9a8", "#ffd9a", "#ffd9a8a", "#gggggg", "#12345g", " #ffd9a8", "#ffd9a8 ", "#+12345", "orange" })
        {
            Assert.Null(LibraryModel.ColourAsKept(none));
        }

        // As one to draw with, and written again.
        Assert.Equal(Color.FromRgb(0xFF, 0x8A, 0x1F), SheetLook.Written("#ff8a1f"));
        Assert.Equal(Color.FromRgb(0x00, 0x00, 0x00), SheetLook.Written("#000000"));
        Assert.Equal(Color.FromRgb(0xAB, 0xCD, 0xEF), SheetLook.Written("#ABCDEF"));
        foreach (var none in new string?[] { null, "", "ff8a1f", "#ff8a1", "#ff8a1f0", "#xyzxyz", "#+12345", "#-12345" })
        {
            Assert.Null(SheetLook.Written(none));
        }
        Assert.Equal("#ff8a1f", SheetLook.AsWritten(Color.FromRgb(0xFF, 0x8A, 0x1F)));
        Assert.Equal("#000a0b", SheetLook.AsWritten(Color.FromRgb(0, 10, 11)));
        // (How see-through a colour is, is not kept: a chord is drawn solid.)
        Assert.Equal("#102030", SheetLook.AsWritten(Color.FromArgb(0x40, 0x10, 0x20, 0x30)));

        // As the system's colour picker writes one, the red lowest; and read back.
        Assert.Equal(0x332211, ColourPicker.AsTheSystemWrites(Color.FromRgb(0x11, 0x22, 0x33)));
        Assert.Equal(Color.FromRgb(0x11, 0x22, 0x33), ColourPicker.FromTheSystem(0x332211));
        Assert.Equal(Color.FromRgb(0xFF, 0x8A, 0x1F), ColourPicker.FromTheSystem(ColourPicker.AsTheSystemWrites(Color.FromRgb(0xFF, 0x8A, 0x1F))));
        // The words on a colour are black where it is a light one.
        Assert.True(SheetLook.IsLight(Colors.White));
        Assert.True(SheetLook.IsLight(Color.FromRgb(0xFF, 0xD9, 0xA8)));
        Assert.False(SheetLook.IsLight(Colors.Black));
        Assert.False(SheetLook.IsLight(Color.FromRgb(0x1D, 0x6F, 0xD6)));
        // A colour as a file has one, each part from nought to one.
        Assert.Equal(Color.FromRgb(255, 128, 0), SheetLook.Of(new Colour(1, 0.5, 0)));
        Assert.Equal(Color.FromRgb(255, 0, 0), SheetLook.Of(new Colour(7, -2, 0)));
    }

    [Fact]
    public void TheColoursOfAChordAreChosenInTheSettingsAndRememberedAndBothEditorsDrawWithThem() => OnAMadeUpPC(async pc =>
    {
        var model = pc.Model();
        var view = new LibraryView(model);
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        model.Open(pc.Song("Amazing Grace"));
        Look.Wear(view, dark: true);
        Pictures.LayOut(view, 1280, 800);
        var canvas = view.SongPane.ChordSheet.Canvas;
        var underlay = view.SongPane.ChordPro.Underlay;

        var settings = new SettingsView(model, new AllCopiesModel(pc.AppData.Copies()));
        Look.Wear(settings, dark: true);
        Pictures.LayOut(settings, 960, 600);
        var chords = settings.ChordColours;
        var orange = Color.FromRgb(0xFF, 0x95, 0x00);

        // As the app comes: the sheet's orange (#ff8a1f, since 2026-10-14) and the
        // text's, with nothing kept and nothing to put back.
        Assert.Null(model.SheetChordColour);
        Assert.Null(model.TextChordColour);
        Assert.Equal(SheetLook.ChordName, Of(Named<Border>(chords, "SheetColour").Background));
        Assert.Equal(SheetLook.ChordName, Of(Named<TextBlock>(chords, "SheetSample").Foreground));
        Assert.Equal(orange, Of(Named<Border>(chords, "TextColour").Background));
        Assert.Equal(orange, ChordColourSettings.UsualInTheText);
        Assert.False(Named<Button>(chords, "SheetUsual").IsEnabled);
        Assert.False(Named<Button>(chords, "TextUsual").IsEnabled);
        Assert.Equal(SheetLook.ChordName, canvas.ChordColour);
        Assert.Equal(orange, Of(underlay.ChordBrush));

        // The well asks for a colour, starting from the one it has. None chosen, and
        // nothing changes.
        Color? from = null;
        Color? answer = null;
        chords.AskForAColour = start =>
        {
            from = start;
            return answer;
        };
        Click(Named<Button>(chords, "SheetWell"));
        Assert.Equal(SheetLook.ChordName, from);
        Assert.Null(model.SheetChordColour);

        // One chosen: it is kept as it is written, and is on the sample and on the
        // sheet behind at once.
        var green = Color.FromRgb(0x33, 0xCC, 0x66);
        answer = green;
        Click(Named<Button>(chords, "SheetWell"));
        Assert.Equal("#33cc66", model.SheetChordColour);
        Assert.Equal("#33cc66", pc.AppData.ReadSettings().SheetChordColour);
        Assert.Equal(green, Of(Named<Border>(chords, "SheetColour").Background));
        Assert.Equal(green, Of(Named<TextBlock>(chords, "SheetSample").Foreground));
        Assert.True(Named<Button>(chords, "SheetUsual").IsEnabled);
        Assert.Equal(green, canvas.ChordColour);
        // (Drawn: the names of the sheet's chords are in it.)
        static int Green((int Width, int Height, int[] Dots) drawn) =>
            drawn.Dots.Count(dot => ((dot >> 8) & 0xFF) > 170 && ((dot >> 16) & 0xFF) < 90 && (dot & 0xFF) is > 60 and < 140);
        Assert.True(Green(Pictures.Drawn(view, 1280, 800)) > 400, "the sheet's chords are not drawn in the colour chosen");
        // At another size they are in it still: the measures are made again for a size,
        // and the colour is not theirs to forget.
        model.ZoomSheet(1);
        Assert.True(Green(Pictures.Drawn(view, 1280, 800)) > 400, "at another size the sheet's chords are not drawn in the colour chosen");
        model.ZoomSheet(-1);
        // And what is typed in the bubble is in it.
        Assert.Equal(green, Of(canvas.Entry.Ink));
        // The text's is its own, and has not changed: nor is there a usual colour for it
        // to be put back to.
        Assert.Null(model.TextChordColour);
        Assert.Equal(orange, Of(underlay.ChordBrush));
        Assert.False(Named<Button>(chords, "TextUsual").IsEnabled);
        // Asked again, the well starts from the colour chosen.
        Click(Named<Button>(chords, "SheetWell"));
        Assert.Equal(green, from);

        // The text's: asked from its usual orange, and then drawn in what was chosen.
        var blue = Color.FromRgb(0x44, 0x88, 0xFF);
        answer = blue;
        Click(Named<Button>(chords, "TextWell"));
        Assert.Equal(orange, from);
        Assert.Equal("#4488ff", model.TextChordColour);
        Assert.Equal(blue, Of(Named<Border>(chords, "TextColour").Background));
        Assert.True(Named<Button>(chords, "TextUsual").IsEnabled);
        Assert.Equal(blue, Of(underlay.ChordBrush));
        Assert.Equal(green, canvas.ChordColour);

        // Both are remembered from one start of the app to the next.
        var again = pc.Model();
        await again.StartAsync();
        Assert.Equal(("#33cc66", "#4488ff"), (again.SheetChordColour, again.TextChordColour));
        // One kept that is no colour is taken as none.
        pc.AppData.Keep(pc.AppData.ReadSettings() with { SheetChordColour = "green", TextChordColour = "#4488FF" });
        var next = pc.Model();
        await next.StartAsync();
        Assert.Equal((null, "#4488ff"), (next.SheetChordColour, next.TextChordColour));

        // Usual Colour puts back the one each came with, and nothing is kept.
        Click(Named<Button>(chords, "SheetUsual"));
        Assert.Null(model.SheetChordColour);
        Assert.True(Named<Button>(chords, "TextUsual").IsEnabled);
        Assert.Equal(SheetLook.ChordName, canvas.ChordColour);
        Assert.Equal(SheetLook.ChordName, Of(canvas.Entry.Ink));
        Assert.Equal(SheetLook.ChordName, Of(Named<Border>(chords, "SheetColour").Background));
        Assert.False(Named<Button>(chords, "SheetUsual").IsEnabled);
        Click(Named<Button>(chords, "TextUsual"));
        Assert.Null(model.TextChordColour);
        Assert.Equal(orange, Of(underlay.ChordBrush));
        Assert.Equal(orange, Of(Named<Border>(chords, "TextColour").Background));
        Assert.Null(pc.AppData.ReadSettings().SheetChordColour);
        Assert.Null(pc.AppData.ReadSettings().TextChordColour);
        // Nothing of any of this is the song's: its file is as it was.
        Assert.False(model.ChangedSinceOpened);
        Assert.Equal(0, Green(Pictures.Drawn(view, 1280, 800)));
    });

    // spec/06, rule 20a: "between those two, the colour of the highlighted line of the
    // chord sheet ... chosen and put back the same way, and shown as two lines of
    // words, the second in the colour. Under its name it says what it is".
    [Fact]
    public void TheColourOfTheHighlightedLineIsChosenInTheSettingsBetweenTheTwoAndRemembered() => OnAMadeUpPC(async pc =>
    {
        var model = pc.Model();
        var view = new LibraryView(model);
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        model.Open(pc.Song("Amazing Grace"));
        Look.Wear(view, dark: true);
        Pictures.LayOut(view, 1280, 800);
        var canvas = view.SongPane.ChordSheet.Canvas;
        var settings = new SettingsView(model, new AllCopiesModel(pc.AppData.Copies()));
        Look.Wear(settings, dark: true);
        Pictures.LayOut(settings, 960, 700);
        var chords = settings.ChordColours;

        // As the app comes: the sheet's orange, with nothing kept and nothing to put back.
        Assert.Null(model.SheetLineColour);
        Assert.Equal("#ff8a1f", SheetLook.AsWritten(SheetLook.LineWords));
        Assert.Equal(SheetLook.LineWords, Of(Named<Border>(chords, "LineColour").Background));
        Assert.Equal(SheetLook.LineWords, Of(Named<TextBlock>(chords, "LineSample").Foreground));
        Assert.False(Named<Button>(chords, "LineUsual").IsEnabled);
        Assert.Equal(SheetLook.LineWords, canvas.LineColour);
        // What it is, in the page's words with Windows' keys.
        Assert.Equal(
            "The line the pointer is on, on the chord sheet: where Ctrl+C copies from and Ctrl+V pastes to.",
            Named<TextBlock>(chords, "LineSays").Text);
        // It is between the sheet's chord colour and the text's.
        double Top(string name) => Named<FrameworkElement>(chords, name).TranslatePoint(new Point(0, 0), chords).Y;
        Assert.True(Top("SheetWell") < Top("LineWell") && Top("LineWell") < Top("TextWell"));

        // The well asks for a colour, starting from the one it has.
        Color? from = null;
        Color? answer = null;
        chords.AskForAColour = start =>
        {
            from = start;
            return answer;
        };
        Click(Named<Button>(chords, "LineWell"));
        Assert.Equal(SheetLook.LineWords, from);
        Assert.Null(model.SheetLineColour);

        // One chosen: it is kept as it is written, and is on the sample and on the
        // sheet behind at once. The chords' colours are their own.
        var green = Color.FromRgb(0x33, 0xCC, 0x66);
        answer = green;
        Click(Named<Button>(chords, "LineWell"));
        Assert.Equal("#33cc66", model.SheetLineColour);
        Assert.Equal("#33cc66", pc.AppData.ReadSettings().SheetLineColour);
        Assert.Equal(green, Of(Named<Border>(chords, "LineColour").Background));
        Assert.Equal(green, Of(Named<TextBlock>(chords, "LineSample").Foreground));
        Assert.True(Named<Button>(chords, "LineUsual").IsEnabled);
        Assert.Equal(green, canvas.LineColour);
        Assert.Null(model.SheetChordColour);
        Assert.Equal(SheetLook.ChordName, canvas.ChordColour);
        Click(Named<Button>(chords, "LineWell"));
        Assert.Equal(green, from);

        // It is remembered from one start of the app to the next; one kept that is no
        // colour is taken as none.
        var again = pc.Model();
        await again.StartAsync();
        Assert.Equal("#33cc66", again.SheetLineColour);
        pc.AppData.Keep(pc.AppData.ReadSettings() with { SheetLineColour = "green" });
        var next = pc.Model();
        await next.StartAsync();
        Assert.Null(next.SheetLineColour);

        // Usual Colour puts back the one it came with, and nothing is kept.
        Click(Named<Button>(chords, "LineUsual"));
        Assert.Null(model.SheetLineColour);
        Assert.Null(pc.AppData.ReadSettings().SheetLineColour);
        Assert.Equal(SheetLook.LineWords, canvas.LineColour);
        Assert.False(Named<Button>(chords, "LineUsual").IsEnabled);
        // Nothing of this is the song's: its file is as it was.
        Assert.False(model.ChangedSinceOpened);
    });

    // spec/03, rule 10a, and spec/06, rules 12 and 20a: sharps and flats are drawn as
    // the music signs on the sheet, in the strip, in what is offered and in the song's
    // two keys, as the app comes; the last thing under Chords in the settings turns that
    // off, and it is remembered only when off. Only drawn: the file, and the ChordPro
    // text, have # and b either way.
    [Fact]
    public void SharpsAndFlatsAreDrawnAsSignsUntilTheSwitchInTheSettingsIsTurnedOff() => OnAMadeUpPC(async pc =>
    {
        var sharp = ((char)0x266F).ToString();
        var flat = ((char)0x266D).ToString();
        var model = pc.Model();
        var view = new LibraryView(model);
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        model.Open(pc.Song("Amazing Grace"));
        Look.Wear(view, dark: true);
        Pictures.LayOut(view, 1280, 800);
        var canvas = view.SongPane.ChordSheet.Canvas;
        var strip = view.SongPane.ChordSheet;
        var key = Named<ComboBox>(view.SongPane, "Key");
        var settings = new SettingsView(model, new AllCopiesModel(pc.AppData.Copies()));
        Look.Wear(settings, dark: true);
        Pictures.LayOut(settings, 960, 648);
        var chords = settings.ChordColours;
        string KeyListed(string name) => key.Items.OfType<SongPane.KeyRow>().Single(row => row.Key == name).Name;
        IEnumerable<string> InTheSong() => new OpenSong(pc.Song("Amazing Grace")).TextBoxes.SelectMany(box => box.Chords).Select(chord => chord.Name);

        // As the app comes: on, and nothing kept of it.
        Assert.True(model.ChordSigns);
        Assert.True(chords.SignsSwitch.IsChecked);
        Assert.Null(pc.AppData.ReadSettings().ChordSigns);
        Assert.Equal(["B" + flat + "m7", "F" + sharp, "E" + flat + "/G"], chords.SignsShown);
        Assert.True(canvas.Metrics.Signs);
        Assert.Equal("B" + flat, canvas.Metrics.Name("Bb").Text);
        Assert.Equal("7 F" + sharp + "dim", System.Windows.Automation.AutomationProperties.GetName(strip.ChordsOfTheKey[6]));
        // A key is listed with its signs, and chosen by its name.
        Assert.Equal("B" + flat, KeyListed("Bb"));
        Assert.Equal("F" + sharp + "m", KeyListed("F#m"));
        Assert.Equal("G", key.SelectedValue);

        // A chord typed with a flat is drawn with its sign, and saved with b; and one of
        // the key's chords with a sharp, put on by its number, is saved with #. (Hand.cs
        // types as a person does.)
        var hand = new Hand(canvas);
        hand.Click("saved");
        hand.Type("Bb");
        Assert.Equal("B" + flat, hand.Box.Text);
        hand.Press(System.Windows.Input.Key.Enter);
        hand.PointAt("wretch");
        hand.Press(System.Windows.Input.Key.D7);
        Assert.Contains("Bb", InTheSong());
        Assert.Contains("F#dim", InTheSong());
        Assert.DoesNotContain(InTheSong(), name => name.Contains(sharp, StringComparison.Ordinal) || name.Contains(flat, StringComparison.Ordinal));
        // The ChordPro text is as ChordPro is written.
        model.Show(LibraryModel.Editor.ChordPro);
        Assert.Contains("[Bb]", model.ChordPro!.Text, StringComparison.Ordinal);
        Assert.Contains("[F#dim]", model.ChordPro.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(sharp, model.ChordPro.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(flat, model.ChordPro.Text, StringComparison.Ordinal);
        model.Show(LibraryModel.Editor.Sheet);
        Pictures.LayOut(view, 1280, 800);

        // Turned off: everything is drawn as it was before there were signs, and that
        // is kept.
        chords.SignsSwitch.IsChecked = false;
        Click(chords.SignsSwitch);
        Pictures.LayOut(view, 1280, 800);
        Assert.False(model.ChordSigns);
        Assert.False(pc.AppData.ReadSettings().ChordSigns);
        Assert.Equal(["Bbm7", "F#", "Eb/G"], chords.SignsShown);
        Assert.False(canvas.Metrics.Signs);
        Assert.Equal("Bb", canvas.Metrics.Name("Bb").Text);
        Assert.Equal("7 F#dim", System.Windows.Automation.AutomationProperties.GetName(strip.ChordsOfTheKey[6]));
        Assert.Equal("Bb", KeyListed("Bb"));
        hand.Click("lost");
        hand.Type("Eb");
        Assert.Equal("Eb", hand.Box.Text);
        hand.Press(System.Windows.Input.Key.Escape);
        // It is remembered from one start of the app to the next.
        var again = pc.Model();
        await again.StartAsync();
        Assert.False(again.ChordSigns);

        // On again: nothing is kept, as it comes.
        chords.SignsSwitch.IsChecked = true;
        Click(chords.SignsSwitch);
        Pictures.LayOut(view, 1280, 800);
        Assert.True(model.ChordSigns);
        Assert.Null(pc.AppData.ReadSettings().ChordSigns);
        Assert.Equal("B" + flat, canvas.Metrics.Name("Bb").Text);
    });

    [Fact]
    public void TheSettingsHaveFourSectionsChordsMendingBackupsAndAbout() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var settings = new SettingsView(model, new AllCopiesModel(pc.AppData.Copies()));
        Look.Wear(settings, dark: true);
        Pictures.LayOut(settings, 960, 600);
        var sections = Named<TabControl>(settings, "Sections");
        Assert.Equal(["Chords", "Mending", "Backups", "About"], sections.Items.OfType<TabItem>().Select(tab => (string)tab.Header));
        Assert.IsType<ChordColourSettings>(((TabItem)sections.SelectedItem).Content);
        settings.Show("Mending");
        Pictures.LayOut(settings, 960, 600);
        Assert.IsType<MendingSettings>(((TabItem)sections.SelectedItem).Content);
        settings.Show("Backups");
        Pictures.LayOut(settings, 960, 600);
        Assert.IsType<AllCopiesView>(((TabItem)sections.SelectedItem).Content);
        settings.Show("About");
        Pictures.LayOut(settings, 960, 600);
        Assert.Same(settings.AboutTheApp, ((TabItem)sections.SelectedItem).Content);
        settings.Show("Chords");
        Assert.Same(settings.ChordColours, ((TabItem)sections.SelectedItem).Content);
    });
}
