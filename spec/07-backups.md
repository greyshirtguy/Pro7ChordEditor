# The copies kept

Before the app changes a file it keeps a copy of it as it was, out of sight, and can
put any copy back. That is what makes saving every change at once a safe thing to do
to somebody's Sunday library.

**New.** Simple Presenter keeps a copy of a file the first time its editor changes it
(under "Edit Backups" in its own data), and has no way to look at them or put one
back. This is that idea finished. Nothing here has been built or tried.

## When a copy is made

1. **Before the first save to a file since it was opened** in the app, the file as it
   is on disk is copied. One copy for a sitting with a song, not one for each chord.
2. **Before a copy is put back**, the file as it is then is copied
   ([Getting one back](#getting-one-back)).
3. **Before a mending** ([08-repair.md](08-repair.md)), likewise.
4. If the newest copy kept of that file is byte for byte what is about to be copied,
   no second one is made.
5. If the copy cannot be made (the disk is full, the folder cannot be written), the
   change that wanted it is refused and the person is told why. The app never changes
   a file it could not first copy.
6. A copy is the file's bytes, unchanged. It is not read, not parsed, not written
   through the app's own code.

## Where, and under what name

7. In the app's own data ([06-library.md](06-library.md)), in a folder `Backups`, in
   folders that follow where the file was: the library's name, then the file's name
   without its ending, as a folder. So the copies of
   `Libraries/Songs/Amazing Grace.pro` are in `Backups/Songs/Amazing Grace/`.
   For a file from a folder chosen by hand, the folder's own name stands for the
   library.
8. Each copy is named for when it was made, to the second, in the computer's own time:
   `2026-10-11 09.14.03.pro`. Beside the copies is `index.json`, a list with an entry
   for each: its file name, when it was made, **why** (`before a change`, `before
   putting back a copy`, `before mending`), the whole path of the file it is a copy
   of, the presentation's id and name as they were inside it, how many chords it had,
   its key, its size, and the app's version.
9. The index is a help, not the truth: if it is lost or damaged the copies are still
   there, named by their time, and the app lists them from the folder.
10. A song is known by where it is: its library and file name. A song renamed or moved
    in ProPresenter starts afresh, and its earlier copies stay under its old name,
    where **All Copies** (rule 18) finds them.

## How long

11. The oldest copy of a file, the file as it was before this app ever touched it, is
    **never** thrown away.
12. Of the rest, everything from the last thirty days is kept, and the twenty newest
    whatever their age. Older ones beyond that go, when the app starts.
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
    each has, and their keys.
17. Esc, or a click outside, puts the panel away.
18. The settings have **All Copies**: every song that has copies, by library, those
    whose song is no longer where it was included. From there a copy can be looked at,
    or saved somewhere as a file.

## Getting one back

19. **Put This One Back** makes the song's file that copy again. It is a save, and so
    is refused while ProPresenter is running, and only done if the file on disk is the
    one the app last read ([05-song-and-saving.md](05-song-and-saving.md), rule 10).
20. First the file as it is now is copied (rule 2). Then the copy chosen is written
    beside the file and swapped in ([01-file-format.md](01-file-format.md), Writing),
    and read back.
21. The song is opened afresh from the file. Its history of undo is empty: what was
    put back is where it starts from.
22. **Taking it back.** Because of rule 20, undoing a putting-back is only putting
    back the copy that was made just before it. For a few seconds after, the line
    under the editor says "Put back the version of 4 Oct. **Undo**"; after that, the
    same copy is at the top of Earlier Versions, marked "before putting back a copy".
    There is no second mechanism, and nothing that can be lost by putting the wrong
    one back.
23. Putting back a copy that belongs to another song (from All Copies, onto a song now
    at that path with another id inside) asks first, saying both names.

## Proofs to write

- A change to a file makes one copy, the same bytes as the file was; a second change
  in the same sitting makes none; opening the song again and changing it makes one.
- A copy that cannot be made refuses the change and leaves the file alone.
- Put back, the file is byte for byte the copy; taken back, byte for byte what it was.
- Rule 12 on a made-up folder of copies with made-up dates: the first is always
  there.
- With the index deleted, the copies are still listed.
