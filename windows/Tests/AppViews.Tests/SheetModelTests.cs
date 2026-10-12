// The chord sheet's part of the window's model (LibraryModel), and the settings' Chords
// section, which is the model's two colours shown and chosen.
//
// What is done on the sheet is held by a hand at it (ChordSheetViewTests.cs). Here: the
// sentences a paste is answered with, word for word (spec/03, rule 52a); what the model
// keeps of its own from one start to the next (the sheet's zoom, the two colours); and
// that with no song open, or the same editor asked for, nothing happens.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Pro7ChordEditor.Song;
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
        Assert.Equal(LibraryModel.SheetHelp, model.Notice);
        // With no spot there is nothing to copy, and nothing is said.
        model.CopyChords(wholeGroup: false);
        Assert.Equal(LibraryModel.SheetHelp, model.Notice);
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

        // As the app comes: the sheet's #ffd9a8 and the text's orange, with nothing kept
        // and nothing to put back.
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
