// A song's lines, one under another, never wrapped: how a song is shown where it is only
// to be looked at. That is the song itself until the ChordPro editor is built (M5), and
// an earlier version of it in the panel of those and in All Copies until the chord sheet
// is (spec/07-backups.md, rule 16).
//
// The lines are Song/SongText.cs's: a group's name before its first slide, a gap between
// slides, each line of words with its chords before the characters they stand over.
// Chords are orange, brackets and all, as spec/04-chordpro-editor.md has them. The
// colouring never changes the text.
//
// They cannot be selected: they are plain words on the window, which is all WPF gives
// that draws off the screen and takes a colour for a part of a line. The ChordPro
// editor is a text box, and is.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using Pro7ChordEditor.Song;

namespace Pro7ChordEditor.AppViews;

/// <summary>A song's lines, to look at.</summary>
public sealed class SongLines : StackPanel
{
    // A line of a song is set in letters all of one width, so that a chord in brackets
    // takes the room it would in the file it came from.
    private static readonly FontFamily OfOneWidth = new("Cascadia Mono, Consolas, Courier New");

    private IReadOnlyList<SongLine>? shown;

    public SongLines()
    {
        HorizontalAlignment = HorizontalAlignment.Left;
    }

    /// <summary>Shows these lines in place of whatever it showed. The same lines again are left as they are.</summary>
    public void Show(IReadOnlyList<SongLine> lines)
    {
        if (ReferenceEquals(lines, shown))
        {
            return;
        }
        shown = lines;
        Children.Clear();
        foreach (var line in lines)
        {
            Children.Add(Drawn(line));
        }
    }

    // One line of the song, as it is drawn.
    private static UIElement Drawn(SongLine line)
    {
        switch (line.Kind)
        {
            case SongLineKind.Group:
                // The group's name, with a spot of its colour before it if the file
                // gives it one.
                var heading = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 2) };
                if (line.GroupColour is { } colour)
                {
                    heading.Children.Add(new Ellipse
                    {
                        Width = 9,
                        Height = 9,
                        Margin = new Thickness(0, 1, 6, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        Fill = new SolidColorBrush(Color.FromRgb(Part(colour.Red), Part(colour.Green), Part(colour.Blue))),
                    });
                }
                var name = new TextBlock { Text = line.Group, FontWeight = FontWeights.SemiBold };
                name.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
                heading.Children.Add(name);
                return heading;

            case SongLineKind.Gap:
                return new Border { Height = 10 };

            case SongLineKind.BlankSlide:
                var blank = new TextBlock { Text = "(a blank slide)", FontFamily = OfOneWidth, Margin = new Thickness(0, 1, 0, 1), ToolTip = "This slide has no words." };
                blank.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorTertiaryBrush");
                return blank;

            default:
                // (A line with nothing in it still takes a line's height.)
                var words = new TextBlock { FontFamily = OfOneWidth, Margin = new Thickness(0, 1, 0, 1) };
                foreach (var (text, isChord) in Parts(line.Text.Length == 0 ? " " : line.Text))
                {
                    var run = new Run(text);
                    if (isChord)
                    {
                        run.SetResourceReference(TextElement.ForegroundProperty, Look.Chord);
                    }
                    words.Inlines.Add(run);
                }
                return words;
        }
    }

    // A part of a colour as the file has it, from nought to one, as a screen has it,
    // from 0 to 255.
    private static byte Part(double ofOne) => (byte)Math.Round(Math.Clamp(ofOne, 0, 1) * 255);

    /// <summary>
    /// A line of ChordPro in its parts, each a chord or not: a chord is everything from
    /// a "[" to its "]", brackets and all. The colouring never changes the text: the
    /// parts put together again are the line.
    /// </summary>
    internal static IReadOnlyList<(string Text, bool IsChord)> Parts(string line)
    {
        var parts = new List<(string, bool)>();
        var from = 0;
        while (from < line.Length)
        {
            var open = line.IndexOf('[', from);
            var close = open < 0 ? -1 : line.IndexOf(']', open);
            if (close < 0)
            {
                break;
            }
            if (open > from)
            {
                parts.Add((line[from..open], false));
            }
            parts.Add((line[open..(close + 1)], true));
            from = close + 1;
        }
        if (from < line.Length)
        {
            parts.Add((line[from..], false));
        }
        return parts;
    }
}
