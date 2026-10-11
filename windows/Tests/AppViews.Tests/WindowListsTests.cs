// The window as far as it can be told without a hand or an eye: how the bar between the
// two lists shares out their height and what it says, the small picture that is drawn
// before each name, how a line of a song is parted into chords and words, and the look
// (spec/06-library.md, rules 6a to 6c; spec/10-platforms.md, "Looks"). What they look
// like is in the pictures (PictureTests); that the bar drags is the maintainer's to try.
//
// MINE. The numbers the first three expect are those the Mac's tests of the same things
// expect (WindowListsTests.swift), so that the two apps share out the height alike and
// draw the same outline.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Pro7ChordEditor.Song;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class WindowListsTests
{
    [Fact]
    public void HowTallTheLibrariesListIsItsShareOfTheHeightButNeverSoMuchOrSoLittleThatEitherListIsLeftWithLessThanTheLeast()
    {
        Assert.Equal(300, ListsLook.Top(0.3, 1000));
        Assert.Equal(300, ListsLook.Top(0.5, 600));
        // Too little, and too much.
        Assert.Equal(ListsLook.LeastLibraries, ListsLook.Top(0.01, 600));
        Assert.Equal(600 - ListsLook.BarHeight - ListsLook.LeastSongs, ListsLook.Top(0.99, 600));
        // In a window too short for both to have their least, the libraries have theirs.
        Assert.Equal(ListsLook.LeastLibraries, ListsLook.Top(0.5, 250));
        Assert.Equal(ListsLook.LeastLibraries, ListsLook.Top(0.9, 0));
        Assert.Equal((22.0, 170.0, 150.0), (ListsLook.BarHeight, ListsLook.LeastLibraries, ListsLook.LeastSongs));
    }

    [Fact]
    public void HowWideTheListsAreWhereTheEdgeWasLeftButNeverSoWideThatTheSongIsLeftWithLessThanItsLeast()
    {
        Assert.Equal(270, ListsLook.Width(ListsLook.UsualWidth, 1280));
        Assert.Equal(400, ListsLook.Width(400, 1280));
        // A window made narrow takes the width from the lists: the song keeps its least.
        Assert.Equal(640 - ListsLook.EdgeWidth - ListsLook.LeastSong, ListsLook.Width(270, 640));
        Assert.Equal(255, ListsLook.Width(270, 640));
        Assert.Equal(255, ListsLook.Width(400, 640));
        // Made wide again, it gives it back.
        Assert.Equal(400, ListsLook.Width(400, 900));
        // Never less than the lists' least, nor more than their most, wherever the edge was left.
        Assert.Equal(ListsLook.LeastWidth, ListsLook.Width(270, 500));
        Assert.Equal(ListsLook.LeastWidth, ListsLook.Width(100, 1280));
        Assert.Equal(ListsLook.MostWidth, ListsLook.Width(600, 2000));
        // The Mac app's numbers (LibraryWindow.swift).
        Assert.Equal((270.0, 210.0, 480.0, 380.0), (ListsLook.UsualWidth, ListsLook.LeastWidth, ListsLook.MostWidth, ListsLook.LeastSong));
    }

    [Fact]
    public void TheBarSaysHowManySongsTheLibraryHasAndHowManyOfThemAreListedWhereTheListIsNarrowed()
    {
        Assert.Equal("8 SONGS", ListsLook.Count(8, 8));
        Assert.Equal("1 SONG", ListsLook.Count(1, 1));
        Assert.Equal("0 SONGS", ListsLook.Count(0, 0));
        Assert.Equal("3 OF 8 SONGS", ListsLook.Count(3, 8));
        Assert.Equal("1 OF 8 SONGS", ListsLook.Count(1, 8));
        Assert.Equal("0 OF 1 SONGS", ListsLook.Count(0, 1));
        Assert.Equal("1234 SONGS", ListsLook.Count(1234, 1234));
    }

    [Fact]
    public void TheMarkOfAPresentationIsDrawnFromItsOutlineABarAndAScreenInsideTheSquareItIsGivenAtAnySize()
    {
        static string Said(Rect room) => room.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var outline = PresentationGlyph.Outline;
        var whole = outline.Bounds;
        // The outline fills its square of 200 from side to side, and nearly from top to
        // bottom.
        Assert.True(Math.Abs(whole.Left) < 0.01 && Math.Abs(whole.Right - 200) < 0.01, Said(whole));
        Assert.True(whole.Top > 4 && whole.Top < 8 && whole.Bottom > 192 && whole.Bottom < 196, Said(whole));
        // Its two parts do not touch: the bar ends before the screen begins.
        Assert.False(outline.FillContains(new Point(66, 100)));
        Assert.True(outline.FillContains(new Point(30, 100)));
        // The screen is an outline: its middle is clear, and its edge is not.
        Assert.False(outline.FillContains(new Point(110, 100)));
        Assert.True(outline.FillContains(new Point(170, 100)));
        // Given a rectangle, it is drawn in the middle of it, as big as fits, square.
        var small = PresentationGlyph.FittedIn(new Rect(10, 20, 40, 16)).Bounds;
        Assert.True(Math.Abs(small.Width - 16) < 0.01 && Math.Abs(((small.Left + small.Right) / 2) - 30) < 0.01, Said(small));
        Assert.True(small.Top >= 20 && small.Bottom <= 36, Said(small));
        var tall = PresentationGlyph.FittedIn(new Rect(0, 0, 10, 50)).Bounds;
        Assert.True(Math.Abs(tall.Width - 10) < 0.01 && tall.Top > 20 && tall.Bottom < 30, Said(tall));
        // The mark's three colours, as the Mac app has them.
        Assert.Equal([Color.FromRgb(255, 201, 0), Color.FromRgb(255, 143, 0), Color.FromRgb(255, 64, 0)], LibraryIcon.Colours);
    }

    [Fact]
    public void ALineOfChordProIsPartedIntoChordsAndWhatIsBetweenThemAndPutTogetherAgainIsTheLine()
    {
        static string Shown(string line) => string.Join("|", SongLines.Parts(line).Select(part => (part.IsChord ? "*" : "") + part.Text));

        Assert.Equal("*[G]|One two three |*[D]|four", Shown("[G]One two three [D]four"));
        Assert.Equal("*[G]| |*[D/F#]| |*[Em]", Shown("[G] [D/F#] [Em]"));
        Assert.Equal("Thirteen", Shown("Thirteen"));
        Assert.Equal("", Shown(""));
        Assert.Equal("e|*[G/B]|leven", Shown("e[G/B]leven"));
        Assert.Equal("*[C]|*[G]", Shown("[C][G]"));
        // A bracket that is never closed, or one that only closes, is words.
        Assert.Equal("one [two", Shown("one [two"));
        Assert.Equal("one] two", Shown("one] two"));
        Assert.Equal("*[C]|one [two", Shown("[C]one [two"));
        Assert.Equal("a]b|*[C]|c", Shown("a]b[C]c"));
        foreach (var line in new[] { "[G]One two three [D]four", "one [two", "a]b[C]c", "[C][G]", " ", "{c: Verse 1}" })
        {
            Assert.Equal(line, string.Concat(SongLines.Parts(line).Select(part => part.Text)));
        }
        // What the band says is the page's sentence (spec/06, rule 19).
        Assert.Equal("ProPresenter is running. Quit it to change chords here.", SongPane.ProPresenterIsRunning);
    }

    // ---- The look

    [Fact]
    public void AGroundIsDarkWhenWordsOnItWouldBeWrittenInWhite()
    {
        Assert.True(Look.IsDark(Color.FromRgb(0x20, 0x20, 0x20)));
        Assert.True(Look.IsDark(Colors.Black));
        Assert.True(Look.IsDark(Color.FromRgb(0x58, 0x58, 0x58)));
        Assert.False(Look.IsDark(Color.FromRgb(0xFA, 0xFA, 0xFA)));
        Assert.False(Look.IsDark(Colors.White));
        Assert.False(Look.IsDark(Color.FromRgb(0xBD, 0xBD, 0xBD)));
    }

    [Fact]
    public void TheListsGreysAreProPresentersOwnInTheDarkAndTheirLikeInTheLightTheLibrariesTheLighter()
    {
        static string Written(Color colour) => "#" + colour.R.ToString("x2", System.Globalization.CultureInfo.InvariantCulture)
            + colour.G.ToString("x2", System.Globalization.CultureInfo.InvariantCulture) + colour.B.ToString("x2", System.Globalization.CultureInfo.InvariantCulture);
        var dark = Look.Colours(dark: true);
        var light = Look.Colours(dark: false);
        // spec/06, rule 6b.
        Assert.Equal(("#585858", "#3e3e3e", "#2f2f2f"), (Written(dark[Look.Libraries]), Written(dark[Look.Songs]), Written(dark[Look.Bar])));
        Assert.Equal(("#ececec", "#d5d5d5", "#bdbdbd"), (Written(light[Look.Libraries]), Written(light[Look.Songs]), Written(light[Look.Bar])));
        // spec/06, "Settings": a chord in the ChordPro text, where a system has no orange of its own.
        Assert.Equal(("#ff9500", "#ff9500"), (Written(dark[Look.Chord]), Written(light[Look.Chord])));
        // Every colour of ours is there for both, and each is one a ground can be: not see-through.
        Assert.Equal(dark.Keys.Order(StringComparer.Ordinal), light.Keys.Order(StringComparer.Ordinal));
        Assert.Equal([Look.Band, Look.Bar, Look.Chord, Look.Libraries, Look.Songs], dark.Keys.Order(StringComparer.Ordinal));
        Assert.All(dark.Values.Concat(light.Values), colour => Assert.Equal(255, colour.A));
        // The band's ground is one that the words on it can be read against.
        Assert.True(Look.IsDark(dark[Look.Band]));
        Assert.False(Look.IsDark(light[Look.Band]));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheSystemsLookHasEveryColourTheViewsAskItFor(bool dark) => Pictures.OnAThreadOfItsOwn(() =>
    {
        var look = new ResourceDictionary { Source = Look.Dictionary(dark) };
        Assert.EndsWith("/Fluent." + (dark ? "Dark" : "Light") + ".xaml", look.Source.ToString(), StringComparison.Ordinal);
        foreach (var name in Look.OfTheSystem)
        {
            Assert.True(look[name] is Brush, name + " is not a colour the " + (dark ? "dark" : "light") + " look has.");
        }
        // And the colour by which a view tells dark from light, which is dark in the one
        // and light in the other.
        Assert.Equal(dark, Look.IsDark((Color)look[Look.Ground]));
    });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AViewIsDressedInOurColoursForTheLookItIsUnderAndAgainWhenTheLookChanges(bool dark) => Pictures.OnAThreadOfItsOwn(() =>
    {
        static Color Of(FrameworkElement view, string name) => ((SolidColorBrush)view.FindResource(name)).Color;

        var view = new Border();
        Look.Follow(view);
        // With no look at all, it is dressed for the light.
        Assert.Equal(Look.Colours(dark: false)[Look.Libraries], Of(view, Look.Libraries));

        Look.Wear(view, dark);
        Assert.All(Look.Colours(dark), colour => Assert.Equal(colour.Value, Of(view, colour.Key)));
        // What is in the view has them from it, and the system's own from the look.
        var inside = new TextBlock();
        view.Child = inside;
        Assert.Equal(Look.Colours(dark)[Look.Bar], Of(inside, Look.Bar));
        Assert.NotNull(inside.TryFindResource("TextFillColorPrimaryBrush"));

        // The look turns over, as when the system is switched from one to the other.
        view.Resources.MergedDictionaries.Clear();
        Look.Wear(view, !dark);
        Assert.All(Look.Colours(!dark), colour => Assert.Equal(colour.Value, Of(view, colour.Key)));
    });

    [Fact]
    public void TheWholeViewsModelIsTheOneItWasHanded() => Pictures.OnAThreadOfItsOwn(() =>
    {
        using var pc = new MadeUpPC();
        var model = pc.Model();
        var view = new LibraryView(model);
        Assert.Same(model, view.Model);
        Assert.Same(model, view.DataContext);
    });

    [Fact]
    public void TheMarkFilterHasTheThreeChoicesTheWindowOffers()
    {
        // (The window has a button for each, by name: one more would have none.)
        Assert.Equal([MarkFilter.All, MarkFilter.WithChords, MarkFilter.WithoutChords], Enum.GetValues<MarkFilter>());
    }
}
