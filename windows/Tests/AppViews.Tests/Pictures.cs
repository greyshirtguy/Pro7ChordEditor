// Drawing a view into a picture file, so that it can be looked at
// (spec/10-platforms.md, "Seeing what you have drawn"); and a thread of the kind a
// window has, for a test to work on.
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
// THE THREAD. A view, and anything in it, must be made and used on one thread of the
// kind Windows calls a single-threaded apartment, which a test's own thread is not. So
// everything here is done inside `OnAThreadOfItsOwn`, and a test that makes a view
// makes it there too.
//
// A window's thread also has a queue of work, and whatever waits for something done
// elsewhere (the model, for a library's marks) comes back by that queue to the thread
// it left. `OnAThreadOfItsOwn` given something that waits runs the queue until it is
// done, so that a test of the model is on one thread throughout, as the model is in
// the app.

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
        LayOut(view, width, height);

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
    /// Gives a view this much room and has it lay itself out in it, as it would in a
    /// window of that size. What a view puts off until its thread is next free (a
    /// list's rows, what a row is bound to) is done before this is.
    /// </summary>
    public static void LayOut(FrameworkElement view, double width, double height)
    {
        // (Twice: a view may change how it is laid out on finding how big it is.)
        for (var pass = 0; pass < 2; pass++)
        {
            view.Measure(new Size(width, height));
            view.Arrange(new Rect(0, 0, width, height));
            view.UpdateLayout();
            Settle();
        }
    }

    /// <summary>Does everything this thread's queue has waiting.</summary>
    public static void Settle() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

    /// <summary>A picture file's size in dots, and how many different colours it has.</summary>
    public static (int Width, int Height, int Colours) Read(string path)
    {
        using var file = File.OpenRead(path);
        var frame = new PngBitmapDecoder(file, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
        var dots = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
        var all = new int[dots.PixelWidth * dots.PixelHeight];
        dots.CopyPixels(all, dots.PixelWidth * 4, 0);
        return (dots.PixelWidth, dots.PixelHeight, all.Distinct().Count());
    }

    // One such thread at a time. An app has one thread for its windows, and WPF is
    // made for that: two threads that each make a view at the same moment trip over
    // each other where the view's own description is read out of the program.
    // (FOUND, 2026-10-12: the tests of three classes run side by side, and the third
    // time they were all run one of them failed there, with nothing wrong in it.)
    private static readonly Lock OneAtATime = new();

    /// <summary>
    /// Does this on a thread of the kind a view needs, waits for it, and hands on
    /// whatever it threw, so that a test that fails there fails here. Only one such
    /// thread runs at a time, whatever else the tests do side by side.
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
        lock (OneAtATime)
        {
            thread.Start();
            thread.Join();
        }
        thrown?.Throw();
    }

    /// <summary>
    /// The same for something that waits: the thread's queue is run until it is done,
    /// and whatever it waited for comes back to this thread, as it would in the app.
    /// </summary>
    public static void OnAThreadOfItsOwn(Func<Task> what) => OnAThreadOfItsOwn(() =>
    {
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        var running = new DispatcherFrame();
        var doing = what();
        doing.ContinueWith(_ => running.Continue = false, TaskScheduler.FromCurrentSynchronizationContext());
        Dispatcher.PushFrame(running);
        doing.GetAwaiter().GetResult();
    });
}
