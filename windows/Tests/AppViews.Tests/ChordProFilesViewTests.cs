// ChordPro files in the window itself: the panel of a new song (NewSongPanel), the two
// commands of the File menu, and the keys while the panel is there. The views laid out
// with no window, as the others are: what they show, and that what is typed, chosen and
// clicked in them reaches the model.
//
// What the model then does is ChordProFilesModelTests'; what a hand on a mouse can
// tell, and the system's own choosers of a file, are for the maintainer (NOTES.md).

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class ChordProFilesViewTests
{
    private static readonly string OpenQ = ((char)0x201C).ToString();
    private static readonly string CloseQ = ((char)0x201D).ToString();

    private static string Q(string name) => OpenQ + name + CloseQ;

    private static void OnAMadeUpPC(Func<MadeUpPC, Task> what) => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        await what(pc);
    });

    private static async Task<(LibraryModel Model, LibraryView View)> AtTheSongs(MadeUpPC pc, string? open = null)
    {
        var model = pc.Model();
        var view = new LibraryView(model);
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        if (open is not null)
        {
            model.Open(pc.Song(open));
        }
        Look.Wear(view, dark: true);
        LayOut(view);
        return (model, view);
    }

    private static void LayOut(LibraryView view) => Pictures.LayOut(view, 1280, 800);

    private static void Click(ButtonBase button) => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

    private static string Label(ContentControl button) => (string)button.Content;

    private static string ASong(string name) => ChordProFilesModelTests.ASong(name);

    // ---- The panel

    [Fact]
    public void ThePanelIsOverTheWindowWithWhatWasFoundAndWhatTheSongIsToBe() => OnAMadeUpPC(async pc =>
    {
        var (model, view) = await AtTheSongs(pc);
        var panel = view.NewSongPanel;
        Assert.Equal(Visibility.Collapsed, panel.Visibility);

        model.BringIn(ASong("amazing-grace.cho"));
        LayOut(view);

        Assert.Equal(Visibility.Visible, panel.Visibility);
        // It is over the whole window.
        Assert.Equal((0.0, view.ActualHeight, view.ActualWidth), (panel.TranslatePoint(new Point(0, 0), view).Y, panel.ActualHeight, panel.ActualWidth));
        var says = panel.Says;
        Assert.Equal("New song from amazing-grace.cho", says.Heading);
        Assert.Equal(
            [
                "Title: Amazing Grace",
                "Artist: John Newton",
                "Key: G",
                "Parts: Verse 1 (4 lines), Verse 2 (4 lines), Verse 3 (4 lines), Ending (1 line)",
                "Chords: 40 chords in all",
            ],
            says.Found);
        Assert.Equal(
            (Q("Songs") + " has a song named " + Q("Amazing Grace") + " already. This one will be named " + Q("Amazing Grace 2") + ".", "Songs", "which makes 7 slides", ""),
            (says.NameNote, says.Library, says.Slides, says.Standing));
        // The fields: the name, the lines to a slide (two, of one to twelve), and the
        // song to look like (the app's own, of that and the library's nine).
        var (named, lines, like) = panel.Fields;
        Assert.True(panel.HasItsFields);
        Assert.Equal("Amazing Grace", named.Text);
        Assert.Equal(("2", 12, "1", "12"), ((string)lines.SelectedItem, lines.Items.Count, (string)lines.Items[0], (string)lines.Items[^1]));
        Assert.Equal(("A song this app carries", 10), (((LibraryModel.LookAlike)like.SelectedItem).Name, like.Items.Count));
        Assert.Equal(MadeUpPC.Songs, like.Items.Cast<LibraryModel.LookAlike>().Skip(1).Select(one => one.Name));
        // The buttons: Make the Song is what Enter gives, and Cancel what Esc gives.
        var (make, other) = panel.Buttons;
        Assert.Equal(("Make the Song", Visibility.Visible, true, true), (Label(make), make.Visibility, make.IsEnabled, make.IsDefault));
        Assert.Equal(("Cancel", true), (Label(other), other.IsCancel));
        // All of it is inside the window, and no row of what was found is cut off.
        Assert.All(new FrameworkElement[] { named, lines, like, make, other }, part =>
        {
            var at = part.TranslatePoint(new Point(0, 0), view);
            Assert.True(part.ActualWidth > 0 && part.ActualHeight > 0, "a part of the panel has no size");
            Assert.True(at.X >= 0 && at.Y >= 0 && at.X + part.ActualWidth <= view.ActualWidth && at.Y + part.ActualHeight <= view.ActualHeight, "a part of the panel is outside the window");
        });

        // Cancel: the panel goes, and nothing was made.
        Click(other);
        Assert.Equal(Visibility.Collapsed, panel.Visibility);
        Assert.Null(model.NewSong);
        Assert.Equal(MadeUpPC.Songs, model.Songs.Select(song => song.Name));
        Assert.Null(model.SongId);
        Assert.Equal(MadeUpPC.Songs.Length, Directory.GetFiles(Path.Combine(pc.Place, "Libraries", "Songs")).Length);
    });

    [Fact]
    public void WhatIsTypedAndChosenInThePanelReachesTheModelAndWhatTheModelSaysComesBack() => OnAMadeUpPC(async pc =>
    {
        var (model, view) = await AtTheSongs(pc, "Plain Song");
        var panel = view.NewSongPanel;
        model.BringIn(ASong("amazing-grace.cho"));
        var (named, lines, like) = panel.Fields;
        // The song that is open is the one to look like.
        Assert.Equal("Plain Song", ((LibraryModel.LookAlike)like.SelectedItem).Name);

        // The name, as it is typed.
        named.Text = "Grace / Again";
        Assert.Equal(("Grace / Again", "Grace - Again"), (model.NewSong!.Name, model.NewSong.WillBeNamed));
        Assert.Equal("As a file can be named, that is " + Q("Grace - Again") + ".", panel.Says.NameNote);
        Assert.Equal("Grace / Again", named.Text);
        named.Text = "Grace Again";
        Assert.Equal("", panel.Says.NameNote);
        // The lines to a slide.
        lines.SelectedIndex = 3;
        Assert.Equal((4, "which makes 4 slides"), (model.NewSong!.LinesToASlide, panel.Says.Slides));
        lines.SelectedItem = "1";
        Assert.Equal((1, "which makes 13 slides"), (model.NewSong!.LinesToASlide, panel.Says.Slides));
        // The song to look like.
        like.SelectedItem = like.Items.Cast<LibraryModel.LookAlike>().Single(one => one.Name == "It Is Well");
        Assert.Equal("It Is Well", model.NewSong!.Like.Name);
        like.SelectedIndex = 0;
        Assert.Equal((null, "A song this app carries"), (model.NewSong!.Like.Id, model.NewSong.Like.Name));

        // What the model says by itself comes into the fields: here, set from outside.
        model.NameTheNewSong("Named elsewhere");
        model.SetLinesToASlide(6);
        model.MakeItLookLike(pc.Song("Holy Holy Holy"));
        Assert.Equal(("Named elsewhere", "6", "Holy Holy Holy"), (named.Text, (string)lines.SelectedItem, ((LibraryModel.LookAlike)like.SelectedItem).Name));

        // Make the Song: the panel goes, and the song is the one in the song's pane.
        Click(panel.Buttons.Make);
        LayOut(view);
        Assert.Equal(Visibility.Collapsed, panel.Visibility);
        Assert.Equal(("Named elsewhere", pc.Song("Named elsewhere")), (model.Shown!.Name, model.SongId));
        Assert.Contains("Named elsewhere", view.ListsPane.RowsShown.Select(row => row.Name));
        // (Six lines to a slide: each verse is one slide, and the ending another.)
        Assert.Equal((40, 4), (model.Shown.Chords, model.Shown.Slides));

        // Asked of another file, the panel is that file's: nothing of the last is in it.
        model.BringIn(ASong("plain-song.cho"));
        Assert.Equal(("New song from plain-song.cho", "Plain Song", "2"), (panel.Says.Heading, named.Text, (string)lines.SelectedItem));
        Assert.Equal("Title: Plain Song", panel.Says.Found[0]);
        Assert.Equal("Named elsewhere", ((LibraryModel.LookAlike)like.SelectedItem).Name);
        Assert.Equal(11, like.Items.Count);
    });

    [Fact]
    public void AFileThatIsNoSongHasOnlyItsSentenceAndClose() => OnAMadeUpPC(async pc =>
    {
        var (model, view) = await AtTheSongs(pc);
        var panel = view.NewSongPanel;
        model.BringIn(ChordProFilesModelTests.AFile(pc, "Only a heading.cho", "{title: Nothing}\n"));
        LayOut(view);

        Assert.Equal(Visibility.Visible, panel.Visibility);
        Assert.Equal(("New song from Only a heading.cho", "Only a heading.cho has no words in it."), (panel.Says.Heading, panel.Says.Standing));
        Assert.Empty(panel.Says.Found);
        Assert.False(panel.HasItsFields);
        var (make, other) = panel.Buttons;
        Assert.Equal((Visibility.Collapsed, "Close", Visibility.Visible), (make.Visibility, Label(other), other.Visibility));
        // A click on the dimmed window behind is Close, there being nothing typed to lose.
        var dimmed = ((Grid)panel.Content).Children.OfType<Border>().First();
        dimmed.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonDownEvent });
        Assert.Equal(Visibility.Collapsed, panel.Visibility);

        // Under a song to be made, such a click does nothing: its name may have been typed.
        model.BringIn(ASong("plain-song.cho"));
        panel.Fields.Named.Text = "Typed with care";
        dimmed.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonDownEvent });
        Assert.Equal((Visibility.Visible, "Typed with care"), (panel.Visibility, model.NewSong!.Name));
        // And it has its fields again, and its button.
        Assert.True(panel.HasItsFields);
        Assert.Equal((Visibility.Visible, "Cancel", ""), (panel.Buttons.Make.Visibility, Label(panel.Buttons.Other), panel.Says.Standing));
    });

    [Fact]
    public void WhileProPresenterIsRunningTheSongCannotBeMadeAndThePanelSaysSo() => OnAMadeUpPC(async pc =>
    {
        var (model, view) = await AtTheSongs(pc);
        var panel = view.NewSongPanel;
        model.BringIn(ASong("plain-song.cho"));
        Assert.True(panel.Buttons.Make.IsEnabled);

        pc.ProPresenterIsRunning = true;
        model.ProPresenterMayHaveChanged();
        Assert.Equal(("ProPresenter is running. Quit it to make a song here.", false, Visibility.Visible), (panel.Says.Standing, panel.Buttons.Make.IsEnabled, panel.Buttons.Make.Visibility));
        // The fields can be filled in all the same.
        panel.Fields.Named.Text = "Meanwhile";
        Assert.Equal("Meanwhile", model.NewSong!.Name);

        pc.ProPresenterIsRunning = false;
        model.ProPresenterMayHaveChanged();
        Assert.Equal(("", true), (panel.Says.Standing, panel.Buttons.Make.IsEnabled));

        // What is refused is said there too, in the model's words.
        panel.Fields.Like.SelectedItem = panel.Fields.Like.Items.Cast<LibraryModel.LookAlike>().Single(one => one.Name == "Nonsense");
        Click(panel.Buttons.Make);
        Assert.Equal(Visibility.Visible, panel.Visibility);
        Assert.Equal("Nonsense.pro could not be read as a ProPresenter presentation: it is not a presentation, or it is damaged.", panel.Says.Standing);
    });

    // ---- The keys, and the menu

    [Fact]
    public void WhileThePanelIsThereEscIsItsCancelAndTheWindowsOtherKeysDoNothingBehindIt() => OnAMadeUpPC(async pc =>
    {
        var (model, view) = await AtTheSongs(pc, "Plain Song");
        model.BringIn(ASong("amazing-grace.cho"));
        Assert.Equal(LibraryModel.Editor.Sheet, model.EditorShown);

        // Ctrl+2 does not show the other editor behind the panel, nor Ctrl+S say anything.
        Assert.True(view.KeyPressed(Key.D2, ModifierKeys.Control));
        Assert.Equal(LibraryModel.Editor.Sheet, model.EditorShown);
        var said = model.Notice;
        Assert.True(view.KeyPressed(Key.S, ModifierKeys.Control));
        Assert.Equal(said, model.Notice);
        Assert.True(view.KeyPressed(Key.NumPad1, ModifierKeys.Control));
        // A key that is not the window's is left to whatever has the keyboard: the name's field.
        Assert.False(view.KeyPressed(Key.A, ModifierKeys.None));
        Assert.False(view.KeyPressed(Key.Enter, ModifierKeys.None));
        Assert.NotNull(model.NewSong);

        // Esc is Cancel.
        Assert.True(view.KeyPressed(Key.Escape, ModifierKeys.None));
        Assert.Null(model.NewSong);
        Assert.Equal(Visibility.Collapsed, view.NewSongPanel.Visibility);
        // And is Close under a file that is no song.
        model.BringIn(ChordProFilesModelTests.AFile(pc, "Empty.cho", ""));
        Assert.NotNull(model.NewSong);
        Assert.True(view.KeyPressed(Key.Escape, ModifierKeys.None));
        Assert.Null(model.NewSong);
        // With the panel gone the keys are the window's again.
        Assert.True(view.KeyPressed(Key.D2, ModifierKeys.Control));
        Assert.Equal(LibraryModel.Editor.ChordPro, model.EditorShown);
    });

    [Fact]
    public void TheFileMenuHasTheTwoCommandsEachGreyedWhenItHasNothingToDo() => OnAMadeUpPC(async pc =>
    {
        var (model, view) = await AtTheSongs(pc);
        var (bringIn, writeOut) = view.ChordProCommands;
        Assert.Equal(("_New Song from ChordPro File" + (char)0x2026, "Save Song as _ChordPro File" + (char)0x2026), ((string)bringIn.Header, (string)writeOut.Header));
        // A library is open, and no song.
        Assert.Equal((true, false), (bringIn.IsEnabled, writeOut.IsEnabled));
        model.Open(pc.Song("Plain Song"));
        Assert.Equal((true, true), (view.ChordProCommands.BringIn.IsEnabled, view.ChordProCommands.WriteOut.IsEnabled));
        // A song that cannot be read cannot be written out.
        model.Open(pc.Song("Nonsense"));
        Assert.Equal((true, false), (view.ChordProCommands.BringIn.IsEnabled, view.ChordProCommands.WriteOut.IsEnabled));
        // While a new song is being asked about, another cannot be begun... nor while a
        // question is over the window.
        model.Open(pc.Song("Plain Song"));
        model.BringIn(ASong("plain-song.cho"));
        model.DontMakeTheSong();
        model.SetKey("A");
        model.AskToRevert();
        Assert.False(view.ChordProCommands.BringIn.IsEnabled);
        model.Answer(false);
        Assert.True(view.ChordProCommands.BringIn.IsEnabled);

        // Both are in the File menu, between choosing a folder and saving.
        var file = (MenuItem)bringIn.Parent;
        Assert.Equal("_File", (string)file.Header);
        var headers = file.Items.Cast<object>().Select(item => item is MenuItem one ? (string)one.Header : "-").ToList();
        Assert.Equal(
            ["Choose _Folder" + (char)0x2026, "-", "_New Song from ChordPro File" + (char)0x2026, "Save Song as _ChordPro File" + (char)0x2026, "-", "_Save"],
            headers.Take(6));

        // The kinds of file the system's chooser is asked to show: not ProPresenter's own.
        Assert.Equal("ChordPro files|*.cho;*.chordpro;*.chopro;*.crd;*.txt|All files|*.*", LibraryView.ChordProFiles);
        Assert.DoesNotContain("*.pro;", LibraryView.ChordProFiles + ";", StringComparison.OrdinalIgnoreCase);

        // On a PC with no library there is none to put a song in.
        pc.HasAProPresenterFolder = false;
        var nowhere = pc.Model();
        var empty = new LibraryView(nowhere);
        await nowhere.StartAsync();
        Assert.Equal((false, false), (empty.ChordProCommands.BringIn.IsEnabled, empty.ChordProCommands.WriteOut.IsEnabled));
    });
}
