// Drawing a view into a picture file, so that it can be looked at
// (spec/10-platforms.md, "Seeing what you have drawn").
//
// HOW. No window is opened. The view is told how much room it has, lays itself out in
// it, and is drawn into a bitmap in memory, which is written out as a PNG. It is drawn
// at twice the size it is given, as a screen of twice the usual fineness would show
// it, so that small text can be read in the picture.
//
// WHERE. Into the folder named by PRO7_PICTURES, if that is set (in PowerShell, from
// the windows folder):
//
//     $env:PRO7_PICTURES = "C:\some\folder"
//     dotnet test --project Tests/AppViews.Tests --filter-class "*PictureTests"
//
// and otherwise into Pro7ChordEditorPictures in the system's temporary folder. The Mac
// app's tests use the same name for the same thing. Each picture is named by the test
// that draws it, with a number first so that they sort in the order they are to be
// looked at. Pictures do not go in the repository.
//
// A view, and anything in it, must be made and used on one thread of the kind Windows
// calls a single-threaded apartment, which a test's own thread is not. So everything
// here is done inside `OnAThreadOfItsOwn`, and a test that makes a view makes it
// there too.

using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Pro7ChordEditor.AppViews.Tests;

public static class Pictures
{
    /// <summary>How many times its size a view is drawn at.</summary>
    public const int Scale = 2;

    /// <summary>The folder the pictures are written into, made if it is not there.</summary>
    public static string Folder
    {
        get
        {
            var asked = Environment.GetEnvironmentVariable("PRO7_PICTURES");
            var folder = string.IsNullOrWhiteSpace(asked) ? Path.Combine(Path.GetTempPath(), "Pro7ChordEditorPictures") : asked;
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    /// <summary>
    /// Draws a view, given this much room, into a PNG of this name in <see cref="Folder"/>,
    /// and answers with where the picture is. To be called on the thread the view was
    /// made on.
    /// </summary>
    public static string Draw(FrameworkElement view, double width, double height, string name)
    {
        view.Measure(new Size(width, height));
        view.Arrange(new Rect(0, 0, width, height));
        view.UpdateLayout();

        var bitmap = new RenderTargetBitmap((int)(width * Scale), (int)(height * Scale), 96 * Scale, 96 * Scale, PixelFormats.Pbgra32);
        bitmap.Render(view);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var path = Path.Combine(Folder, name + ".png");
        using (var file = File.Create(path))
        {
            encoder.Save(file);
        }
        return path;
    }

    /// <summary>
    /// Does this on a thread of the kind a view needs, waits for it, and hands on
    /// whatever it threw, so that a test that fails there fails here.
    /// </summary>
    public static void OnAThreadOfItsOwn(Action what)
    {
        ExceptionDispatchInfo? thrown = null;
        var thread = new Thread(() =>
        {
            try
            {
                what();
            }
            catch (Exception exception)
            {
                thrown = ExceptionDispatchInfo.Capture(exception);
            }
            finally
            {
                // A thread that has made a view has been given a queue of work to do
                // for it. Close the queue, or it is left behind when the thread ends.
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        thrown?.Throw();
    }
}
