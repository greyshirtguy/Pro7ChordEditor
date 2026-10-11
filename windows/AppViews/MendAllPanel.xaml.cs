// The code beside MendAllPanel.xaml, which says what the panel is. Here: it is there
// from the model's question until what was done has been read and put away, with the
// model's words in it, and what is clicked is passed on to the model.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Pro7ChordEditor.AppViews;

/// <summary>Mend All, over the window: its question, how far it has got, and what it did.</summary>
public partial class MendAllPanel : UserControl
{
    private LibraryModel? model;
    private List<LibraryModel.MendOutcome> listed = [];

    public MendAllPanel()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Follow(DataContext as LibraryModel);
    }

    /// <summary>The mending that was begun here, to its end: for a test to wait for.</summary>
    internal Task AtWork { get; private set; } = Task.CompletedTask;

    /// <summary>What the panel says: its heading and the words under it. For a test.</summary>
    internal (string Heading, string Message) Says => (Heading.Text, Message.Text);

    /// <summary>The button that says yes, and the other, which is Cancel, Stop or Done. For a test to click.</summary>
    internal (Button Yes, Button Other) Buttons => (Yes, Other);

    /// <summary>What is listed as done, a line for each song: its name, then how many chords or why not. For a test.</summary>
    internal IReadOnlyList<string> Lines => [.. listed.Select(Line)];

    private void Follow(LibraryModel? now)
    {
        if (model is not null)
        {
            model.PropertyChanged -= TheModelChanged;
        }
        model = now;
        if (model is not null)
        {
            model.PropertyChanged += TheModelChanged;
        }
        Show();
    }

    private void TheModelChanged(object? sender, PropertyChangedEventArgs e) => Show();

    private static Visibility Seen(bool seen) => seen ? Visibility.Visible : Visibility.Collapsed;

    private void Show()
    {
        var question = model?.MendAllQuestion;
        var run = model?.MendingAll;
        var wasOpen = Visibility == Visibility.Visible;
        Visibility = Seen(question is not null || run is not null);
        if (question is not null)
        {
            Heading.Text = question.Title;
            Message.Text = question.Message;
            Yes.Content = question.Yes;
            Yes.Visibility = Visibility.Visible;
            Other.Content = "Cancel";
            Other.IsEnabled = true;
            Progress.Visibility = DoneSeen.Visibility = Visibility.Collapsed;
            List([]);
            if (!wasOpen)
            {
                // Cancel has the keyboard: it is what Enter and the space bar give.
                Other.Focus();
            }
            return;
        }
        if (run is null)
        {
            List([]);
            return;
        }
        Heading.Text = "Mend All";
        Message.Text = run.Sentence;
        Yes.Visibility = Visibility.Collapsed;
        if (run.Finished)
        {
            Other.Content = "Done";
            Other.IsEnabled = true;
        }
        else if (Other.IsEnabled)
        {
            Other.Content = "Stop";
        }
        Progress.Visibility = Seen(!run.Finished);
        Progress.Maximum = Math.Max(1, run.Of);
        Progress.Value = run.Outcomes.Count;
        DoneSeen.Visibility = Seen(run.Outcomes.Count > 0);
        List(run.Outcomes);
        if (!run.Finished)
        {
            // The song last done is kept in sight as the list grows.
            DoneSeen.ScrollToEnd();
        }
    }

    // Lists what was done. While it is the list as it was with more at its end, only
    // the more is added, so that the list stays where it was scrolled to.
    private void List(IReadOnlyList<LibraryModel.MendOutcome> outcomes)
    {
        if (outcomes.Count < listed.Count || !listed.SequenceEqual(outcomes.Take(listed.Count)))
        {
            listed = [];
            Done.Children.Clear();
        }
        foreach (var outcome in outcomes.Skip(listed.Count).ToList())
        {
            listed.Add(outcome);
            Done.Children.Add(RowOf(outcome));
        }
    }

    private static string Line(LibraryModel.MendOutcome outcome) => outcome.NotMended is null
        ? outcome.Name + ": " + (outcome.Chords == 1 ? "1 chord" : outcome.Chords.ToString(System.Globalization.CultureInfo.InvariantCulture) + " chords")
        : outcome.Name + ": not mended. " + outcome.NotMended;

    // A song's row: its name, and at the right how many chords; or, under its name, why
    // it was not mended.
    private static FrameworkElement RowOf(LibraryModel.MendOutcome outcome)
    {
        var name = new TextBlock { Text = outcome.Name, TextTrimming = TextTrimming.CharacterEllipsis };
        FrameworkElement row;
        if (outcome.NotMended is null)
        {
            var chords = new TextBlock
            {
                Text = outcome.Chords == 1 ? "1 chord" : outcome.Chords.ToString(System.Globalization.CultureInfo.InvariantCulture) + " chords",
                Margin = new Thickness(12, 0, 0, 0),
            };
            chords.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            DockPanel.SetDock(chords, Dock.Right);
            var across = new DockPanel { LastChildFill = true };
            across.Children.Add(chords);
            across.Children.Add(name);
            row = across;
        }
        else
        {
            var why = new TextBlock { Text = "Not mended. " + outcome.NotMended, TextWrapping = TextWrapping.Wrap, FontSize = 12.5, Margin = new Thickness(0, 1, 0, 0) };
            why.SetResourceReference(TextBlock.ForegroundProperty, "SystemFillColorCautionBrush");
            var down = new StackPanel();
            down.Children.Add(name);
            down.Children.Add(why);
            row = down;
        }
        row.Margin = new Thickness(0, 3, 4, 3);
        // (What a screen reader says of the row.)
        System.Windows.Automation.AutomationProperties.SetName(row, Line(outcome));
        return row;
    }

    // A click on the dimmed window behind: no, at the question; and, once all is done,
    // the same as Done. While the work is going on it does nothing.
    private void OutsideWasClicked(object sender, MouseButtonEventArgs e)
    {
        if (model?.MendAllQuestion is not null)
        {
            model.DontMendAll();
        }
        else
        {
            model?.CloseMendAll();
        }
    }

    // Cancel, Stop or Done, as it says.
    private void TheOtherWasClicked(object sender, RoutedEventArgs e)
    {
        if (model is null)
        {
            return;
        }
        if (model.MendAllQuestion is not null)
        {
            model.DontMendAll();
        }
        else if (model.MendingAll is { Finished: false })
        {
            // (The song it is at is finished first: the button waits with it.)
            model.StopMendingAll();
            Other.Content = "Stopping" + (char)0x2026;
            Other.IsEnabled = false;
        }
        else
        {
            model.CloseMendAll();
        }
    }

    private async void YesWasClicked(object sender, RoutedEventArgs e)
    {
        if (model is { MendAllQuestion: not null } asked)
        {
            AtWork = asked.MendAllAsync();
            await AtWork;
        }
    }
}
