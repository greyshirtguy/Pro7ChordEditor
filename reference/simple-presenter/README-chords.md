<!-- The part of Simple Presenter's README that describes its chord features to the person using it, as it is
     after the change that lets a slide with no words take chords (after version 0.82).
     Kept here as it was, for how the editor is explained in plain words. Its links go to that repository. -->

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

![A stage layout in the editor: the text box that shows the current slide's words is picked, with the live song's chords drawn over them, and at the foot of its Text tab are Chords, with Show ticked and a colour, and As, set to Chords](https://raw.githubusercontent.com/greyshirtguy/Simple-Presenter/0.82/docs/stage-layout-chords.png)

**The key.** A song's chords are written in the file in its original key. Over the
slides of a song that has chords there is a **Key** to pick, starting at the key the
file says the song was last shown in, and the stage shows the chords in the key picked.
Picking a key changes nothing in the file and lasts while the app is open.

### The chord editor

With a presentation in the editor, the buttons at the right of its toolbar give three
ways of working: **Slides** (the editor as it always was), **Chords** and **ChordPro**.
The last two are for a song's chords, and are this app's own: ProPresenter shows the
chords a song came with and has no way to put one on.

Putting chords on a song by hand is usually a chore. **Chords** was made so that it is
not, and it has turned out to be the part of this app that is most fun to use. The
whole song is one sheet, each slide's lines in a card under the name of its group,
because chording a song a slide at a time would be slow going. (Each chord is still
kept with its own slide's words, in ProPresenter's file.)

| Point at a letter, and press 1 to 7 | Or type a chord, and take what is offered |
|:---:|:---:|
| <img src="https://raw.githubusercontent.com/greyshirtguy/Simple-Presenter/0.82/docs/chords-point.png" width="420" alt="The top of the chord editor: the key, G, with its seven chords numbered 1 to 7 beside it, and under them the first verse in its card, with a bubble in outline over the word the pointer is on"> | <img src="https://raw.githubusercontent.com/greyshirtguy/Simple-Presenter/0.82/docs/chords-type.png" width="420" alt="The same card with a bubble open over a word, D and a slash typed in it, and over that the bass notes it might be going on to: D/F#, D/A, D/G, D/B, D/C and D/E"> |
| **Drag a chord to where it belongs** | **Or type the lot as ChordPro** |
| <img src="https://raw.githubusercontent.com/greyshirtguy/Simple-Presenter/0.82/docs/chords-drag.png" width="420" alt="The same card with the chord D being dragged from one word to another: faint where it was, and bright where it would land"> | <img src="https://raw.githubusercontent.com/greyshirtguy/Simple-Presenter/0.82/docs/chordpro-type.png" width="420" alt="The ChordPro editor: the same song as text, its groups in curly brackets and its chords in square brackets in orange, one of them just typed"> |

What makes it quick:

- **A spot shows where the next chord will go.** It is a bubble in outline that follows
  the pointer from letter to letter (and the arrow keys from word to word), with its
  tail on the letter the chord will stand over. Any letter will do: chords fall in the
  middle of words as often as at their start.
- **Most chords are one key.** The song's key is at the top of the sheet with its seven
  chords beside it, numbered. Point at a letter and press **4**, and the key's fourth
  chord is there: in G, that is C. Most of most songs is those seven.
- **Typing is helped along.** A letter from A to G opens a bubble to type the chord in.
  It takes only what could be a chord, and offers what it might be going to be: the
  chords the song already has, then the key's own, then the usual kinds on that note,
  and after a slash the likeliest bass notes. Down and Up pick one.
- **Nothing has to be pressed to say a chord is done.** While one is being typed the
  spot goes on following the pointer. Click the next place, and the chord is kept and
  the bubble opens there: click, type, click, type, down the song. Enter keeps a chord
  too, and Tab keeps it and moves on to the next word. Only Esc throws one away.
- **Chords are things to pick up.** Drag one to another word, on any slide; hold Ctrl
  to leave a copy behind.
- **A verse's chords go onto the next verse in one go.** Ctrl+C copies the chords of a
  line, or with Shift of a whole group, and Ctrl+V puts them on other lines word for
  word.
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
- **Nothing is lost.** Every change is saved at once, into the song's own file, and
  Ctrl+Z takes it back. The words are never changed here, only the chords.

All of it, key by key:

| Do this | And |
| --- | --- |
| 1 to 7 | The key's own chord on that note of its scale goes on the spot: in C, 1 is C, 4 is F, 6 is Am. The strip along the top shows them, and they can be clicked there |
| A to G | Starts a chord by name. Only what could be a chord can be typed, and over it are the chords it might be going to be: the song's own first, then the key's, then the usual kinds |
| Down, Up | Pick one of those; Right puts it in the box to go on from (for a bass note, say) |
| Enter | Keeps the chord. Tab keeps it and moves the spot on to the next word. Esc throws it away |
| Click | On a place: type a chord there, or change the one that is there. While a chord is being typed, a click on another place keeps it and starts on the next; a click anywhere else on the sheet keeps it too |
| Drag a chord | Moves it to another word, on any slide. With Ctrl held it is copied |
| Delete | Takes off the chord at the spot |
| Shift | Under the pointer, the spot keeps to the starts of words. With the arrow keys, it goes a letter at a time |
| Ctrl+C, Ctrl+Shift+C | Copies the chords of the spot's line, or of its whole group |
| Ctrl+V | Puts them on the lines from the spot's on, word for word: a second verse gets the chords of the first |
| Ctrl+Z, Ctrl+Shift+Z | Undo and redo |

The **Key** at the top left of the sheet says what key the chords are written in, which
is what 1 to 7 go by and what the stage transposes from. A song that names no key is
taken to be in C until one is set.

**ChordPro** is the same song as text, each chord in square brackets where it is played:
`[G]Amazing [C]grace`. Chords are typed, changed and deleted among the words, and the
words themselves cannot be typed over. `[` brings its `]` with it, and Backspace on a
bracket takes the whole chord. It is for someone who thinks in ChordPro, or has a chart
to copy from. A line of chords with no words is its chords with spaces between,
`[C] [G] [Am]`, and a slide with no words is an empty line, which chords can be typed
on: the one after the empty line that parts it from the slide before. (The two look
alike. With the caret on the slide's own, that slide is the one picked in the list at
the side, and what is typed on the other does not stay.)

### Importing a ChordPro file

The **+** beside Libraries has **Import ChordPro File…**, which makes a new song of a
ChordPro file in the library that is open: a group for each part the file marks
(verses, choruses, a bridge, or a line that only says "Verse 2:"), two lines of words
to a slide unless another number is asked for, the chords over the words, and the
song's key. The same **+** has **New Library**.

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
  while the song is open in the editor: chords taken off another day leave an empty
  text box where one was added.
- Numbers, numerals and Do Re Mi are written the way musicians write them.
  ProPresenter does not say exactly what it writes, so they may differ in the corners.
- A song imported or chorded here is not marked as a Multitracks song, which is a mark
  for songs licensed from them.
- The chord editors do not change words; the Slides editor does, and the chords of
  the rest of the text box stay on their words when it does.
- ProPresenter's other chord thing, a chord chart shown as a picture, is not shown.
