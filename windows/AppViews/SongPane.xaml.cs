// The code beside SongPane.xaml, which says what the pane is. Here: what the model
// says of the song is put into the pane whenever the model says it has changed, and
// what the person clicks is passed on.
//
// Nothing here is a rule. What a song's lines are is Song/SongText.cs's, and what the
// band, the quiet line and the words in place of a song say is LibraryModel's, bar the
// few words of the header, which only name the key.
//
// ONE THING THE PANE DOES BY ITSELF: the offer to undo a putting back goes after a few
// seconds (spec/07-backups.md, rule 22). The model is told it has gone; the copy is in
// the earlier versions all the same.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Pro7ChordEditor.AppViews;

/// <summary>The right of the window: the song that is open.</summary>
public partial class SongPane : UserControl
{
    /// <summary>What the band says while ProPresenter is running (spec/06, rule 19).</summary>
    public const string ProPresenterIsRunning = "ProPresenter is running. Quit it to change chords here.";

    /// <summary>How long the offer to undo a putting back stays.</summary>
    public static readonly TimeSpan TheOfferStays = TimeSpan.FromSeconds(8);

    private readonly DispatcherTimer offer = new() { Interval = TheOfferStays };
    private LibraryModel? model;
    private LibraryModel.ShownSong? shownSong;
    private LibraryModel.PutBack? offered;

    public SongPane()
    {
        InitializeComponent();
        BandWords.Text = ProPresenterIsRunning;
        offer.Tick += (_, _) =>
        {
            offer.Stop();
            model?.ForgetWhatWasPutBack();
        };
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

        // The quiet line. What went wrong is said in full, on as many lines as it
        // needs, and stays until the next thing is done; anything else is one line.
        var wrong = model.Refused is not null;
        Quiet.Text = model.Notice;
        Quiet.TextWrapping = wrong ? TextWrapping.Wrap : TextWrapping.NoWrap;
        Quiet.TextTrimming = wrong ? TextTrimming.None : TextTrimming.CharacterEllipsis;
        Quiet.SetResourceReference(TextBlock.ForegroundProperty, wrong ? "TextFillColorPrimaryBrush" : "TextFillColorSecondaryBrush");
        Undo.Visibility = Seen(model.JustPutBack is not null && !wrong);
        OpenAgain.Visibility = Seen(model.OffersOpeningAgain);
        // The offer to undo is counted from when it was made, not from each time
        // something else changes.
        if (!ReferenceEquals(model.JustPutBack, offered))
        {
            offered = model.JustPutBack;
            offer.Stop();
            if (offered is not null)
            {
                offer.Start();
            }
        }

        var song = model.Shown;
        Header.Visibility = UnderTheHeader.Visibility = Seen(song is not null);
        Earlier.Visibility = Seen(model.HasEarlierVersions);
        NoSong.Text = model.NoSongSentence;
        NoSong.Visibility = Seen(song is null);
        Problem.Visibility = Seen(song?.Problem is not null);
        Scroll.Visibility = Seen(song is not null && song.Problem is null);
        if (ReferenceEquals(song, shownSong))
        {
            return;
        }
        shownSong = song;
        Lines.Show(song?.Lines ?? []);
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
        Scroll.ScrollToHome();
    }

    private void EarlierVersionsWasClicked(object sender, RoutedEventArgs e) => model?.ShowEarlierVersions();

    private void UndoWasClicked(object sender, RoutedEventArgs e) => model?.TakeBack();

    private void OpenAgainWasClicked(object sender, RoutedEventArgs e) => model?.OpenAgain();
}
