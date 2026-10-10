# The library

Finding ProPresenter's songs on the computer, listing them, and knowing whether
ProPresenter is running.

**New**: Simple Presenter opens a ProPresenter folder as its own workspace and has
none of this. The old Windows editor had the bones of it
(`windows/Pro7ChordEditor/MainWindow.xaml.cs` until the Windows app is rewritten), and
what it learnt is kept here.

## Where ProPresenter keeps its songs

1. ProPresenter has one folder for everything it keeps, here called its folder. Unless
   it has been moved, that is `ProPresenter` in the person's Documents folder, on both
   systems.
2. It can be moved, in ProPresenter's settings. On **Windows** the place it was moved
   to is in the text file
   `%APPDATA%\RenewedVision\ProPresenter\PathSettings.proPaths`, in a line of the form
   `Base=C:\\Some\\Folder;` (each backslash written twice). The file is only there if
   the folder was moved: no file means Documents. (**Known**: the old editor reads it
   so, and its issue 1 was a computer without the file.)
3. On the **Mac** there is no such file (**known**: the maintainer's word, and
   their Mac, 2026-10-10). Newer versions of ProPresenter have **workspaces** instead, each
   with a folder of its own, and how they are recorded may differ a little from one
   version to another. So the Mac app shows the workspaces it finds and lets one be
   picked, and a library of an older ProPresenter, or one kept anywhere else, is
   opened with **Choose Folder…** (rule 6). How ProPresenter records its workspaces
   and where each one's `Libraries` folder is, is **not known yet**: it is to be
   found out at M3 by reading, and only reading, ProPresenter's own settings, and
   written here. Whether ProPresenter on Windows has workspaces too is for the
   Windows session to find out; if it has, they are shown and picked there the same
   way.
4. Ask the system for Documents; do not build its path. On Windows it is often inside
   OneDrive, and on a Mac it may be kept in iCloud.
5. Inside ProPresenter's folder, `Libraries` has one folder for each library, named as
   ProPresenter shows it, and each of those has the library's presentations as `.pro`
   files, directly in it.
6. If no such folder is found, or it has no `Libraries`, the app says so plainly and
   offers **Choose Folder…**, for any folder of `.pro` files. It is not an error:
   someone may keep a copy of a library elsewhere, or have no ProPresenter on this
   computer at all. What was chosen is remembered.

## The window

One window, in three parts. This is a proposal, to be tried and changed with the
maintainer; the old editor's two lists beside an editing area are its bones.

```
+----------------+---------------------------+----------------------------------------+
| LIBRARIES      | Search                    |  Song name          Key [G]  Shown in [G]  [Chords|ChordPro]
|  Songs         | ------------------------- | --------------------------------------- |
|  Kids          | Amazing Grace        ♪ ↺  |                                        |
|  Christmas     | Be Thou My Vision    ♪    |        the chord sheet, or the         |
|                | Holy Holy Holy            |        ChordPro text                   |
|                | It Is Well           ♪ !  |                                        |
|                |                           | --------------------------------------- |
|                |                           |  a line that says what just happened   |
+----------------+---------------------------+----------------------------------------+
```

7. **Libraries** at the left: each folder under `Libraries`, by name. The one last used
   is opened at the start.
8. **Songs** in the middle: the library's `.pro` files by name, sorted as the system
   sorts names, with a search box over them that narrows the list as it is typed in.
9. Beside a song's name, small **marks**:
   - it has chords;
   - copies of it are kept ([07-backups.md](07-backups.md));
   - something in it wants mending ([08-repair.md](08-repair.md));
   - it is one of the old editor's "(Chords)" copies.

   A mark has a tip that says what it means in words.
10. The marks need each file to be read, which for a big library takes a few seconds.
    The names are listed at once; the marks are worked out afterwards, off the thread
    that draws, and kept in the app's own data by the file's path, size and time of
    last change so that the next start has them at once.
11. A choice over the list narrows it by mark: all, with chords, without, wanting
    mending.
12. **The song** at the right: its name, its two keys
    ([05-song-and-saving.md](05-song-and-saving.md)), and a switch between the chord
    sheet and ChordPro. Under the editor, one quiet line says what the keys do, or for
    a few seconds what just happened ("4 chords copied…", or what went wrong).
13. A click on a song opens it. If a chord was being typed in the song being left, it
    is kept first ([03-chord-sheet.md](03-chord-sheet.md), rule 39;
    [04-chordpro-editor.md](04-chordpro-editor.md), rule 13).
14. A song that cannot be read is listed and says so when clicked. Nothing is done to
    it.
15. When the app comes to the front, the lists are read again: ProPresenter may have
    added songs meanwhile.

Later, and not to begin with: ProPresenter's playlists in the left column, so that
Sunday's songs can be worked through in order (`Playlists/Library` in its folder;
Simple Presenter reads it, `src/playlistfile.*` in its repository); search by a song's
words; dropping a `.pro` file on the window to open it.

## Whether ProPresenter is running

16. Nothing is saved while ProPresenter is running. It has the library open, and what
    it does with a song that changes under it is not known; the old editor warned of
    damaged files. (That it does harm is **a guess**. That nobody wants to find out
    on their Sunday library is not.)
17. It is running if a process is: on Windows one named `ProPresenter`; on the Mac an
    application whose bundle identifier is `com.renewedvision.propresenter`
    (**known**: `osascript -e 'id of app "ProPresenter"'` says so on the maintainer's
    Mac, and it is the same in each of the versions looked at there, from 7.9.2 to
    21.3).
18. This is asked when the app starts, when it comes to the front, and **before every
    save**. The first two are for telling the person early; only the last is the
    safeguard.
19. While it is running, everything can be looked at and nothing changed. A band
    across the top of the song says: "ProPresenter is running. Quit it to change
    chords here." The band goes by itself when ProPresenter does.
20. There is no way to switch this off in the app.

## What the app keeps of its own

On the Mac in `~/Library/Application Support/Pro7ChordEditor`, on Windows in
`%LOCALAPPDATA%\Pro7ChordEditor`:

- its settings: the folder chosen, the library last open, the window's size;
- the marks it has worked out (rule 10);
- the copies of files ([07-backups.md](07-backups.md));
- a log of what it did, a line for each thing, with the names of files and never the
  words of songs: for working out what happened when something has gone wrong.

Nothing of the app's is ever put in ProPresenter's folder.
