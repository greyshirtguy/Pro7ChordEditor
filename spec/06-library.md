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
   their Mac, 2026-10-10). ProPresenter 21 has **workspaces** instead, each a folder
   of its own laid out as rule 5 says, and it lists them in its own settings (the
   settings kept for `com.renewedvision.propresenter`, read with the system's way of
   reading another program's settings) under `userWorkspaces`: a list, each entry
   with a `name`, a `url` (the folder, as a file URL) and `isActive` (whether it is
   the one in use). **Known** of version 21 on the maintainer's Mac, where the list
   is JSON kept as data and the workspace's folder is under
   `~/Library/Application Support/RenewedVision/ProPresenter/UserWorkspaces`. That
   other versions keep it the same way is **a guess**; the same JSON as text, and a
   list ready made, are taken too. Version 18 has no such list.

   So the Mac app finds **places**, and shows them all for one to be picked:
   - each workspace so listed, the one in use first;
   - the folder those same settings name under `applicationShowDirectory`, if it has
     a `Libraries` in it and is not one of the workspaces (**a guess** that this is
     how a ProPresenter from before workspaces says its folder was moved; on the
     maintainer's Mac it names the workspace);
   - `ProPresenter` in Documents (rule 1);
   - each folder chosen by hand (rule 6).

   A place whose folder is not there is left out, and one found twice is shown once.
   ProPresenter's settings are only read. Whether ProPresenter on Windows has
   workspaces too is for the Windows session to find out; if it has, they are found
   and picked there the same way.
4. Ask the system for Documents; do not build its path. On Windows it is often inside
   OneDrive, and on a Mac it may be kept in iCloud.
5. Inside ProPresenter's folder, `Libraries` has one folder for each library, named as
   ProPresenter shows it, and each of those has the library's presentations as `.pro`
   files, directly in it.
6. If no such folder is found, or it has no `Libraries`, the app says so plainly and
   offers **Choose Folder…**, for any folder of `.pro` files. It is not an error:
   someone may keep a copy of a library elsewhere, or have no ProPresenter on this
   computer at all. What was chosen is remembered. A folder chosen is taken for what
   it has: if it has `Libraries` in it, it is a ProPresenter folder; if it has `.pro`
   files directly in it, it is one library, by its own name; otherwise its libraries
   are those of its folders that have `.pro` files in them.

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

7. **Libraries** at the left: each folder under `Libraries`, by name, whether or not it
   has songs in it yet. The one last used is opened at the start. Over them, where
   there is more than one place (rule 3), a choice of place, and **Choose Folder…**.
   What is not a folder, or is hidden, is no library.
8. **Songs** in the middle: the library's `.pro` files by name (those directly in it,
   not hidden, and not folders), sorted as the system sorts names, with a search box
   over them that narrows the list as it is typed in: to the names that have what is
   typed in them, capitals and accents apart.
9. Beside a song's name, small **marks**:
   - it has chords: any, on the slides and boxes the editors would show;
   - copies of it are kept ([07-backups.md](07-backups.md));
   - something in it wants mending ([08-repair.md](08-repair.md));
   - it is one of the old editor's "(Chords)" copies: its name ends ` (Chords)`;
   - it cannot be read as a presentation.

   A mark has a tip that says what it means in words.
10. The marks need each file to be read, which for a big library takes a few seconds.
    The names are listed at once; the marks are worked out afterwards, off the thread
    that draws, and kept in the app's own data by the file's path, size and time of
    last change so that the next start has them at once.
11. A choice over the list narrows it by mark: all, with chords, without, wanting
    mending. A song whose marks are not worked out yet, or that cannot be read, is
    shown only under "all": nobody knows whether it has chords.
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

- its settings: the folders chosen, the place and library last open (`settings.json`),
  and the window's size, which the system keeps for it;
- the marks it has worked out (rule 10; `marks.json`);
- the copies of files ([07-backups.md](07-backups.md));
- a log of what it did, a line for each thing, with the names of files and never the
  words of songs or the names of chords: for working out what happened when something
  has gone wrong (`log.txt`; begun again when it passes a megabyte, the one before
  kept beside it).

None of these is anyone's work. One that cannot be read is taken as not there, and the
settings are chosen, or the marks worked out, again.

Nothing of the app's is ever put in ProPresenter's folder.
