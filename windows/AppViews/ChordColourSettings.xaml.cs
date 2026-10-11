// The code beside ChordColourSettings.xaml, which says what the view is. Here: the two
// colours the model has are shown in their wells and on their samples whenever the
// model says it has changed, and a colour chosen, or the usual one asked for, is passed
// on.
//
// Nothing here is a rule. A colour is kept by the window's model as it is written,
// "#rrggbb", and nothing is kept where the usual colour is (LibraryModel); the sheet and
// the text draw with what the model then has, at once, in the window behind this one.
//
// Asking for a colour opens a dialog of the system's, which a test cannot answer. So it
// is asked through `AskForAColour`, which a test hands its own answer.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Pro7ChordEditor.AppViews;

/// <summary>The colour of a chord in each of the two editors, each with what it looks like and a way back to the usual colour.</summary>
public partial class ChordColourSettings : UserControl
{
    private readonly LibraryModel model;

    public ChordColourSettings(LibraryModel model)
    {
        this.model = model;
        InitializeComponent();
        Look.Follow(this);
        AskForAColour = from => ColourPicker.Ask(Window.GetWindow(this), from);
        model.PropertyChanged += TheModelChanged;
        Unloaded += (_, _) => model.PropertyChanged -= TheModelChanged;
        Loaded += (_, _) =>
        {
            // (Shown again after being put away: it follows the model again.)
            model.PropertyChanged -= TheModelChanged;
            model.PropertyChanged += TheModelChanged;
            Show();
        };
        Show();
    }

    /// <summary>
    /// Asks the person for a colour, starting from the one given: with the system's own
    /// picker, over the window this is in. Nothing, if they chose none.
    /// </summary>
    public Func<Color, Color?> AskForAColour { get; set; }

    /// <summary>The colour a chord in ChordPro text comes with (spec/06, "Settings": `#ff9500` where a system has no orange of its own).</summary>
    internal static Color UsualInTheText => Look.Colours(dark: false)[Look.Chord];

    private void TheModelChanged(object? sender, PropertyChangedEventArgs e) => Show();

    // Puts the two colours the model has into the wells and onto the samples.
    private void Show()
    {
        var onTheSheet = Brush(SheetLook.Written(model.SheetChordColour) ?? SheetLook.ChordName);
        SheetColour.Background = onTheSheet;
        SheetSample.Foreground = onTheSheet;
        SheetUsual.IsEnabled = model.SheetChordColour is not null;

        // The text's usual colour is the look's, and is asked of it by its name: so
        // that it is whatever the look has, in the light and in the dark.
        if (SheetLook.Written(model.TextChordColour) is { } chosen)
        {
            var inTheText = Brush(chosen);
            TextColour.Background = inTheText;
            TextSampleOne.Foreground = inTheText;
            TextSampleTwo.Foreground = inTheText;
        }
        else
        {
            TextColour.SetResourceReference(Border.BackgroundProperty, Look.Chord);
            TextSampleOne.SetResourceReference(TextElement.ForegroundProperty, Look.Chord);
            TextSampleTwo.SetResourceReference(TextElement.ForegroundProperty, Look.Chord);
        }
        TextUsual.IsEnabled = model.TextChordColour is not null;
    }

    private static SolidColorBrush Brush(Color colour)
    {
        var brush = new SolidColorBrush(colour);
        brush.Freeze();
        return brush;
    }

    private void ChooseTheSheets(object sender, RoutedEventArgs e)
    {
        if (AskForAColour(SheetLook.Written(model.SheetChordColour) ?? SheetLook.ChordName) is { } chosen)
        {
            model.SetSheetChordColour(SheetLook.AsWritten(chosen));
        }
    }

    private void ChooseTheTexts(object sender, RoutedEventArgs e)
    {
        if (AskForAColour(SheetLook.Written(model.TextChordColour) ?? UsualInTheText) is { } chosen)
        {
            model.SetTextChordColour(SheetLook.AsWritten(chosen));
        }
    }

    private void TheSheetsUsual(object sender, RoutedEventArgs e) => model.SetSheetChordColour(null);

    private void TheTextsUsual(object sender, RoutedEventArgs e) => model.SetTextChordColour(null);
}
