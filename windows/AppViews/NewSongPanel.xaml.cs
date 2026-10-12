// The code beside NewSongPanel.xaml, which says what the panel is. Here: it is there
// while the model has a file being brought in, with the model's words in it; what is
// typed and chosen is passed on to the model as it is typed and chosen, and comes back
// as what the model then says (the name the song will have, how many slides).

using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FoundInAFile = Pro7ChordEditor.Song.FoundInAFile;
using NewSong = Pro7ChordEditor.Song.NewSong;

namespace Pro7ChordEditor.AppViews;

/// <summary>A new song from a ChordPro file, over the window: what was found in the file, and what it is to be made as.</summary>
public partial class NewSongPanel : UserControl
{
    private LibraryModel? model;

    // The file whose finds the rows above show, and whether the panel is setting its
    // own fields from the model (and is not being typed in).
    private FoundInAFile? listed;
    private List<string> rows = [];
    private bool showing;

    public NewSongPanel()
    {
        InitializeComponent();
        Lines.ItemsSource = Enumerable.Range(1, NewSong.MostLinesToASlide).Select(lines => lines.ToString(CultureInfo.InvariantCulture)).ToList();
        DataContextChanged += (_, _) => Follow(DataContext as LibraryModel);
    }

    /// <summary>What the panel says, top to bottom. For a test.</summary>
    internal (string Heading, IReadOnlyList<string> Found, string NameNote, string Library, string Slides, string Standing) Says => (
        Heading.Text,
        Found.Visibility == Visibility.Visible ? rows : [],
        NameNote.Visibility == Visibility.Visible ? NameNote.Text : "",
        InLibrary.Text,
        Slides.Text,
        Standing.Visibility == Visibility.Visible ? Standing.Text : "");

    /// <summary>The name's field, the lines to a slide, and the song to look like. For a test to type in and choose from.</summary>
    internal (TextBox Named, ComboBox Lines, ComboBox Like) Fields => (Named, Lines, Like);

    /// <summary>"Make the Song", and the other, which is Cancel or Close. For a test to click.</summary>
    internal (Button Make, Button Other) Buttons => (Make, Other);

    /// <summary>Whether the rows of what was found, and the fields under them, are there. For a test.</summary>
    internal bool HasItsFields => Form.Visibility == Visibility.Visible;

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
        var asked = model?.NewSong;
        var wasOpen = Visibility == Visibility.Visible;
        Visibility = Seen(asked is not null);
        if (asked is null)
        {
            return;
        }
        showing = true;
        try
        {
            Heading.Text = asked.Heading;
            List(asked);
            var isASong = asked.Found is not null;
            Found.Visibility = Form.Visibility = Seen(isASong);
            // (What is in the field is left alone while it is what the model has: the
            // caret stays where it is being typed at.)
            if (!string.Equals(Named.Text, asked.Name, StringComparison.Ordinal))
            {
                Named.Text = asked.Name;
            }
            NameNote.Text = asked.NameNote;
            NameNote.Visibility = Seen(asked.NameNote.Length > 0);
            InLibrary.Text = asked.Library;
            Lines.SelectedIndex = asked.LinesToASlide - 1;
            Slides.Text = asked.Slides;
            if (!ReferenceEquals(Like.ItemsSource, asked.LookAlikes))
            {
                Like.ItemsSource = asked.LookAlikes;
            }
            Like.SelectedItem = asked.Like;
            Standing.Text = asked.Standing ?? "";
            Standing.Visibility = Seen(asked.Standing is not null);
            Make.Visibility = Seen(isASong);
            Make.IsEnabled = asked.CanMake;
            Other.Content = isASong ? "Cancel" : "Close";
        }
        finally
        {
            showing = false;
        }
        if (!wasOpen)
        {
            // The keyboard is in the name, all of it picked out, so that typing gives
            // it another; or, under a file that is no song, on Close.
            if (asked.Found is not null)
            {
                Named.Focus();
                Named.SelectAll();
            }
            else
            {
                Other.Focus();
            }
        }
    }

    // The rows of what was found: what each is, and what the file says of it. Made
    // again only for another file's finds, which are another thing than the last's
    // even where the file is the same one, read again.
    private void List(LibraryModel.NewSongAsked asked)
    {
        if (ReferenceEquals(listed, asked.Found))
        {
            return;
        }
        listed = asked.Found;
        rows = [];
        Found.Children.Clear();
        Found.RowDefinitions.Clear();
        foreach (var (what, says) in asked.Summary)
        {
            var row = Found.RowDefinitions.Count;
            Found.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var name = new TextBlock { Text = what, Margin = new Thickness(0, row == 0 ? 0 : 4, 16, 0) };
            name.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            var said = new TextBlock { Text = says, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, row == 0 ? 0 : 4, 0, 0) };
            Grid.SetRow(name, row);
            Grid.SetRow(said, row);
            Grid.SetColumn(said, 1);
            // (What a screen reader says of the row.)
            System.Windows.Automation.AutomationProperties.SetName(said, what + ": " + says);
            Found.Children.Add(name);
            Found.Children.Add(said);
            rows.Add(what + ": " + says);
        }
    }

    private void TheNameWasTyped(object sender, TextChangedEventArgs e)
    {
        if (!showing)
        {
            model?.NameTheNewSong(Named.Text);
        }
    }

    private void TheLinesWereChosen(object sender, SelectionChangedEventArgs e)
    {
        if (!showing && Lines.SelectedIndex >= 0)
        {
            model?.SetLinesToASlide(Lines.SelectedIndex + 1);
        }
    }

    private void TheLookAlikeWasChosen(object sender, SelectionChangedEventArgs e)
    {
        if (!showing && Like.SelectedItem is LibraryModel.LookAlike like)
        {
            model?.MakeItLookLike(like.Id);
        }
    }

    // A click on the dimmed window behind: nothing, while there is a song to be made
    // and its name may have been typed; under a file that is no song, the same as Close.
    private void OutsideWasClicked(object sender, MouseButtonEventArgs e)
    {
        if (model?.NewSong is { Found: null })
        {
            model.DontMakeTheSong();
        }
    }

    private void MakeWasClicked(object sender, RoutedEventArgs e) => model?.MakeTheSong();

    private void TheOtherWasClicked(object sender, RoutedEventArgs e) => model?.DontMakeTheSong();
}
