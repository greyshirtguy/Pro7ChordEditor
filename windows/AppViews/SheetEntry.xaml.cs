// The code beside SheetEntry.xaml, which says what the bubble is. Here: what is offered
// is shown as its buttons, the box is opened on some text, and a click on one that is
// offered is passed on.
//
// Nothing here is a rule, and nothing here knows of the song. What the box takes, what
// is offered and what a key does are Song/ChordSheet.cs's; the sheet's view
// (ChordSheetCanvas.cs) stands between, and listens to the box.
//
// The Mac's is `SheetEntry`, in ChordSheetView.swift.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Pro7ChordEditor.AppViews;

/// <summary>The bubble a chord is typed in.</summary>
public partial class SheetEntry : UserControl
{
    private static readonly Brush ChipBrush = Frozen(SheetLook.Chip);
    private static readonly Brush AccentBrush = Frozen(SheetLook.Accent);
    private static readonly Brush WordsBrush = Frozen(SheetLook.Words);
    private static readonly Brush GroundBrush = Frozen(SheetLook.Ground);

    private Brush ink = Frozen(SheetLook.ChordName);

    public SheetEntry()
    {
        InitializeComponent();
        Panel.Background = Frozen(SheetLook.Panel);
        Panel.BorderBrush = AccentBrush;
        Box.Foreground = ink;
        Box.CaretBrush = ink;
        Box.SelectionBrush = AccentBrush;
        // (A click on the bubble itself goes no further than the bubble.)
        MouseLeftButtonDown += (_, e) => e.Handled = true;
        MouseLeftButtonUp += (_, e) => e.Handled = true;
    }

    /// <summary>The box that is typed in.</summary>
    public TextBox Field => Box;

    /// <summary>One that is offered was clicked.</summary>
    public Action<string> Chosen { get; set; } = _ => { };

    /// <summary>What is offered now, in order, and which of them is picked (or -1).</summary>
    public IReadOnlyList<string> Shown { get; private set; } = [];

    public int Picked { get; private set; } = -1;

    /// <summary>The colour of what is typed: the colour of a chord's name on the sheet.</summary>
    public Brush Ink
    {
        get => ink;
        set
        {
            ink = value;
            Box.Foreground = value;
            Box.CaretBrush = value;
        }
    }

    /// <summary>Shows what is offered, the one picked lit in orange. The bubble is as tall as that needs.</summary>
    public void Show(IReadOnlyList<string> offered, int picked)
    {
        if (Shown.SequenceEqual(offered) && picked == Picked)
        {
            return;
        }
        Shown = [.. offered];
        Picked = picked;
        Offered.Children.Clear();
        for (var n = 0; n < offered.Count; n++)
        {
            var name = offered[n];
            var chip = new Border
            {
                Height = 26,
                MinWidth = 34,
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 0, 5, 5),
                Background = n == picked ? AccentBrush : ChipBrush,
                Cursor = Cursors.Hand,
                Child = new TextBlock
                {
                    Text = name,
                    FontFamily = SheetLook.Font,
                    FontWeight = FontWeights.Bold,
                    FontSize = SheetLook.ChipSize,
                    Foreground = n == picked ? GroundBrush : WordsBrush,
                    Margin = new Thickness(8, 0, 8, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };
            chip.MouseLeftButtonDown += (_, e) =>
            {
                e.Handled = true;
                Chosen(name);
            };
            Offered.Children.Add(chip);
        }
        Offered.Visibility = offered.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// Opens the box on some text: all of it selected to be typed over, or the caret at
    /// its end. The box takes the keyboard.
    /// </summary>
    public void Begin(string text, bool selected)
    {
        Box.Text = text;
        // (If the box is being typed in already, it stays so: this is the next chord.)
        if (!Box.IsKeyboardFocused)
        {
            Box.Focus();
        }
        if (selected)
        {
            Box.SelectAll();
        }
        else
        {
            Box.Select(text.Length, 0);
        }
    }

    /// <summary>Puts other text in the box, with the caret at a place or at its end.</summary>
    public void Set(string text, int? caret = null)
    {
        Box.Text = text;
        Box.Select(Math.Min(caret ?? text.Length, text.Length), 0);
    }

    private static SolidColorBrush Frozen(Color colour)
    {
        var brush = new SolidColorBrush(colour);
        brush.Freeze();
        return brush;
    }
}
