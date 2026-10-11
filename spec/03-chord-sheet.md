# The chord sheet

The main editor: the whole song as one sheet of words, with its chords in bubbles over
them, to be put on, changed, moved and taken off with the mouse and the keys.

![The chord sheet](pictures/chord-editor.png)

This page is the working editor of Simple Presenter written down:
`reference/simple-presenter/qml/ChordSheet.qml` (1,200 lines, a sixth of them comments
that say why) and `chordlayout.js`. Read that file before building this; where this
page and that file differ, this page says so and says why.
`reference/simple-presenter/tests/chords-scripted-test.qml` is that app's own test of
it, eighty checks that read as a list of what it must do.

Keys are named here as Windows names them. On the Mac, Ctrl is Command and Delete is
either delete key: [10-platforms.md](10-platforms.md).

## Why it is the way it is

- **One sheet, where the file has slides.** In the file each slide has its own text
  box, and a chord belongs to one box. Working a slide at a time would mean paging
  through thirty of them to chord one song. So every line of every box is laid out
  one under another. Nothing is joined underneath: a chord put on a word is written to
  that word's own text box on its own slide.
- **The words cannot be changed here.** Only chords.
- **Point with one hand, press with the other.** There is one place the next chord
  goes to, the *spot*. It follows the pointer, and the arrow keys move it too. Most
  chords are then one key press.
- **Nothing has to be pressed to say a chord is done.** Click, type, click, type, down
  the song.

## The rows

The sheet is a list of rows made from the song's text boxes
([05-song-and-saving.md](05-song-and-saving.md) says what those are):

1. For each text box in order: if it is the first box of a new slide, and that slide
   starts a group that has a name, a **group row** with the group's name and colour.
2. Then one **line row** for each line of the box's words (split at line breaks, an
   empty line included). A line row knows its text box, which line of it this is, the
   place its line starts at in the box, its words, its chords with their places counted
   *along the line*, and whether it is a line of chords alone.
2a. **The empty lines a box's words end with get no row**: words that end with a line
   break, or several, would otherwise make their slide that many lines taller, and
   every card of the song with it (rule 8). One that a chord stands on is kept, with
   any before it; and a box always has one line. Nothing of the words is changed by
   this: it is only what is drawn. (**New**, asked for by the maintainer on
   2026-10-11 after seeing cards too tall in their own library, where 40 text boxes
   of 6,091 end so, in 19 songs of 422. Simple Presenter gives each such line a row.
   `sheet-model.json` has no such box.)
3. A chord belongs to the line its place is in: from the line's start up to but not
   including its line break. An empty line is taken as one place wide, so that a chord
   standing on it is not lost.
4. A line row can take the spot (is *usable*) if its words are not empty. A group row
   and an empty line cannot. The one exception is the row of a **blank slide**
   ([01-file-format.md](01-file-format.md)): its box has no words, so it is one empty
   line, and it is usable. It is a line of chords alone with none yet: rule 13 draws
   it, rule 22 gives it its one stop, and a chord put there asks the song for "this
   line of chords alone now has these". (**New**, and in Simple Presenter
   since 2026-10-10: `sheet-model.json` has it in the song `blank`. Its song `bare` is
   a slide with no text box, whose row is the same and names no box:
   [01-file-format.md](01-file-format.md), "A slide with no text box".)
5. Each slide's line rows know how many they are, which is the first and which the
   last, and the sheet knows the greatest number any slide has. That is what the cards
   are sized by.

Vectors: `sheet-model.json`, `rows`.

## How it looks

6. Dark: the sheet `#1b1c1f`, the strip across its top `#202226`, a card `#222428` with
   a one-pixel edge `#33363c`, words `#e6e6e6`. The accent is orange, `#ff8a1f`.
7. **A card for each slide**: a rounded rectangle (radius 8) behind the slide's lines,
   with the slide's number to its left, right-aligned in a gutter 54 wide. The card
   of the slide that is picked has an edge of `#8a5a22` and its number is orange.
8. **All cards are one size**: as wide as the widest line of the whole song with its
   chords, and as tall as the slide with the most lines. So the sheet reads as a column
   of slides and not as a ragged list. (Asked for by the maintainer after trying cards
   of differing sizes.)
9. A group's name is over its first card: white bold 12 on a small rounded label in
   the group's colour (ProPresenter's colour for a group of that name, or a neutral
   grey).
10. **A line** is its words at 21 pixels with a row above them for chords. A chord is a
    **bubble**: its name in bold 14, `#ffd9a8`, on a rounded rectangle `#34363c` edged
    `#5c5f66`, with 7 pixels either side of the name, and under it a **tail**, a line 2
    wide that comes down to the character the chord stands at.
11. **Where a bubble stands.** Its left edge is where its character starts. If that
    would run into the bubble before it on the line, it is pushed right until it clears
    that one by 4. Its tail still points at its own character. That is all of the
    layout, and it is one small function (`place`) so that everything that draws chords
    puts them in the same spot. Vectors: `layout.json`.
12. A line never wraps. The sheet scrolls both ways.
13. A line of chords alone has no words to show: a thin rule stands where they would
    be, and its bubbles sit side by side.
14. The strip across the top has the key's seven chords as small buttons, each with
    its number 1 to 7 in grey before its name. If the song states no key, they are
    C's, and the right of the strip says "The song names no key: pick the one its
    chords are in". **The key is not picked here**: it is picked in the song's header
    over the sheet ([06-library.md](06-library.md), rule 12), which is there for both
    editors. (In Simple Presenter the strip begins with the word "Key" and the key's
    picker, and these pages asked for one in both places, so that the sheet showed the
    key twice. The maintainer, 2026-10-11: "we only need one".)

The sizes above are Simple Presenter's and need not be matched to the pixel. The
proportions and the look should be: the pictures in `spec/pictures` are what to hold
your own pictures up against.

## The spot

15. There is one spot: a row and a place on its line. Where there is no chord at it, it
    shows as a bubble **in outline**, orange, with a "+" in it and a long tail down
    through the words. Where there is a chord, that chord's bubble is lit instead
    (`#4a3a26`, orange edge and tail).
16. **It follows the pointer.** Over a line it goes to the character under the pointer:
    the one whose left edge is nearest on the left. Over the space between two words it
    goes to the word after. With **Shift** held it goes to the start of the word
    instead. Over a chord's bubble it goes to that chord.
17. It follows the pointer only when the pointer itself has moved. A row that is laid
    out again, or scrolls, under a pointer lying still must not take the spot back from
    the keys.
18. The pointer is followed only over the cards. Beside them is bare sheet.
18a. **A place is never the second half of a character that takes two units** (one from
    beyond the first 65,536): the pointer goes to its first half, and the keys pass
    over the second. And a character that has no width at all (a zero-width space
    beside another, a soft hyphen) cannot be pointed at: the place is the next one
    along. The keys still reach it, with Shift. (**New**: the page was silent, and the
    reference stops on both halves.)
19. **Right and Left** move it a word at a time: to the next or the previous start of a
    word on the line, and from the end of a line to the first word of the next line
    that can take it (from the start, to the last word of the one before). With
    **Shift** they move a character at a time, spaces passed over. Note that this is
    the other way about from the pointer, on purpose: a letter at a time is a long walk
    with the keys, and a word at a time is too coarse for the mouse.
20. **Down and Up** move it to the next or previous line that can take it, to the stop
    that is nearest above or below where it was *on the screen*.
21. With no spot yet, the first arrow key puts it on the first word of the song.
22. On a **line of chords alone** the stops are each chord and one more place after the
    last, for a new one. Places there are 0, 2, 4 ... (the stand-ins).
23. Moving the spot with the keys scrolls the sheet to keep it in view, and picks its
    slide (rule 7).
23a. **The keys go where the pointer is.** The sheet takes the keyboard when it is
    clicked, and also when the pointer moves over it, unless a box of text elsewhere
    in the window is being typed in. Pointing and pressing 4 then needs no click
    first. (**New**, on the Mac, where a view does not have the keyboard until it is
    given it; the maintainer agreed to it on 2026-10-11. In the reference the sheet
    has the keyboard whenever its page is open.)

Vectors: `sheet-model.json`: `wordStarts`, `stops`, `stopAt`, `walks`.

## Putting a chord on

24. **1 to 7** put the key's own chord for that note of its scale on the spot, in place
    of whatever is there. The buttons in the strip do the same when clicked.
25. **A letter A to G** opens the typing bubble on the spot with that letter in it, as
    a capital.
26. **Enter, or F2**, opens it on what is at the spot: the chord there, all of it
    selected to be typed over, or empty.
27. **A click** on a place of a line does the same there (and on a chord's bubble, on
    that chord).
28. **Delete or Backspace** takes off the chord at the spot.

## The typing bubble

![Typing a chord](pictures/chords-type.png)

29. It is a small panel at the place the chord is for, growing **upwards** from where
    the chord will stand, so that the word and the rest of its line stay in sight. It
    has a one-line box to type in, and above that what the chord might be going to be,
    as a row of buttons that wraps: `completions`, the first fourteen.
30. The box takes only what could be a chord (`couldBecome`). A key press that would
    make it otherwise is undone at once. What is typed is tidied as it is typed (`f`
    becomes `F`). At most sixteen characters.
31. **Down and Up** go through what is offered, round and round; the one picked is lit
    in orange. **Right**, at the end of the box with one picked, puts that one in the
    box to be gone on with (for a bass note, say).
32. **Enter** keeps the chord: the one picked, or else what is typed. **A click** on
    one that is offered keeps that one.
33. **Tab** keeps it and moves the spot on to the next word. If nothing is picked and
    what is typed is not yet a chord, Tab takes the first that is offered.
34. **Esc** throws the typing away and changes nothing. It is the only thing that does.
35. **Backspace or Delete in an empty box** takes off the chord at that place.
36. A slash on the end of what is kept, typed on the way to a bass note that never
    came, is left off: `G/` is kept as `G`.
37. What is kept must be a chord (`isChord`). If it is not, nothing is put on.
38. **While a chord is being typed, the spot goes on following the pointer**, and shows
    where a click would start the next one. The place being typed for is remembered
    apart from the spot.
39. **A click on another place keeps what was typed and opens the bubble there.** So
    does a click on another chord. A click on the bare sheet keeps it and closes the
    bubble. So does leaving the sheet for anything else. A click on the strip's 1 to 7
    puts *that* chord where the typing was, in place of what was typed.
40. A chord cannot be dragged while another is being typed: pressing and letting go on
    it is a click on it (rule 39).

Getting rules 38 to 40 right is most of what makes the sheet quick. Note what follows
from them for the code: keeping a chord lays the sheet out again, the row that was
clicked with it, so the click must be handled by row number and place and not by the
thing that was clicked.

Vectors: `sheet-model.json`, `typing`.

## Moving and copying a chord

![Dragging a chord](pictures/chords-drag.png)

41. A chord's bubble can be **dragged** to any place of any line, on any slide. The
    drag starts once the pointer has moved 6 pixels with the button down. While it
    lasts the bubble left behind is faint, and the spot follows the pointer as in rule
    16, showing the chord's name in a lit bubble where it would land.
42. Let go, and the chord moves there. With **Ctrl** held as it is let go, it is copied
    and the first stays.
43. Whatever chord was at the place it lands on gives way to it.
44. Along its own line it is one change. To another line, it is put there and then
    taken from here, as two changes that undo as one (the second is *joined* to the
    first).
45. Along a line of chords alone it changes places with the others: it is taken out and
    put in at the new position, and the rest close up.
46. Dropped where it started, nothing happens.

Vectors: `sheet-model.json`, `edits`.

## A line's chords onto another line

47. **Ctrl+C** copies the chords of the spot's line. **Ctrl+Shift+C** copies those of
    its whole group: every line from the group's name down to the next group's.
48. What is copied is each chord by **which word** of its line it is on, how far into
    that word, and its name. A chord standing on the space before a word counts as on
    the word before. A line of chords alone is copied as its chords' names.
49. A line at the bottom of the window says what was copied, for a few seconds: "4
    chords copied from the line: Ctrl+V puts them on the lines from the spot's on, word
    for word". The key is named as the platform names it: on the Mac, "Command+V".
    What was copied is the sheet's own, and is not put where the system keeps what
    was copied for other apps.
50. **Ctrl+V** puts them on the lines from the spot's line on, one copied line to each
    line that can take the spot. For a line of words: each chord goes on the same
    numbered word, as far into it as it was but not past the word's last letter; a
    chord for a word the line has not got is left out. The line's own chords are
    replaced by these, all of them.
51. A copied line of chords alone goes only onto a line of chords alone, and replaces
    its chords; a copied line of words goes only onto a line of words. Where the kinds
    differ, that line is passed over, and the next copied line goes to the next line.
52. However many lines it changes, a paste undoes as one: each change after the first
    that is *made* is joined to the one before. (**Unlike the reference**, which joins
    every copied line but the first, whether or not the first changed anything. Where
    the first copied line is passed over by rule 51, its paste is joined to whatever
    was done before it, and one undo takes that back too. `sheet-model.json` has no
    such case.)

This is how a second verse gets the chords of the first. Vectors: `sheet-model.json`,
`copyPaste`.

## Undo, and the key

53. **Ctrl+Z** undoes the last change and **Ctrl+Shift+Z** or **Ctrl+Y** puts it back.
    Every change is one step, or several joined as one (rules 44 and 52).
54. The key picker, in the song's header (rule 14), sets the key the chords are
    written in ([05-song-and-saving.md](05-song-and-saving.md)). 1 to 7 and what is
    offered while typing go by it. A song that states no key is taken to be in C until one is set.
    Changing the key does not change any chord.

## What each thing asks of the song

The sheet changes nothing itself. Everything above comes down to one of two requests
to the song, each of which is saved at once:

- **"This text box now has these chords"**: the slide, the box, and the whole list of
  its chords, each with its place counted along the *whole box*. Used for every change
  to a line of words: the line's chords are worked out, and the box's other lines'
  chords are handed back with them unchanged.
- **"This line of chords alone now has these"**: the slide, the box, which line, and
  the names in order. The song makes the stand-ins to match
  ([01-file-format.md](01-file-format.md)).

Each can be marked as joined to the change before it. `sheet-model.json` records, for
every action above, exactly what is asked: `edits`, `typing` and `copyPaste`. Build
the part of the app that turns "this was done at this spot" into those requests as
plain code with no window in it, and hold it to that file.

## Chords that are not chords

Some files have chord names that are not chords: `C /// | Am ///`, `N.C.`. They are
shown as they are and kept as they are. **Unlike Simple Presenter's sheet**, which
refuses to put on anything that is not a chord and so cannot move or copy one of
these, this app holds only *newly typed* names to `isChord` (rule 37). A name the song
already has may be dragged, copied and pasted like any other, and is put as it is: a
slash on its end is not left off either (rule 36 is for what is typed). To change
one, it is typed over.

## Until chords with no words are built (M7)

The sheet is built before the app can add a stand-in to a line of chords alone or take
one away ([11-plan.md](11-plan.md)). Until it can, such a line and a blank slide's are
drawn and take the spot like any other; the sheet asks the song as rules 22 and 45
say; and the song refuses what would change how many chords the line has, in its own
sentence ([05-song-and-saving.md](05-song-and-saving.md)), which the quiet line under
the sheet shows. A chord there can be renamed, and moved along its line.

## What to hold your build to

- The model: `sheet-model.json` and `layout.json`, every case.
- The look: draw the sheet for `testdata/files/made-by-simple-presenter/Amazing
  Grace.pro` into a picture and set it beside `pictures/chord-editor.png`
  ([10-platforms.md](10-platforms.md)).
- The feel, which only a person can tell: chord a verse of that song by pointing and
  pressing 1 to 7; type a run of chords click, type, click, type; drag one; copy a
  verse onto the next. Ask the maintainer to, and to say what is slower or odder than
  it was in Simple Presenter.
