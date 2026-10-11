// The code beside SongPane.xaml, which says what the pane is. Here: what the model
// says of the song is put into the pane whenever the model says it has changed, and
// what the person clicks or picks is passed on.
//
// Nothing here is a rule. What may be typed is Song/ChordProEditor.cs's, what the band,
// the quiet line, the pickers' lists and the words in place of a song say is
// LibraryModel's, and which way the header is laid out in the room it has is SongLook's.
//
// ONE THING THE PANE DOES BY ITSELF: the offer to undo a putting back goes after a few
// seconds (spec/07-backups.md, rule 22). The model is told it has gone; the copy is in
// the earlier versions all the same.

using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Pro7ChordEditor.Song;

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

    // What is being put into the pickers is from the model, and not a key picked.
    private bool showing;

    // How the header is laid out now.
    private HeaderRows laidOut = HeaderRows.One;

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
        Header.SizeChanged += (_, _) => LayOutTheHeader();
        Keys.SizeChanged += (_, _) => LayOutTheHeader();
        Earlier.SizeChanged += (_, _) => LayOutTheHeader();
    }

    /// <summary>A key as a picker lists it: its name, or "None" where the song states none.</summary>
    public sealed record KeyRow(string Key)
    {
        public string Name => Key.Length == 0 ? "None" : Key;

        // (What a screen reader says of the row: ListsPane.xaml.cs.)
        public override string ToString() => Name;
    }

    /// <summary>The ChordPro text: for the window to give the keyboard to, and for a test to type into.</summary>
    internal ChordProText ChordPro => Text;

    private void Follow(LibraryModel? now)
    {
        if (model is not null)
        {
            model.PropertyChanged -= TheModelChanged;
        }
        model = now;
        Text.Asked = model is null ? null : new ChordProText.Asks(model.TextMayBecome, direction => model.ZoomText(direction));
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
        var read = song is { Problem: null };
        Header.Visibility = UnderTheHeader.Visibility = Seen(song is not null);
        Earlier.Visibility = Seen(model.HasEarlierVersions);
        NoSong.Text = model.NoSongSentence;
        NoSong.Visibility = Seen(song is null);
        Problem.Visibility = Seen(song?.Problem is not null);

        // Nothing is saved by hand (spec/06, rule 12a): Revert, and where a Save button
        // would be, the words that say why there is none.
        Saving.Visibility = Seen(read);
        Revert.IsEnabled = model.CanRevert;
        Autosave.Text = model.SavingCaption;
        Autosave.ToolTip = model.SavingTip;

        // The text. It is put in place of what the box has only when the model says it
        // was made again from the song; what is typed is the box's own.
        var text = read ? model.ChordPro : null;
        Editor.Visibility = Seen(text is not null);
        Text.SetScale(model.TextZoom);
        Smaller.IsEnabled = model.CanZoomTextOut;
        Larger.IsEnabled = model.CanZoomTextIn;
        if (text is not null)
        {
            Text.Show(text, model.CanType);
            // One line of help; or, in a warmer colour, why this song can be read here
            // and not changed (spec/04, rules 5, 20 and 20a).
            Help.Text = text.Barred ?? ChordProEditor.Help;
            Help.SetResourceReference(TextBlock.ForegroundProperty, text.Barred is null ? "TextFillColorSecondaryBrush" : "SystemFillColorCautionBrush");
        }

        Keys.Visibility = Seen(read);
        if (song is not null && read)
        {
            ShowTheKeys(song, model.CanChange);
        }
        if (!ReferenceEquals(song, shownSong))
        {
            shownSong = song;
            SongName.Text = song?.Name ?? "";
            Problem.Text = song?.Problem ?? "";
        }
        LayOutTheHeader();
    }

    // The song's two keys, each to be set: the key its chords are written in, and,
    // quieter beside it, the key ProPresenter is to show them in. Neither changes any
    // chord in the file (spec/05, rules 5 to 7).
    private void ShowTheKeys(LibraryModel.ShownSong song, bool canChange)
    {
        showing = true;
        try
        {
            List(Key, song.Key);
            List(ShownKey, song.ShownKey);
        }
        finally
        {
            showing = false;
        }
        Key.IsEnabled = canChange;
        Key.ToolTip = song.Key.Length == 0
            ? "This song does not say what key it is in. Pick the key its chords are written in."
            : "The key the song" + (char)0x2019 + "s chords are written in. Changing it changes no chord.";
        // (A song that states no key has no second key to set until it has a first.)
        ShownKey.IsEnabled = canChange && song.Key.Length > 0;
        ShownKey.ToolTip = song.Key.Length == 0
            ? "Set the song" + (char)0x2019 + "s key first, and then the key ProPresenter is to show it in."
            : "The key ProPresenter shows this song" + (char)0x2019 + "s chords in. It moves them there itself; the chords here stay as they are written.";
    }

    private static void List(ComboBox picker, string key)
    {
        var listed = LibraryModel.KeysListedFor(key);
        if (picker.ItemsSource is not List<KeyRow> has || !has.Select(row => row.Key).SequenceEqual(listed))
        {
            picker.DisplayMemberPath = nameof(KeyRow.Name);
            picker.SelectedValuePath = nameof(KeyRow.Key);
            picker.ItemsSource = listed.Select(one => new KeyRow(one)).ToList();
        }
        picker.SelectedValue = key;
    }

    // The name at the left, and Earlier Versions and the keys at the right, on one row
    // where the name can be read whole beside them. Otherwise the keys go on a row of
    // their own under the name: with Earlier Versions at that row's right where there
    // is room for both, and by themselves where there is not, Earlier Versions then
    // being at the right of the name. Which, is SongLook's to say.
    private void LayOutTheHeader()
    {
        if (Header.Visibility != Visibility.Visible || Header.ActualWidth <= 0)
        {
            return;
        }
        var rows = SongLook.Rows(
            WidthOf(SongName),
            Earlier.Visibility == Visibility.Visible ? Earlier.ActualWidth : 0,
            Keys.Visibility == Visibility.Visible ? Keys.ActualWidth : 0,
            Header.ActualWidth);
        if (rows == laidOut)
        {
            return;
        }
        laidOut = rows;
        var one = rows == HeaderRows.One;
        var earlierWithTheName = rows != HeaderRows.KeysUnderTheName;
        // The name has the whole of its row, unless Earlier Versions is on it.
        Grid.SetColumnSpan(SongName, rows == HeaderRows.KeysUnderTheName ? 3 : 1);
        Grid.SetRow(Earlier, earlierWithTheName ? 0 : 1);
        Grid.SetColumn(Earlier, one ? 1 : 2);
        Earlier.Margin = new Thickness(SongLook.Gap, earlierWithTheName ? 0 : 8, 0, 0);
        Grid.SetRow(Keys, one ? 0 : 1);
        Grid.SetColumn(Keys, one ? 2 : 0);
        Grid.SetColumnSpan(Keys, rows == HeaderRows.KeysByThemselves ? 3 : 1);
        Keys.Margin = one ? new Thickness(SongLook.Gap, 0, 0, 0) : new Thickness(0, 8, 0, 0);
        Keys.HorizontalAlignment = HorizontalAlignment.Left;
    }

    // How wide a line of words is when none of it is cut short.
    private double WidthOf(TextBlock words) => new FormattedText(
        words.Text,
        CultureInfo.CurrentUICulture,
        FlowDirection.LeftToRight,
        new Typeface(words.FontFamily, words.FontStyle, words.FontWeight, words.FontStretch),
        words.FontSize,
        Brushes.Black,
        VisualTreeHelper.GetDpi(this).PixelsPerDip).WidthIncludingTrailingWhitespace;

    private void AKeyWasPicked(object sender, SelectionChangedEventArgs e)
    {
        if (!showing && Key.SelectedValue is string { Length: > 0 } key && !string.Equals(key, model?.Shown?.Key, StringComparison.Ordinal))
        {
            model?.SetKey(key);
        }
    }

    private void AShownKeyWasPicked(object sender, SelectionChangedEventArgs e)
    {
        if (!showing && ShownKey.SelectedValue is string { Length: > 0 } key && !string.Equals(key, model?.Shown?.ShownKey, StringComparison.Ordinal))
        {
            model?.SetShownKey(key);
        }
    }

    private void EarlierVersionsWasClicked(object sender, RoutedEventArgs e) => model?.ShowEarlierVersions();

    private void UndoWasClicked(object sender, RoutedEventArgs e) => model?.TakeBack();

    private void OpenAgainWasClicked(object sender, RoutedEventArgs e) => model?.OpenAgain();

    private void RevertWasClicked(object sender, RoutedEventArgs e) => model?.AskToRevert();

    private void SmallerWasClicked(object sender, RoutedEventArgs e) => model?.ZoomText(-1);

    private void LargerWasClicked(object sender, RoutedEventArgs e) => model?.ZoomText(1);
}
