// The code beside SongPane.xaml, which says what the pane is. Here: what the model
// says of the song is put into the pane whenever the model says it has changed, and
// what the person clicks or picks is passed on.
//
// Nothing here is a rule. What may be typed is Song/ChordProEditor.cs's, what is done on
// the sheet is Song/ChordSheet.cs's, what the band, the quiet line, the pickers' lists
// and the words in place of a song say is LibraryModel's, and which way the header is
// laid out in the room it has is SongLook's.
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

    // How many lines of the quiet line what the keys do on the sheet may take.
    private const int MostLinesOfHelp = 4;

    private readonly DispatcherTimer offer = new() { Interval = TheOfferStays };
    private LibraryModel? model;
    private LibraryModel.ShownSong? shownSong;
    private LibraryModel.PutBack? offered;

    // What is being put into the pickers is from the model, and not a key picked.
    private bool showing;

    // How the header is laid out now; nothing before it has been.
    private HeaderRows? laidOut;

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
        SizeChanged += (_, _) => LayOutTheHeader();
        Keys.SizeChanged += (_, _) => LayOutTheHeader();
        Earlier.SizeChanged += (_, _) => LayOutTheHeader();
        Editors.SizeChanged += (_, _) => LayOutTheHeader();
        Foot.SizeChanged += (_, _) => KeepTheLineAsTallAsTheHelp();
        Saving.SizeChanged += (_, _) => KeepTheLineAsTallAsTheHelp();
        // A click on the switch has shown the other editor: the keyboard goes to it, so
        // that what is typed next goes where the eye now is.
        Editors.Clicked += (_, _) => GiveTheEditorTheKeyboard();
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

    /// <summary>The chord sheet with its strip: for a test to point at and to click in.</summary>
    internal ChordSheetView ChordSheet => SheetView;

    /// <summary>The switch between the two editors: for a test to click.</summary>
    internal EditorSwitch Switch => Editors;

    /// <summary>How the header is laid out: for a test to ask.</summary>
    internal HeaderRows? HeaderLaidOut => laidOut;

    /// <summary>
    /// Gives the keyboard to the editor that is shown: the sheet, where the keys act on
    /// its spot, or the text, where they are typed.
    /// </summary>
    internal void GiveTheEditorTheKeyboard()
    {
        if (SheetView.Visibility == Visibility.Visible)
        {
            SheetView.Canvas.Focus();
        }
        else if (Editor.Visibility == Visibility.Visible)
        {
            Text.TextBox.Focus();
        }
    }

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

    // How tall a line of the quiet line is.
    private double LineOfTheQuietLine => Math.Ceiling(Quiet.FontFamily.LineSpacing * Quiet.FontSize);

    // Puts what the model has into the pane.
    private void Show()
    {
        if (model is null)
        {
            return;
        }
        Band.Visibility = Seen(model.ProPresenterIsRunning);

        var song = model.Shown;
        var read = song is { Problem: null };
        var onTheSheet = read && model.EditorShown == LibraryModel.Editor.Sheet && model.Sheet is not null;

        // The quiet line. What went wrong is said in full, on as many lines as it
        // needs, and stays until the next thing is done. What the keys do on the sheet
        // is long, and has up to four lines. Anything else is one line.
        var wrong = model.Refused is not null;
        Quiet.Text = model.Notice;
        Quiet.TextWrapping = wrong || onTheSheet ? TextWrapping.Wrap : TextWrapping.NoWrap;
        Quiet.TextTrimming = wrong ? TextTrimming.None : TextTrimming.CharacterEllipsis;
        Quiet.MaxHeight = wrong || !onTheSheet ? double.PositiveInfinity : MostLinesOfHelp * LineOfTheQuietLine;
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

        Header.Visibility = UnderTheHeader.Visibility = Seen(song is not null);
        Editors.Visibility = Seen(read);
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

        // The sheet follows the model by itself (ChordSheetView.xaml.cs): here it is
        // only shown, or put away.
        SheetView.Visibility = Seen(onTheSheet);
        KeepTheLineAsTallAsTheHelp();

        // The text. It is put in place of what the box has only when the model says it
        // was made again from the song; what is typed is the box's own.
        var text = read && !onTheSheet ? model.ChordPro : null;
        Editor.Visibility = Seen(text is not null);
        Text.SetScale(model.TextZoom);
        Text.SetChordColour(SheetLook.Written(model.TextChordColour));
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

    // The name at the left, the switch in the middle of the pane, and Earlier Versions
    // and the keys at the right, on one row where the name can be read whole beside
    // them. Otherwise the keys go on a row of their own under the name and the switch,
    // with Earlier Versions at that row's right; and where even that is too wide, the
    // name with Earlier Versions, the switch, and the keys have a row each. Which, is
    // SongLook's to say.
    //
    // The header has five columns: the name's, the switch's, one for what is left over,
    // Earlier Versions' and the keys'. On the one row the name's is as wide as SongLook
    // says, and the third takes what is left. On the other ways the name's has all the
    // room the rest leave, the third has none, and what has a row to itself lies across
    // all five.
    //
    // Nothing but the name is ever given less room than it needs. That matters beyond
    // how it looks: how wide the things are is read here from how wide they were laid
    // out, and a thing laid out narrower than it is would be read as narrower, and laid
    // out narrower again. (FOUND: the first way this was written gave Earlier Versions
    // exactly its width in a column that took what was left; a fraction of a pixel
    // short, and it went round for ever.)
    //
    // And the room there is, is the pane's and not the header's own width: a header
    // whose columns want more than the pane has is laid out as wide as they want, and
    // cut off at the pane's edge. (FOUND the same way: read from the header, the room
    // never got less than one row wanted, and it stayed one row at any width.)
    private void LayOutTheHeader()
    {
        var room = ActualWidth - Header.Margin.Left - Header.Margin.Right;
        if (Header.Visibility != Visibility.Visible || room <= 0)
        {
            return;
        }
        var name = WidthOf(SongName);
        var editors = Editors.Visibility == Visibility.Visible ? Editors.ActualWidth : 0;
        var earlier = Earlier.Visibility == Visibility.Visible ? Earlier.ActualWidth : 0;
        var keys = Keys.Visibility == Visibility.Visible ? Keys.ActualWidth : 0;
        var rows = SongLook.Rows(name, editors, earlier, keys, room);
        var one = rows == HeaderRows.One;
        var rest = new GridLength(1, GridUnitType.Star);
        // (A whole number of pixels, and never more than was worked out: so that what
        // is at the right is not pushed off the row by a part of one.)
        Header.ColumnDefinitions[0].Width = one
            ? new GridLength(Math.Max(0, Math.Floor(SongLook.NameSide(name, editors, SongLook.AtTheRight(earlier, keys), room))))
            : rest;
        Header.ColumnDefinitions[2].Width = one ? rest : new GridLength(0);
        if (rows == laidOut)
        {
            return;
        }
        laidOut = rows;
        const double under = 8;
        switch (rows)
        {
            case HeaderRows.One:
                Put(Editors, 0, 1, 1, HorizontalAlignment.Center, new Thickness(SongLook.Gap, 0, 0, 0));
                Put(Earlier, 0, 3, 1, HorizontalAlignment.Right, new Thickness(SongLook.Gap, 0, 0, 0));
                Put(Keys, 0, 4, 1, HorizontalAlignment.Right, new Thickness(SongLook.Gap, 0, 0, 0));
                break;
            case HeaderRows.KeysUnder:
                Put(Editors, 0, 1, 4, HorizontalAlignment.Right, new Thickness(SongLook.Gap, 0, 0, 0));
                Put(Keys, 1, 0, 5, HorizontalAlignment.Left, new Thickness(0, under, 0, 0));
                Put(Earlier, 1, 0, 5, HorizontalAlignment.Right, new Thickness(SongLook.Gap, under, 0, 0));
                break;
            default:
                Put(Earlier, 0, 1, 4, HorizontalAlignment.Right, new Thickness(SongLook.Gap, 0, 0, 0));
                Put(Editors, 1, 0, 5, HorizontalAlignment.Center, new Thickness(0, under, 0, 0));
                Put(Keys, 2, 0, 5, HorizontalAlignment.Left, new Thickness(0, under, 0, 0));
                break;
        }
    }

    private static void Put(FrameworkElement what, int row, int column, int across, HorizontalAlignment side, Thickness margin)
    {
        Grid.SetRow(what, row);
        Grid.SetColumn(what, column);
        Grid.SetColumnSpan(what, across);
        what.HorizontalAlignment = side;
        what.Margin = margin;
    }

    // While the sheet is shown, the quiet line is kept as tall as what the keys do
    // needs at the width it has, whatever it says just then: so that the sheet does not
    // grow and shrink by a line each time something is said for a few seconds.
    private void KeepTheLineAsTallAsTheHelp()
    {
        var tall = 0.0;
        if (SheetView.Visibility == Visibility.Visible && Foot.ActualWidth > 0)
        {
            var room = Foot.ActualWidth - Foot.Padding.Left - Foot.Padding.Right - Saving.ActualWidth;
            if (room > 40)
            {
                var help = new FormattedText(
                    LibraryModel.SheetHelp,
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    new Typeface(Quiet.FontFamily, Quiet.FontStyle, Quiet.FontWeight, Quiet.FontStretch),
                    Quiet.FontSize,
                    Brushes.Black,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip)
                {
                    MaxTextWidth = room,
                };
                tall = Math.Min(Math.Ceiling(help.Height), MostLinesOfHelp * LineOfTheQuietLine);
            }
        }
        if (Quiet.MinHeight != tall)
        {
            Quiet.MinHeight = tall;
        }
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
