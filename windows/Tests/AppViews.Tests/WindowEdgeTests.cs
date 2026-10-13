// The edge between the lists and the song; the title over the choice of place; Earlier
// Versions in a colour of its own; and the app's icon. spec/06-library.md, rules 6a and
// 7; spec/07-backups.md, rule 15; spec/10-platforms.md, "Where each follows its
// platform". All asked for on the Mac on 2026-10-13, and built here to look and behave
// as there.
//
// MINE: none of it is in Simple Presenter, so there is no vector. What each case
// expects is the page's sentence, with the Mac's numbers (210, 480, 345, 380, 8).
//
// What only a hand can tell is not here: that the pointer changes shape the moment it
// is over the bar (the test holds that the bar says which pointer it is to have, and
// that every point of its width is the bar's), and how the bar feels to drag.

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Pro7ChordEditor.Tests;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class WindowEdgeTests
{
    private static void OnAMadeUpPC(Func<MadeUpPC, Task> what) => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        await what(pc);
    });

    private static async Task<(LibraryModel Model, LibraryView View)> Open(MadeUpPC pc, string? song = "Amazing Grace", double width = 1280, double height = 800, bool dark = true)
    {
        var model = pc.Model();
        var view = new LibraryView(model);
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        if (song is not null)
        {
            model.Open(pc.Song(song));
        }
        Look.Wear(view, dark);
        Pictures.LayOut(view, width, height);
        return (model, view);
    }

    private static T Named<T>(FrameworkElement within, string name)
        where T : class =>
        within.FindName(name) as T ?? throw new InvalidOperationException("there is no " + name + " there");

    private static Rect PartOf(FrameworkElement part, FrameworkElement whole) => new(part.TranslatePoint(new Point(0, 0), whole), new Size(part.ActualWidth, part.ActualHeight));

    // Everything drawn under a part of the window, as the kinds of thing it is made of.
    private static IEnumerable<DependencyObject> Under(DependencyObject top)
    {
        for (var n = 0; n < VisualTreeHelper.GetChildrenCount(top); n++)
        {
            var child = VisualTreeHelper.GetChild(top, n);
            yield return child;
            foreach (var deeper in Under(child))
            {
                yield return deeper;
            }
        }
    }

    // The colour of most of the dots of a part of a picture.
    private static Color MostOf((int Width, int Height, int[] Dots) picture, Rect part)
    {
        var counted = new Dictionary<int, int>();
        for (var y = (int)Math.Ceiling(part.Top * Pictures.Scale); y < (int)(part.Bottom * Pictures.Scale); y++)
        {
            for (var x = (int)Math.Ceiling(part.Left * Pictures.Scale); x < (int)(part.Right * Pictures.Scale); x++)
            {
                var dot = picture.Dots[(y * picture.Width) + x] & 0xFFFFFF;
                counted[dot] = counted.GetValueOrDefault(dot) + 1;
            }
        }
        var most = counted.MaxBy(pair => pair.Value).Key;
        return Color.FromRgb((byte)(most >> 16), (byte)(most >> 8), (byte)most);
    }

    // ---- The edge (spec/06, rule 6a)

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheEdgeIsABarThatIsSeenEightWideWithAGripAndLiesOverNeitherPart(bool dark) => OnAMadeUpPC(async pc =>
    {
        var (_, view) = await Open(pc, dark: dark);
        var edge = view.TheEdge;
        var lists = PartOf(view.ListsPane, view);
        var song = PartOf(view.SongPane, view);
        var bar = PartOf(edge, view);
        // As it starts: the lists half way between their least and their most.
        Assert.Equal(345, lists.Width);
        Assert.Equal(8, bar.Width);
        Assert.Equal(ListsLook.EdgeWidth, bar.Width);
        // It lies between the two, over neither: the lists end where it begins, and
        // the song begins where it ends.
        Assert.Equal(lists.Right, bar.Left);
        Assert.Equal(bar.Right, song.Left);
        Assert.Equal(1280, song.Right);
        Assert.Equal(song.Height, bar.Height);
        // It is the colour of the bar between the two lists, all of its width: seen.
        var picture = Pictures.Drawn(view, 1280, 800);
        var grey = Look.Colours(dark)[Look.Bar];
        Assert.Equal(grey, MostOf(picture, bar));
        Assert.Equal(grey, MostOf(picture, new Rect(bar.Left, bar.Top + 40, 1, 200)));
        Assert.Equal(grey, MostOf(picture, new Rect(bar.Right - 1, bar.Top + 40, 1, 200)));
        Assert.Equal(grey, MostOf(picture, PartOf(Named<FrameworkElement>(view.ListsPane, "Bar"), view)));
        // With a small grip in its middle, as that bar has: upright here.
        var grip = Under(edge).OfType<Border>().Single(border => border.Name == "EdgeGrip");
        var theOther = Named<Border>(view.ListsPane, "Grip");
        Assert.Equal((theOther.Height, theOther.Width), (grip.Width, grip.Height));
        Assert.Equal(theOther.Opacity, grip.Opacity);
        var gripAt = PartOf(grip, view);
        Assert.Equal(bar.Left + ((bar.Width - gripAt.Width) / 2), gripAt.Left, 0.6);
        Assert.Equal(bar.Top + ((bar.Height - gripAt.Height) / 2), gripAt.Top, 0.6);
        Assert.NotEqual(grey, MostOf(picture, gripAt));
        // It says which pointer it is to have: the one for dragging sideways. And it
        // does not take the keyboard.
        Assert.Equal(Cursors.SizeWE, edge.Cursor);
        Assert.False(edge.Focusable);
        Assert.Contains("Click twice", (string)edge.ToolTip, StringComparison.Ordinal);
    });

    [Fact]
    public void TheListsKeepTheirWidthAsTheWindowIsMadeWiderOrNarrowerTheSongHavingTheDifference() => OnAMadeUpPC(async pc =>
    {
        var (_, view) = await Open(pc);
        double Lists(double window)
        {
            Pictures.LayOut(view, window, 800);
            Assert.Equal(window, view.ListsPane.ActualWidth + view.TheEdge.ActualWidth + view.SongPane.ActualWidth);
            return view.ListsPane.ActualWidth;
        }

        Assert.Equal(345, Lists(1280));
        Assert.Equal(345, Lists(1920));
        Assert.Equal(345, Lists(2560));
        Assert.Equal(345, Lists(900));
        Assert.Equal(345, Lists(345 + 8 + 380));
        // So narrow that the song would be left with less than its own least: the
        // lists give way, down to theirs.
        Assert.Equal(700 - 8 - 380, Lists(700));
        Assert.Equal(380, view.SongPane.ActualWidth);
        Assert.Equal(640 - 8 - 380, Lists(640));
        // Made wide again, they have their width back.
        Assert.Equal(345, Lists(1280));
    });

    [Fact]
    public void WhereTheEdgeIsLeftIsRememberedAndClickedTwiceItGoesBackToWhereItStarts() => OnAMadeUpPC(async pc =>
    {
        var (model, view) = await Open(pc);
        Assert.Equal(345, model.ListsWidth);
        Assert.Null(pc.AppData.ReadSettings().ListsWidth);

        // Dragged, and let go where the lists are 400 wide.
        view.EdgeLeftAt(400);
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal(400, view.ListsPane.ActualWidth);
        Assert.Equal(400, model.ListsWidth);
        Assert.Equal(400, pc.AppData.ReadSettings().ListsWidth);
        // The lists keep that width as the window changes, as they kept the other.
        Pictures.LayOut(view, 1700, 800);
        Assert.Equal(400, view.ListsPane.ActualWidth);
        Pictures.LayOut(view, 1280, 800);

        // The app is started again: the lists are as wide as they were left.
        var (again, another) = await Open(pc);
        Assert.Equal(400, again.ListsWidth);
        Assert.Equal(400, another.ListsPane.ActualWidth);

        // Clicked twice, it goes back to where it starts; and nothing is kept.
        view.TheEdge.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = Control.MouseDoubleClickEvent });
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal(345, view.ListsPane.ActualWidth);
        Assert.Equal(345, model.ListsWidth);
        Assert.Null(pc.AppData.ReadSettings().ListsWidth);

        // Never less than the lists' least nor more than their most, whatever is said.
        model.SetListsWidth(100);
        Assert.Equal(210, model.ListsWidth);
        model.SetListsWidth(900);
        Assert.Equal(480, model.ListsWidth);
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal(480, view.ListsPane.ActualWidth);
        model.SetListsWidth(double.NaN);
        Assert.Equal(345, model.ListsWidth);
        // One kept that is out of bounds is brought within them when the app starts.
        pc.AppData.Keep(pc.AppData.ReadSettings() with { ListsWidth = 5000 });
        var next = pc.Model();
        await next.StartAsync();
        Assert.Equal(480, next.ListsWidth);
    });

    // What WPF's own thing for dragging does when it is dragged: the column is made
    // that much wider there and then, and when it is let go the model is told.
    [Fact]
    public void DraggedTheEdgeMakesTheListsThatMuchWiderAndLetGoTheModelIsTold() => OnAMadeUpPC(async pc =>
    {
        var (model, view) = await Open(pc);
        var edge = view.TheEdge;
        edge.RaiseEvent(new DragStartedEventArgs(0, 0));
        edge.RaiseEvent(new DragDeltaEventArgs(60, 0));
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal(405, view.ListsPane.ActualWidth);
        // (Not yet the model's: it is told when the edge is let go.)
        Assert.Equal(345, model.ListsWidth);
        edge.RaiseEvent(new DragCompletedEventArgs(60, 0, canceled: false));
        Assert.Equal(405, model.ListsWidth);
        Assert.Equal(405, pc.AppData.ReadSettings().ListsWidth);
        // Dragged past the most the lists can be, they stop at it.
        edge.RaiseEvent(new DragStartedEventArgs(0, 0));
        edge.RaiseEvent(new DragDeltaEventArgs(400, 0));
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal(480, view.ListsPane.ActualWidth);
        edge.RaiseEvent(new DragCompletedEventArgs(400, 0, canceled: false));
        Assert.Equal(480, model.ListsWidth);
    });

    // ---- The title over the choice of place (spec/06, rule 7)

    [Fact]
    public void TheTopLeftPartSaysWhatItIsInTheSmallCapitalsTheListOfLibrariesHasForItsOwnTitle() => OnAMadeUpPC(async pc =>
    {
        var (_, view) = await Open(pc, song: null);
        var title = Named<TextBlock>(view.ListsPane, "PlaceTitle");
        Assert.Equal("WORKSPACE OR PROPRESENTER FOLDER", title.Text);
        var libraries = Under(view.ListsPane).OfType<TextBlock>().Single(words => words.Text == "LIBRARIES");
        Assert.Equal(libraries.FontSize, title.FontSize);
        Assert.Equal(((SolidColorBrush)libraries.Foreground).Color, ((SolidColorBrush)title.Foreground).Color);
        // It is over the choice of place and Choose Folder, which are over the libraries.
        double Top(FrameworkElement part) => PartOf(part, view).Top;
        Assert.True(Top(title) + title.ActualHeight <= Top(Named<ComboBox>(view.ListsPane, "Places")));
        Assert.True(Top(Named<ComboBox>(view.ListsPane, "Places")) < Top(Named<Button>(view.ListsPane, "Choose")));
        Assert.True(Top(Named<Button>(view.ListsPane, "Choose")) < Top(libraries));
        // And is read whole at the lists' usual width, and at their least.
        Assert.True(title.ActualWidth >= title.DesiredSize.Width - 0.5);
        Pictures.LayOut(view, 640, 480);
        Assert.Equal(252, view.ListsPane.ActualWidth);
        title.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var whole = title.DesiredSize.Width;
        Pictures.LayOut(view, 640, 480);
        Assert.True(title.ActualWidth >= whole - 0.5, "the title is " + whole + " wide and has " + title.ActualWidth);
    });

    // ---- Earlier Versions (spec/07, rule 15)

    [Fact]
    public void EarlierVersionsIsAButtonInABlueOfItsOwnWithWhiteWordsAndBeforeThemTheSongsPicture() => OnAMadeUpPC(async pc =>
    {
        pc.GivePlainSongAHistory();
        var (model, view) = await Open(pc, "Plain Song");
        var button = Named<Button>(view.SongPane, "Earlier");
        Assert.True(model.HasEarlierVersions);
        Assert.Equal(Visibility.Visible, button.Visibility);
        Assert.Equal("Earlier Versions", button.Content);
        Assert.Equal("#2978db", SheetLook.AsWritten(HeaderLook.EarlierVersions));

        // The picture a song has in the list, in white, and after it the words, white.
        var icon = Under(button).OfType<PresentationIcon>().Single();
        var words = Under(button).OfType<TextBlock>().Single();
        Assert.Equal(Colors.White, ((SolidColorBrush)icon.Fill).Color);
        Assert.Equal(Colors.White, ((SolidColorBrush)words.Foreground).Color);
        Assert.Equal("Earlier Versions", words.Text);
        Assert.True(PartOf(icon, view).Right <= PartOf(words, view).Left);
        Assert.True(icon.ActualWidth >= 12 && icon.ActualHeight >= 12);
        // Drawn: most of it is the blue, with white in it; in the light look as in the dark.
        foreach (var dark in new[] { true, false })
        {
            Look.Wear(view, dark);
            var picture = Pictures.Drawn(view, 1280, 800);
            var where = PartOf(button, view);
            Assert.Equal(HeaderLook.EarlierVersions, MostOf(picture, where));
            var white = 0;
            for (var y = (int)(where.Top * Pictures.Scale); y < (int)(where.Bottom * Pictures.Scale); y++)
            {
                for (var x = (int)(where.Left * Pictures.Scale); x < (int)(where.Right * Pictures.Scale); x++)
                {
                    if ((picture.Dots[(y * picture.Width) + x] & 0xFFFFFF) == 0xFFFFFF)
                    {
                        white++;
                    }
                }
            }
            Assert.True(white > 150, "only " + white + " dots of the button are white");
        }

        // It opens the panel, as it did.
        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Assert.NotNull(model.Earlier);
        model.CloseEarlierVersions();

        // "Nothing in the song's header is ever cut short but the song's name": at
        // every width the window can have, the button has all the room it asks for, its
        // picture and its words whole. (On the Mac it was being squeezed to its
        // picture alone in a tight header.)
        button.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var asked = button.DesiredSize.Width - button.Margin.Left - button.Margin.Right;
        var ways = new HashSet<HeaderRows>();
        for (var width = 640.0; width <= 1700; width += 53)
        {
            Pictures.LayOut(view, width, 800);
            ways.Add(view.SongPane.HeaderLaidOut!.Value);
            Assert.True(button.ActualWidth >= asked - 0.5, "at " + width + " the button has " + button.ActualWidth + " of the " + asked + " it asks for");
            Assert.True(PartOf(words, view).Right <= PartOf(button, view).Right);
            Assert.True(PartOf(button, view).Right <= PartOf(view.SongPane, view).Right);
        }
        Assert.True(ways.Count > 1, "the header was laid out one way only: this held nothing of a tight one");
        // A song with no copies kept has no such button.
        model.Open(pc.Song("Amazing Grace"));
        Pictures.LayOut(view, 1280, 800);
        Assert.Equal(Visibility.Collapsed, button.Visibility);
    });

    // ---- The icon (spec/10, "Where each follows its platform")

    [Fact]
    public void TheProgramIsGivenTheMaintainersIconAndAboutShowsIt() => Pictures.OnAThreadOfItsOwn(() =>
    {
        var folder = Path.Combine(TestData.Repository, "windows", "App");
        var icon = Path.Combine(folder, "ChordEditor.ico");
        Assert.True(File.Exists(icon));
        // The file is an icon, with pictures of several sizes in it, a large one among them.
        var bytes = File.ReadAllBytes(icon);
        Assert.Equal((0, 0, 1, 0), (bytes[0], bytes[1], bytes[2], bytes[3]));
        var decoded = new System.Windows.Media.Imaging.IconBitmapDecoder(new Uri(icon), System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
        Assert.True(decoded.Frames.Count >= 3);
        Assert.Contains(decoded.Frames, frame => frame.PixelWidth >= 256);
        Assert.Contains(decoded.Frames, frame => frame.PixelWidth == 16);
        // The program names it as its own: so its file and its windows have it.
        Assert.Contains("<ApplicationIcon>ChordEditor.ico</ApplicationIcon>", File.ReadAllText(Path.Combine(folder, "App.csproj")), StringComparison.Ordinal);

        // About shows it, over the app's name: the same picture, from the same file.
        var about = new AboutSettings();
        Look.Wear(about, dark: true);
        Pictures.LayOut(about, 620, 520);
        var shown = Named<Image>(about, "Icon");
        Assert.NotNull(shown.Source);
        Assert.Equal((72.0, 72.0), (shown.ActualWidth, shown.ActualHeight));
        Assert.True(PartOf(shown, about).Bottom <= PartOf(Named<TextBlock>(about, "Named"), about).Top);
        // Drawn, it has the orange of the icon in it.
        var picture = Pictures.Drawn(about, 620, 520);
        var where = PartOf(shown, about);
        var orange = 0;
        for (var y = (int)(where.Top * Pictures.Scale); y < (int)(where.Bottom * Pictures.Scale); y++)
        {
            for (var x = (int)(where.Left * Pictures.Scale); x < (int)(where.Right * Pictures.Scale); x++)
            {
                var dot = picture.Dots[(y * picture.Width) + x];
                if (((dot >> 16) & 0xFF) > 200 && ((dot >> 8) & 0xFF) is > 80 and < 190 && (dot & 0xFF) < 110)
                {
                    orange++;
                }
            }
        }
        Assert.True(orange > 2000, "only " + orange + " dots of the icon are orange");
    });
}
