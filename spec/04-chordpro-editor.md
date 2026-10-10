# The ChordPro editor

The same song as text: its words in one piece, with each chord in square brackets
where it is played, to be typed among. The chord sheet is for the mouse; this is for
someone who thinks in ChordPro, or has a chart to copy from. It is also what the old
Windows editor was, done properly, so people who used that will know it at once.

![The ChordPro editor](pictures/chordpro-editor.png)

The working code is `reference/simple-presenter/qml/ChordProEditor.qml`, 365 lines.
Build this before the chord sheet ([11-plan.md](11-plan.md), M5): it is far less work,
and with it the app is already of use.

## The text

1. The whole song is one piece of plain text, in a fixed-width font, that never wraps.
   Line by line it is:

   | A line of the text | What it is | Can it be changed |
   | --- | --- | --- |
   | `{c: Verse 1}` | The name of a group, before its first slide | No |
   | (an empty line) | The gap between one slide and the next | No |
   | `[G]Words and [D]more words` | A line of a slide's words, with its chords | Only its chords |
   | `[C] [G] [Am]` | A line of chords alone | Its chords, freely |

2. It is made from the song's text boxes in order
   ([05-song-and-saving.md](05-song-and-saving.md)): an empty line before each new
   slide but the first; then, if that slide starts a group that has a name, the group's
   line; then each line of each box as `toChordPro` writes it
   ([02-chord-rules.md](02-chord-rules.md)).
3. Beside the text the editor keeps, for every line of it, what that line is: which
   text box and which line of it, where that line starts in the box, its words without
   chords, and whether it is a line of chords alone. Every rule below goes by that.
4. Chords are coloured orange, brackets and all. The lines in curly brackets are
   greyed. The colouring never changes the text.
5. Over the text is one line of help: "Type chords in square brackets among the words:
   [G]  [D/F#]  [Am7].  Only the chords can be changed here; the words are the
   slides'."

## What may be typed

6. **The one rule.** A change to the text is let through only if, with the chords taken
   out, the text is still the words it started as. That is: it has the same number of
   lines, and for each line
   - a group's line or a gap is exactly what it was;
   - a line of words, with every `[...]` taken out (`withoutChords`), is exactly its
     words, and has no stray `[` or `]` left in it;
   - a line of chords alone, with every `[...]` taken out, is nothing but spaces;
   - whatever is in any pair of brackets could become a chord (`couldBecome`, after
     trimming).
7. A change that fails the rule is put back as it was, at once, with the caret left
   where the eye expects it: where it was for a deletion, one back for a character
   typed.
8. That single rule covers typing in the wrong place, pasting, cutting, dropping and
   undoing alike. Do not write a rule for each.

## Help with the brackets

Since brackets are all there is to type:

9. `[` typed with nothing selected puts `[]` and leaves the caret between them.
10. `]` typed where the next character is already `]` steps over it.
11. **Backspace** just after a `[` or a `]`, or **Delete** just before one, takes the
    whole chord, brackets and all. (Only when the caret is in or beside a chord on its
    own line; otherwise the key does what it always does, and rule 6 decides.)
12. None of this happens while Ctrl, Alt or the Windows or Command key is down.

![Typing a chord as ChordPro](pictures/chordpro-type.png)

## Saving

13. A change is saved **700 milliseconds after the typing stops**, and once more on the
    way out of the editor (to the sheet, to another song, on quitting). Nothing is
    saved while the text fails rule 6, which it cannot for long.
14. What is saved is each text box whose chords are no longer what the text says, as
    one change each ([05-song-and-saving.md](05-song-and-saving.md)). The chords of a
    line are read with `chordsOf`: each at the place it has among the line's words.
15. **Empty brackets, and ones that hold what is not yet a chord (`isChord`), are left
    out of what is saved and left in the text.** Someone half way through typing `[F#`
    must not have it saved as a chord nor taken away from them.
16. A chord typed after the last word of a line hangs on the line's last character.
17. A line of chords alone is saved as its chords' names in order. Since that can
    change its text box's words (the stand-ins), the song is read again after it, and
    any other lines still to be saved go in the next round.
18. All the changes of one round of saving undo as one step.
19. When the song changes from elsewhere (an undo, the other editor), the text is made
    again from the song. When it changes because of what was just typed here, the text
    is left exactly as typed.

## Words that have brackets of their own

20. If any text box's words have a `[` or a `]` in them, ChordPro text cannot tell them
    from chords. The song is then shown here but cannot be changed here, and the line
    of help says so, in a warmer colour: "This song's words have square brackets of
    their own in them, so it cannot be changed as ChordPro text. The chord sheet can
    still be used."

## Which slide is picked

21. The caret's line says which slide is picked: the same notion as on the sheet. When
    a slide is picked from elsewhere, the caret goes to the start of its first line and
    the text scrolls to it.

## Different from the reference, on purpose

22. **A chord the song already has that is not a chord.** Rule 6 asks that every
    bracket could become a chord. In Simple Presenter a song that already has
    `[C /// | Am ///]` in it therefore fails the rule from the start, and nothing in it
    can be changed as ChordPro. Here, the names the song has when it is opened are let
    through as they are: rule 6 holds a bracket to `couldBecome` only if what is in it
    is not, exactly, one of those names. They are saved back as they were (rule 15
    likewise lets them through).
23. **Undo** here is the text box's own, as in any box of text: Ctrl+Z takes back what
    was typed, a keystroke or a word at a time as the platform has it. Whatever it
    leaves is held to rule 6 and saved by rule 13 like anything typed, so the text and
    the song cannot come to say different things. That is how the reference does it.
    The song's own history ([05-song-and-saving.md](05-song-and-saving.md)) still gets
    a step for each round of saving, which the sheet's Ctrl+Z will walk back through
    later.

## What to hold your build to

- Rule 6 as a plain function of (the lines as they were, the text as it is now), with
  tests of your own: there are no vectors for it, since it lives in the QML. Take the
  cases from `reference/simple-presenter/tests/chords-scripted-test.qml` (search for
  "ChordPro"), which tries typing over a word, into a group's name, a chord in each
  kind of line, and a paste.
- What is saved (rules 14 to 17) against the same song changed on the sheet: the same
  chords put on both ways must give the same file.
