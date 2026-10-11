// The code beside SongPane.xaml, which says what the pane is. Here: what the model
// says of the song is put into the pane whenever the model says it has changed, and the
// song's lines are made into lines of text.
//
// Nothing here is a rule. What a song's lines are is Song/SongText.cs's, and what the
// band, the quiet line and the words in place of a song say is LibraryModel's, bar the
// few words of the header, which only name the key.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using Pro7ChordEditor.Song;

namespace Pro7ChordEditor.AppViews;

/// <summary>The right of the window: the song that is open.</summary>
public partial class SongPane : UserControl
{
    /// <summary>What the band says while ProPresenter is running (spec/06, rule 19).</summary>
    public const string ProPresenterIsRunning = "ProPresenter is running. Quit it to change chords here.";

    // A line of a song is set in letters all of one width, so that a chord in brackets
    // takes the room it would in the file it came from.
    private static readonly FontFamily OfOneWidth = new("Cascadia Mono, Consolas, Courier New");

    private LibraryModel? model;
    private LibraryModel.ShownSong? shownSong;

    public SongPane()
    {
        InitializeComponent();
        BandWords.Text = ProPresenterIsRunning;
        DataContextChanged += (_, _) => Follow(DataContext as LibraryModel);
    }

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

    // Puts what the model has into the pane.
    private void Show()
    {
        if (model is null)
        {
            return;
        }
        Band.Visibility = Seen(model.ProPresenterIsRunning);
        Quiet.Text = model.Notice;

        var song = model.Shown;
        Header.Visibility = UnderTheHeader.Visibility = Seen(song is not null);
        NoSong.Text = model.NoSongSentence;
        NoSong.Visibility = Seen(song is null);
        Problem.Visibility = Seen(song?.Problem is not null);
        Scroll.Visibility = Seen(song is not null && song.Problem is null);
        if (ReferenceEquals(song, shownSong))
        {
            return;
        }
        shownSong = song;
        Lines.Children.Clear();
        if (song is null)
        {
            return;
        }
        SongName.Text = song.Name;
        Problem.Text = song.Problem ?? "";

        // The key: what the song's chords are written in, and, if it is another, what
        // ProPresenter is set to show them in. A song that could not be read has none.
        var read = song.Problem is null;
        Key.Visibility = Seen(read);
        Key.Text = song.Key.Length == 0 ? "No key" : "Key " + song.Key;
        Key.ToolTip = song.Key.Length == 0 ? "This song does not say what key it is in." : "The key the song" + (char)0x2019 + "s chords are written in.";
        Key.Opacity = song.Key.Length == 0 ? 0.6 : 1;
        var shownInAnother = read && song.ShownKey.Length > 0 && !string.Equals(song.ShownKey, song.Key, StringComparison.Ordinal);
        ShownKey.Visibility = Seen(shownInAnother);
        ShownKey.Text = "Shown in " + song.ShownKey;
        ShownKey.ToolTip = "The key ProPresenter is set to show this song" + (char)0x2019 + "s chords in.";

        foreach (var line in song.Lines)
        {
            Lines.Children.Add(Drawn(line));
        }
        Scroll.ScrollToHome();
    }

    // One line of the song, as it is drawn.
    private UIElement Drawn(SongLine line)
    {
        switch (line.Kind)
        {
            case SongLineKind.Group:
                // The group's name, with a spot of its colour before it if the file
                // gives it one.
                var heading = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 2) };
                if (line.GroupColour is { } colour)
                {
                    heading.Children.Add(new Ellipse
                    {
                        Width = 9,
                        Height = 9,
                        Margin = new Thickness(0, 1, 6, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        Fill = new SolidColorBrush(Color.FromRgb(Part(colour.Red), Part(colour.Green), Part(colour.Blue))),
                    });
                }
                var name = new TextBlock { Text = line.Group, FontWeight = FontWeights.SemiBold };
                name.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
                heading.Children.Add(name);
                return heading;

            case SongLineKind.Gap:
                return new Border { Height = 10 };

            case SongLineKind.BlankSlide:
                var blank = new TextBlock { Text = "(a blank slide)", FontFamily = OfOneWidth, Margin = new Thickness(0, 1, 0, 1), ToolTip = "This slide has no words." };
                blank.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorTertiaryBrush");
                return blank;

            default:
                // (A line with nothing in it still takes a line's height.)
                var words = new TextBlock { FontFamily = OfOneWidth, Margin = new Thickness(0, 1, 0, 1) };
                foreach (var (text, isChord) in Parts(line.Text.Length == 0 ? " " : line.Text))
                {
                    var run = new Run(text);
                    if (isChord)
                    {
                        run.SetResourceReference(TextElement.ForegroundProperty, Look.Chord);
                    }
                    words.Inlines.Add(run);
                }
                return words;
        }
    }

    // A part of a colour as the file has it, from nought to one, as a screen has it,
    // from 0 to 255.
    private static byte Part(double ofOne) => (byte)Math.Round(Math.Clamp(ofOne, 0, 1) * 255);

    /// <summary>
    /// A line of ChordPro in its parts, each a chord or not: a chord is everything from
    /// a "[" to its "]", brackets and all. The colouring never changes the text: the
    /// parts put together again are the line.
    /// </summary>
    internal static IReadOnlyList<(string Text, bool IsChord)> Parts(string line)
    {
        var parts = new List<(string, bool)>();
        var from = 0;
        while (from < line.Length)
        {
            var open = line.IndexOf('[', from);
            var close = open < 0 ? -1 : line.IndexOf(']', open);
            if (close < 0)
            {
                break;
            }
            if (open > from)
            {
                parts.Add((line[from..open], false));
            }
            parts.Add((line[open..(close + 1)], true));
            from = close + 1;
        }
        if (from < line.Length)
        {
            parts.Add((line[from..], false));
        }
        return parts;
    }
}
