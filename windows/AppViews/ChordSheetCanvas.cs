// The chord sheet, drawn: one element that draws every slide of the song itself, follows
// the pointer, and takes the keys (spec/03-chord-sheet.md; spec/10-platforms.md: "the
// sheet is one custom-drawn element ... and does its own hit testing. Not a
// RichTextBox").
//
// Nothing here is a rule. What a key or a click does to the song is Song/ChordSheet.cs's,
// with no window in it; where the slides are and what is under the pointer is
// SheetGrid.cs's; which key a key press is, is SheetKeys.cs's. This element tells the
// sheet what was done (the pointer is over this place, this key was pressed) and draws
// what it then has. After each thing done it calls `Sync`, which lays the slides out
// again if the song's rows have changed, opens or closes the bubble a chord is typed
// in, and draws.
//
// It is drawn in two layers. Under: the slides, their words and their chords, which
// change when the song does. Over: the ring round the slide that is picked, and the
// spot, which follow the pointer. So the pointer moving draws only the little that
// moves with it, and the spot keeps up with it.
//
// What Windows has its own way (spec/10-platforms.md): Ctrl where the Mac has Command;
// Ctrl held as a chord is let go copies it; Delete or Backspace; Enter or F2 opens the
// bubble on what is at the spot; Ctrl with the mouse's wheel zooms, where a Mac has two
// fingers on a trackpad.
//
// The keyboard. The element has the keyboard while no chord is being typed, and the box
// in the bubble has it while one is. It takes the keyboard when it is clicked, and also
// when the pointer moves over it (unless a box of text elsewhere is being typed in): the
// spot follows the pointer, and the keys act on the spot, so the keys are to go where
// the pointer is, without a click to say so first (rule 23a).
//
// A test cannot press a button of a mouse, nor a key. So everything the pointer and the
// keys do comes in by methods a test can call with a place and what is held
// (`PointerMoved`, `Pressed`, `Dragged`, `Released`, `KeyPressed`, `KeyInTheBox`), and
// the handlers of the real pointer and keys only call those.
//
// The Mac's is `SheetCanvas`, in ChordSheetView.swift.

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Song;

namespace Pro7ChordEditor.AppViews;

/// <summary>The chord sheet: every slide of the song, drawn, with its spot.</summary>
public sealed class ChordSheetCanvas : FrameworkElement
{
    private static readonly Brush GroundBrush = Frozen(SheetLook.Ground);
    private static readonly Brush CardBrush = Frozen(SheetLook.Card);
    private static readonly Brush RuleBrush = Frozen(SheetLook.Rule);
    private static readonly Brush BubbleBrush = Frozen(SheetLook.Bubble);
    private static readonly Brush LitBrush = Frozen(SheetLook.Lit);
    private static readonly Brush EdgeBrush = Frozen(SheetLook.BubbleEdge);
    private static readonly Brush AccentBrush = Frozen(SheetLook.Accent);
    private static readonly Brush FaintAccent = Frozen(SheetLook.Accent, 0.75);

    private readonly DrawingVisual under = new();
    private readonly DrawingVisual over = new();
    private Point entryAt;

    // The typing the bubble is open for: `ChordSheet.TypingsBegun` as it was.
    private int? typingShown;

    // The button is down: on which row, on which chord's place if on a bubble, where,
    // and whether the pointer has since moved far enough to be carrying it.
    private (int Row, int? ChordAt, Point Point, bool Moved)? pressed;

    // The keyboard is being moved by this element itself, to the box or back from it:
    // that is not the person leaving the box for something else.
    private bool movingTheKeys;

    // The text in the box is being set from here, and was not typed.
    private bool settingTheBox;
    private bool layingOut;
    private uint? copiedWhen;
    private Color chordColour = SheetLook.ChordName;

    public ChordSheetCanvas()
    {
        AddVisualChild(under);
        AddVisualChild(over);
        AddVisualChild(Entry);
        AddLogicalChild(Entry);
        Entry.Visibility = Visibility.Collapsed;
        Entry.Chosen = name =>
        {
            Sheet?.FinishTyping(name, onward: false);
            Sync();
        };
        Entry.Field.TextChanged += (_, _) => TheBoxChanged();
        Entry.Field.PreviewKeyDown += (_, e) =>
        {
            if (KeyInTheBox(e.Key == Key.System ? e.SystemKey : e.Key, Keyboard.Modifiers))
            {
                e.Handled = true;
            }
        };
        Entry.Field.LostKeyboardFocus += (_, _) => TheBoxWasLeft();
        Focusable = true;
        FocusVisualStyle = null;
        ClipToBounds = true;
        AutomationProperties.SetName(this, "The song's chord sheet");

        // Undo, Redo, Copy and Paste in the Edit menu reach whatever has the keyboard as
        // these commands, and the sheet answers them as it does their keys.
        Bind(ApplicationCommands.Undo, SheetKeyKind.Undo, () => Asked.CanUndo());
        Bind(ApplicationCommands.Redo, SheetKeyKind.Redo, () => Asked.CanRedo());
        Bind(ApplicationCommands.Copy, SheetKeyKind.Copy, () => Sheet is { } sheet && sheet.Usable(sheet.Spot.Row));
        Bind(ApplicationCommands.Paste, SheetKeyKind.Paste, () => Sheet is { Copied: not null } sheet && sheet.Usable(sheet.Spot.Row));
    }

    /// <summary>What the element asks of the window's model, which has the song.</summary>
    /// <param name="Zoom">
    /// The words a step larger or smaller, or their usual size: answers with how many
    /// times their usual size they then are.
    /// </param>
    public sealed record Asks(Action<bool> Copy, Action Paste, Action Undo, Action Redo, Func<bool> CanUndo, Func<bool> CanRedo, Func<int, double?> Zoom)
    {
        public static Asks Nothing { get; } = new(_ => { }, () => { }, () => { }, () => { }, () => false, () => false, _ => null);

        /// <summary>As the window wires them.</summary>
        public static Asks Of(LibraryModel model) =>
            new(model.CopyChords, model.PasteChords, model.Undo, model.Redo, () => model.CanUndo, () => model.CanRedo, direction => model.ZoomSheet(direction));
    }

    public ChordSheet? Sheet { get; private set; }

    public Asks Asked { get; set; } = Asks.Nothing;

    public SheetMetrics Metrics { get; private set; } = new(1);

    /// <summary>The slides as they are laid out; nothing while no song is shown.</summary>
    public SheetGrid? Grid { get; private set; }

    /// <summary>The bubble a chord is typed in.</summary>
    public SheetEntry Entry { get; } = new();

    /// <summary>What the sheet is seen through, which scrolls it. Given by the view that holds both.</summary>
    public ScrollViewer? Scroll { get; set; }

    /// <summary>
    /// A number that changes whenever anything is copied, anywhere on this PC: the
    /// system's count of changes to its clipboard. While it is what it was when a line's
    /// chords were last copied here (which are not put on that clipboard), a line's
    /// chords are the last thing that was copied.
    /// </summary>
    public Func<uint> WhatWasCopiedLast { get; set; } = GetClipboardSequenceNumber;

    /// <summary>The colour of a chord's name: the usual one, or the one chosen in the settings.</summary>
    public Color ChordColour
    {
        get => chordColour;
        set
        {
            if (value == chordColour)
            {
                return;
            }
            chordColour = value;
            Metrics.ChordBrush = Frozen(value);
            Entry.Ink = Frozen(value);
            Draw();
        }
    }

    protected override int VisualChildrenCount => 3;

    protected override Visual GetVisualChild(int index) => index switch
    {
        0 => under,
        1 => over,
        _ => Entry,
    };

    [DllImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint GetClipboardSequenceNumber();

    private static SolidColorBrush Frozen(Color colour, double alpha = 1)
    {
        var brush = new SolidColorBrush(colour) { Opacity = alpha };
        brush.Freeze();
        return brush;
    }

    private void Bind(RoutedUICommand command, SheetKeyKind kind, Func<bool> can) => CommandBindings.Add(new CommandBinding(
        command,
        (_, e) =>
        {
            KeyPressed(new SheetKey(kind));
            e.Handled = true;
        },
        (_, e) =>
        {
            e.CanExecute = can();
            e.Handled = true;
        }));

    // ---- What is shown

    /// <summary>
    /// Shows a song's sheet: another song's from the top, the same one's where it was.
    /// With a size, at that zoom.
    /// </summary>
    public void Show(ChordSheet? shown, double? scale = null)
    {
        if (scale is { } wanted && wanted != Metrics.Scale)
        {
            SetScale(wanted);
        }
        if (!ReferenceEquals(shown, Sheet))
        {
            Sheet = shown;
            if (shown is not null)
            {
                shown.Reveal = RevealSpot;
            }
            Grid = null;
            typingShown = null;
            pressed = null;
            Scroll?.ScrollToHome();
            Scroll?.ScrollToHorizontalOffset(0);
        }
        Sync();
    }

    // ---- Laying out

    // How wide the sheet is seen to be: what the slides are fitted across.
    private double SeenWidth => Scroll is { ViewportWidth: > 0 } scroll ? scroll.ViewportWidth : ActualWidth > 0 ? ActualWidth : 800;

    // What of the sheet is seen.
    private Rect Seen => Scroll is { ViewportWidth: > 0 } scroll
        ? new Rect(scroll.HorizontalOffset, scroll.VerticalOffset, scroll.ViewportWidth, scroll.ViewportHeight)
        : new Rect(0, 0, ActualWidth, ActualHeight);

    private void LayOut()
    {
        if (Sheet is not { } sheet)
        {
            Grid = null;
            InvalidateMeasure();
            return;
        }
        Metrics.Hold(sheet.Rows);
        Grid = SheetGrid.LayOut(sheet, Metrics, SeenWidth);
        InvalidateMeasure();
    }

    // Lays out again, for another width or another zoom, with the slide that was at the
    // top of what is seen still there.
    private void LayOutWhereItWas()
    {
        if (layingOut)
        {
            return;
        }
        layingOut = true;
        try
        {
            var seen = Seen;
            var atTop = Grid?.Cells.FirstOrDefault(cell => cell.Frame.Bottom > seen.Top + 1);
            var within = atTop is null ? 0 : atTop.Frame.Top - seen.Top;
            LayOut();
            if (atTop is not null && Grid?.Cells.FirstOrDefault(cell => cell.FirstRow == atTop.FirstRow) is { } now)
            {
                Scroll?.ScrollToVerticalOffset(Math.Max(0, now.Frame.Top - within));
                // (Scrolled now, and not when the window next lays itself out: so that
                // a second step of zoom straight after this one starts from where the
                // sheet is, and not from where it was. Asked for in the middle of a
                // laying out, this does nothing, and the laying out does it.)
                Scroll?.UpdateLayout();
            }
            PlaceEntry();
            Draw();
        }
        finally
        {
            layingOut = false;
        }
    }

    /// <summary>
    /// The sheet is seen through something of another width than the slides were laid out
    /// for: they are laid out again, fewer or more to a row.
    /// </summary>
    public void Fit()
    {
        if (!layingOut && Sheet is not null && Grid is { } grid && Math.Abs(SeenWidth - grid.SeenWidth) > 0.5)
        {
            LayOutWhereItWas();
        }
        InvalidateMeasure();
    }

    /// <summary>Draws everything so many times its usual size.</summary>
    public void SetScale(double scale)
    {
        if (scale == Metrics.Scale)
        {
            return;
        }
        Metrics = new SheetMetrics(scale, VisualTreeHelper.GetDpi(this).PixelsPerDip) { ChordBrush = Frozen(chordColour) };
        if (Sheet is not null)
        {
            LayOutWhereItWas();
        }
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        // (Letters are measured for the screen they are drawn on.)
        Metrics = new SheetMetrics(Metrics.Scale, newDpi.PixelsPerDip) { ChordBrush = Frozen(chordColour) };
        if (Sheet is not null)
        {
            LayOutWhereItWas();
        }
    }

    // The element is as big as the song, and never smaller than what it is seen through:
    // a click anywhere in what is seen is a click on the sheet.
    protected override Size MeasureOverride(Size availableSize)
    {
        Entry.Measure(new Size(SheetLook.EntryWidth, double.PositiveInfinity));
        var content = Grid?.Content ?? default;
        var seen = Scroll is { ViewportWidth: > 0 } scroll ? new Size(scroll.ViewportWidth, scroll.ViewportHeight) : default;
        return new Size(Math.Max(content.Width, seen.Width), Math.Max(content.Height, seen.Height));
    }

    // The bubble a chord is typed in is put at the place the chord is for. It grows
    // upwards from where the chord will stand, so that the word being given the chord,
    // and the rest of its line, stay in sight; and it is kept within what is seen.
    //
    // It is placed here, as the sheet is laid out, and not when what is offered in it
    // changes: how tall it then is, is not known until it has been laid out again.
    // (FOUND in the first picture of it: placed at once, it was placed for the height
    // it had before the key.)
    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = Entry.DesiredSize;
        if (Sheet is { Typing: true } sheet && Grid is { } grid && grid.IsLaid(sheet.TypingAt.Row))
        {
            var x = grid.XOf(sheet.TypingAt);
            var y = grid.Lines[sheet.TypingAt.Row].ChordTop + Metrics.BubbleHeight + 3 - size.Height;
            var seen = Seen;
            // (Before the sheet has been given its size there is nothing to keep it within.)
            if (seen.Width > size.Width)
            {
                x = Math.Max(seen.Left + 4, Math.Min(x, seen.Right - size.Width - 12));
                y = Math.Max(seen.Top + 4, y);
            }
            entryAt = new Point(x, Math.Max(4, y));
        }
        Entry.Arrange(new Rect(entryAt, size));
        return finalSize;
    }

    /// <summary>
    /// After anything was done: the slides laid out again if the song's rows changed, the
    /// bubble for typing opened, moved or closed, and everything drawn.
    /// </summary>
    public void Sync()
    {
        if (Sheet is not { } sheet)
        {
            LayOut();
            Entry.Visibility = Visibility.Collapsed;
            Draw();
            return;
        }
        if (Grid is null || !ReferenceEquals(sheet.Rows, Grid.Rows))
        {
            LayOut();
        }
        if (sheet.Typing && Grid!.IsLaid(sheet.TypingAt.Row))
        {
            Entry.Show(sheet.Offered, sheet.Entry.Pick);
            if (typingShown != sheet.TypingsBegun)
            {
                typingShown = sheet.TypingsBegun;
                Entry.Visibility = Visibility.Visible;
                movingTheKeys = true;
                settingTheBox = true;
                Entry.Begin(sheet.Entry.Text, sheet.Entry.Selected);
                settingTheBox = false;
                movingTheKeys = false;
            }
            else if (!string.Equals(Entry.Field.Text, sheet.Entry.Text, StringComparison.Ordinal))
            {
                settingTheBox = true;
                Entry.Set(sheet.Entry.Text);
                settingTheBox = false;
            }
            PlaceEntry();
        }
        else if (typingShown is not null)
        {
            typingShown = null;
            // The keyboard comes back to the sheet, if the box had it. (Asked before the
            // bubble is put away: a box that is put away gives the keyboard up.)
            var hadTheKeys = Entry.Field.IsKeyboardFocusWithin;
            movingTheKeys = true;
            Entry.Visibility = Visibility.Collapsed;
            if (hadTheKeys)
            {
                Focus();
            }
            movingTheKeys = false;
        }
        Draw();
    }

    // The bubble is to be put where it now belongs: when the sheet is next laid out,
    // which follows at once (ArrangeOverride).
    private void PlaceEntry()
    {
        InvalidateMeasure();
        InvalidateArrange();
    }

    /// <summary>Where the bubble a chord is typed in stands on the sheet, and how big it is.</summary>
    internal Rect EntryFrame => new(entryAt, Entry.DesiredSize);

    // Brings the spot into view, when the keys have moved it: its slide's whole frame if
    // that can be seen whole, and the place itself whatever else.
    private void RevealSpot()
    {
        if (Sheet is not { } sheet)
        {
            return;
        }
        if (Grid is null || !ReferenceEquals(sheet.Rows, Grid.Rows))
        {
            LayOut();
        }
        if (Grid is not { } grid || !grid.IsLaid(sheet.Spot.Row) || Scroll is null)
        {
            return;
        }
        var item = grid.Lines[sheet.Spot.Row];
        var frame = grid.Cells[item.Cell].Frame;
        frame.Inflate(SheetLook.Around, SheetLook.Around);
        var seen = Seen;
        if (frame.Height <= seen.Height && frame.Width <= seen.Width)
        {
            ScrollToSee(frame);
        }
        ScrollToSee(new Rect(grid.XOf(sheet.Spot) - 40, item.ChordTop - 10, Math.Min(160, Math.Max(80, seen.Width - 20)), Metrics.LineHeight + 20));
    }

    // Scrolls as little as brings this into what is seen.
    private void ScrollToSee(Rect wanted)
    {
        if (Scroll is not { ViewportWidth: > 0 } scroll)
        {
            return;
        }
        var seen = Seen;
        if (wanted.Top < seen.Top)
        {
            scroll.ScrollToVerticalOffset(Math.Max(0, wanted.Top));
        }
        else if (wanted.Bottom > seen.Bottom)
        {
            scroll.ScrollToVerticalOffset(Math.Max(0, Math.Min(wanted.Top, wanted.Bottom - seen.Height)));
        }
        if (wanted.Left < seen.Left)
        {
            scroll.ScrollToHorizontalOffset(Math.Max(0, wanted.Left));
        }
        else if (wanted.Right > seen.Right)
        {
            scroll.ScrollToHorizontalOffset(Math.Max(0, Math.Min(wanted.Left, wanted.Right - seen.Width)));
        }
        // (So that the next thing asked of what is seen is asked of where it now is.)
        scroll.UpdateLayout();
    }

    // ---- The pointer

    /// <summary>The pointer has moved, with no button down.</summary>
    public void PointerMoved(Point point, bool shift)
    {
        if (Sheet is not { } sheet || Grid is not { } grid)
        {
            return;
        }
        TakeTheKeysIfNobodyIsTyping();
        if (grid.PlaceAt(point, shift) is { } place)
        {
            sheet.PointerOver(place.Row, place.At);
            Cursor = place.OnChord ? Cursors.Hand : Cursors.Arrow;
        }
        else
        {
            Cursor = Cursors.Arrow;
        }
        DrawOver();
    }

    /// <summary>The button is pressed.</summary>
    public void Pressed(Point point, bool shift)
    {
        if (Sheet is not { } sheet || Grid is not { } grid)
        {
            return;
        }
        if (!sheet.Typing)
        {
            Focus();
        }
        if (grid.PlaceAt(point, shift) is { } place)
        {
            sheet.SetSpot(place.Row, place.At);
            pressed = (place.Row, place.OnChord ? place.At : null, point, false);
        }
        else
        {
            pressed = (-1, null, point, false);
        }
        DrawOver();
    }

    /// <summary>The pointer has moved with the button down.</summary>
    public void Dragged(Point point, bool shift)
    {
        if (Sheet is not { } sheet || Grid is not { } grid || pressed is not { ChordAt: { } chordAt } down)
        {
            return;
        }
        // A chord is not carried off while another is being typed: letting go of it then
        // is a click on it, which keeps the one and opens this one.
        if (sheet.Typing)
        {
            return;
        }
        // It is carried once the pointer has gone 6 with the button down.
        if (!down.Moved && Math.Abs(point.X - down.Point.X) + Math.Abs(point.Y - down.Point.Y) < 6)
        {
            return;
        }
        pressed = down with { Moved = true };
        var lifting = sheet.Dragged is null;
        if (lifting)
        {
            sheet.Lift(down.Row, chordAt);
        }
        if (grid.PlaceCarriedTo(point, shift) is { } place)
        {
            sheet.Carry(place.Row, place.At);
        }
        Cursor = Cursors.SizeAll;
        if (lifting)
        {
            Draw();
        }
        else
        {
            DrawOver();
        }
    }

    /// <summary>The button is let go. <paramref name="copy"/>: the key that copies as it drags is held.</summary>
    public void Released(Point point, bool shift, bool copy)
    {
        if (Sheet is not { } sheet || Grid is not { } grid || pressed is not { } down)
        {
            return;
        }
        pressed = null;
        if (sheet.Dragged is not null)
        {
            sheet.Drop(copy);
            Cursor = Cursors.Arrow;
        }
        else if (down.ChordAt is { } chordAt)
        {
            if (!down.Moved)
            {
                sheet.Pick(down.Row, chordAt);
            }
        }
        else if (down.Row >= 0)
        {
            // A click on a place of a line: the bubble opens there.
            if (grid.PlaceAt(point, shift) is { } place && place.Row == down.Row)
            {
                sheet.Pick(place.Row, place.At);
            }
        }
        else if (grid.PlaceAt(point, shift) is null)
        {
            // A click on the bare sheet keeps the chord that was being typed.
            sheet.Settle();
        }
        Sync();
    }

    private void TakeTheKeysIfNobodyIsTyping()
    {
        if (Sheet?.Typing == true || IsKeyboardFocused || Window.GetWindow(this) is not { IsActive: true })
        {
            return;
        }
        // (A box of text that is being typed in keeps the keyboard.)
        if (Keyboard.FocusedElement is TextBoxBase)
        {
            return;
        }
        Focus();
    }

    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters) =>
        new PointHitTestResult(this, hitTestParameters.HitPoint);

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var point = e.GetPosition(this);
        var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        if (e.LeftButton == MouseButtonState.Pressed && pressed is not null)
        {
            Dragged(point, shift);
            if (Sheet?.Dragged is not null)
            {
                ScrollTowards(point);
            }
        }
        else
        {
            PointerMoved(point, shift);
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        Pressed(e.GetPosition(this), Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        ReleaseMouseCapture();
        Released(e.GetPosition(this), Keyboard.Modifiers.HasFlag(ModifierKeys.Shift), copy: Keyboard.Modifiers.HasFlag(ModifierKeys.Control));
        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        // (The button was let go somewhere this was not told of: nothing is carried on.)
        if (pressed is not null && Mouse.LeftButton != MouseButtonState.Pressed)
        {
            pressed = null;
            if (Sheet?.Dragged is not null)
            {
                Sheet.DropNowhere();
                Draw();
            }
        }
        base.OnLostMouseCapture(e);
    }

    // A chord carried to the edge of what is seen: the sheet scrolls that way, so that it
    // can be carried on to a slide that was out of sight.
    private void ScrollTowards(Point point)
    {
        if (Scroll is not { } scroll)
        {
            return;
        }
        var seen = Seen;
        if (point.Y > seen.Bottom - 24)
        {
            scroll.ScrollToVerticalOffset(scroll.VerticalOffset + 16);
        }
        else if (point.Y < seen.Top + 24)
        {
            scroll.ScrollToVerticalOffset(scroll.VerticalOffset - 16);
        }
    }

    // ---- The keys

    /// <summary>A key pressed with no chord being typed.</summary>
    public void KeyPressed(SheetKey key, bool shift = false)
    {
        if (Sheet is not { } sheet)
        {
            return;
        }
        switch (key.Kind)
        {
            case SheetKeyKind.Left:
                sheet.Step(-1, exact: shift);
                break;
            case SheetKeyKind.Right:
                sheet.Step(1, exact: shift);
                break;
            case SheetKeyKind.Up:
                EnsureLaidOut(sheet)?.StepLine(-1);
                break;
            case SheetKeyKind.Down:
                EnsureLaidOut(sheet)?.StepLine(1);
                break;
            case SheetKeyKind.Delete:
                sheet.TakeAtSpot();
                break;
            case SheetKeyKind.Open:
                sheet.StartTyping();
                break;
            case SheetKeyKind.Number:
                sheet.PutOfKey(key.Number);
                break;
            case SheetKeyKind.Letter:
                sheet.StartTyping(key.Letter);
                break;
            case SheetKeyKind.Copy:
                Asked.Copy(key.WholeGroup);
                copiedWhen = WhatWasCopiedLast();
                break;
            case SheetKeyKind.Paste:
                Asked.Paste();
                break;
            case SheetKeyKind.Undo:
                Asked.Undo();
                break;
            case SheetKeyKind.Redo:
                Asked.Redo();
                break;
            case SheetKeyKind.Zoom:
                if (Asked.Zoom(key.Number) is { } scale)
                {
                    SetScale(scale);
                }
                break;
            default:
                break;
        }
        Sync();
    }

    private SheetGrid? EnsureLaidOut(ChordSheet sheet)
    {
        if (Grid is null || !ReferenceEquals(sheet.Rows, Grid.Rows))
        {
            LayOut();
        }
        return Grid;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // (A key pressed in the box of the bubble comes by here too, on its way up from
        // the box: it is the box's, or was answered there.)
        if (!ReferenceEquals(e.OriginalSource, this))
        {
            return;
        }
        if (SheetKeys.Of(e.Key == Key.System ? e.SystemKey : e.Key, Keyboard.Modifiers) is { } found)
        {
            KeyPressed(found.Key, found.Shift);
            e.Handled = true;
        }
    }

    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            KeyPressed(SheetKey.Zoom(e.Delta > 0 ? 1 : -1));
            e.Handled = true;
        }
    }

    // ---- The box a chord is typed in

    /// <summary>
    /// A key pressed in the box, with these others held. Answers whether it was answered
    /// here; if not, the box does with it what a box does.
    /// </summary>
    /// <remarks>
    /// Most keys with Ctrl are the box's own: it is a box of text, and Ctrl+V pastes text
    /// into it. But a click on a line opens the box, and the one who clicks a line and
    /// presses Ctrl+V means the chords just copied to go on that line. So, while NOTHING
    /// HAS BEEN TYPED in the box (rule 52b):
    ///
    ///   - Ctrl+V, if the last thing copied anywhere was a line's chords here, closes the
    ///     box, changing nothing, and pastes them from the box's line on;
    ///   - Ctrl+C and Ctrl+Shift+C, if the box is empty and so has nothing of its own to
    ///     copy, copy the chords of the box's line, or of its group.
    ///
    /// The keys that zoom are the sheet's whoever has the keyboard.
    /// </remarks>
    internal bool KeyInTheBox(Key key, ModifierKeys held)
    {
        if (Sheet is not { Typing: true } sheet)
        {
            return false;
        }
        // (Shift with Tab would take the keyboard out of the box, and so end the typing
        // by the back door.)
        if (key == Key.Tab && held == ModifierKeys.Shift)
        {
            return true;
        }
        if (SheetKeys.InTheBox(key, held) is { } inTheBox)
        {
            var field = Entry.Field;
            var atEnd = field.SelectionLength == 0 && field.SelectionStart == field.Text.Length;
            var answered = sheet.Press(inTheBox, atEnd);
            if (answered)
            {
                Sync();
            }
            return answered;
        }
        if (!held.HasFlag(ModifierKeys.Control) || SheetKeys.Of(key, held) is not { } found)
        {
            return false;
        }
        var pressedKey = found.Key;
        if (pressedKey.Kind == SheetKeyKind.Zoom)
        {
            KeyPressed(pressedKey);
            return true;
        }
        if (!string.Equals(sheet.Entry.Text, sheet.Entry.Begun, StringComparison.Ordinal))
        {
            return false;
        }
        var line = sheet.TypingAt;
        if (pressedKey.Kind == SheetKeyKind.Paste && sheet.Copied is not null && copiedWhen == WhatWasCopiedLast())
        {
            sheet.FinishTyping(null, onward: false);
            sheet.SetSpot(line.Row, line.At);
            Sync();
            KeyPressed(pressedKey);
            return true;
        }
        if (pressedKey.Kind == SheetKeyKind.Copy && sheet.Entry.Text.Length == 0)
        {
            sheet.SetSpot(line.Row, line.At);
            KeyPressed(pressedKey);
            return true;
        }
        return false;
    }

    // What is in the box has changed, by a key or a paste.
    private void TheBoxChanged()
    {
        if (settingTheBox || Sheet is not { Typing: true } sheet)
        {
            return;
        }
        var field = Entry.Field;
        var typed = field.Text;
        var caret = field.SelectionStart;
        var held = sheet.Type(typed);
        if (!string.Equals(held, typed, StringComparison.Ordinal))
        {
            // Tidied, the caret stays where it was; undone, it goes back the one place
            // the key had moved it.
            var tidied = held.Length == typed.Length;
            settingTheBox = true;
            Entry.Set(held, tidied ? caret : Math.Max(0, caret - 1));
            settingTheBox = false;
        }
        Sync();
    }

    // The keyboard has gone from the box to something else than the sheet: leaving the
    // sheet for anything else keeps the chord that was being typed.
    private void TheBoxWasLeft()
    {
        if (movingTheKeys || Sheet is not { Typing: true } sheet)
        {
            return;
        }
        sheet.Settle();
        Sync();
    }

    // ---- Drawing

    private void Draw()
    {
        DrawUnder();
        DrawOver();
    }

    // (An edge is drawn down the middle of its own width.)
    private static void Rounded(DrawingContext context, Rect rect, double radius, Brush? fill, Brush? edge, double width = 1)
    {
        var within = new Rect(rect.X + (width / 2), rect.Y + (width / 2), Math.Max(0, rect.Width - width), Math.Max(0, rect.Height - width));
        context.DrawRoundedRectangle(fill, edge is null ? null : new Pen(edge, width), edge is null ? rect : within, radius, radius);
    }

    private void BubbleOf(DrawingContext context, string name, double x, double top, double width, bool lit, Brush edge)
    {
        Rounded(context, new Rect(x, top, width, Metrics.BubbleHeight), 6 * Metrics.Scale, lit ? LitBrush : BubbleBrush, edge);
        var text = Metrics.Name(name);
        context.DrawText(text, new Point(x + ((width - text.WidthIncludingTrailingWhitespace) / 2), top + ((Metrics.BubbleHeight - text.Height) / 2)));
    }

    private double TailWidth => Math.Max(1.5, 2 * Metrics.Scale);

    // A chord of a line: its tail on its character, and its bubble.
    private void ChordOf(DrawingContext context, int index, int n, bool lit, Brush edge)
    {
        var grid = Grid!;
        var row = Sheet!.Rows[index];
        var item = grid.Lines[index];
        var placed = item.Placed!;
        var x = placed.Xs[n];
        var width = placed.Widths[n];
        var scale = Metrics.Scale;
        // Where its character is, which is where the tail points even when the bubble has
        // had to move right of it.
        var character = row.Alone ? x + (width / 2) : Metrics.Advance(index, Math.Min(row.Chords[n].At, row.Text.Length));
        context.DrawRectangle(
            edge,
            null,
            new Rect(item.Left + x + Math.Max(3 * scale, Math.Min(width - (5 * scale), character - x + 1)), item.ChordTop + Metrics.BubbleHeight - 1, TailWidth, Metrics.Tail + (3 * scale)));
        BubbleOf(context, row.Chords[n].Name, item.Left + x, item.ChordTop, width, lit, edge);
    }

    // The slides: each a card in a frame of its group's colour, with its lines and
    // their chords.
    private void DrawUnder()
    {
        using var context = under.RenderOpen();
        context.DrawRectangle(GroundBrush, null, new Rect(0, 0, Math.Max(ActualWidth, Grid?.Content.Width ?? 0) + 4000, Math.Max(ActualHeight, Grid?.Content.Height ?? 0) + 4000));
        if (Sheet is not { } sheet || Grid is not { } grid || grid.Lines.Count != sheet.Rows.Count)
        {
            return;
        }
        var rows = sheet.Rows;
        foreach (var cell in grid.Cells)
        {
            // The frame is the colour of the slide's group; the card is laid on it, and
            // under the card is its foot, with the slide's number and, on the first slide
            // of each group, the group's name.
            var colour = cell.Colour is { } own ? SheetLook.Of(own) : SheetLook.Group;
            Rounded(context, cell.Frame, 4, Frozen(colour), null);
            Rounded(context, cell.Card, 2, CardBrush, null);
            var label = Metrics.Text(cell.Label, bold: false, SheetLook.LabelSize, SheetLook.IsLight(colour) ? Brushes.Black : Brushes.White);
            var room = cell.Frame.Width - 16;
            if (label.WidthIncludingTrailingWhitespace > room && room > 20)
            {
                label.MaxTextWidth = room;
                label.MaxLineCount = 1;
                label.Trimming = TextTrimming.CharacterEllipsis;
            }
            context.DrawText(label, new Point(cell.Frame.Left + 8, cell.Card.Bottom + ((SheetLook.Foot - label.Height) / 2)));

            for (var index = cell.FirstRow; index < cell.FirstRow + cell.Count; index++)
            {
                var row = rows[index];
                var item = grid.Lines[index];
                var wordsTop = item.ChordTop + Metrics.BubbleHeight + Metrics.Tail;
                if (row.Alone)
                {
                    // A line of chords alone has nothing to read: a rule stands where its
                    // words would be.
                    context.DrawRectangle(RuleBrush, null, new Rect(item.Left, wordsTop + (Metrics.WordHeight / 2), Math.Max(1, cell.Card.Right - Metrics.CardPad - item.Left), 1));
                }
                else
                {
                    Metrics.Line(index)?.Draw(context, new Point(item.Left, wordsTop), System.Windows.Media.TextFormatting.InvertAxes.None);
                }
                // The chords, each a bubble with its tail on its character. The one that
                // is being carried is left faint where it was.
                for (var n = 0; n < row.Chords.Count && item.Placed is not null; n++)
                {
                    var lifted = sheet.Dragged is { } carried && carried.From == new Spot(index, row.Chords[n].At);
                    if (lifted)
                    {
                        context.PushOpacity(0.35);
                    }
                    ChordOf(context, index, n, lit: false, EdgeBrush);
                    if (lifted)
                    {
                        context.Pop();
                    }
                }
            }
        }
    }

    // What follows the pointer and the keys: the ring round the slide that is picked, and
    // the spot.
    private void DrawOver()
    {
        using var context = over.RenderOpen();
        if (Sheet is not { } sheet || Grid is not { } grid || grid.Lines.Count != sheet.Rows.Count)
        {
            return;
        }
        // The slide that is picked has an orange ring, outside its frame and set off from
        // it by a little of the sheet, so that it reads against a frame of any colour.
        foreach (var cell in grid.Cells)
        {
            if (cell.Slide == sheet.Picked)
            {
                var ring = cell.Frame;
                ring.Inflate(4.5, 4.5);
                Rounded(context, ring, 7, null, AccentBrush, 2);
            }
        }

        var spot = sheet.Spot;
        if (!grid.IsLaid(spot.Row) || !sheet.Usable(spot.Row))
        {
            return;
        }
        var item = grid.Lines[spot.Row];
        var chords = sheet.Rows[spot.Row].Chords;
        var there = -1;
        for (var n = 0; n < chords.Count; n++)
        {
            if (chords[n].At == spot.At)
            {
                there = n;
            }
        }
        // Where there is a chord at the spot, that chord's own bubble is lit.
        if (there >= 0 && sheet.Dragged is null && item.Placed is not null)
        {
            ChordOf(context, spot.Row, there, lit: true, AccentBrush);
            return;
        }
        // Where there is none: a bubble in outline with its tail on the character. It is
        // shown while a chord is being typed as well, wherever the pointer has gone on
        // to, and not where the typing is, which has its own bubble.
        if (sheet.Typing && spot == sheet.TypingAt)
        {
            return;
        }
        var scale = Metrics.Scale;
        var x = grid.XOf(spot);
        var carriedName = sheet.Dragged?.Name;
        var width = Math.Max(26 * scale, carriedName is null ? 0 : Metrics.Bubble(carriedName));
        context.DrawRectangle(FaintAccent, null, new Rect(x + (3 * scale), item.ChordTop + Metrics.BubbleHeight - 1, TailWidth, Metrics.Tail + Metrics.WordHeight + (3 * scale)));
        if (carriedName is not null)
        {
            BubbleOf(context, carriedName, x, item.ChordTop, width, lit: true, AccentBrush);
        }
        else
        {
            Rounded(context, new Rect(x, item.ChordTop, width, Metrics.BubbleHeight), 6 * scale, null, FaintAccent);
            var plus = Metrics.Text("+", bold: true, SheetLook.ChordSize * scale, FaintAccent);
            context.DrawText(plus, new Point(x + ((width - plus.WidthIncludingTrailingWhitespace) / 2), item.ChordTop + ((Metrics.BubbleHeight - plus.Height) / 2)));
        }
    }
}
