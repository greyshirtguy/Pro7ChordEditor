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

One window, in two parts: at the left the libraries and, under them, the songs of the
one chosen; at the right the song. It is laid out as ProPresenter's own window is (and
Simple Presenter's), so that someone who knows that one knows this one. (At first it
was three parts side by side, the songs between the libraries and the song; the
maintainer moved the songs under the libraries on 2026-10-11, which also gives the
song the width they took.)

```
+--------------------------+----------------------------------------------------------+
| [place]  Choose Folder…  |  Song name      Key [G]  Shown in [G]  [Chords|ChordPro] |
| LIBRARIES                | -------------------------------------------------------- |
|  ▣ Songs                 |                                                          |
|  ▣ Kids                  |                                                          |
|  ▣ Christmas             |           the chord sheet, or the ChordPro text          |
|== 4 SONGS ====----=======|                                                          |
| Search                   |                                                          |
| [All|With chords|Without]|                                                          |
|  ▫ Amazing Grace     ♪ ↺ |                                                          |
|  ▫ Be Thou My Vision ♪   | -------------------------------------------------------- |
|  ▫ Holy Holy Holy        |  a line that says what just happened                     |
|  ▫ It Is Well        ♪ ! |                                                          |
+--------------------------+----------------------------------------------------------+
```

6a. **The edge between the left and the right is dragged sideways**, to give the lists
   or the song more width. **The bar between the libraries and the songs is dragged
   up and down**, to give one of them more height and the other less; neither can be
   dragged away altogether; clicked twice, it goes back to where it usually is (the
   libraries with a little over a third of the height). Where it was left is
   remembered. The bar says how many songs the list under it has ("8 SONGS", or "3 OF
   8 SONGS" where the list is narrowed), and has a small grip in its middle to show
   that it can be dragged.
6b. **The two lists have grounds of different greys**, as in ProPresenter: the
   libraries' the lighter, the songs' the darker, and the bar between them darker
   still. With the system dark they are ProPresenter's own three (`#585858`, `#3e3e3e`
   and `#2f2f2f`, read off a picture of its window); with the system light, their
   like (`#ececec`, `#d5d5d5`, `#bdbdbd`).
6c. **A small picture stands before each name**: before a library, the orange square
   that is ProPresenter's mark; before a song, the grey outline ProPresenter draws
   before a presentation. Both are ProPresenter's own pictures, used at the
   maintainer's asking to show what is a ProPresenter library and what a
   presentation; they are Renewed Vision's, and nothing in this repository's licence
   makes them anyone else's. On the Mac they are drawn from the outline's path and
   are in one file (`ListIcons.swift`), so that they can be taken out in one place.
7. **Libraries** at the top left: each folder under `Libraries`, by name, whether or
   not it has songs in it yet. The one last used is opened at the start. Over them,
   where there is more than one place (rule 3), a choice of place, and **Choose
   Folder…**. What is not a folder, or is hidden, is no library.
8. **Songs** under the libraries: the library's `.pro` files by name (those directly in it,
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
    shown only under "all": nobody knows whether it has chords. The song that is open
    stays in the list whatever the list is narrowed to: its first chord must not take
    it out of "without" from under the person typing it. A song's marks are worked
    out again when it is saved.
12. **The song** at the right: its name, its two keys
    ([05-song-and-saving.md](05-song-and-saving.md)), and a switch between the chord
    sheet and ChordPro. Under the editor, one quiet line says what the keys do, or for
    a few seconds what just happened ("4 chords copied…", or what went wrong).
    Each key is a picker of the twenty-six keys ([02-chord-rules.md](02-chord-rules.md));
    a song that states no key shows "None" in the first, and its second cannot be set
    until the first is. Where the pane is too narrow for the name to be read beside
    them, the keys go on a row of their own under it. What went wrong stays on the
    quiet line until the next thing is done.
13. A click on a song opens it. If a chord was being typed in the song being left, it
    is kept first ([03-chord-sheet.md](03-chord-sheet.md), rule 39;
    [04-chordpro-editor.md](04-chordpro-editor.md), rule 13).
14. A song that cannot be read is listed and says so when clicked. Nothing is done to
    it.
15. When the app comes to the front, the lists are read again: ProPresenter may have
    added songs meanwhile. The song that is open stays as it is, with what is being
    typed in it and its undo, if its file is still the one that was read; if
    something else has changed the file, the song is read again.

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
18. This is asked when the app starts, when it comes to the front, whenever the
    system says ProPresenter has started or quit, and **before every save**. The
    first three are for telling the person early; only the last is the safeguard.
    (**Found**, on the Mac, 2026-10-11: asked only on coming to the front, the band
    stayed after ProPresenter was quit. Quit from its own window, it is still going
    when this app comes to the front; quit from its Dock icon, this app was in front
    all the while. On the Mac the system's news is `NSWorkspace`'s notices that an
    application has launched or terminated. Where a system gives no such news, a
    look every second or two while the app's window is open does as well.)
19. While it is running, everything can be looked at and nothing changed: the text
    cannot be typed in and the keys cannot be set. A band
    across the top of the song says: "ProPresenter is running. Quit it to change
    chords here." The band goes by itself when ProPresenter does. If it was started
    without the app being told (the app was in front all the while), the next save is
    refused, and the band is there from then.
20. There is no way to switch this off in the app. (Whether the rule should hold for
    a file that the running ProPresenter cannot have open, such as a copy of a
    library somewhere else, was raised by the maintainer on 2026-10-11 and put on
    hold. `mac/NOTES.md`, "Asked for by the maintainer, for later", has what was
    thought. Until it is decided, rules 16 to 19 are the rule.)

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
