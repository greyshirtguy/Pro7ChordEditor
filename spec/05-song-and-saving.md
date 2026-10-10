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
| element | The text box's id, by which a change names it |
| words | Its plain text |
| chords | Its chords in order, each a place along the words and a name |
| blank | Whether it is a blank slide's box: no words, no chords, and handed over only so that chords can be put on it ([01-file-format.md](01-file-format.md)). **New** |

`usedChords()`: every chord's name in song order, one for each use. It is what the
typing bubble's offers go by.

`testdata/files/expected/as-read-by-simple-presenter.json` has this for every test
file, the blank slides of Blank Slides among them. (Its last slide has no text box at
all, and is there with an empty `element`: that one is Simple Presenter's own, and an
app that makes no text boxes hands nothing over for it.
[01-file-format.md](01-file-format.md), "Where Simple Presenter goes further".)

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
   ([01-file-format.md](01-file-format.md), Chords with no words).
3. **Set the key**: see below.

If the text box named has gone (the file was changed by something else since), the
answer says so and nothing is changed. The same if what is named is on the slide and
has no text at all: chords are not put on a shape.

A request that would change nothing saves nothing, keeps no copy, and is no step of
undo.

Request 1 tidies every name it is handed, those of chords that were there before among
them, since an editor hands back the whole box. So a chord in a file whose name is not
as `tidied` would have it ("g", or a name with a space at its end) is written tidied
the first time anything in its box is changed. None of the 1,299 chords in the songs
looked at has such a name.

Request 2, until M7 is built, is done only where the line has as many stand-ins as
there are names (its chords renamed or put in another order), and is otherwise refused.

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
   (Simple Presenter has no such picker: it sets both together, always, and lets its
   own show choose a key for the night.) For a song that states no key it is refused:
   a `music` made with only a `user` in it would read as a song in A flat. The person
   is told to set the song's key first.
8. The editors never draw chords in the user key. They show what is in the file.

A change of key is a step of undo like any other.

## Saving

9. Every change is saved the moment it is made. There is no Save button and no
   "unsaved" state. This is what the editors were built round, and with a copy of the
   file kept first ([07-backups.md](07-backups.md)) and undo to hand, it is safe.
10. A save is refused, and the change not made, if
    - ProPresenter is running ([06-library.md](06-library.md)), or
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
| One more or fewer chords on a line of chords alone, before M7 | A chord cannot yet be added to a line that has no words, or taken from one. Its chords can be renamed. Nothing was changed. |
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
