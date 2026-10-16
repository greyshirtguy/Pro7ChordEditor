# The song, and saving it

Between the file and the two editors there is one thing: the song that is open. It
reads the file, hands the editors the words and chords, takes their changes, saves
each one at once, and can undo them. Neither editor touches a file.

In Simple Presenter this is `PresentationEditor`
(`reference/simple-presenter/src/presentationeditor.cpp`: see `song`, `usedChords`,
`setChords`, `setChordsAlone`, `key`, `setKey`, `undo`, `redo`). That class also edits
slides, props and stage layouts, none of which is wanted here: take the six chord
functions and the way it changes, saves and undoes, and leave the rest.

Build it as plain code with no window in it, and test it on copies of
`testdata/files` in a temporary folder.

## What the editors are handed

`song()`: a list with one entry for each text box that holds the song's words
([01-file-format.md](01-file-format.md) says which boxes those are), in the order of
the slides and, on a slide, of its boxes:

| | |
| --- | --- |
| slide | Which slide it is on, counted from 0 in the order the slides are shown |
| group | The name of the slide's group, or nothing |
| group colour | The group's colour, if the file gives it one |
| starts its group | Whether this slide is the first of its group |
| element | The text box's id, by which a change names it, with its slide. **It is not the only one of its name in a song**: a slide made by duplicating another keeps its boxes' ids, and in one real song the boxes of 27 slides had one id between them. A change names the slide as well for that reason, and an editor must never tell two boxes apart by id alone |
| words | Its plain text |
| chords | Its chords in order, each a place along the words and a name |
| blank | Whether it is a blank slide's box: no words, no chords, and handed over only so that chords can be put on it ([01-file-format.md](01-file-format.md)). Its `element` is empty if the slide has no text box yet. **New** |

`usedChords()`: every chord's name in song order, one for each use. It is what the
typing bubble's offers go by.

`testdata/files/expected/as-read-by-simple-presenter.json` has this for every test
file, the blank slides of Blank Slides among them. (Its last slide has no text box at
all, and is there with an empty `element`: a chord put there gives the slide a box,
[01-file-format.md](01-file-format.md), "A slide with no text box".)

A group's colour: the file's cue group has one if someone set it in ProPresenter.
Most have none, and ProPresenter then colours the group by its *name*, from a list it
keeps for the whole library (`Configuration/Groups` in its folder, an
`rv.data.ProGroupsDocument`). Reading that list is a nicety for later; without it, a
group with no colour of its own is a neutral grey.

## What the editors may ask

Each of these changes the song, **saves it at once**, and answers with nothing or with
a sentence saying what went wrong. Each takes a flag, *joined*, that makes it one step
of undo with the change before it.

1. **Set a text box's chords**: the slide, the element, the whole list of its chords.
   Names are tidied (`tidied`); the list is tidied (`tidy`); the box's chord entries
   are replaced ([01-file-format.md](01-file-format.md), Writing a box's chords).
2. **Set a line of chords alone**: the slide, the element, which line of its words, the
   names in order. The line's text becomes that many stand-ins and its chords go on
   them; the chords of the lines after it move with their words
   ([01-file-format.md](01-file-format.md), Chords with no words). For a blank slide
   it names the slide's box and line 0, or no box at all (an empty `element`) if the
   slide has none, and the slide is given its words, or a box
   ([01-file-format.md](01-file-format.md), Chords onto a blank slide).
3. **Set the key**: see below.

If the text box named has gone (the file was changed by something else since), the
answer says so and nothing is changed. The same if what is named is on the slide and
has no text at all: chords are not put on a shape.

A request that would change nothing saves nothing, keeps no copy, and is no step of
undo. **What is nothing is judged by the file**: a request has changed something when
the slide it was made of would be written otherwise than it was, though no chord is
other than it was. One such case is known. A text box with no settings written at all
(its `attributes`, in which its chords are kept; ProPresenter has written them for
every text box seen so far), asked by request 1 or 2 to have no chords, is given an
empty set of settings: two bytes more in the file, which read as none does. That is a
change. It is saved, a copy is kept, and it is a step of undo; asked again, it is
nothing. (So the reference has it: its `writeChords` makes the settings before
anything else, and it saves whenever a slide's bytes are other than they were. The
Mac app did the same from the first. The Windows app left such a box alone until
2026-10-14, when the maintainer, told that it alone parted from the reference, said:
"yes change windows to match".)

Request 1 tidies every name it is handed, those of chords that were there before among
them, since an editor hands back the whole box. So a chord in a file whose name is not
as `tidied` would have it ("g", or a name with a space at its end) is written tidied
the first time anything in its box is changed. None of the 1,299 chords in the songs
looked at has such a name.

Request 2, to the letter:

- The names are tidied (`tidied`), and one that is then nothing is left out: it gets
  no stand-in, as it would get no entry.
- Only a line of chords alone is made stand-ins. A line that has words, and an empty
  line, are refused.
- **With no names the line is one zero-width space**, whatever it hung its chords
  on, and is a line of chords alone still, with no chord on it: one can be put on
  it again ([01-file-format.md](01-file-format.md), Chords with no words).
- Where the line is those stand-ins already (its chords renamed, or put in another
  order), only the chord entries change, and not a byte of the RTF.
- A chord that stands on the line, or on the line break that ends it, is one of the
  line's, and is replaced by the names. One before the line stays; one after it
  moves by as much as the line grew or shrank.
- Where the line's text is changed, what the box says of a range of its words that
  is not a chord has the ends of that range go with the words
  ([01-file-format.md](01-file-format.md), The box's other ranges).
- No names for a blank slide is nothing to do, whether it has a box or none: nothing
  is written, and nothing is made.
- An empty `element` for a slide that is not a blank slide with no box (it has a box
  by now, with words or without) is refused: "That slide is no longer blank."
- The last chord taken off a blank slide that was given its chords while the song
  has been open unmakes what was made for them
  ([01-file-format.md](01-file-format.md), A change of mind), and that comes first:
  nothing is kept, and the slide is blank again, as the file had it.

It is one step of undo like any other, whatever it wrote: the RTF, a box.

## The key

The file has two keys ([01-file-format.md](01-file-format.md)): the one the chords are
written in, *original*, and the one ProPresenter shows them in, *user*.

4. `key()` is the original key, as a musician writes it, or nothing if the song states
   none.
5. **Setting the key** (the sheet's picker) sets the original key. It changes no
   chord: it says what key the chords that are there are in.
6. With it, the user key is set to the same, **if** the song had no key, or its two
   keys were the same. If they differed, someone chose to show the song in another key
   and that is left alone. (A `music` with an `original` and no `user` in it is taken
   as two keys the same: nobody chose another. **A guess**: no song looked at has one.)
7. **New: a second picker, "ProPresenter shows it in"**, sets the user key alone. The
   old Windows editor had the two side by side, and people use it to have ProPresenter
   transpose a song for them. Put it in the song's header beside the first, quieter.
   (Simple Presenter had no such picker up to its version 0.88: it set both together,
   always, and let its own show choose a key for the night. It has one since, "Shown
   in", beside its "Key".) For a song that states no key it is refused:
   a `music` made with only a `user` in it would read as a song in A flat. The person
   is told to set the song's key first.
8. The editors never draw chords in the user key. They show what is in the file.

A change of key is a step of undo like any other.

## Saving

9. Every change is saved the moment it is made. There is no Save button and no
   "unsaved" state. This is what the editors were built round, and with a copy of the
   file kept first ([07-backups.md](07-backups.md)) and undo to hand, it is safe.
10. A save is refused, and the change not made, if
    - ProPresenter is running ([06-library.md](06-library.md), rule 16), and changes
      are not allowed while it is (rule 20 there), or
    - the file on disk is no longer the one that was read (its size or its time of last
      change differs): something else has changed it. The person is told, and offered
      the song read again. Its bytes are compared as well, with what was last read from
      it or written to it: a file brought down by a syncing service can come with its
      old time on it, and a song is small.
    - no way to save was given at all: a song opened only to be looked at.

    They are asked in that order, and before the copy of rule 12 is made, so that a
    refusal leaves no copy behind it.
11. A save writes the whole file beside itself and swaps it in, then reads it back
    ([01-file-format.md](01-file-format.md), Writing). If any of that fails, the song
    in memory is put back as it was before the change and the answer says what
    happened. The file is then whatever it was.
12. Before the first save to a file since it was opened, a copy of it is kept
    ([07-backups.md](07-backups.md)). If the copy cannot be made, the save is refused.

## What a refusal says

Whatever is refused changes nothing, in the file or in the song as the editors see it,
and answers with one of these, to be shown as it is. Both apps say the same.

| When | The sentence |
| --- | --- |
| The file cannot be read at all | *name* could not be read as a ProPresenter presentation: *what the system says of it* |
| The file is read and is no presentation | *name* could not be read as a ProPresenter presentation: it is not a presentation, or it is damaged. |
| A copy to be put back is no presentation | That copy could not be read as a ProPresenter presentation: it is not a presentation, or it is damaged. |
| The song was opened only to be looked at | This song was opened only to be looked at. Nothing was changed. |
| No slide of that number | That slide is no longer in the song. Nothing was changed. |
| No text box of that id on the slide | That text box is no longer on the slide. Nothing was changed. |
| No line of that number in the box | The text box has no such line. Nothing was changed. |
| What was given is no key | “*key*” is not a key. Nothing was changed. |
| The shown-in key, for a song with no key | This song does not say what key it is in. Set its key first, and then the key ProPresenter is to show it in. |
| A line of chords alone is asked for, and the line has words or is empty | That is not a line of chords with no words. Nothing was changed. |
| The stand-ins could not be written into the box's RTF and read back as meant | A chord could not be added to this line, or taken from it: its text box is written in a way that cannot be changed here safely. Nothing was changed. |
| A chord for a blank slide, in a song with no box that has words to copy, or to take an opening from | This song has no text box with words in it to model one on, so chords cannot be put on a slide that has none. Nothing was changed. |
| A chord for a slide with no text box, that has one by now | That slide is no longer blank. Nothing was changed. |
| ProPresenter is running | ProPresenter is running. Quit it to change chords here. Nothing was changed. |
| The file was changed by something else | This song's file has been changed by something else since it was opened here. Nothing was changed. Open the song again to see it as it is now. |
| The copy could not be kept | A copy of the song's file could not be kept first, so the song was not changed: *why* |
| The file could not be written, or did not read back as written | The song could not be saved, and is as it was: *why* |

## Undo

13. The song keeps a history of its changes for as long as it is open: for each, the
    part of the file that changed as it was before and as it was after. (Simple
    Presenter keeps the one cue, serialised. Keeping the whole message would do as
    well: songs are small.)
14. **Undo** puts the last change back as it was before and saves; **redo** does it
    again. A step that was joined to the one before it is undone with it, in one go.
15. A new change throws away what could have been redone.
16. The history is forgotten when another song is opened. What was done before that
    can still be got back, as a whole file, from the copies kept.

## Proofs to write

- Every test file read gives what `as-read-by-simple-presenter.json` says.
- **Read and written with nothing changed, a file is the same file.** Try this first,
  byte for byte, on every file in `testdata/files`. Both libraries write fields in the
  order of their numbers, as the program that made these files did, so it should hold.
  If it does not for some file, do not shrug: find out which field moved and why, and
  say. (For a file from ProPresenter itself the bytes may well differ and the content
  not; then compare the two as decoded messages. `protoc --decode` of each, compared
  as text, is the plainest way.)
- A change and its undo leave the same file.
- A change to one box's chords leaves every other field of the file as it was: decode
  before and after, and the only lines that differ are that box's chord entries.
- The same chords put on through request 1 as Simple Presenter's sheet asks for them
  (`sheet-model.json` has what it asks) give the ranges `ranges.json` says. (With each
  name tidied, which request 1 does and `ranges` does not: one case of that file has
  "g" and "f#m/a" in it. And only those cases whose text is the words of a box in a
  test file can be tried on a file: ten of the twenty.)
- The slides in the order of their groups, and a box's chords in order of place,
  where the file has them otherwise. No test file does: make one.
- A save with the file changed underneath it, and one with the copy impossible to
  make, are refused and leave everything as it was.
