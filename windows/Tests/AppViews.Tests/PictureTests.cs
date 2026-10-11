// Pictures of the window, to be looked at (Pictures.cs says where they go).
//
// A test here cannot tell whether a picture looks right: only eyes can, and whoever
// changes how something is drawn is to open the pictures and say what was seen
// (spec/10-platforms.md). What a test CAN tell is that a picture was drawn at all, at
// the size asked for, and has something in it; a picture that failed to draw would
// otherwise be a blank one that nobody happened to open.
//
// Milestone M0 has the one view there is. From M3 on these are the library's lists,
// the song, the chord sheet, and the cases spec/10 lists.

using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class PictureTests
{
    [Fact]
    public void TheWindowAsItOpens()
    {
        Pictures.OnAThreadOfItsOwn(() =>
        {
            // What LibraryWindow holds, in the room that window opens with.
            var path = Pictures.Draw(new NotBuiltYet(), 1280, 800, "01 the window as it opens");

            var picture = Read(path);
            Assert.Equal(1280 * Pictures.Scale, picture.Width);
            Assert.Equal(800 * Pictures.Scale, picture.Height);
            // A ground and two lines of words in two colours, with the shades between
            // that smooth the letters' edges: some dozens of colours. A view that drew
            // nothing would be one.
            Assert.True(picture.Colours > 10, $"{path} has only {picture.Colours} colours in it: was anything drawn?");
        });
    }

    // A picture file's size in dots, and how many different colours it has.
    private static (int Width, int Height, int Colours) Read(string path)
    {
        using var file = File.OpenRead(path);
        var frame = new PngBitmapDecoder(file, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
        var dots = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
        var all = new int[dots.PixelWidth * dots.PixelHeight];
        dots.CopyPixels(all, dots.PixelWidth * 4, 0);
        return (dots.PixelWidth, dots.PixelHeight, all.Distinct().Count());
    }
}
