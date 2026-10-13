// The guide's pictures: of this app's own sheet, drawn here from a test song, and kept in
// the repository beside the guide's words (windows/AppViews/Guide), from where they go
// into the app. spec/13-guide.md, "The pictures", rules 12 to 15.
//
// Each is the strip and the first row of slides of Amazing Grace, as the window shows
// them with the sheet at its usual size, in a pane as wide as the guide's page (720),
// with the sheet in the state the picture is of: the spot on a word, a chord being
// typed, a chord being dragged. In each the line the person is at is the highlighted
// one, its words in their own colour, as the sheet draws it for whoever does these
// things. A view that is drawn has no pointer in it, so the pointer is drawn in
// afterwards, where it would be: Windows' own arrow, drawn here and not taken from the
// system, whose own is whatever size and colour the person has made theirs. They are
// drawn at two dots to the unit, so 1440 across, whatever screen this PC has; and
// with no scroll bar in them.
//
// TO MAKE THEM AGAIN, when how the sheet or its strip is drawn has changed (in
// PowerShell, from windows/):
//
//     $env:PRO7_GUIDE_PICTURES = "write"
//     dotnet test --project Tests/AppViews.Tests --filter-class "*GuidePictureTests"
//
// and then LOOK AT THEM (they are written to the pictures' folder too, as "guide
// <name>.png": Pictures.cs says where that is), and commit them.
//
// What holds them true. `TheGuidesPicturesAreWhatTheAppDrawsNow` draws each afresh and
// holds the kept one to it, dot for dot: a picture in the guide of a sheet the app no
// longer draws is a comment that has become untrue. It is not asked on GitHub's
// machine, which may draw a letter a hair otherwise than the PC the pictures were made
// on (it has other versions of the fonts, for one); there the pictures are only seen to
// be there, of the size they should be, and not blank. Should it fail on a PC where
// nothing of the sheet was changed, look at the fresh pictures, and if they are right
// write them again.

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Pro7ChordEditor.Song;
using Pro7ChordEditor.Tests;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class GuidePictureTests
{
    /// <summary>Where the kept pictures are, in the source: beside the guide's words.</summary>
    internal static readonly string Kept = Path.Combine(TestData.Repository, "windows", "AppViews", "Guide");

    private static readonly bool Writing = string.Equals(Environment.GetEnvironmentVariable("PRO7_GUIDE_PICTURES"), "write", StringComparison.Ordinal);

    private static readonly bool OnGitHub = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") is not null || Environment.GetEnvironmentVariable("CI") is not null;

    /// <summary>The names of the three, in the order the guide has them.</summary>
    internal static readonly string[] Names = ["point-and-press", "typing", "dragging"];

    // Windows' own arrow, as it comes: white, with a black edge, its tip at the top
    // left. In units, from its tip.
    private static readonly Point[] Arrow =
    [
        new(0, 0), new(0, 16.6), new(3.9, 13.1), new(6.6, 19.4), new(9.1, 18.3), new(6.4, 12.2), new(11.9, 12.2),
    ];

    // The sheet's view with Amazing Grace on it, in a pane as wide as the guide's page,
    // with no scroll bar; and a hand at it.
    private static async Task<(ChordSheetView View, Hand Hand)> Scene(MadeUpPC pc)
    {
        var model = pc.Model();
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        model.Open(pc.Song("Amazing Grace"));
        var view = new ChordSheetView { DataContext = model };
        ((ScrollViewer)view.FindName("Scroll")).VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        Look.Wear(view, dark: true);
        Pictures.LayOut(view, GuideView.PageWidth, 600);
        return (view, new Hand(view.Canvas));
    }

    // The scene as a picture: the strip and the first row of slides, and no more; with
    // the pointer drawn in at a point of the sheet, if it has one.
    private static BitmapSource Drawn(ChordSheetView view, Hand hand, Point? pointer)
    {
        Pictures.LayOut(view, GuideView.PageWidth, 600);
        var canvasAt = hand.Canvas.TranslatePoint(new Point(0, 0), view);
        var firstRow = hand.Grid.Cells.Where(cell => cell.GridRow == 0).Max(cell => cell.Frame.Bottom);
        // (Down to under the first row's frames, with the room the ring round a slide
        // that is picked takes, and a little of the sheet's ground under that.)
        var tall = Math.Ceiling(canvasAt.Y + firstRow + 9);
        var whole = new RenderTargetBitmap((int)(GuideView.PageWidth * Pictures.Scale), (int)(600 * Pictures.Scale), 96 * Pictures.Scale, 96 * Pictures.Scale, PixelFormats.Pbgra32);
        whole.Render(view);
        if (pointer is { } at)
        {
            var arrow = new DrawingVisual();
            using (var context = arrow.RenderOpen())
            {
                var shape = new StreamGeometry();
                using (var drawing = shape.Open())
                {
                    drawing.BeginFigure(new Point(canvasAt.X + at.X + Arrow[0].X, canvasAt.Y + at.Y + Arrow[0].Y), isFilled: true, isClosed: true);
                    drawing.PolyLineTo([.. Arrow.Skip(1).Select(point => new Point(canvasAt.X + at.X + point.X, canvasAt.Y + at.Y + point.Y))], isStroked: true, isSmoothJoin: false);
                }
                shape.Freeze();
                context.DrawGeometry(Brushes.White, new Pen(Brushes.Black, 1) { LineJoin = PenLineJoin.Miter }, shape);
            }
            whole.Render(arrow);
        }
        var cut = new CroppedBitmap(whole, new Int32Rect(0, 0, whole.PixelWidth, (int)(tall * Pictures.Scale)));
        cut.Freeze();
        return cut;
    }

    // Where the pointer's tip is for a letter of a line: at the foot of the letter.
    private static Point AtTheFootOf(Spot letter, Hand hand)
    {
        var item = hand.Grid.Lines[letter.Row];
        var metrics = hand.Canvas.Metrics;
        return new Point(Math.Round(hand.Grid.XOf(letter) + 5), Math.Round(item.ChordTop + metrics.BubbleHeight + metrics.Tail + (metrics.WordHeight * 0.7)));
    }

    /// <summary>Each of the three, drawn afresh: its name, and the picture.</summary>
    internal static async Task<IReadOnlyList<(string Name, BitmapSource Picture)>> Fresh(MadeUpPC pc)
    {
        var made = new List<(string, BitmapSource)>();

        // As the song opens, with the spot on the "w" of "wretch" in "That saved a
        // wretch like me", which has no chord: so the spot is the orange outline with a
        // plus in it. The pointer is on that letter.
        var (view, hand) = await Scene(pc);
        var wretch = hand.PlaceOf("wretch");
        Assert.Equal("", hand.Sheet.ChordAt(wretch.Row, wretch.At));
        hand.PointAt(wretch);
        made.Add((Names[0], Drawn(view, hand, AtTheFootOf(wretch, hand))));

        // With the bubble a chord is typed in open at that same place, and "D" typed in
        // it: the chords it offers are over the box. No pointer.
        (view, hand) = await Scene(pc);
        hand.Click(hand.PlaceOf("wretch"));
        hand.Type("D");
        Assert.True(hand.BubbleIsOpen);
        Assert.Equal(14, hand.Canvas.Entry.Shown.Count);
        hand.Canvas.PointerLeft();
        made.Add((Names[1], Drawn(view, hand, null)));

        // With that line's chord, the D, picked up and carried over the "s" of "saved":
        // the bubble it left is faint, and its name is lit where it would land. The
        // pointer is on that letter.
        (view, hand) = await Scene(pc);
        var saved = hand.PlaceOf("saved");
        var line = hand.Sheet.Rows[saved.Row];
        var chord = Assert.Single(line.Chords);
        Assert.Equal("D", chord.Name);
        hand.Drag(new Spot(saved.Row, chord.At), saved, letGo: false);
        Assert.Equal((new Spot(saved.Row, chord.At), "D"), hand.Sheet.Dragged);
        Assert.Equal(saved, hand.Sheet.Spot);
        made.Add((Names[2], Drawn(view, hand, AtTheFootOf(saved, hand))));
        return made;
    }

    private static void Write(BitmapSource picture, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(picture));
        using var file = File.Create(path);
        encoder.Save(file);
    }

    // A picture's dots, each as its blue, green, red and opacity.
    private static byte[] DotsOf(BitmapSource picture)
    {
        var plain = new FormatConvertedBitmap(picture, PixelFormats.Bgra32, null, 0);
        var dots = new byte[plain.PixelWidth * plain.PixelHeight * 4];
        plain.CopyPixels(dots, plain.PixelWidth * 4, 0);
        return dots;
    }

    private static BitmapSource Read(string path)
    {
        var decoded = new PngBitmapDecoder(new MemoryStream(File.ReadAllBytes(path)), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
        decoded.Freeze();
        return decoded;
    }

    [Fact]
    public void TheGuidesPicturesAreWhatTheAppDrawsNow() => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        var fresh = await Fresh(pc);
        Assert.Equal(Names, fresh.Select(one => one.Name));
        foreach (var (name, picture) in fresh)
        {
            // (For whoever is to look at them: beside the other pictures.)
            Write(picture, Path.Combine(Pictures.Folder, "guide " + name + ".png"));
            var kept = Path.Combine(Kept, name + ".png");
            if (Writing)
            {
                Write(picture, kept);
                continue;
            }
            Assert.True(File.Exists(kept), "the guide has no picture " + name + ": run this with PRO7_GUIDE_PICTURES=write, look at what it makes, and commit it");
            if (OnGitHub)
            {
                continue;
            }
            var was = Read(kept);
            Assert.True(
                (was.PixelWidth, was.PixelHeight) == (picture.PixelWidth, picture.PixelHeight) && DotsOf(was).AsSpan().SequenceEqual(DotsOf(picture)),
                "the guide's picture " + name + " is not what the app draws now. If the sheet was meant to change, run this with PRO7_GUIDE_PICTURES=write, LOOK at the pictures it makes (they are in " + Pictures.Folder + " too), and commit them");
        }
    });

    [Fact]
    public void EveryPictureTheGuidesWordsAskForIsKeptTwiceAsManyDotsAcrossAsThePageIsWideAndNotBlankAndNoOtherIsKept() => Pictures.OnAThreadOfItsOwn(() =>
    {
        var guide = GuideFiles.Guide;
        Assert.NotNull(guide);
        Assert.Equal(Names, guide.PicturesNamed);
        // In the source, and in the app as it is built: the same files, and no other.
        var inTheSource = Directory.EnumerateFiles(Kept, "*.png").Select(path => Path.GetFileNameWithoutExtension(path)!).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(Names.Order(StringComparer.Ordinal), inTheSource);
        Assert.Equal(Names.Select(name => name + ".png").Append(GuideFiles.WordsName).Order(StringComparer.Ordinal), GuideFiles.Names);
        foreach (var name in Names)
        {
            Assert.Equal(File.ReadAllBytes(Path.Combine(Kept, name + ".png")), GuideFiles.Picture(name));
            var picture = GuideView.PictureNamed(name);
            Assert.NotNull(picture);
            // 1440 dots across, which it says are 720 units: two dots to the unit.
            Assert.Equal(1440, picture.PixelWidth);
            Assert.Equal(GuideView.PageWidth, picture.Width, 0.01);
            Assert.True(picture.PixelHeight is > 400 and < 1100, name + " is " + picture.PixelHeight + " dots tall");
            // Not blank: the sheet's dark ground, words, and the sheet's orange.
            var dots = DotsOf(picture);
            var colours = new HashSet<int>();
            var orange = 0;
            for (var dot = 0; dot < dots.Length; dot += 4)
            {
                colours.Add((dots[dot + 2] << 16) | (dots[dot + 1] << 8) | dots[dot]);
                if (dots[dot + 2] > 230 && dots[dot + 1] is > 110 and < 165 && dots[dot] < 70)
                {
                    orange++;
                }
            }
            Assert.True(colours.Count > 200, name + " has only " + colours.Count + " colours");
            Assert.True(orange > 300, name + " has only " + orange + " dots of the sheet's orange");
            // And it says what it shows, for whoever cannot see it.
            Assert.True(guide.Pictures[name].Length > 40);
        }
        // What is asked for and is not there is said to be nowhere.
        Assert.Null(GuideFiles.Picture("no-such-picture"));
        Assert.Null(GuideView.PictureNamed("no-such-picture"));
    });
}
