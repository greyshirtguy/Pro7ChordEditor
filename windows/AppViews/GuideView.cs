// The guide's page, and the window it is in: one page that says how chords are put on a
// song, with pictures of this app's own sheet. spec/13-guide.md.
//
// It is a window of its own, and not a panel that must be put away before the app is
// used: what it says is to be tried as it is read, on the song beside it. It opens by
// itself once, at the first start of the app on this PC (LibraryModel says when), and
// is under Help, on F1, and behind the ? at the right of the sheet's strip, ever after.
//
// Nothing here is a rule, and nothing in the window does anything to a song. The words
// are the guide's own (Guide.cs), laid out in the order they come:
//
//   the title;
//   what to know before starting, in a box that is not to be missed;
//   then each section: its heading, the one sentence that is written large (the first
//   section's: the quick way), its picture, its paragraphs, and its ways of doing a
//   thing, each a few bold words and what they mean, some with a picture under them;
//   a line of ChordPro text, where a section has one, shown as the ChordPro editor
//   shows one;
//   and at the foot, where the page is found again.
//
// IT IS A DOCUMENT, as WPF has documents: paragraphs and lists that flow into the width
// there is, in something that scrolls them. So the words can be selected and copied,
// across as many paragraphs as wanted, which the page says they can be; and a picture
// stands among them as a block of its own. (The box at the top is square-cornered for
// it: a document's part has an edge and a ground, and no rounded corners.)
//
// THE NAMES OF KEYS ARE KEPT WHOLE where a line is broken. Left to itself a line may be
// broken at the plus of "Ctrl+1", which then reads "Ctrl" and, on the next line, "+1"
// (SEEN, in the first picture of the whole page). So each plus of the words has either
// side of it a character with no width that joins what is beside it, as the quiet line
// has (SongPane.WithKeysWhole). They are in what is copied from the page too, where
// they are not seen either.
//
// The page follows the system, light or dark. The pictures are of the sheet, which is
// dark in both. Each is shown at the size it was drawn, or smaller where the window is
// narrower, and never larger.
//
// The Mac's is GuideWindow.swift, and the sizes are its.

using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Pro7ChordEditor.AppViews;

/// <summary>The guide's page: its words and its pictures, down one page that scrolls.</summary>
public sealed class GuideView : UserControl
{
    /// <summary>What the window is called, in its title and in the Help menu.</summary>
    public const string Title = "Pro7 Chord Editor Guide";

    /// <summary>What the ? at the right of the strip says when it is pointed at.</summary>
    public const string Tip = "The guide: the ways to put a chord on, on one page. It is in the Help menu too.";

    /// <summary>What the window says where the app cannot find its copy of the words.</summary>
    public const string NotFound = "The guide could not be found in this copy of the app.";

    /// <summary>As wide as a line of the words is let grow, which is as wide as the pictures were drawn; and the margin round the page.</summary>
    public const double PageWidth = 720;

    public const double PageMargin = 30;

    private static readonly DependencyProperty GroundOfTheLookProperty = DependencyProperty.Register(
        "GroundOfTheLook",
        typeof(Color),
        typeof(GuideView),
        new PropertyMetadata(Colors.White, (view, change) => ((GuideView)view).Wear(Look.IsDark((Color)change.NewValue))));

    private static readonly FontFamily Words = new("Segoe UI Variable Text, Segoe UI");
    private static readonly FontFamily OfOneWidth = new("Cascadia Mono, Consolas, Courier New");
    private static readonly Dictionary<string, BitmapSource?> Read = [];

    private readonly Guide? guide;
    private readonly FlowDocumentScrollViewer page = new()
    {
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        Focusable = false,
        // (No bar of the system's own over the page: there is nothing to zoom or find.)
        IsToolBarVisible = false,
    };

    private Color? chordColour;
    private bool dark;

    /// <param name="guide">The guide, in this platform's words; nothing if it could not be found.</param>
    /// <param name="chordColour">The colour of a chord in ChordPro text, as the settings have it; nothing for the usual one.</param>
    public GuideView(Guide? guide, Color? chordColour = null)
    {
        this.guide = guide;
        this.chordColour = chordColour;
        FontFamily = Words;
        UseLayoutRounding = true;
        // (This app's own colours, for the look it is under: a chord in the line of
        // ChordPro text is one of them.)
        Look.Follow(this);
        SetResourceReference(BackgroundProperty, "ApplicationBackgroundBrush");
        SetResourceReference(ForegroundProperty, "TextFillColorPrimaryBrush");
        Content = page;
        Build();
        // (Dark or light as the window is: the tints are made for the one or the other.)
        SetResourceReference(GroundOfTheLookProperty, Look.Ground);
        SizeChanged += (_, _) => KeepTheWordsInTheMiddle();
    }

    /// <summary>The guide of this copy of the app, as the window shows it.</summary>
    public GuideView()
        : this(GuideFiles.Guide)
    {
    }

    /// <summary>The colour of a chord in the line of ChordPro text: the usual one, or the one chosen in the settings.</summary>
    public Color? ChordColour
    {
        get => chordColour;
        set
        {
            if (value != chordColour)
            {
                chordColour = value;
                Build();
            }
        }
    }

    /// <summary>The page as a document: for a test to read.</summary>
    internal FlowDocument Document => page.Document;

    /// <summary>Everything the page says, as it would be copied from it.</summary>
    internal string Says => new TextRange(page.Document.ContentStart, page.Document.ContentEnd).Text;

    /// <summary>The pictures on the page, in the order they come: for a test to count and to measure.</summary>
    internal IReadOnlyList<Image> PicturesShown { get; private set; } = [];

    /// <summary>What scrolls the page: for a test to scroll.</summary>
    internal FlowDocumentScrollViewer Page => page;

    private void Wear(bool isDark)
    {
        if (isDark != dark)
        {
            dark = isDark;
            Build();
        }
    }

    // A line of the words is at most 720 wide, in the middle of a window that is wider,
    // with a margin of 30 round the page.
    private void KeepTheWordsInTheMiddle()
    {
        if (page.Document is { } document && ActualWidth > 0)
        {
            var side = Math.Max(PageMargin, Math.Floor((ActualWidth - SystemParameters.VerticalScrollBarWidth - PageWidth) / 2));
            var padding = new Thickness(side, PageMargin, side, PageMargin);
            if (document.PagePadding != padding)
            {
                document.PagePadding = padding;
            }
        }
    }

    private Color Ink => dark ? Colors.White : Color.FromRgb(0x1B, 0x1B, 0x1B);

    private SolidColorBrush Faint(Color colour, double share) => Frozen(Color.FromArgb((byte)Math.Round(share * 255), colour.R, colour.G, colour.B));

    private static SolidColorBrush Frozen(Color colour)
    {
        var brush = new SolidColorBrush(colour);
        brush.Freeze();
        return brush;
    }

    // Makes the page from the guide's words, for the look it is under.
    private void Build()
    {
        var pictures = new List<Image>();
        var document = new FlowDocument
        {
            FontFamily = Words,
            FontSize = 14,
            TextAlignment = TextAlignment.Left,
            IsOptimalParagraphEnabled = false,
            IsHyphenationEnabled = false,
            PagePadding = new Thickness(PageMargin),
            LineHeight = 21,
        };
        document.SetResourceReference(FlowDocument.ForegroundProperty, "TextFillColorPrimaryBrush");
        document.SetResourceReference(FlowDocument.BackgroundProperty, "ApplicationBackgroundBrush");
        var accent = Frozen(SheetLook.Accent);

        if (guide is null)
        {
            // (It is the app's own, and is always there. But if it is not, that is
            // said here and the app goes on.)
            var none = new Paragraph(new Run(NotFound));
            none.SetResourceReference(TextElement.ForegroundProperty, "TextFillColorSecondaryBrush");
            document.Blocks.Add(none);
        }
        else
        {
            document.Blocks.Add(new Paragraph(new Run(guide.Title)) { FontSize = 28, FontWeight = FontWeights.Bold, LineHeight = 36, Margin = new Thickness(0, 0, 0, 22) });

            // What to know before starting: in a box of the sheet's orange, faint, so
            // that it is seen before anything else and read as apart from the ways
            // that follow.
            var first = new Section
            {
                Background = Faint(SheetLook.Accent, 0.11),
                BorderBrush = Faint(SheetLook.Accent, 0.4),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16, 14, 16, 8),
                Margin = new Thickness(0, 0, 0, 26),
            };
            first.Blocks.Add(new Paragraph(new Run(guide.First.Heading)) { FontSize = 15, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8) });
            first.Blocks.Add(Pointed(guide.First.Points.Select(point => (Lead: (string?)null, Text: point, Picture: (string?)null)), accent, pictures));
            document.Blocks.Add(first);

            foreach (var section in guide.Sections)
            {
                document.Blocks.Add(new Paragraph(new Run(section.Heading)) { FontSize = 19, FontWeight = FontWeights.Bold, LineHeight = 26, Margin = new Thickness(0, 0, 0, 10) });
                if (section.Lead is { } lead)
                {
                    // The one thing to take away, if only one is.
                    document.Blocks.Add(new Paragraph(new Run(lead)) { FontSize = 27, FontWeight = FontWeights.SemiBold, LineHeight = 35, Foreground = accent, Margin = new Thickness(0, 0, 0, 12) });
                }
                if (section.Picture is { } picture)
                {
                    document.Blocks.Add(PictureOf(picture, guide.Pictures.GetValueOrDefault(picture, ""), pictures, new Thickness(0, 0, 0, 12)));
                }
                foreach (var paragraph in section.Paragraphs ?? [])
                {
                    document.Blocks.Add(new Paragraph(new Run(SongPane.WithKeysWhole(paragraph))) { Margin = new Thickness(0, 0, 0, 12) });
                }
                if (section.Items is { Count: > 0 } items)
                {
                    document.Blocks.Add(Pointed(items.Select(item => ((string?)item.Lead, item.Text, item.Picture)), accent, pictures));
                }
                if (section.Sample is { } sample)
                {
                    document.Blocks.Add(SampleOf(sample));
                }
                // (The room between one section and the next.)
                document.Blocks.LastBlock.Margin = new Thickness(document.Blocks.LastBlock.Margin.Left, document.Blocks.LastBlock.Margin.Top, 0, 26);
            }

            var line = new Border { Height = 1 };
            line.SetResourceReference(Border.BackgroundProperty, "DividerStrokeColorDefaultBrush");
            document.Blocks.Add(new BlockUIContainer(line) { Margin = new Thickness(0, 0, 0, 14) });
            var foot = new Paragraph(new Run(guide.Foot)) { FontSize = 12.5, Margin = new Thickness(0) };
            foot.SetResourceReference(TextElement.ForegroundProperty, "TextFillColorSecondaryBrush");
            document.Blocks.Add(foot);
        }

        PicturesShown = pictures;
        page.Document = document;
        KeepTheWordsInTheMiddle();
    }

    // Ways of doing a thing, or points to know: each with a small orange point before
    // its first line; a few words in bold, if it has them, run on into what they mean;
    // and its picture under it, if it has one.
    private List Pointed(IEnumerable<(string? Lead, string Text, string? Picture)> items, Brush accent, List<Image> pictures)
    {
        var list = new List
        {
            MarkerStyle = TextMarkerStyle.Disc,
            // (The points are the colour of the list's own words, which is why the
            // words within say their own.)
            Foreground = accent,
            MarkerOffset = 9,
            Padding = new Thickness(20, 0, 0, 0),
            Margin = new Thickness(0),
        };
        foreach (var (lead, text, picture) in items)
        {
            var words = new Paragraph { Margin = new Thickness(0, 0, 0, 9) };
            words.SetResourceReference(TextElement.ForegroundProperty, "TextFillColorPrimaryBrush");
            if (lead is not null)
            {
                words.Inlines.Add(new Run(SongPane.WithKeysWhole(lead)) { FontWeight = FontWeights.Bold });
                words.Inlines.Add(new Run(" "));
            }
            words.Inlines.Add(new Run(SongPane.WithKeysWhole(text)));
            var item = new ListItem(words);
            if (picture is not null)
            {
                item.Blocks.Add(PictureOf(picture, guide!.Pictures.GetValueOrDefault(picture, ""), pictures, new Thickness(0, 2, 0, 12)));
            }
            list.ListItems.Add(item);
        }
        return list;
    }

    // One of the guide's pictures, at the size it was drawn or smaller, with what it
    // shows said in words for whoever cannot see it. Where the picture is not there,
    // the words stand in its place.
    private Block PictureOf(string name, string says, List<Image> pictures, Thickness margin)
    {
        if (PictureNamed(name) is not { } picture)
        {
            var instead = new Paragraph(new Run(says)) { FontStyle = FontStyles.Italic, FontSize = 13, Margin = margin };
            instead.SetResourceReference(TextElement.ForegroundProperty, "TextFillColorSecondaryBrush");
            return instead;
        }
        var image = new Image
        {
            Source = picture,
            Stretch = Stretch.Uniform,
            // (Narrower, it is shown smaller; wider, never larger than it was drawn.)
            StretchDirection = StretchDirection.DownOnly,
            MaxWidth = picture.Width,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
        AutomationProperties.SetName(image, says);
        // Its corners are rounded, with a faint edge round it.
        image.SizeChanged += (_, _) => image.Clip = new RectangleGeometry(new Rect(0, 0, image.ActualWidth, image.ActualHeight), 8, 8);
        var edge = new Border
        {
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            BorderBrush = Faint(Ink, 0.18),
            IsHitTestVisible = false,
        };
        var both = new Grid { HorizontalAlignment = HorizontalAlignment.Left, MaxWidth = picture.Width };
        both.Children.Add(image);
        both.Children.Add(edge);
        pictures.Add(image);
        return new BlockUIContainer(both) { Margin = margin };
    }

    /// <summary>
    /// The picture of that name, read once. It says its own size, which is half its
    /// dots each way: it was drawn at two dots to the unit, for a screen with dots to
    /// spare.
    /// </summary>
    internal static BitmapSource? PictureNamed(string name)
    {
        if (Read.TryGetValue(name, out var known))
        {
            return known;
        }
        BitmapSource? picture = null;
        if (GuideFiles.Picture(name) is { } bytes)
        {
            try
            {
                var decoded = new PngBitmapDecoder(new MemoryStream(bytes), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
                decoded.Freeze();
                picture = decoded;
            }
            catch (Exception exception) when (exception is NotSupportedException or FileFormatException or IOException or ArgumentException)
            {
                // (Not a picture: its words stand in its place.)
            }
        }
        Read[name] = picture;
        return picture;
    }

    // A line of ChordPro text as the ChordPro editor shows one: in letters of one
    // width, its chords in their colour, brackets and all.
    private Paragraph SampleOf(string line)
    {
        var sample = new Paragraph
        {
            FontFamily = OfOneWidth,
            FontSize = 14,
            Padding = new Thickness(12, 8, 12, 8),
            Background = Faint(Ink, 0.06),
            BorderBrush = Faint(Ink, 0.14),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0),
        };
        foreach (var (text, isChord) in SongLines.Parts(line))
        {
            var run = new Run(text);
            if (isChord)
            {
                run.FontWeight = FontWeights.Bold;
                if (chordColour is { } chosen)
                {
                    run.Foreground = Frozen(chosen);
                }
                else
                {
                    run.SetResourceReference(TextElement.ForegroundProperty, Look.Chord);
                }
            }
            sample.Inlines.Add(run);
        }
        return sample;
    }
}

/// <summary>
/// The guide's window: beside the app's main window, which is used as ever while this
/// one is open. It has nothing to press but what closes a window.
/// </summary>
public sealed class GuideWindow : Window
{
    private readonly GuideView view;

    public GuideWindow(LibraryModel model)
    {
        Title = GuideView.Title;
        // Some 780 wide and 820 tall, which shows everything down to the end of the
        // quick way; and it can be made any size.
        Width = GuideView.PageWidth + (2 * GuideView.PageMargin);
        Height = 820;
        MinWidth = 460;
        MinHeight = 320;
        view = new GuideView(GuideFiles.Guide, SheetLook.Written(model.TextChordColour));
        Content = view;
        SetResourceReference(BackgroundProperty, "ApplicationBackgroundBrush");
        // A chord in its line of ChordPro text is the colour chords have in the text,
        // if another is chosen in the settings while it is open.
        void Follow(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => view.ChordColour = SheetLook.Written(model.TextChordColour);
        model.PropertyChanged += Follow;
        Closed += (_, _) => model.PropertyChanged -= Follow;
    }

    /// <summary>
    /// Where the window opens: in front of the main window and to one side of it where
    /// the screen has room; over its right-hand part where it has none.
    /// </summary>
    /// <param name="main">Where the main window is.</param>
    /// <param name="screen">The part of the screen that windows have.</param>
    /// <param name="size">How big this window is.</param>
    internal static Point PlaceBeside(Rect main, Rect screen, Size size)
    {
        const double apart = 12;
        double left;
        if (main.Right + apart + size.Width <= screen.Right)
        {
            left = main.Right + apart;
        }
        else if (main.Left - apart - size.Width >= screen.Left)
        {
            left = main.Left - apart - size.Width;
        }
        else
        {
            left = Math.Max(screen.Left, screen.Right - size.Width - apart);
        }
        var top = Math.Min(Math.Max(screen.Top, main.Top + 30), Math.Max(screen.Top, screen.Bottom - size.Height));
        return new Point(left, top);
    }
}
