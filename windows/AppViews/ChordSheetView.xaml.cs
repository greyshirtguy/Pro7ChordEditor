// The code beside ChordSheetView.xaml, which says what the view is. Here: what the model
// has of the sheet is handed to the sheet's own element whenever the model says it has
// changed, the strip is given the chords of the song's key, and a click in the strip is
// passed on.
//
// Nothing here is a rule. What a click on one of the key's chords does is
// Song/ChordSheet.cs's, by way of the model; whether there is room in the strip for what
// is said of a song with no key is the one sum here, in a function of its own that a
// test can ask.
//
// ONE OF THE KEY'S CHORDS IS ALSO DRAGGED OUT OF THE STRIP onto a word (spec/03, rules
// 46a to 46h), and this is the near end of that: "One of the key's chords picked up",
// below. A press on a button that is let go is the button's click, as it always was.
// A press pulled 6 away picks the chord up, the once: a chord that was being typed is
// kept (the model's), and the system is handed the chord's name to carry
// (`DragDrop.DoDragDrop`), which does not come back until the chord is let go or Esc
// is pressed. The far end is the sheet's (ChordSheetCanvas.cs). What a press and a pull
// come to is in two methods a test can call (`ChordPressed`, `ChordPulled`), and what
// is handed to the system is behind `Carry`, which a test puts its own in the place of:
// **the system's own dragging is not run by any test**, since it reads the real mouse.
//
// While the other editor is shown the sheet's element is left as it was: nothing of it
// is laid out or drawn for a song that is being typed into as text. When the sheet is
// come back to it is brought up to date, and is where it was scrolled to.

using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Pro7ChordEditor.AppViews;

/// <summary>The chord sheet with its strip, as the song's pane holds it.</summary>
public partial class ChordSheetView : UserControl
{
    private static readonly Brush CaptionBrush = Frozen(SheetLook.Caption);
    private static readonly Brush WordsBrush = Frozen(SheetLook.Words);

    private LibraryModel? model;
    private IReadOnlyList<string> listed = [];

    public ChordSheetView()
    {
        InitializeComponent();
        // (For its scroll bars: the sheet is dark whatever the system is.)
        Look.Wear(Scroll, dark: true);
        Sheet.Scroll = Scroll;
        Scroll.ScrollChanged += (_, e) =>
        {
            if (e.ViewportWidthChange != 0 || e.ViewportHeightChange != 0)
            {
                Sheet.Fit();
            }
        };
        Strip.SizeChanged += (_, _) => LayOutTheStrip();
        DataContextChanged += (_, _) => Follow(DataContext as LibraryModel);
    }

    /// <summary>The sheet itself: for the pane to give the keyboard to, and for a test to point at.</summary>
    internal ChordSheetCanvas Canvas => Sheet;

    /// <summary>The buttons of the key's chords, in order: for a test to click.</summary>
    internal IReadOnlyList<Button> ChordsOfTheKey => [.. Chords.Children.OfType<Button>()];

    /// <summary>
    /// Whether the strip has room to say that the song names no key, beside the key's
    /// chords and the two buttons that zoom: nothing in the strip is ever squeezed.
    /// </summary>
    /// <param name="room">How wide the strip is inside its own edges.</param>
    internal static bool HasRoomToSay(double chords, double said, double zoom, double room) => chords + 12 + said + 8 + zoom <= room;

    private static SolidColorBrush Frozen(Color colour)
    {
        var brush = new SolidColorBrush(colour);
        brush.Freeze();
        return brush;
    }

    private void Follow(LibraryModel? now)
    {
        if (model is not null)
        {
            model.PropertyChanged -= TheModelChanged;
        }
        model = now;
        Sheet.Asked = model is null ? ChordSheetCanvas.Asks.Nothing : ChordSheetCanvas.Asks.Of(model);
        if (model is not null)
        {
            model.PropertyChanged += TheModelChanged;
        }
        Show();
    }

    private void TheModelChanged(object? sender, PropertyChangedEventArgs e) => Show();

    // Puts what the model has into the strip and the sheet.
    private void Show()
    {
        if (model is null || model.EditorShown != LibraryModel.Editor.Sheet)
        {
            return;
        }
        var sheet = model.Sheet;
        Sheet.ChordColour = SheetLook.Written(model.SheetChordColour) ?? SheetLook.ChordName;
        Sheet.Show(sheet, model.SheetZoom);
        Smaller.IsEnabled = model.CanZoomSheetOut;
        Larger.IsEnabled = model.CanZoomSheetIn;

        var inKey = sheet?.InKey ?? [];
        if (!listed.SequenceEqual(inKey, StringComparer.Ordinal))
        {
            listed = [.. inKey];
            Chords.Children.Clear();
            for (var n = 0; n < listed.Count; n++)
            {
                Chords.Children.Add(OfTheKey(n + 1, listed[n]));
            }
            ChordsSeen.ScrollToHorizontalOffset(0);
        }
        LayOutTheStrip();
    }

    // One of the key's chords, as a button: its number, quiet, and its name.
    private Button OfTheKey(int number, string name)
    {
        var numbered = number.ToString(CultureInfo.InvariantCulture);
        var words = new StackPanel { Orientation = Orientation.Horizontal };
        words.Children.Add(new TextBlock { Text = numbered, FontSize = 11, Foreground = CaptionBrush, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) });
        words.Children.Add(new TextBlock { Text = name, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = WordsBrush, VerticalAlignment = VerticalAlignment.Center });
        var button = new Button
        {
            Style = (Style)Resources["OfTheKey"],
            Content = words,
            ToolTip = Says(name, number),
        };
        System.Windows.Automation.AutomationProperties.SetName(button, numbered + " " + name);
        button.Click += (_, _) => model?.PutOfKey(number);
        // The button's own press and click are left as they are. These only watch for
        // the press being pulled away, which is the chord picked up.
        button.PreviewMouseLeftButtonDown += (_, e) => ChordPressed(button, e.GetPosition(button));
        button.PreviewMouseMove += (_, e) => ChordPulled(button, name, e.GetPosition(button), e.LeftButton == MouseButtonState.Pressed);
        button.PreviewMouseLeftButtonUp += (_, _) => pressedOn = null;
        return button;
    }

    /// <summary>What one of the key's chords says of itself when it is pointed at (spec/03, rule 46h).</summary>
    internal static string Says(string name, int number) =>
        "Puts " + name + " on the spot. The key " + number.ToString(CultureInfo.InvariantCulture) + " does the same. Or drag it onto a word.";

    // ---- One of the key's chords picked up (spec/03, rules 46a and 46e)

    // The button of the strip that is pressed, and where on it: until it is let go, or
    // pulled far enough to be its chord picked up.
    private (Button Button, Point At)? pressedOn;

    /// <summary>How faint a button is while its chord is out being carried.</summary>
    internal const double FaintWhileCarried = 0.45;

    /// <summary>
    /// What carries a chord once it is picked up: the system's own dragging, which comes
    /// back when the chord is let go or the carrying is given up. (A test puts its own
    /// here. The system's reads the real mouse.)
    /// </summary>
    internal Action<Button, string> Carry { get; set; } =
        (button, name) => DragDrop.DoDragDrop(button, ChordSheetCanvas.Carrying(name), DragDropEffects.Copy);

    /// <summary>The mouse's button is pressed on one of the strip's.</summary>
    internal void ChordPressed(Button button, Point at) => pressedOn = (button, at);

    /// <summary>
    /// The pointer has moved over a button of the strip. If that button is pressed and
    /// the pointer has gone 6 from where it was pressed, its chord is picked up, and
    /// carried until it is let go: answers whether it was.
    /// </summary>
    internal bool ChordPulled(Button button, string name, Point to, bool buttonIsDown)
    {
        if (!buttonIsDown || pressedOn is not { } pressed || !ReferenceEquals(pressed.Button, button))
        {
            // (Let go somewhere this was not told of, or another button's.)
            pressedOn = buttonIsDown ? pressedOn : null;
            return false;
        }
        if (Math.Abs(to.X - pressed.At.X) + Math.Abs(to.Y - pressed.At.Y) < 6)
        {
            return false;
        }
        // Picked up, the once: the press is no longer one that a click can come of.
        pressedOn = null;
        button.ReleaseMouseCapture();
        // A chord that was being typed is kept first, and its bubble closed (rule 46e).
        model?.LiftOfKey();
        var was = button.Opacity;
        button.Opacity = FaintWhileCarried;
        try
        {
            Carry(button, name);
        }
        finally
        {
            // Wherever it ended, and whether or not the sheet was told: the button is
            // itself again, and nothing is left looking as if a chord were still over
            // the sheet.
            button.Opacity = was;
            model?.CarriedOfKeyEnded();
        }
        return true;
    }

    // What is said of a song with no key is there only where there is room for it.
    private void LayOutTheStrip()
    {
        var noKey = model?.Shown is { Key.Length: 0 } && model.EditorShown == LibraryModel.Editor.Sheet;
        var seen = false;
        if (noKey && Strip.ActualWidth > 0)
        {
            Chords.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Zoom.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            // (Measured as it would be if it were there. Put away, it measures as
            // nothing, and nothing always has room: FOUND at M8, when a test made the
            // pane narrower a little at a time and the words came and went by turns.)
            NoKey.Visibility = Visibility.Visible;
            // With which key its chords look like, if that is known and there is room to
            // say it (spec/08-repair.md, check 3); else the page's words alone; else
            // nothing.
            foreach (var sentence in new[] { model!.NoKeySentence, LibraryModel.NoKey }.Distinct())
            {
                NoKey.Text = sentence;
                NoKey.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                seen = HasRoomToSay(
                    Chords.DesiredSize.Width,
                    NoKey.DesiredSize.Width - NoKey.Margin.Left,
                    Zoom.DesiredSize.Width - Zoom.Margin.Left,
                    Strip.ActualWidth - Strip.Padding.Left - Strip.Padding.Right);
                if (seen)
                {
                    break;
                }
            }
        }
        NoKey.Visibility = seen ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SmallerWasClicked(object sender, RoutedEventArgs e) => model?.ZoomSheet(-1);

    private void LargerWasClicked(object sender, RoutedEventArgs e) => model?.ZoomSheet(1);

    // The key's chords scroll sideways where the strip is too narrow for them, and the
    // wheel is what scrolls them: there is no bar.
    private void TheWheelTurnedOverTheChords(object sender, MouseWheelEventArgs e)
    {
        if (ChordsSeen.ScrollableWidth > 0)
        {
            ChordsSeen.ScrollToHorizontalOffset(ChordsSeen.HorizontalOffset - (e.Delta / 3.0));
            e.Handled = true;
        }
    }
}
