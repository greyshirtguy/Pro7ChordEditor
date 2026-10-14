<!-- The part of Simple Presenter's README that describes its chord features to the person using it, as it is
     at commit 2e67b1a (2026-10-14, the change after version 0.94). Kept here as it was, for how the editor is
     explained in plain words. Its links go to that repository. -->

## Chords

A song can have chords over its words, for a stage screen to show the band. They are
ProPresenter's: it gets them with a song imported from Multitracks, keeps them in the
song's file, and shows them on a stage layout. This app reads and shows them the same
way, and adds what ProPresenter has not: a quick way to put chords on by hand, and a
way to bring a song in from a ChordPro file.

### On the stage

In a stage layout, a text box that shows the current or the next slide's text has
**Chords** in its Text tab: **Show**, a colour, and how they are written, which are
ProPresenter's four: **Chords** (C#m7/E), **Numbers** (6m7/1), **Numerals** (vi7/I) and
**Do Re Mi** (Do#m7/Mi). The chords are drawn over the syllables they belong to. So that
they stay there, a line of words with chords is never broken in two: text too wide for
its box is made smaller instead. A slide that is chords and no words (an intro, a
turnaround) is shown as its chords.

![A stage layout in the editor: the text box that shows the current slide's words is picked, with the live song's chords drawn over them, and at the foot of its Text tab are Chords, with Show ticked and a colour, and As, set to Chords](https://raw.githubusercontent.com/greyshirtguy/Simple-Presenter/2e67b1aa4aa875ba128374b0c26a01acd84421e8/docs/stage-layout-chords.png)

**The key.** A song's chords are written in the file in its original key. Over the
slides of a song that has chords, at the left of the line that has the song's name in
its middle, there is a **Key** to pick, starting at the key the file says the song was
last shown in, and the stage shows the chords in the key picked.
Picking a key changes nothing in the file and lasts while the app is open.

### The chord editor

With a presentation in the editor, the switch in the middle of its toolbar gives three
ways of working: **Slides** (the editor as it always was), **Chords** and **ChordPro**.
The last two are for a song's chords, and are this app's own: ProPresenter shows the
chords a song came with and has no way to put one on.

The same two editors are a program of their own for the Mac and for Windows, [Pro7
Chord Editor](https://github.com/greyshirtguy/Pro7ChordEditor), which began as a copy
of this one. What was thought of and found in building that has come back here, and the
two are meant to work and look alike. (Where they cannot: there, Ctrl+1 and Ctrl+2 show
the two editors and Ctrl+S says that there is nothing to save; here those keys are
ProPresenter's, for the screens and for showing, and stay so.)

Putting chords on a song by hand is usually a chore. **Chords** was made so that it is
not, and it has turned out to be the part of this app that is most fun to use. The
whole song is one sheet, because chording a song a slide at a time would be slow going.
(Each chord is still kept with its own slide's words, in ProPresenter's file.) Its
slides are a grid, like the slides of a presentation in the operator window, but each
is its words at a size to be read, with room over every line for its chords: a card in
a frame of its group's colour, with the slide's number on the frame's foot and, on the
first slide of each group, the group's name. They go across the sheet in the order of
the song, as many to a row as fit.

| Point at a letter, and press 1 to 7 | Or type a chord, and take what is offered |
|:---:|:---:|
| <img src="https://raw.githubusercontent.com/greyshirtguy/Simple-Presenter/2e67b1aa4aa875ba128374b0c26a01acd84421e8/docs/chords-point.png" width="420" alt="The top of the chord sheet: the seven chords of the song's key numbered 1 to 7, and under them the first slide of the first verse in its card, with a bubble in outline over the word the pointer is on"> | <img src="https://raw.githubusercontent.com/greyshirtguy/Simple-Presenter/2e67b1aa4aa875ba128374b0c26a01acd84421e8/docs/chords-type.png" width="420" alt="The same card with a bubble open over a word, D and a slash typed in it, and over that the bass notes it might be going on to: D/F#, D/A, D/G, D/B, D/C and D/E"> |
| **Drag a chord to where it belongs** | **Or type the lot as ChordPro** |
| <img src="https://raw.githubusercontent.com/greyshirtguy/Simple-Presenter/2e67b1aa4aa875ba128374b0c26a01acd84421e8/docs/chords-drag.png" width="420" alt="The same card with the chord D being dragged from one word to another: faint where it was, and bright where it would land"> | <img src="https://raw.githubusercontent.com/greyshirtguy/Simple-Presenter/2e67b1aa4aa875ba128374b0c26a01acd84421e8/docs/chordpro-type.png" width="420" alt="The ChordPro editor: the same song as text, its groups in curly brackets and its chords in square brackets in orange, one of them just typed"> |

What makes it quick:

- **A spot shows where the next chord will go.** It is a bubble in outline that follows
  the pointer from letter to letter (and the arrow keys from word to word), with its
  tail on the letter the chord will stand over. Any letter will do: chords fall in the
  middle of words as often as at their start.
- **Most chords are one key.** The song's key is in the toolbar, and its seven chords
  are along the top of the sheet, numbered. Point at a letter and press **4**, and the
  key's fourth chord is there: in G, that is C. Most of most songs is those seven.
- **Or one of the seven is dragged onto a word.** Each of them is a button, which puts
  its chord on the spot when clicked; and it can be picked up and carried down to a
  word, on any slide, which is slower than a key and is what a hand does unasked. Its
  picture goes with the pointer, and over a word a lit bubble shows the chord where it
  would land, in place of a chord that is there. That bubble is the whole of the
  promise: let go between two slides, on a frame's foot or back on the strip, or press
  Esc, and nothing is put on. Held near the bottom of the sheet, or its top, it scrolls
  the sheet to slides that are out of sight. One can be picked up while another chord
  is being typed, which is kept.
- **Typing is helped along.** A letter from A to G opens a bubble to type the chord in.
  It takes only what could be a chord, and offers what it might be going to be: the
  chords the song already has, then the key's own, then the usual kinds on that note,
  and after a slash the likeliest bass notes. Down and Up pick one.
- **Nothing has to be pressed to say a chord is done.** While one is being typed the
  spot goes on following the pointer. Click the next place, and the chord is kept and
  the bubble opens there: click, type, click, type, down the song. Enter keeps a chord
  too, and Tab keeps it and moves on to the next word. Only Esc throws one away. A
  bubble emptied and kept so, by Enter or a click, takes off the chord that was there.
- **A line under the sheet says what the keys do, where you are.** On a chord: that
  Delete takes it off, 1 to 7 change it, it can be dragged, and a click types over
  it. On a word with none: 1 to 7, or a letter. While a chord is typed: Enter, Tab,
  Up and Down, Esc. While one is carried: where letting go puts it, and that Ctrl
  leaves a copy. Over the strip: that its chords can be dragged down. And with **Ctrl**
  held, how the lit line's chords are copied and pasted, and whether a paste there
  would replace chords; where the window is too narrow for that side by side, it goes
  on two lines. It is blue, to be seen, and lights up for a moment when what it says
  changes. Under it, the line along the bottom says how much there is of the song
  ("24 slides, 94 chords."), and for a few seconds what was just done.
- **The line you are at is lit.** The words of the line the pointer is on are drawn
  in orange, and so are those of the line a chord is being typed on, wherever the
  pointer has gone. It is the line Ctrl+C copies from and Ctrl+V pastes to. Over the
  bare sheet between the slides, or off the sheet, no line is lit; the arrow keys
  light the spot's line again. A line of chords with no words has its rule lit
  instead, twice as thick.
- **Sharps and flats are drawn as ♯ and ♭**: F♯m7, B♭, C7♭9, E♭/G, in the bubbles,
  the strip, what is offered and the bubble a chord is typed in (where F# becomes F♯
  as the # is typed), and in the song's two keys. Only those that are clearly a sharp
  or a flat: the root's and the bass's, and one before a digit. It is only how they
  are drawn: the song's file, what is copied and the ChordPro text keep `#` and `b`.
- **Chords are things to pick up.** Drag one to another word, on any slide; hold Ctrl
  to leave a copy behind. A name that is no chord, which some files have ("N.C."), is
  picked up, copied and pasted like any other; only what is newly typed has to be a
  chord.
- **A verse's chords go onto the next verse in one go.** Ctrl+C copies the chords of a
  line, or with Shift of a whole group, and Ctrl+V puts them on other lines word for
  word. A paste says what came of it, on the line along the bottom: how many chords
  went on how many lines, how many had no word to go on, and why a line was passed
  over (chords copied from a line of words go only onto a line of words). And since
  the natural thing to do before pasting onto a line is to click it, a click on a line
  and then Ctrl+V pastes there: the bubble the click opened gives way, so long as
  nothing has been typed in it and nothing has been copied anywhere else since.
- **As much of the song in sight as the window has room for.** The slides go across as
  well as down, and the sheet zooms. Every card of a song is one width, that of its
  widest slide and no wider, and the shape of a screen, though never taller for that
  than four lines need; a row of the grid is taller only where a slide of it has more
  lines. No room is kept for what might be put on a line later: a line that comes to
  need more, for a chord at its end, is given it then. The empty lines a text box's
  words end with get no row. The **−** and **+** at the right of the strip make the
  words smaller and larger, in nine steps from half the size to half as large again;
  so do Ctrl with minus and plus, Ctrl with the wheel, and two fingers spread or closed
  on a trackpad. Ctrl+0 is the usual size, which is eight tenths. The size is kept
  from one start of the app to the next.
- **A slide with no words takes chords too.** An intro, an instrumental or an ending
  is often a slide with nothing on it. It is on the sheet as a card with one empty
  line: point at it and put chords on, one after another, as anywhere else. They are
  kept as Multitracks keeps the chords of an intro, on characters that do not show, so
  the slide shows no more than it did.
- **And nothing is added to a slide that is not needed.** A slide with no words is not
  touched until a chord is put on it. The characters its chords hang on go into the
  text box it has, empty as that is; only a slide with no text box at all is given
  one, a copy of the song's own, set and placed as the song's words are. A change of
  mind leaves nothing behind: take the chords off again while the song is still open
  in the editor, and the slide's file is exactly what it was, an added text box gone
  with them.
- **A line of chords with no words that loses its last chord takes chords again.** It
  is left one character that does not show, and is still on the sheet with the spot on
  it. (It was left with none, and under a verse such a line was then gone for good but
  for undo.) Where the chords of such a line are only renamed, or put in another
  order, nothing of the text box's words is written; and a chord more or fewer changes
  the line's own characters in the file and leaves the rest of the text box as
  ProPresenter wrote it, byte for byte.
- **Nothing is lost.** Every change is saved at once, into the song's own file, and
  Ctrl+Z takes it back. The words are never changed here, only the chords.

All of it, key by key:

| Do this | And |
| --- | --- |
| 1 to 7 | The key's own chord on that note of its scale goes on the spot: in C, 1 is C, 4 is F, 6 is Am. The strip along the top shows them, and they can be clicked there |
| Drag one of the strip's seven | Puts it on the word it is let go on, on any slide, where the lit bubble shows; with Shift held, on the start of the word. Let go anywhere else, or with Esc pressed, nothing happens |
| A to G | Starts a chord by name. Only what could be a chord can be typed, and over it are the chords it might be going to be: the song's own first, then the key's, then the usual kinds |
| Down, Up | Pick one of those; Right puts it in the box to go on from (for a bass note, say) |
| Enter | Keeps the chord. Tab keeps it and moves the spot on to the next word. Esc throws it away. A bubble emptied and kept takes off the chord that was there |
| Click | On a place: type a chord there, or change the one that is there. While a chord is being typed, a click on another place keeps it and starts on the next; a click anywhere else on the sheet keeps it too |
| Drag a chord | Moves it to another word, on any slide. With Ctrl held it is copied |
| Delete | Takes off the chord at the spot |
| Right, Left | The spot goes a word along its line, and from the end of a line on to the next line of the song |
| Down, Up | The spot goes to the line under or over it on the screen: from a slide's last line, to the slide below it in the grid (the next slide of the song is beside it, and Right goes there) |
| Shift | Under the pointer, the spot keeps to the starts of words. With Right and Left, it goes a letter at a time |
| Ctrl+C, Ctrl+Shift+C | Copies the chords of the spot's line, or of its whole group |
| Ctrl+V | Puts them on the lines from the spot's on, word for word: a second verse gets the chords of the first |
| Ctrl+Z, Ctrl+Shift+Z | Undo and redo |
| Ctrl with −, + and 0 | The words a step smaller, a step larger, and their usual size. Ctrl with the wheel does the first two |

**The song's two keys** are in the toolbar, over both chord editors. **Key** says what
key the chords are written in, which is what 1 to 7 go by and what the stage transposes
from; it changes no chord. A song that names no key shows **None**, and is taken to be
in C until one is set. **Shown in** is the key ProPresenter shows the song in, moving
the chords there itself, and the key this app's own stage starts from. It follows the
first while the two are the same; where someone has had a song shown in another key, a
tone lower for a singer say, saying what key its chords are written in leaves that
alone. A change of key is undone like any other change.

**ChordPro** is the same song as text, each chord in square brackets where it is played:
`[G]Amazing [C]grace`. Chords are typed, changed and deleted among the words, and the
words themselves cannot be typed over. `[` brings its `]` with it, and Backspace on a
bracket takes the whole chord. It is for someone who thinks in ChordPro, or has a chart
to copy from. A line of chords with no words is its chords with spaces between,
`[C] [G] [Am]`, and a slide with no words is an empty line, which chords can be typed
on: the one after the empty line that parts it from the slide before. (The two look
alike. With the caret on the slide's own, that slide is the one picked in the list at
the side, and what is typed on the other does not stay.) What is typed is saved a
moment after the typing stops, to the text boxes that were typed in and no others, and
all of one saving is one thing to undo. A name the song already has that is no chord
is left as it is. The two letters **A** over the text, and Ctrl with minus, plus and 0,
change its size, which is kept apart from the sheet's.

**Nothing is saved by hand**, and under the two chord editors the line along the bottom
says so: **Autosave on**, where a Save button would be. Beside it is **Revert…**, which
makes the song what it was when it was opened in the editor, taking back every change
made since, of whatever kind, whichever of the three ways of working made it. It asks
first, since its name alone could be taken for Undo's. The song as it was just before
is kept as a copy with the others the editor keeps, and for a few seconds the line
offers **Undo**, which puts it back.

**The colour of a chord** in each of the two editors can be chosen in the settings,
under **Chords**, and so can that of the sheet's lit line, each with a way back to the
colour it came with; and the same section has the switch for drawing sharps and flats
as signs, on as it comes. They are only how the editors draw a song. Nothing in a
song's file says any of them: the stage shows chords in the colour its layout gives
them, with `#` and `b`.

### Importing a ChordPro file

The **+** beside Libraries has **Import ChordPro File…**, which makes a new song of a
ChordPro file in the library that is open: a group for each part the file marks
(verses, choruses, a bridge, or a line that only says "Verse 2:"), two lines of words
to a slide unless another number is asked for, the chords over the words, and the
song's key. The same **+** has **New Presentation…** and **New Library**.

### What is a guess, or not done

- How the chords are kept was worked out from songs ProPresenter imported from
  Multitracks, and what is written here is in the same form, down to the stretch of
  characters a chord is written over: up to the next chord or the end of its line
  where there are words, and its own character and no more where there are none.
  (Counted in 22 of those songs: all 290 chords with no words are written so, and all
  but 60 of the 1,009 over words, the 60 being in two songs that another program had
  saved since.) A song with chords put on here has not been opened in ProPresenter
  yet; nor has a song imported here.
- Chords put on a slide with no words are hung on characters written in that text
  box's own font and size, which the file keeps for an empty text box too. That
  ProPresenter then shows the slide's chords as it shows a Multitracks intro's is a
  guess until it has been seen there. Whether anything in ProPresenter that asks if a
  slide has text counts such characters is not known either: a slide from Multitracks
  with chords and no words has them just the same.
- A text box added for the chords of a slide that had none is a copy of the nearest
  text box of the song that has words, with those words taken out. That ProPresenter
  is content with it is a guess too. (A song with no text box anywhere gets one as
  the **T** in Slides makes one.) What was made for a slide's chords is only unmade
  while the song is open in the editor: chords taken off another day leave the text
  box, with one character in it that does not show.
- A line of chords with no words that has lost its last chord is left one zero-width
  space, whatever its chords hung on, as the Mac and Windows editors leave it. That
  ProPresenter makes nothing of such a line is a guess: a line of the kind with chords
  on it has been seen there, and one with none has not.
- A chord more or fewer on a line of chords with no words changes that line's own
  characters where they stand in the text box's RTF, and not another byte of it, as
  the Mac and Windows editors do it and by the same rule, held to the same cases
  (`src/rtfstandins.h`). What comes of it is read back before it is kept. Where that
  cannot be done cleanly, the text box's words are written again whole, as this app
  writes text, and the log says so; the other two editors, which have no writer of
  RTF, refuse there instead. One kind of RTF is known to want that: one that has a
  character follow each numbered one to stand in for it, which Apple's text system
  never writes. What ProPresenter on Windows writes is not known.
- With such a line made longer or shorter, what its text box says of a range of its
  words and is not a chord ("these are in capitals", a gradient over some of them)
  has its range go with the words: one that ended with the line or went on past it
  still does, and one over all the words is over all of them still. That is what a
  text system does when characters are typed after others, and what ProPresenter is
  taken to do. The other two editors have done the same since 2026-10-13; until then
  they left such ranges as they were. It matters in real songs: of 164 text boxes
  with a line of chords alone in the libraries looked at, 96 say of all their words
  that they are in capitals, and with a chord taken off, that range left alone would
  end past the last of the words.
- The first chords of a slide with no words are still written with this app's own
  writer, in the font the text box keeps for what is typed into it. The other two
  editors take the opening of a neighbouring text box's RTF instead.
- A song whose file names the key its chords are written in and not the one it is
  shown in is taken as shown in its own. No file looked at is like that.
- Two fingers spread or closed on a trackpad are meant to zoom the sheet, as on the
  Mac. No test can make the gesture and it has not been tried by a hand.
- Numbers, numerals and Do Re Mi are written the way musicians write them.
  ProPresenter does not say exactly what it writes, so they may differ in the corners.
- A song imported or chorded here is not marked as a Multitracks song, which is a mark
  for songs licensed from them.
- The chord editors do not change words; the Slides editor does, and the chords of
  the rest of the text box stay on their words when it does.
- ProPresenter's other chord thing, a chord chart shown as a picture, is not shown.
