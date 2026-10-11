// The mending in the window: the offers under the song's header, the mark and the fourth
// choice in the list, Mend All's panel, and the settings' switch. spec/08-repair.md, and
// spec/06-library.md, rules 9 and 11.
//
// MINE. What the offers say and what they do to a song is the model's, and is held in
// MendingModelTests.cs; here it is held that the window shows what the model has and
// that a click reaches the model. What only eyes can tell (that an offer reads well
// where it stands, the colour of its ground) is in the pictures (PictureTests.cs, 50 to
// 57), which were looked at.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Pro7ChordEditor.Song;
using Pro7ChordEditor.Tests;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class MendingViewTests
{
    private const string Old = "Old copies";
    private const string MinorKey = "A Song - In A Minor Key (Chords)";
    private const string Grace = "Amazing Grace (Chords)";
    private const string Well = "It Is Well (Chords)";
    private const string Plain = "Plain Song (Chords)";

    private static void OnAMadeUpPC(Func<MadeUpPC, Task> what) => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        await what(pc);
    });

    // The window's view on the library of the old editor's copies, with one of them
    // open or none.
    private static async Task<(LibraryModel Model, LibraryView View)> AtTheOldCopies(MadeUpPC pc, string? song = null)
    {
        var model = pc.Model();
        var view = new LibraryView(model);
        await model.StartAsync();
        model.ChooseLibrary(pc.Library(Old));
        await model.MarksAreWorkedOutAsync();
        if (song is not null)
        {
            model.Open(pc.Song(song, Old));
        }
        Look.Wear(view, dark: true);
        Pictures.LayOut(view, 1280, 800);
        return (model, view);
    }

    private static void LayOut(LibraryView view) => Pictures.LayOut(view, 1280, 800);

    private static void Click(ButtonBase button) => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

    private static string Label(ContentControl button) => (string)button.Content;

    // Where an element's top and bottom are, in its view.
    private static (double Top, double Bottom) Stands(FrameworkElement element, FrameworkElement within)
    {
        var top = element.TranslatePoint(new Point(0, 0), within).Y;
        return (top, top + element.ActualHeight);
    }

    // ---- The offers, under the song's header

    [Fact]
    public void WhatIsFoundInTheSongIsSaidUnderItsHeaderEachWithItsButtonAndAClickDoesIt() => OnAMadeUpPC(async pc =>
    {
        var (model, view) = await AtTheOldCopies(pc, MinorKey);
        var pane = view.SongPane;
        var shown = pane.OffersShown;
        Assert.Equal(model.Offers.Select(offer => offer.Sentence), shown.Select(row => row.Sentence));
        Assert.Equal(2, shown.Count);
        Assert.Equal([["Mend"], ["Set Key to F#m"]], shown.Select(row => row.Buttons.Select(Label).ToArray()));
        Assert.All(shown.SelectMany(row => row.Buttons), button => Assert.True(button.IsEnabled));
        // They stand between the header and the song.
        var offers = (FrameworkElement)pane.FindName("Offers");
        var header = (FrameworkElement)pane.FindName("Header");
        Assert.True(Stands(offers, pane).Top >= Stands(header, pane).Bottom);
        Assert.True(Stands(pane.ChordSheet, pane).Top >= Stands(offers, pane).Bottom);
        Assert.True(offers.ActualHeight > 50);

        // Mend.
        Click(shown[0].Buttons[0]);
        LayOut(view);
        Assert.Equal(0, model.Shown!.Found!.ShownTwice);
        Assert.Equal(["Set Key to F#m"], pane.OffersShown.SelectMany(row => row.Buttons).Select(Label));
        // Set Key.
        Click(pane.OffersShown[0].Buttons[0]);
        LayOut(view);
        Assert.Equal("F#m", model.Shown!.Key);
        Assert.Empty(pane.OffersShown);
        // With nothing to offer, the song has the room.
        Assert.Equal(0, offers.ActualHeight);
        Assert.True(TestData.InFiles("made-by-simple-presenter/A Song - In A Minor Key.pro").AsSpan().SequenceEqual(pc.BytesOf(MinorKey, Old)));
    });

    [Fact]
    public void WhileProPresenterIsRunningTheButtonsThatChangeTheSongCannotBeClickedAndThoseThatShowItCan() => OnAMadeUpPC(async pc =>
    {
        var (model, view) = await AtTheOldCopies(pc, Grace);
        model.SetOffersToMoveBack(true);
        pc.ProPresenterIsRunning = true;
        model.ProPresenterMayHaveChanged();
        LayOut(view);
        var pane = view.SongPane;
        IEnumerable<(string, bool)> Buttons() => pane.OffersShown.SelectMany(row => row.Buttons).Select(button => (Label(button), button.IsEnabled));
        Assert.Equal([("Mend", false), ("Show Me", true)], Buttons());

        // Show Me shows, and changes nothing.
        Click(pane.OffersShown[1].Buttons[0]);
        LayOut(view);
        Assert.Equal(LibraryModel.Way.AsItWouldBe, model.Judging);
        Assert.Equal([("Mend", false), ("Show As It Is", true), ("Move the Chords", false), ("Leave Them", true)], Buttons());
        Click(pane.OffersShown[1].Buttons[0]);
        LayOut(view);
        Assert.Equal(LibraryModel.Way.AsItIs, model.Judging);
        Assert.Equal(["Mend", "Show Moved", "Move the Chords", "Leave Them"], Buttons().Select(button => button.Item1));

        // ProPresenter is quit: everything can be clicked.
        pc.ProPresenterIsRunning = false;
        model.ProPresenterMayHaveChanged();
        LayOut(view);
        Assert.All(Buttons(), button => Assert.True(button.Item2));
        // Leave Them.
        Click(pane.OffersShown[1].Buttons[2]);
        LayOut(view);
        Assert.Null(model.Judging);
        Assert.Equal(["Mend", "Show Me"], Buttons().Select(button => button.Item1));
    });

    [Fact]
    public void ASongWithNothingFoundInItAndASongThatCannotBeReadHaveNoOffers() => OnAMadeUpPC(async pc =>
    {
        var (model, view) = await AtTheOldCopies(pc, Plain);
        Assert.Single(view.SongPane.OffersShown);
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        model.Open(pc.Song("Amazing Grace"));
        LayOut(view);
        Assert.Empty(view.SongPane.OffersShown);
        model.Open(pc.Song("Nonsense"));
        LayOut(view);
        Assert.Empty(view.SongPane.OffersShown);
        // And none while an earlier version of the song is looked at, over it.
        model.ChooseLibrary(pc.Library(Old));
        model.Open(pc.Song(Plain, Old));
        model.Take(LibraryModel.OfferAction.Mend);
        model.Undo();
        LayOut(view);
        Assert.Single(view.SongPane.OffersShown);
        model.ShowEarlierVersions();
        LayOut(view);
        Assert.Empty(view.SongPane.OffersShown);
        model.CloseEarlierVersions();
        LayOut(view);
        Assert.Single(view.SongPane.OffersShown);
    });

    // ---- The list: the mark, and the fourth choice

    [Fact]
    public void ASongThatWantsMendingHasAMarkWithItsTipAndTheFourthChoiceIsThereOnlyWhereALibraryHasSuchASong() => OnAMadeUpPC(async pc =>
    {
        var (model, view) = await AtTheOldCopies(pc);
        var lists = view.ListsPane;
        var (row, choice, mendAll) = lists.MendingParts;
        Assert.Equal(4, lists.RowsShown.Count);
        Assert.All(lists.RowsShown, shown => Assert.True(shown.WantsMending));
        Assert.Equal(
            "1 chord is written so that ProPresenter shows it twice. It says it is in A flat, but its chords look like F sharp minor.",
            lists.RowsShown[0].MendingTip);
        Assert.Contains("3 chords are written so that ProPresenter shows them twice.", lists.RowsShown[3].ToString(), StringComparison.Ordinal);

        // The choice is there; Mend All only once it is chosen.
        Assert.Equal((Visibility.Visible, false, Visibility.Collapsed), (row.Visibility, choice.IsChecked, mendAll.Visibility));
        Assert.Equal("Wanting mending", Label(choice));
        Click(choice);
        LayOut(view);
        Assert.Equal(MarkFilter.WantingMending, model.Filter);
        Assert.Equal((true, Visibility.Visible, true), (choice.IsChecked, mendAll.Visibility, mendAll.IsEnabled));
        // (A click on the one already chosen leaves it chosen.)
        Click(choice);
        Assert.Equal((MarkFilter.WantingMending, true), (model.Filter, choice.IsChecked));
        // Mend All cannot be clicked while ProPresenter is running.
        pc.ProPresenterIsRunning = true;
        model.ProPresenterMayHaveChanged();
        Assert.Equal((Visibility.Visible, false), (mendAll.Visibility, mendAll.IsEnabled));
        pc.ProPresenterIsRunning = false;
        model.ProPresenterMayHaveChanged();
        // Another of the choices is chosen: Mend All goes.
        Click((ToggleButton)lists.FindName("All"));
        Assert.Equal((MarkFilter.All, false, Visibility.Collapsed), (model.Filter, choice.IsChecked, mendAll.Visibility));

        // Switched on the system's way, as a program does it for someone who cannot
        // click: the list is narrowed as by a click. And the one that is chosen, switched
        // off so, is put back on.
        static void Toggle(ToggleButton button) =>
            ((System.Windows.Automation.Provider.IToggleProvider)new System.Windows.Automation.Peers.ToggleButtonAutomationPeer(button)).Toggle();
        Toggle(choice);
        Assert.Equal((MarkFilter.WantingMending, true, false), (model.Filter, choice.IsChecked, ((ToggleButton)lists.FindName("All")).IsChecked));
        Assert.Equal(4, lists.RowsShown.Count);
        Toggle(choice);
        Assert.Equal((MarkFilter.WantingMending, true), (model.Filter, choice.IsChecked));
        Toggle((ToggleButton)lists.FindName("WithChords"));
        Assert.Equal((MarkFilter.WithChords, false, true), (model.Filter, choice.IsChecked, ((ToggleButton)lists.FindName("WithChords")).IsChecked));
        Toggle((ToggleButton)lists.FindName("All"));
        Assert.Equal(MarkFilter.All, model.Filter);

        // A library in which nothing wants mending has three choices.
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        LayOut(view);
        Assert.Equal(Visibility.Collapsed, row.Visibility);
        Assert.All(lists.RowsShown, shown => Assert.False(shown.WantsMending));
        // Come to with the fourth chosen, it is shown so that it can be seen to be.
        model.ChooseLibrary(pc.Library(Old));
        await model.MarksAreWorkedOutAsync();
        Click(choice);
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        LayOut(view);
        Assert.Equal((Visibility.Visible, true, Visibility.Collapsed), (row.Visibility, choice.IsChecked, mendAll.Visibility));
        Assert.Empty(lists.RowsShown);
    });

    // ---- Mend All, over the window

    [Fact]
    public void MendAllAsksOverTheWindowDoesItAndListsWhatItDid() => OnAMadeUpPC(async pc =>
    {
        var (model, view) = await AtTheOldCopies(pc);
        var (_, choice, mendAll) = view.ListsPane.MendingParts;
        var panel = view.MendAllPanel;
        Assert.Equal(Visibility.Collapsed, panel.Visibility);
        Click(choice);
        Click(mendAll);
        LayOut(view);

        // The question, with the model's words; Cancel is the button that Enter gives.
        Assert.Equal(Visibility.Visible, panel.Visibility);
        Assert.Equal(("Mend 4 songs?", model.MendAllQuestion!.Message), panel.Says);
        Assert.Equal(("Mend 4 Songs", Visibility.Visible, "Cancel", true), (Label(panel.Buttons.Yes), panel.Buttons.Yes.Visibility, Label(panel.Buttons.Other), panel.Buttons.Other.IsDefault));
        Assert.Empty(panel.Lines);
        // It is over the whole window.
        Assert.Equal((0.0, view.ActualHeight), Stands(panel, view));
        Assert.Equal(view.ActualWidth, panel.ActualWidth);
        // Cancel.
        Click(panel.Buttons.Other);
        Assert.Equal(Visibility.Collapsed, panel.Visibility);
        Assert.Null(model.MendAllQuestion);
        Assert.Equal(3, new OpenSong(pc.Song(Plain, Old)).Findings.ShownTwice);

        // Asked again, and yes.
        Click(mendAll);
        Click(panel.Buttons.Yes);
        // At work: how far it has got, and Stop.
        Assert.Equal(("Mend All", "Mending 1 of 4" + (char)0x2026), panel.Says);
        Assert.Equal((Visibility.Collapsed, "Stop", true), (panel.Buttons.Yes.Visibility, Label(panel.Buttons.Other), panel.Buttons.Other.IsEnabled));
        await panel.AtWork;
        LayOut(view);
        // Done: what it did, by name.
        Assert.Equal(("Mend All", "4 songs were mended: 14 chords that ProPresenter showed twice."), panel.Says);
        Assert.Equal(
            [MinorKey + ": 1 chord", Grace + ": 6 chords", Well + ": 4 chords", Plain + ": 3 chords"],
            panel.Lines);
        Assert.Equal((Visibility.Collapsed, "Done", true), (panel.Buttons.Yes.Visibility, Label(panel.Buttons.Other), panel.Buttons.Other.IsEnabled));
        Assert.Equal(Visibility.Visible, panel.Visibility);
        // Behind it the list has what is left to mend, which Mend All does not do.
        Assert.Equal([MinorKey], view.ListsPane.RowsShown.Select(row => row.Name));
        Assert.Equal(Visibility.Collapsed, mendAll.Visibility);
        // Done.
        Click(panel.Buttons.Other);
        Assert.Equal(Visibility.Collapsed, panel.Visibility);
        Assert.Null(model.MendingAll);
    });

    [Fact]
    public void MendAllIsStoppedByItsButtonAndASongThatWasNotMendedSaysWhy() => OnAMadeUpPC(async pc =>
    {
        var (model, view) = await AtTheOldCopies(pc);
        var (_, choice, mendAll) = view.ListsPane.MendingParts;
        var panel = view.MendAllPanel;
        Click(choice);
        Click(mendAll);
        Click(panel.Buttons.Yes);
        // Stop: the button waits with the song that is being mended.
        Click(panel.Buttons.Other);
        Assert.Equal(("Stopping" + (char)0x2026, false), (Label(panel.Buttons.Other), panel.Buttons.Other.IsEnabled));
        await panel.AtWork;
        Assert.Equal("1 song was mended: 1 chord that ProPresenter showed twice. 3 more were not tried.", panel.Says.Message);
        Assert.Equal((MinorKey + ": 1 chord", "Done", true), (panel.Lines.Single(), Label(panel.Buttons.Other), panel.Buttons.Other.IsEnabled));
        Click(panel.Buttons.Other);

        // The rest, with ProPresenter started when one more is done.
        Click(mendAll);
        Assert.Equal("Mend 3 songs?", panel.Says.Heading);
        pc.IsStartedOnceAsked = pc.TimesAsked + 1;
        Click(panel.Buttons.Yes);
        await panel.AtWork;
        Assert.Equal(
            [Grace + ": 6 chords", Well + ": not mended. ProPresenter is running. Quit it to change chords here. Nothing was changed."],
            panel.Lines);
        Assert.Equal("1 song was mended: 6 chords that ProPresenter showed twice. 1 song was not mended, and is as it was. 1 more was not tried.", panel.Says.Message);
        Assert.True(model.ProPresenterIsRunning);
    });

    [Fact]
    public void WhileMendAllIsOverTheWindowEscIsItsOwnAndTheWindowsOtherKeysDoNothingBehindIt() => OnAMadeUpPC(async pc =>
    {
        var (model, view) = await AtTheOldCopies(pc, Plain);
        var (_, choice, mendAll) = view.ListsPane.MendingParts;
        var panel = view.MendAllPanel;
        Click(choice);
        Click(mendAll);
        // At the question: Ctrl+2 shows no other editor behind it, Ctrl+S saves nothing
        // and says nothing, and Esc is Cancel.
        Assert.True(view.KeyPressed(Key.D2, ModifierKeys.Control));
        Assert.Equal(LibraryModel.Editor.Sheet, model.EditorShown);
        Assert.True(view.KeyPressed(Key.S, ModifierKeys.Control));
        Assert.Null(model.Said);
        Assert.False(view.KeyPressed(Key.A, ModifierKeys.None));
        Assert.True(view.KeyPressed(Key.Escape, ModifierKeys.None));
        Assert.Null(model.MendAllQuestion);
        Assert.Equal(Visibility.Collapsed, panel.Visibility);
        // With it gone the keys are the window's again.
        Assert.True(view.KeyPressed(Key.D2, ModifierKeys.Control));
        Assert.Equal(LibraryModel.Editor.ChordPro, model.EditorShown);
        Assert.True(view.KeyPressed(Key.D1, ModifierKeys.Control));

        // While it is at work Esc does nothing; once it has finished, Esc is Done.
        Click(mendAll);
        Click(panel.Buttons.Yes);
        Assert.True(view.KeyPressed(Key.Escape, ModifierKeys.None));
        Assert.NotNull(model.MendingAll);
        Assert.False(model.MendingAll!.Finished);
        await panel.AtWork;
        Assert.True(model.MendingAll!.Finished);
        Assert.Equal(4, model.MendingAll.Mended);
        Assert.True(view.KeyPressed(Key.Escape, ModifierKeys.None));
        Assert.Null(model.MendingAll);
        Assert.Equal(Visibility.Collapsed, panel.Visibility);
        // (The song that is open was mended with the rest.)
        Assert.Empty(model.SongsToMend);
        Assert.Empty(view.SongPane.OffersShown);
    });

    // ---- The old editor's copies

    [Fact]
    public void PuttingACopysChordsInAsksOverTheWindowAndThenWhatOfTheCopyAndEscGivesTheSecondButtonOfEach() => OnAMadeUpPC(async pc =>
    {
        pc.GiveTheOldCopiesTheirOriginals();
        var (model, view) = await AtTheOldCopies(pc, Plain);
        var pane = view.SongPane;
        var panel = view.QuestionPanel;
        var open = ((char)0x201C).ToString();
        var close = ((char)0x201D).ToString();
        Button Fold() => pane.OffersShown[^1].Buttons.Single();
        Assert.Equal("Put Its Chords into " + open + "Plain Song" + close + (char)0x2026, Label(Fold()));
        Assert.Equal(Visibility.Collapsed, panel.Visibility);

        // The question that says what is about to be done; Cancel is the button that
        // Enter gives.
        Click(Fold());
        LayOut(view);
        Assert.Equal(Visibility.Visible, panel.Visibility);
        Assert.Equal((model.FoldQuestion!.Title, model.FoldQuestion.Message, "Put the Chords In", "Cancel"), panel.Says);
        Assert.True(panel.Buttons.Other.IsDefault && panel.Buttons.Other.IsCancel);
        Assert.False(panel.Buttons.Yes.IsDefault);
        // Esc: Cancel. Nothing is done.
        Assert.True(view.KeyPressed(Key.Escape, ModifierKeys.None));
        Assert.Equal(Visibility.Collapsed, panel.Visibility);
        Assert.Equal(pc.Song(Plain, Old), model.SongId);

        // Asked again, and yes: the original is open, with the chords, and the next
        // question is asked at once, with its own two buttons.
        Click(Fold());
        Click(panel.Buttons.Yes);
        LayOut(view);
        Assert.Equal(pc.Song("Plain Song", Old), model.SongId);
        Assert.Equal(11, model.Shown!.Chords);
        Assert.Equal(Visibility.Visible, panel.Visibility);
        Assert.Equal((model.CopyQuestion!.Title, model.CopyQuestion.Message, "Move It Out", "Leave It"), panel.Says);
        // The window's other keys do nothing behind it.
        Assert.True(view.KeyPressed(Key.D2, ModifierKeys.Control));
        Assert.Equal(LibraryModel.Editor.Sheet, model.EditorShown);
        // Esc: Leave It.
        Assert.True(view.KeyPressed(Key.Escape, ModifierKeys.None));
        Assert.Equal(Visibility.Collapsed, panel.Visibility);
        Assert.Contains(Plain, model.Songs.Select(song => song.Name));

        // The copy, opened again, offers to be moved out; its button asks the same, and
        // Move It Out takes it from the list.
        model.Open(pc.Song(Plain, Old));
        LayOut(view);
        Assert.Equal("Move This Copy Out of the Library" + (char)0x2026, Label(Fold()));
        Click(Fold());
        Assert.Equal(("Move It Out", "Leave It"), (panel.Says.Yes, panel.Says.Other));
        Click(panel.Buttons.Yes);
        LayOut(view);
        Assert.Equal(Visibility.Collapsed, panel.Visibility);
        Assert.DoesNotContain(Plain, view.ListsPane.RowsShown.Select(row => row.Name));
        Assert.Contains("Plain Song", view.ListsPane.RowsShown.Select(row => row.Name));

        // While ProPresenter is running neither button can be clicked.
        model.Open(pc.Song(Well, Old));
        pc.ProPresenterIsRunning = true;
        model.ProPresenterMayHaveChanged();
        LayOut(view);
        Assert.StartsWith("Put Its Chords into", Label(Fold()), StringComparison.Ordinal);
        Assert.False(Fold().IsEnabled);
    });

    // ---- The settings' switch

    [Fact]
    public void TheSettingsHaveASectionForMendingWhoseSwitchIsOffAsTheAppComesAndSwitchesTheOfferOn() => OnAMadeUpPC(async pc =>
    {
        var (model, view) = await AtTheOldCopies(pc, Grace);
        var settings = new SettingsView(model, new AllCopiesModel(pc.AppData.Copies()));
        Look.Wear(settings, dark: true);
        settings.Show("Mending");
        Pictures.LayOut(settings, 960, 600);
        var tabs = (TabControl)settings.FindName("Sections");
        Assert.Equal(["Chords", "Mending", "Backups"], tabs.Items.OfType<TabItem>().Select(tab => (string)tab.Header));
        var mending = settings.MendingSwitch;
        Assert.Same(mending, tabs.SelectedContent);
        Assert.False(mending.Switch.IsChecked);
        Assert.Single(view.SongPane.OffersShown);

        // Switched on: the window behind has the offer at once, and it is remembered.
        mending.Switch.IsChecked = true;
        Click(mending.Switch);
        Assert.True(model.OffersToMoveBack);
        LayOut(view);
        Assert.Equal(["Mend", "Show Me"], view.SongPane.OffersShown.SelectMany(row => row.Buttons).Select(Label));
        Assert.True(pc.AppData.ReadSettings().OffersToMoveBack);
        // Switched off from elsewhere, the switch shows it.
        model.SetOffersToMoveBack(false);
        Assert.False(mending.Switch.IsChecked);
        // And off by the switch.
        model.SetOffersToMoveBack(true);
        Assert.True(mending.Switch.IsChecked);
        mending.Switch.IsChecked = false;
        Click(mending.Switch);
        Assert.False(model.OffersToMoveBack);
    });
}
