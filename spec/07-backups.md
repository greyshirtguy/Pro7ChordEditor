# The copies kept

Before the app changes a file it keeps a copy of it as it was, out of sight, and can
put any copy back. That is what makes saving every change at once a safe thing to do
to somebody's Sunday library.

**New.** Simple Presenter keeps a copy of a file the first time its editor changes it
(under "Edit Backups" in its own data), and has no way to look at them or put one
back. (Since its version 0.89 it has one way back, taken from these pages: Revert,
which puts the song being edited back as it was when it was opened.) This is that
idea finished. Nothing here has been built or tried.

## When a copy is made

1. **Before the first save to a file since it was opened** in the app, the file as it
   is on disk is copied. One copy for a sitting with a song, not one for each chord.
2. **Before a copy is put back**, the file as it is then is copied
   ([Getting one back](#getting-one-back)).
3. **Before a mending** ([08-repair.md](08-repair.md)), likewise.
4. If the newest copy kept of that file is byte for byte what is about to be copied,
   no second one is made. Only the newest is held against it: a copy the same as an
   older one is made all the same, so that the copies read as what happened, in order.
5. If the copy cannot be made (the disk is full, the folder cannot be written), the
   change that wanted it is refused and the person is told why. The app never changes
   a file it could not first copy.
6. A copy is the file's bytes, unchanged. It is not read, not parsed, not written
   through the app's own code.

## Where, and under what name

7. In the app's own data ([06-library.md](06-library.md)), in a folder `Backups`, in
   folders that follow where the file was: the place's name
   ([06-library.md](06-library.md), rule 3: a workspace's name, "Documents", or the
   name of a folder chosen by hand), then the library's name, then the file's name
   without its ending, as a folder. So the copies of `Songs/Amazing Grace.pro` in the
   workspace "Sunday" are in `Backups/Sunday/Songs/Amazing Grace/`. (The place was
   added when the Mac app was built: since there can be several places, two of them
   can each have a library called "Default".) A name is made fit to be a folder's:
   `/`, `:`, `\` and line ends in it become `-`.
8. Each copy is named for when it was made, to the second, in the computer's own time:
   `2026-10-11 09.14.03.pro`. A second made in the same second (which rule 4 lets happen
   only if its bytes are others) has ` 2` after the time, a third ` 3`. A copy is never
   written over one that is there. Beside the copies is `index.json`, a list with an
   entry for each: its file name, when it was made, **why** (`before a change`, `before
   putting back a copy`, `before mending`), the whole path of the file it is a copy of,
   the presentation's id and name as they were inside it, how many chords it had, its
   key, its size, and the app's version.
9. The index is a help, not the truth: if it is lost or damaged the copies are still
   there, named by their time, and the app lists them from the folder: every file in
   it that is named as a copy is one. Of a copy the index does not know, when it was
   made is read from its name, and its song's name, chords and key from the copy
   itself; why it was made, and of which file, are then not known. If the index
   cannot be written, the copy is kept all the same.
10. A song is known by where it is: its place, its library and its file name. A song
    renamed or moved in ProPresenter starts afresh, and its earlier copies stay under
    its old name, where **All Copies** (rule 18) finds them.

## How long

11. The oldest copy of a file, the file as it was before this app ever touched it, is
    **never** thrown away.
12. Of the rest, everything from the last thirty days is kept, and the twenty newest
    whatever their age. Older ones beyond that go, when the app starts. Nothing is
    thrown away but files in a song's folder of copies that are named as copies, and
    the index is then written again without them.
13. Songs are small: tens of kilobytes. A library of a thousand songs, each changed on
    twenty days, is some hundreds of megabytes. Say in the settings how much the
    copies take, with a button that opens their folder.

## Seeing them

14. A song that has copies has a mark in the list ([06-library.md](06-library.md)).
15. The song's header has **Earlier Versions**. It opens a panel over the editor:

    ```
    Earlier versions of "Amazing Grace"
    +------------------------------------+-------------------------------------+
    | Today 09:14     before a change    |   the song as it was then, on the   |
    | Sunday 4 Oct    before a change    |   chord sheet, to look at and not   |
    | 12 Sep          before mending     |   to change                         |
    | 2 Aug  (the first) before a change |                                     |
    +------------------------------------+-------------------------------------+
    | 31 chords, in G.  Now: 34 chords, in G.            [ Put This One Back ] |
    +---------------------------------------------------------------------------+
    ```

16. Picking one shows it at the right as the chord sheet draws it, not to be changed,
    and under it one line that sets it against the song as it is now: how many chords
    each has, and their keys ("11 chords, in G.  Now: 8 chords, in E."). The newest is
    picked when the panel opens. (Until the chord sheet is built, M6, a copy is shown
    as a song is in the window: its lines, with chords in brackets.) When a copy was
    made is said as "Today 09:14", by the day's name for the six days before
    ("Sunday 4 Oct"), "12 Sep" earlier in the year, and "2 Aug 2025" before that.
17. Esc, or a click outside, puts the panel away.
18. The settings have **All Copies**: every song that has copies, by place and
    library, those whose song is no longer where it was included (and said to be so:
    the file its newest copy says it is a copy of is not there). From there a copy can
    be looked at, or saved somewhere as a file: its bytes, unchanged. It also says
    how much room the copies take (rule 13) and shows their folder. Nothing is put
    back from there: that is done from the song (rule 19), where the song is open.

## Getting one back

19. **Put This One Back** makes the song's file that copy again. It is a save, and so
    is refused while ProPresenter is running, and only done if the file on disk is the
    one the app last read ([05-song-and-saving.md](05-song-and-saving.md), rule 10).
20. First the file as it is now is copied (rule 2). Then the copy chosen is written
    beside the file and swapped in ([01-file-format.md](01-file-format.md), Writing),
    and read back.
21. The song is opened afresh from the file. Its history of undo is empty: what was
    put back is where it starts from, and its next change keeps a copy as the first
    of a sitting does (rule 1). A copy that cannot be read as a presentation is not
    put back. Putting back what the file already is, is nothing to do.
22. **Taking it back.** Because of rule 20, undoing a putting-back is only putting
    back the copy that was made just before it. For a few seconds after, the line
    under the editor says "Put back the version of 4 Oct. **Undo**"; after that, the
    same copy is at the top of Earlier Versions, marked "before putting back a copy".
    There is no second mechanism, and nothing that can be lost by putting the wrong
    one back.
22a. **Revert** ([06-library.md](06-library.md), rule 12a, which has the question it
    asks first) is a putting back like any other, of the song as it was when it was
    opened here: the bytes its file had when
    the app read it, which the app keeps in memory for as long as the song is open.
    Rules 19 to 22 hold for it as they do for a copy: it is a save, refused as one;
    the file as it is now is copied first; the song is opened afresh, with no
    history of undo; and the line under the editor says "The song is as it was when
    it was opened here. **Undo**", the copy just made being what Undo puts back. It
    is offered only while the file is no longer those bytes. A song left and come
    back to is opened again: what it then is, is what Revert goes back to.
23. Putting back a copy that belongs to another song (a song now at that path with
    another id inside than the copy has) asks first, saying both names: "This copy is
    of “*its name*”, and the song here now is “*this one's*”: another song under the
    same name. Put the copy in its place?"

## Proofs to write

- A change to a file makes one copy, the same bytes as the file was; a second change
  in the same sitting makes none; opening the song again and changing it makes one.
- A copy that cannot be made refuses the change and leaves the file alone.
- Put back, the file is byte for byte the copy; taken back, byte for byte what it was.
- Rule 12 on a made-up folder of copies with made-up dates: the first is always
  there.
- With the index deleted, the copies are still listed.
- A put back and its taking back, one after the other, leave two copies more than
  there were, each saying "before putting back a copy".
