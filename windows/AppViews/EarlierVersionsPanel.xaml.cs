// The code beside EarlierVersionsPanel.xaml, which says what the panel is. Here: what
// the model has of the song's earlier versions is put into the panel whenever the model
// says it has changed, and what the person picks and clicks is passed on.
//
// Nothing here is a rule. Which copies there are, what is said of each, what the line
// at the foot says and what putting one back does are LibraryModel's.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Pro7ChordEditor.Song;

namespace Pro7ChordEditor.AppViews;

/// <summary>The copies kept of the song that is open, over the window.</summary>
public partial class EarlierVersionsPanel : UserControl
{
    private LibraryModel? model;
    private bool showing;
    private IReadOnlyList<KeptCopy>? shownCopies;

    public EarlierVersionsPanel()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Follow(DataContext as LibraryModel);
    }

    /// <summary>A copy as the list has it: when it was made, and why.</summary>
    public sealed record CopyRow(string Id, string When, string Why);

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

    // Puts what the model has into the panel; with no earlier versions to show, the
    // panel is not there.
    private void Show()
    {
        var panel = model?.Earlier;
        var wasOpen = Visibility == Visibility.Visible;
        Visibility = Seen(panel is not null);
        if (model is null || panel is null)
        {
            shownCopies = null;
            return;
        }
        showing = true;
        try
        {
            Heading.Text = "Earlier versions of " + (char)0x201C + (model.Shown?.Name ?? "") + (char)0x201D;
            if (!ReferenceEquals(shownCopies, panel.Copies))
            {
                shownCopies = panel.Copies;
                Copies.ItemsSource = panel.Copies.Select(copy => new CopyRow(copy.Id, model.When(copy), LibraryModel.Why(copy))).ToList();
            }
            Copies.SelectedValue = panel.PickedId;

            var picked = panel.Picked;
            Problem.Text = picked?.Problem ?? "";
            Problem.Visibility = Seen(picked?.Problem is not null);
            Scroll.Visibility = Seen(picked is not null && picked.Problem is null);
            Lines.Show(picked?.Lines ?? []);

            // The foot: the question, if one is being asked, with its two answers; else
            // what went wrong, or the line that sets the copy against the song now.
            var asking = panel.Question is not null;
            var wrong = !asking && model.Refused is not null;
            Foot.Text = panel.Question ?? model.Refused ?? panel.Comparison;
            Foot.SetResourceReference(TextBlock.ForegroundProperty, asking || wrong ? "TextFillColorPrimaryBrush" : "TextFillColorSecondaryBrush");
            Done.Visibility = PutBack.Visibility = Seen(!asking);
            Leave.Visibility = Sure.Visibility = Seen(asking);
            PutBack.IsEnabled = picked is not null && picked.Problem is null;
        }
        finally
        {
            showing = false;
        }
        if (!wasOpen)
        {
            // The list has the keyboard, so that the arrows go through the versions.
            Copies.Focus();
        }
    }

    private void ACopyWasPicked(object sender, SelectionChangedEventArgs e)
    {
        if (!showing && Copies.SelectedValue is string id)
        {
            model?.PickCopy(id);
        }
    }

    private void OutsideWasClicked(object sender, MouseButtonEventArgs e) => model?.CloseEarlierVersions();

    private void DoneWasClicked(object sender, RoutedEventArgs e) => model?.CloseEarlierVersions();

    private void PutBackWasClicked(object sender, RoutedEventArgs e) => model?.PutBackPicked();

    // (Picking the one picked again leaves the question.)
    private void LeaveItWasClicked(object sender, RoutedEventArgs e) => model?.PickCopy(model.Earlier?.PickedId);

    private void PutItBackWasClicked(object sender, RoutedEventArgs e) => model?.PutBackPicked(sure: true);
}
