// How the window looks: the system's own look for lists, buttons and fields, dark and
// light as the system is (spec/10-platforms.md, "Looks"), and the few colours that are
// this app's own.
//
// THE SYSTEM'S LOOK. WPF's usual look is that of a Windows two versions back, and is
// light whatever the system is. Since .NET 9 it also has the look of Windows 11, in a
// light form and a dark one, which it calls Fluent, and which is part of .NET and no
// dependency of ours. Each form is a dictionary of styles and colours.
//
//   - The app is told to go by the system (`GiveTheSystemsLook`): WPF then puts the
//     dark or the light one on, changes it when the system changes while the app runs,
//     and makes the window's own title bar dark or light to match. The one switch for
//     that, `ThemeMode`, .NET still marks as on trial: it may be spelt otherwise in a
//     later .NET, and the compiler says so unless told it is known (the line that
//     says it is, is beside the one use). If it ever goes, nothing else here hangs on
//     it: what follows is done with the dictionaries themselves.
//   - A test that draws a picture puts the dark or the light one on a view by itself
//     (`Wear`), so that a picture is the same whatever the PC it is drawn on is set to.
//
// (The dictionary named Fluent.xaml, with neither word in its name, is not either of
// them and has no colours in it: FOUND, 2026-10-12, by asking it for one.)
//
// OUR OWN COLOURS. The two lists at the left have grounds of different greys, as in
// ProPresenter (spec/06-library.md, rule 6b), and the bar between them is darker
// still. With the system dark they are ProPresenter's own three; with it light, their
// like. Which the system is, a view asks of the look itself: it follows the colour the
// look gives a window's ground (`Follow`), and whenever that changes it is dressed again
// in the greys that go with it. So nothing here reads the system's settings, and a view
// in a picture and a view in the window come by their colours the same way.
//
// A colour is asked for in a view by its name here, as a resource that may change:
//
//     Background="{DynamicResource Pro7.Libraries}"
//
// and the system's own by the names its dictionary gives them, of which the views use
// those listed in `OfTheSystem`. A name the dictionary has not got draws nothing and
// says nothing; so a test asks both dictionaries for every one (WindowListsTests).

using System.Windows;
using System.Windows.Media;

namespace Pro7ChordEditor.AppViews;

public static class Look
{
    /// <summary>The ground of the libraries' list, the lighter of the two.</summary>
    public const string Libraries = "Pro7.Libraries";

    /// <summary>The ground of the songs' list.</summary>
    public const string Songs = "Pro7.Songs";

    /// <summary>The bar between the two lists, and the edge between them and the song.</summary>
    public const string Bar = "Pro7.Bar";

    /// <summary>The ground of the band that says ProPresenter is running.</summary>
    public const string Band = "Pro7.Band";

    /// <summary>A chord in a line of ChordPro (spec/06, "Settings": `#ff9500` where a system has no orange of its own).</summary>
    public const string Chord = "Pro7.Chord";

    /// <summary>The names of the system's own colours that the views ask for.</summary>
    public static readonly IReadOnlyList<string> OfTheSystem =
    [
        "ApplicationBackgroundBrush", "TextFillColorPrimaryBrush", "TextFillColorSecondaryBrush", "TextFillColorTertiaryBrush",
        "DividerStrokeColorDefaultBrush",
    ];

    /// <summary>The name of the colour the look gives a window's ground, by which a view tells dark from light.</summary>
    public const string Ground = "ApplicationBackgroundColor";

    private static readonly DependencyProperty GroundProperty = DependencyProperty.RegisterAttached(
        "Ground",
        typeof(Color),
        typeof(Look),
        new PropertyMetadata(Colors.White, (view, change) => Dress((FrameworkElement)view, IsDark((Color)change.NewValue))));

    /// <summary>Where .NET keeps the look: the dark one, or the light one.</summary>
    public static Uri Dictionary(bool dark)
    {
        // An address that begins "pack:" is of a kind .NET does not know until WPF has
        // taught it, which WPF does the first time anything is asked of an app. A test
        // may ask for the look before anything else has, so it is asked here.
        if (!UriParser.IsKnownScheme("pack"))
        {
            _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
            _ = Application.Current;
        }
        return new Uri("pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent." + (dark ? "Dark" : "Light") + ".xaml", UriKind.Absolute);
    }

    /// <summary>Gives the whole app the system's look, dark or light as the system is, and has it go on following the system.</summary>
    public static void GiveTheSystemsLook(Application app)
    {
        // (On trial in .NET, and known to be: see the head of this file.)
#pragma warning disable WPF0001
        app.ThemeMode = ThemeMode.System;
#pragma warning restore WPF0001
    }

    /// <summary>Puts the dark look or the light one on a view by itself, whatever the system is: for a picture.</summary>
    public static void Wear(FrameworkElement view, bool dark) =>
        view.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = Dictionary(dark) });

    /// <summary>
    /// Has a view follow the look it is under: it is dressed in this app's own colours
    /// now, and again whenever the look turns from light to dark or back.
    /// </summary>
    public static void Follow(FrameworkElement view)
    {
        // (Light, until the look says otherwise: with no look at all it stays so.)
        Dress(view, dark: false);
        view.SetResourceReference(GroundProperty, Ground);
    }

    /// <summary>Whether a ground of this colour is a dark one: one on which words are written in white.</summary>
    internal static bool IsDark(Color ground) => (0.299 * ground.R) + (0.587 * ground.G) + (0.114 * ground.B) < 128;

    /// <summary>This app's own colours, for a dark look or a light one, by their names.</summary>
    internal static IReadOnlyDictionary<string, Color> Colours(bool dark) => new Dictionary<string, Color>(StringComparer.Ordinal)
    {
        // ProPresenter's own three in the dark, read off a picture of its window, and
        // their like in the light (spec/06, rule 6b).
        [Libraries] = dark ? Color.FromRgb(0x58, 0x58, 0x58) : Color.FromRgb(0xEC, 0xEC, 0xEC),
        [Songs] = dark ? Color.FromRgb(0x3E, 0x3E, 0x3E) : Color.FromRgb(0xD5, 0xD5, 0xD5),
        [Bar] = dark ? Color.FromRgb(0x2F, 0x2F, 0x2F) : Color.FromRgb(0xBD, 0xBD, 0xBD),
        // A yellow that words can be read on, in white or in black.
        [Band] = dark ? Color.FromRgb(0x6B, 0x57, 0x00) : Color.FromRgb(0xFF, 0xE7, 0x8A),
        // (The same on both: it is the page's, for a system with no orange of its own.)
        [Chord] = Color.FromRgb(0xFF, 0x95, 0x00),
    };

    private static void Dress(FrameworkElement view, bool dark)
    {
        foreach (var (name, colour) in Colours(dark))
        {
            var brush = new SolidColorBrush(colour);
            brush.Freeze();
            view.Resources[name] = brush;
        }
    }
}
