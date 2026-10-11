// What the ChordPro text box works out for itself, with no text box in it: the one
// change between two texts, line breaks as the song counts them, and the box's own
// undo (spec/04-chordpro-editor.md, rules 7, 8 and 23).
//
// None of this is a rule of what may be typed: that is Song/ChordProEditor.cs's, asked
// by way of the model. This is what a WPF text box needs beside it to keep to that
// rule, and it is here, and not in the box's code, so that tests can hold it without a
// window.
//
// WHY THE BOX DOES NOT USE ITS OWN UNDO. WPF's text box does not ask before it changes:
// it changes, and says so. So a change that is not the song's words is put back after
// it has happened (rule 7, as the reference does), and the box's own undo would then
// remember both the change and the putting back, and Ctrl+Z would walk back into what
// was refused. The box's own undo is switched off, and what was let through is
// remembered here instead: a step for each run of typing, as WPF's has it.
//
// The Mac needs none of this: its text view asks first (ChordProView.swift).

namespace Pro7ChordEditor.AppViews;

/// <summary>One change to a piece of text: so many units at a place taken out, and others put there.</summary>
public readonly record struct TextChange(int Start, int Removed, string Inserted)
{
    /// <summary>
    /// The one change that makes a text into another, said as small as it can be: what
    /// the two have in common at their start and at their end is left out. Nothing if
    /// they are the same.
    /// </summary>
    /// <remarks>
    /// Never from the middle of a character that is two units: a text box will not put
    /// its caret there, and a change said so would be made somewhere else.
    /// </remarks>
    public static TextChange? Between(string before, string after)
    {
        if (string.Equals(before, after, StringComparison.Ordinal))
        {
            return null;
        }
        var most = Math.Min(before.Length, after.Length);
        var start = 0;
        while (start < most && before[start] == after[start])
        {
            start++;
        }
        if (start > 0 && char.IsHighSurrogate(before[start - 1]))
        {
            start--;
        }
        var end = 0;
        while (end < most - start && before[before.Length - 1 - end] == after[after.Length - 1 - end])
        {
            end++;
        }
        if (end > 0 && char.IsLowSurrogate(before[before.Length - end]))
        {
            end--;
        }
        return new TextChange(start, before.Length - start - end, after.Substring(start, after.Length - start - end));
    }

    /// <summary>The text with this change made to it.</summary>
    public string AppliedTo(string text) => string.Concat(text.AsSpan(0, Start), Inserted, text.AsSpan(Start + Removed));
}

public static class ChordProTyping
{
    /// <summary>
    /// A text with every line break made the one unit the song counts
    /// (spec/10-platforms.md, "Counting characters"). What is pasted on Windows has two
    /// to a line break, a carriage return and a line feed; a carriage return by itself
    /// is a line break too.
    /// </summary>
    public static string WithLineBreaksOfOneUnit(string text) =>
        text.Contains('\r', StringComparison.Ordinal)
            ? text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n')
            : text;

    /// <summary>Where a place in such a text is, once its line breaks are one unit each.</summary>
    public static int PlaceWithLineBreaksOfOneUnit(string text, int place)
    {
        place = Math.Min(Math.Max(place, 0), text.Length);
        var fewer = 0;
        for (var at = 0; at + 1 < text.Length && at < place; at++)
        {
            if (text[at] == '\r' && text[at + 1] == '\n')
            {
                fewer++;
            }
        }
        // (A place between the two units of a line break is the place before it.)
        return place - fewer;
    }
}

/// <summary>
/// The text box's own undo: what was let through, a step for each run of typing, to be
/// walked back through and forward again.
/// </summary>
public sealed class TypingHistory
{
    /// <summary>How many steps are remembered. The oldest is forgotten when there are more.</summary>
    public const int Most = 300;

    /// <summary>A text and what was selected in it: a caret, if nothing was.</summary>
    public readonly record struct State(string Text, int Start, int Length = 0);

    private enum Kind
    {
        Typed,
        Deleted,
        Other,
    }

    private sealed record Step(State Before, State After, Kind Kind);

    private readonly List<Step> done = [];
    private readonly List<Step> undone = [];

    // Whether the last step is over, so that nothing joins it: a step was taken back
    // since anything was typed.
    private bool ended;

    public bool CanUndo => done.Count > 0;

    public bool CanRedo => undone.Count > 0;

    /// <summary>How many steps Undo has to take back.</summary>
    public int Steps => done.Count;

    /// <summary>
    /// A change was let through. It is a step of its own, unless it goes on from the
    /// last: one more character typed where the last left the caret, or one more
    /// deleted there, with nothing done between, an undo or a redo least of all. What
    /// was undone can be redone no more.
    /// </summary>
    public void Note(State before, State after)
    {
        if (TextChange.Between(before.Text, after.Text) is not { } change)
        {
            return;
        }
        var kind = change.Removed == 0 && IsOneCharacter(change.Inserted) ? Kind.Typed
            : change.Inserted.Length == 0 && IsOneCharacter(before.Text.Substring(change.Start, change.Removed)) ? Kind.Deleted
            : Kind.Other;
        Note(before, after, kind);
    }

    /// <summary>
    /// The same, of a change made in the course of typing whatever it is: a bracket
    /// that brought its other with it.
    /// </summary>
    public void NoteTyped(State before, State after)
    {
        if (!string.Equals(before.Text, after.Text, StringComparison.Ordinal))
        {
            Note(before, after, Kind.Typed);
        }
    }

    private void Note(State before, State after, Kind kind)
    {
        undone.Clear();
        if (!ended && kind != Kind.Other && done.Count > 0 && done[^1] is { } last && last.Kind == kind && last.After == before)
        {
            done[^1] = last with { After = after };
            return;
        }
        ended = false;
        done.Add(new Step(before, after, kind));
        if (done.Count > Most)
        {
            done.RemoveAt(0);
        }
    }

    /// <summary>What Undo would go back to, if there is a step to take back.</summary>
    public State? Before => done.Count > 0 ? done[^1].Before : null;

    /// <summary>What Redo would go forward to, if a step was taken back.</summary>
    public State? After => undone.Count > 0 ? undone[^1].After : null;

    /// <summary>The last step was taken back: it can be redone.</summary>
    public void SteppedBack()
    {
        if (done.Count > 0)
        {
            ended = true;
            undone.Add(done[^1]);
            done.RemoveAt(done.Count - 1);
        }
    }

    /// <summary>The step last taken back was made again.</summary>
    public void SteppedForward()
    {
        // (The run is over already: a step is only made again after one was taken back.)
        if (undone.Count > 0)
        {
            done.Add(undone[^1]);
            undone.RemoveAt(undone.Count - 1);
        }
    }

    /// <summary>Forgets everything: the text was made again from the song (rules 19 and 19a).</summary>
    public void Forget()
    {
        done.Clear();
        undone.Clear();
    }

    // One character as a person counts one: a unit, or the two of a pair.
    private static bool IsOneCharacter(string text) =>
        text.Length == 1 || (text.Length == 2 && char.IsSurrogatePair(text[0], text[1]));
}
