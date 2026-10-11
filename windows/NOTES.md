# Notes on the Windows app

For the next session, which will remember nothing of the ones before: what was decided
on the way and why, what was found out on Windows, and what is left rough. Read it
after `AGENTS.md`, `spec/11-plan.md` and `mac/NOTES.md`: the Mac app was built first,
and most of what it found out is true here too. Add to it at the end of every
milestone; put right what has become untrue.

## Where things stand

| | | |
| --- | --- | --- |
| **M0** Setting up | Done, 2026-10-12 | The four layers and the app, empty; the reader of a `.pro` made at each build; forty-nine tests; one picture; the app published as one file, and started; the workflow, which passed on GitHub's Windows machine at the first push (its run of the tests, the picture among them, and the app as one file). The first editor moved to `old/` |
| **M1** The rules | Done, 2026-10-12 | `ChordRules`: every rule of `spec/02`, the words of an RTF, the stand-ins written into one, the key fit and the first editor's shift. Every case of the sixteen vector files M1 is held to passes (some thirty-one thousand answers), with cases of our own for what no vector holds |
| **M2** The song | Done, 2026-10-12 | `Song`: a song opened and read (blank slides among its boxes), its chords and its two keys set, every change saved at once and safely, refusals, undo and redo. The proofs at the foot of `spec/05`, on copies of the test files. **And the song's part of M7**: a chord more or fewer on a line of chords alone, chords onto a blank slide, a slide given a box, a change of mind (decision 15) |
| **M3** The library | Done, 2026-10-12, bar what only the maintainer can try | Where songs are kept as files on a PC, the lists, the search, the marks, a song shown read only, whether ProPresenter is running, the app's own settings and log; and the window, in the look of Windows 11, dark and light. `spec/06`. **Nobody has seen the window itself**: it was drawn into pictures, and started ("Left rough"). **And on this PC it finds no library**, for the reason in the next section |
| **M4** The copies | Next | Kept before a first change; listed; looked at; put back; taken back. `spec/07`. `OpenSong.PutBack` with it |
| **M7** Chords with no words | The song's part done at M2 | What is left: the two editors letting it be asked (M5, M6), and the maintainer seeing in ProPresenter a song so changed |

## For the maintainer to decide: ProPresenter 22 keeps no `.pro` files

**Found 2026-10-12, on this PC, by reading and nothing else.** It is the look `spec/06`
(rule 3) leaves to the Windows session, and it came out as nobody expected. It is not
decided, and `spec/` is not changed: both are the maintainer's. The Mac's session needs
to know of it too.

**What is on this PC** (ProPresenter 22, `ProPresenter.exe` 22.0.0.110, "22
(369098862)" in its own words; not running while it was looked at):

- **There is no `ProPresenter` in Documents** (Documents is in OneDrive here), and
  `%APPDATA%\RenewedVision\ProPresenter\PathSettings.proPaths` **is there and is
  empty**: no bytes at all. `spec/06`, rule 2, says the file is only there if the
  folder was moved.
- **It has workspaces**, in `%APPDATA%\RenewedVision\ProPresenter\Workspaces`, each a
  folder named by an id. Which there are is in a SQLite database beside them,
  `registry-v1.sqlite3`: a table `workspaces` (`local_uuid`, `root_path`,
  `display_name`, `cloud_uuid`, `schema_version`, `availability`) and a table `meta`
  whose `active_local_uuid` is the one in use. Here: one workspace, shown as
  "ProPresenter", `schema_version` "22.5.0", `availability` "local".
  (`media-manager.toml`, in the folder above, has the same folder as text under
  `[[workspaces]]`, `path = '...'`.)
- **A workspace has no songs as files.** Its `Libraries` folder has one folder, with
  one `.pro.bak` of 2022 in it, and no `.pro` file is anywhere under ProPresenter's
  folder in `%APPDATA%`. The songs are rows of another SQLite database,
  `<workspace>\DocumentStore\workspace-v1.sqlite3` (its `application_id` spells
  "RVWS"): table `workspace_streams`, one row for each document, with its `kind`
  ("Presentation", "Theme", "Stage", "Macros", "PlaylistLibrary" and sixteen more),
  its `core_path` (for a presentation, the library's name, a slash, and the song's
  name with no ending) and its `snapshot`. Eighteen presentations here: seventeen in
  three libraries, two of which have no folder on the disk at all, and one in none.
- **A snapshot is not the bytes of a `.pro` file.** Every one begins with the four
  letters `loro`, then sixteen bytes and `00 03`: the form in which the Loro library
  (loro.dev, a library for documents that several people change at once and that are
  merged afterwards) writes a whole document. Inside are blocks packed with LZ4, in
  which the names of the `.pro` format's fields can be read as text (`cues`,
  `base_slide`, `custom_attributes`, `chord`), and RTF. So the song is kept as a
  tree, a part for each field, with the history that merging needs; and beside it
  the database keeps a count of its bytes, a SHA-256, a "version vector", and a table
  of receipts for every change. The names say the files were taken into the
  database once (`presentation_placement_cutover`, `workspace_cutover_acknowledgements`,
  `workspace_migration_quarantine`); the workspace's oldest files are of 2026-09-17.
- Two more folders are in `Workspaces` that the registry does not list: one with a
  database of the same kind (and an `imprint.cloud`), one named
  `ProPresenter-` and a long number, with a store of another kind (`.sst`, `.log`).
  What they are is **not known**; earlier tries of the same thing, at a guess.

**What is not known.** Whether every copy of version 22 is like this or this one is
ahead of the rest: its settings list "feature flags" (`cloud-v2` among them), and a
search of the web found nothing said about it. Whether version 22 on a
Mac is the same (the maintainer's Mac has 21, where songs are files: `spec/06`, rule
3). Whether 22 still takes a `.pro` put into `Libraries` by hand, or only through its
own Import; and whether what it exports is still the `.pro` this repository reads.
Only the maintainer, with ProPresenter open, can say.

**What it means.** Everything in `spec/` is about a file: read whole, changed in one
place, written back whole, with a copy kept and the bytes compared. On a PC like this
one there is no file. The app as specified finds no library here and says so (`spec/06`,
rule 6), and can still work on any folder of `.pro` files that is chosen: an export, a
copy, the library of a ProPresenter up to 21.

To change a song where version 22 keeps it would be another program: SQLite and Loro
to read it (two dependencies, on both systems), the tree's layout worked out by
looking, and to write, a change made as Loro makes one, with the version vector, the
SHA-256, the count and the receipt that ProPresenter expects beside it, none of which
is written down anywhere. "Byte for byte what it was" (`AGENTS.md`, the first thing
that must stay true) has no meaning there, and a mistake is in the one place the
person's whole library is. **This session will not write there, and advises against
it** unless Renewed Vision says how it is to be done.

**What this session proposes** (to be agreed, and then written into `spec/06` for both
apps):

1. The app goes on being about `.pro` files, as specified. Versions up to 21 are found
   as `spec/06` says.
2. Where the app finds version 22's workspaces and no folder of songs, it says so in
   plain words in place of the bare "no library found": that this ProPresenter keeps
   its songs where the app cannot open them, and that songs exported from it as
   `.pro` files can be chorded from a folder that is chosen. (Not built: it is a new
   sentence, and the maintainer's to word.)
3. The maintainer tries, in ProPresenter 22: export a song; put chords on it with the
   Mac app or Simple Presenter; import it again. Does it come back with its chords,
   and does it take the place of the song or stand beside it? That says whether the
   way round is one anyone would use.
4. Reading version 22's store, only reading, so that songs can be seen and chorded
   copies written out as `.pro` files, is possible and is a good deal of work. Not
   before the answer to 3.

**Meanwhile** M3 is built for folders of `.pro` files, which is needed whatever is
decided. On this PC the maintainer can try it on a ProPresenter folder of the old kind
that is in their Documents under another name (who made it, the maintainer or
ProPresenter's move to workspaces, is not known), by Choose Folder.

## What the maintainer decided, 2026-10-12

1. **The first editor is moved to `windows/old`**, as it was, with its history ("happy
   to move old app"). It is there to be read: for what it knew (`spec/06`) and for
   what it did to files (`spec/08`). It still builds where it is.
2. **Visual Studio Community 2026 is on this PC**, by the maintainer's own choice ("I
   would use it anyway"). Nothing here needs it: everything is built, tested and
   published with `dotnet` from the command line, which is also all that GitHub's
   machine has. But the maintainer may open the solution in it, so it has to open and
   run there. The maintainer opened it there the same day, and its Test Explorer
   listed the forty-nine tests ("Left rough" has what was not said).

And what I chose, each the maintainer's to change:

3. **The reader is made at each build from the `proto/` submodule**, as `spec/10`
   says, and none of it is kept in the repository, where the Mac keeps its own. The
   maintainer was asked, and said the idea of a submodule was not clear; it was
   explained (a pointer to another repository, which an ordinary clone leaves empty),
   and the choice was left as the page has it. What it costs: a clone has to be made
   with its submodule, on GitHub's machine too. What it buys: what is built can never
   be older than the descriptions beside it, which on the Mac nothing checks.
4. **The Mac's four layers, under the Mac's names**: `ChordRules`, `ProFormat`, `Song`,
   `AppViews`, and `App`. So that one app can be read beside the other, and because
   the Mac found that the window has to be a library for tests to draw it.
5. **The three layers with no window are built for plain .NET** (`net10.0`), and only
   `AppViews` and `App` for Windows (`net10.0-windows`). In the first three there is no
   WPF to reach for, so a rule cannot stray into the window's world by accident.
6. **Six rules about text stop a build if broken**, and so does using what may be
   nothing (`.editorconfig`, `Directory.Build.props`). They are the Windows answer to
   what the Mac met in Swift (`mac/NOTES.md`, "At M1"): .NET's everyday calls on a
   string compare by the computer's language unless told otherwise, and `spec/02`
   wants unit by unit. Tried on a scratch file of eight careless lines: every one
   stopped the build. They will be a nuisance now and then (`n.ToString()` has to say
   which language's digits), and that is the point.
7. **xUnit's third version, at 4.0, and nothing else for the tests.** It runs on
   .NET's newer way of running tests, which `global.json` switches on.
8. **One picture test at M0**, which the plan shown to the maintainer did not have.
   It is how "the app shows its window" was checked by something better than a guess,
   and it proves the drawing of pictures before M3 leans on it.
9. **A view to draw**: `NotBuiltYet`, the app's name and one sentence. It goes at M3.
10. **The commits are in the name on the Mac's commits**, "Dan Owen", with the
    maintainer's address. Git on this PC has no name or address set, and I set none:
    each commit was given them for itself (`git -c user.name=… -c user.email=… commit`).
    Asked, the maintainer said "that git name is fine". Nothing was said of setting it
    on this PC, so each commit is still given it for itself.
11. **The commits are on `master`**, as the Mac's are. They are pushed when the
    maintainer asks; and, since 2026-10-12, a rule or a decision the maintainer has
    agreed is pushed at once (`spec/11`, "How a milestone goes"), so that the other
    app's session has it. **Start a session by fetching**, as that page now says: the
    Mac's session changes `spec/` and `testdata/` while this one works.

And the same day, later:

12. **"push and keep going!!"** The maintainer's, on being told M0 was built: so M0
    was pushed, and M1 was built with no plan shown first and no wait between the two
    (as the Mac's M5 and M6 were). What the plan asks for at a milestone's start is in
    the report at its end instead.
13. **The stand-ins written into an RTF are M1's here** (`RtfWords.WithLine`,
    `WithTheOpeningOf`), where the Mac built them at its M7. Their vector file,
    `stand-ins-in-rtf.json`, came with the Mac's M7, after the plan's row for M1 was
    written, and that row holds M1 to every vector file but the chord sheet's two.
    Only the plain functions are here. What asks them (a chord more or fewer on a
    line of chords alone, chords onto a blank slide) is still M7's, in the song.
14. **A directive and a heading in a ChordPro file are found by hand**, not with .NET's
    regular expressions, as on the Mac (`ChordProSong.cs`); and so are the chords
    taken out of ChordPro text (`WithoutChords`).
15. **The song's part of M7 was built at M2.** When the Mac's session built its M2,
    `spec/05` said what a song was to do about chords with no words until M7 (refuse,
    in a sentence of its own). Since the Mac's M7 the page has only the finished rule,
    to the letter, and that sentence is gone from it. So there was nothing on the page
    to build an in-between from, the plain functions it needs were M1's already
    (decision 13), and the Mac's `OpenSong` was there to port whole. It is built once,
    here, with M7's two proofs that a test can make. What M7 still is on Windows: the
    two editors letting it be asked (they are not built yet, so they will never have
    the limits the Mac's had for a while), and the maintainer seeing in ProPresenter a
    song so changed.
16. **A refusal is thrown**, as on the Mac (`Refusal`, which is an `Exception`), and
    not answered: whoever asks the song for a change gets the change, or catches the
    refusal and shows its sentence.
17. **The song is ported from the Mac's `OpenSong.swift`**, file for file, with the
    reference beside, and not from the reference alone: so that where the Mac's
    session and the maintainer settled something the reference does otherwise, the two
    apps do the same.

And at M3, each this session's, on "push and keep going" with no plan shown first, and
each the maintainer's to change:

18. **On Windows a place is one of three things**: the folder ProPresenter's own file
    of paths names (`PathSettings.proPaths`, read as the first editor read it, and
    only if the folder has `Libraries` in it), `ProPresenter` in Documents, and a
    folder chosen by hand. There is no workspace among them, as there is on the Mac:
    the only ProPresenter looked at on Windows that has workspaces keeps their songs
    in a database (the section above). `spec/06`, rule 3, leaves that to this session
    to find out, and this is what was found; the page is not changed until the
    maintainer has decided what is to be done about version 22.
19. **Hidden is what Windows marks hidden, and also a name that begins with a full
    stop.** `spec/06` leaves out "what is hidden" and does not say what that is on
    Windows. A Mac hides a name that begins with a full stop, and writes such files
    beside songs on a disk that is not its own kind (`._Name.pro`); so one folder
    lists the same in both apps. **For `spec/06`, if the maintainer agrees.**
20. **Names are sorted by Windows itself**, by the function File Explorer sorts with
    (`StrCmpLogicalW`), asked through .NET's way of calling the system. It is the
    one place the app calls Windows by name, and is no dependency. None of the six
    rules about text had to be switched off for it.
21. **Whether ProPresenter is running is in `Song`**, not with the window as on the
    Mac: on Windows asking needs nothing but .NET. The window asks again every two
    seconds while it is open, off its own thread, which `spec/06`, rule 18, gives as
    the way where a system has no news of a program starting. **If the system will
    not say, it is taken as running**: nothing is to be saved on not knowing.
22. **The window wears the look of Windows 11 that .NET has had since its ninth
    version** (WPF calls it Fluent), dark and light as the system is, as `spec/10`
    asks ("Looks"). It is part of .NET and no dependency. **The one switch that has
    the app follow the system, `ThemeMode`, .NET still marks as on trial**: the
    compiler is told so in one place (`Look.cs`), and if a later .NET spells it
    otherwise that is the one line to change. The pictures do not use it: each is
    drawn in the dark look or the light one, named by the test.
23. **The app's own colours follow the look, and nothing reads the system's
    settings.** A view asks the look what colour a window's ground is, and is dressed
    in the greys that go with a dark ground or a light one, again whenever that
    changes (`Look.Follow`). The two lists' greys are the page's (rule 6b).
24. **The lists give way to the song in a narrow window** (`ListsLook.Width`): made
    narrower than the lists, the edge and the song's least width together, the window
    takes the width from the lists, down to their own least, and made wide again
    gives it back. The Mac's own split view does as much by itself. A picture showed
    the need: at its smallest the window cut the song's header short.
25. **The sentences of the lists are in the model**, not in the views ("No
    ProPresenter library was found on this PC.", "This folder has no libraries in
    it.", what a mark's tip says), so that a test holds them. They are the Mac's,
    word for word, with "PC" for "Mac".
26. **The song is shown as lines that cannot be selected**, in M3 only: it is a
    stand-in until the ChordPro editor of M5, which is a text box. And its key is
    words in the header ("Key G", "Shown in A", "No key"), as the Mac's were at its
    M3; the two pickers are M5's.
27. **The small marks after a song's name are letters of Windows' own font of signs**
    (Segoe Fluent Icons on Windows 11, Segoe MDL2 Assets before it), where the Mac
    uses its system's symbols: a note for chords, a triangle for a file that cannot
    be read, two pages for the first editor's copy.
28. **The app's own files are JSON written by .NET itself** (`System.Text.Json`, no
    dependency), under the names the page gives; the log's times are by the clock at
    Greenwich with a Z after them. Neither app reads the other's.
29. **The app says of itself to Windows how it is to be drawn on a fine screen**
    (`App/app.manifest`): afresh for each screen it comes to, and not stretched.
    Windows was asked of the running window and says it is so.

## How it is laid out, and why

```
windows/
  Pro7ChordEditor.slnx        every project, as one
  Directory.Build.props       what all the projects have in common
  Directory.Packages.props    every package taken, and its version
  .editorconfig               rules for the code; six stop a build
  global.json                 how `dotnet test` runs the tests
  ChordRules/                 the rules (M1)                 plain .NET, no package
  ProFormat/                  the reader of a .pro, made at each build; MadeFrom.cs by hand
  Song/                       the song, the library, the copies, the checks (M2 on)
  AppViews/                   the window: its views, and later its model (M3 on)
  App/                        the app: a few lines, and how it is published
  Tests/
    TestData/                 how tests find testdata/
    ChordRules.Tests/         held to testdata/vectors
    Song.Tests/               held to testdata/files
    AppViews.Tests/           the window's model, and pictures of the window
  old/                        the first editor, as it was
```

- **Everything is run from `windows/`**: `dotnet test`, `dotnet publish App`,
  `dotnet run --project App`. `global.json` is only found from there or below, and
  without it `dotnet test` stops ("What was found out").
- **Each project says its own assembly's name and namespace**, though they all follow
  one pattern. Said once for all of them, the building of a window breaks
  (`Directory.Build.props` says why).
- **The versions of packages are in one file**, so that it is the whole list of what
  the app depends on. A line added there is a new dependency: ask first.
- **Only what a presentation needs is made**: the thirty-two files, named in
  `ProFormat.csproj`. The build fails if one is missing from the list, and a test
  fails if one is there that is not needed. **When the `proto/` submodule is moved
  on**, nothing has to be made again by hand, as it must on the Mac; build, and run
  the tests. If a newer ProPresenter's `presentation.proto` takes in a file it did
  not before, the build says a name does not exist in
  `Pro.SerializationInterop.RVProtoData`, many times: add the file to the list. And
  the count of thirty-two in `TheReaderTests` and in `spec/01` is then another.
- **Tests find `testdata/` by walking up from where they were built**, and find the
  files in it by looking, not from a list. The Mac's tests name three folders of
  presentations; these take every `.pro` under `testdata/files`, so the fourth folder
  that came with the Mac's M7 was tried here without a line written for it.
- **A view is drawn into a picture with no window opened** (`Pictures.cs`): measured,
  arranged, and drawn into a bitmap at twice its size, on a thread of the kind a view
  needs. To see the window: run the tests and open the PNGs in
  `%TEMP%\Pro7ChordEditorPictures`, or say where with `PRO7_PICTURES`. Do this after
  any change to how something is drawn, and say what was seen.
- **`old/` has three files with nothing in them**, each a fence: the settings, the
  packages' versions and the rules of the folder above are not to reach the first
  editor's project.
- **`windows/.gitignore` is Visual Studio's own long list**, and it leaves out, with
  no word said, any folder named `Backup…`, `Log`, `Logs`, `Packages`, `publish`,
  `Debug`, `Release` or `Out`, wherever it is. **Do not give source such a name**: the
  copies of M4 are not to be in a folder called `Backups`. After adding a folder, look
  at `git status`.

### The rules (M1)

`ChordRules/Chords.cs` is the way in and lists the files, which are the Mac's under
the same names. What to know before changing any of it:

- **The rules stand under `Chords`**, as the C++ has them under `chords::`, with the
  names they have there in the way C# writes names: `Chords.Transposed("C#m", "E",
  "Db")`. Three things that are not from `chords.cpp` have names of their own:
  `RtfWords`, `KeyFit`, `OldEditor`.
- **A text box's words are a plain `string`, and a place is an `int` into it.** .NET
  counts in UTF-16 units already, so there is nothing like the Mac's `Words` and
  `Units`. But every call that compares or searches text says `Ordinal`, the build
  stops on one that does not, and sorting is LINQ's `OrderBy`, which keeps equals in
  order where `List.Sort` does not. Keep it so.
- **White space, a letter and a number are the page's own lists** (`Text.cs`), never
  .NET's. As it happens .NET's white space IS Qt's list today, and a test says so if
  that ever changes.
- **No file of the rules has a character outside ASCII in it, nor a backslash and a
  "u".** The characters that cannot be seen are written as numbers
  (`(char)0x200B`), and RTF in a test is put together from pieces (`Rtf(...)` in
  `NotHeldByVectorsTests`). After writing anything about those characters or about
  RTF, look: `grep -rlP '[^\x00-\x7F]' windows --include=*.cs --exclude-dir=old`.
- **Tests**: one test for each list of each vector file, reading every case
  (`Tests/ChordRules.Tests/Vectors.cs` says how they are compared and what is said of
  one that fails: which file, which case, what was got, with every character that
  cannot be seen written as its number). Tests marked "Mine, not a vector" are just
  that.

### The song (M2)

`Song/Song.cs` is the way in and lists the files, which are the Mac's under the same
names. What to know before changing any of it:

- **Everything that may be asked ends in `OpenSong.Save`.** A new state of the whole
  message is made beside the one in memory; the safeguards are asked; the file is
  written beside itself, pushed onto the disk, swapped in and read back; and only then
  does the new state become the song. So a refusal or a failure anywhere leaves the
  song in memory as it was, with nothing to put back. Keep it so: nothing but `Save`
  puts a message in the song's place.
- **A message here is an object, where in Swift it is a value.** Giving one another
  name is the same message, and changing the one changes the other. So everything
  that changes one first takes a copy (`Clone`), and a message kept for undo is never
  changed again. And a part that is not there is nothing (`null`), where in Swift it
  reads as an empty one: `Reading.cs` says what is done about each. This is where a
  port from the Mac goes wrong unseen; `Song.cs` has it at more length.
- **The order in `Save`** is the order in `spec/05`, rule 10: no safeguards;
  ProPresenter running; the file changed by something else (size, time AND bytes);
  the copy; the write. A refusal before the copy leaves no copy.
- **A step of undo** holds the part that changed, before and after: one cue, or
  `music` (which a song may not have at all). 200 steps are kept, as the reference
  keeps.
- **The generated messages are named through `RV`** in this layer
  (`using RV = Pro.SerializationInterop.RVProtoData;`): one of them is called
  `Action`, which .NET has too, and another `Slide`.
- **Slides do not change while a song is open**, so they are worked out once. The
  text boxes are read from the message each time they are asked for. If that is ever
  slow (it parses every box's RTF), keep them and throw them away in `Save`.
- **Tests** work at a `Workbench`: a folder of its own under the system's temporary
  folder, with copies of the test files and safeguards a test can set. Nothing is
  ever saved into `testdata/`. **A second copy of a test file asked of one workbench
  goes over the first**: a test that wants a song and the same song untouched reads
  the second from `testdata/` itself, or under another name.

### The library and the window (M3)

- **In `Song`, with no window**: `Places.cs` (finding), `Listing.cs` (libraries,
  songs, search), `Marks.cs`, `AppData.cs` (the app's own folder), `SongText.cs` (a
  song as lines of text: what is shown now, and what the ChordPro editor will work
  on at M5), `ProPresenterApp.cs`. They are the Mac's under the same names.
- **In `AppViews`**: `LibraryModel` is everything the window shows and does, with no
  drawing in it, and is tested so. The views only show it: `LibraryWindow` frames
  `LibraryView`, which is `ListsPane` at the left and `SongPane` at the right.
  `ListsLook` is the arithmetic of the bar and the edge, `Look` the colours,
  `ListIcons` the two small pictures. `LibraryModel`, the finder and the song are
  each handed what they would otherwise ask the PC for, so that tests hand them a
  made-up PC (`Tests/AppViews.Tests/MadeUpPC.cs`).
- **The model says only that something has changed, never what.** Each view then
  asks it again for what it shows (`Show`). A view that puts the model's choice into
  a list is told by the list that its choice changed, as by a click; so while a view
  is showing, what its lists say is not passed on (`showing`). The songs' list is
  not made again while it is the same songs in the same order: each row is told what
  it now is, so that marks coming in do not move the list from where it was scrolled.
- **One thread.** The model is asked from the window's thread alone. A library's
  marks are read on another and come back to the window's by its queue. A test of
  the model, or of a view, is run on a thread with such a queue
  (`Pictures.OnAThreadOfItsOwn`), one test at a time.
- **To see the window**: from `windows`, in PowerShell,
  `$env:PRO7_PICTURES = "C:\some\folder"; dotnet test --project Tests/AppViews.Tests
  --filter-class "*PictureTests"`, then open the PNGs. Do this after any change to
  how something is drawn, and say what was seen. Each picture is the whole view on a
  made-up PC, in the dark look or the light one as its test says.
- **A view asks the look for a colour by name, as a resource that may change**
  (`DynamicResource`), never as one fixed when the view is made (`StaticResource`):
  a test puts the look on a view after the view is made, and the app's look changes
  when the system's does. For the same reason no style here is built on one of the
  look's own. Where a control of the look's is wanted otherwise (a list's rows less
  tall), the look is given another answer where it asks: a resource of the same
  name, nearer by (`ListsPane.xaml`).
- **A song is opened with no safeguards at M3**, so it cannot be changed. M5 hands it
  real ones: `ProPresenterApp.IsRunning`, and the copies of M4.

## What was found out

### At M0

- **Every test file read and written back is the same, to the byte**: all twelve,
  the one ProPresenter wrote and the one the Mac's M7 added among them, through
  Google.Protobuf 3.36.2. And **a field the descriptions have no name for is kept**
  through a read, a change elsewhere in the message, and a write: tried by putting
  one at the end of each test file. Both are tests (`TheReaderTests`).
- **How the tests were shown to bite.** All forty-nine passed the first time they
  ran, which proves little. So nine things were broken, one at a time, in a clone
  made for it in a temporary folder (never in this one: `mac/NOTES.md` says why), and
  the tests run each time. All nine were caught: a reader told to throw away what it
  does not understand (12 tests failed); a file more than is needed in the list of
  thirty-two (1), and a file fewer (the build stops); the version of ProPresenter not
  written into the reader (1); a zero-width space in a vector turned into a question
  mark (1); a vector file with no `about` (1); a test file with its name written a
  second time at its end, so that its bytes are not in the usual order (3); a chord
  the expected file names otherwise (1); a view that draws nothing (1).
- **A clone with no submodule stops with one sentence** that says what to run, for
  `dotnet build` and `dotnet test` alike. And a clone with it, in which nothing had
  been built, passed every test and published. Both tried.
- **xUnit 4 will not run under `dotnet test` as it has always worked.** It answers
  "Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on
  .NET 10 SDK and later". `global.json` with `"test": { "runner":
  "Microsoft.Testing.Platform" }` is the cure, and is found only from the folder
  `dotnet` is run in or one above it. In that way of running:
  - a word `dotnet test` does not know is handed on to the tests, which refuse what
    they do not know: `dotnet test -nologo` gives "Zero tests ran", exit code 5;
  - some of the tests are picked with xUnit's own words, not `--filter`:
    `dotnet test --project Tests/AppViews.Tests --filter-class "*PictureTests"`
    (tried; xUnit has `--filter-method` and `--filter-namespace` too, not tried);
  - a project is named with `--project` (tried), a solution with `--solution`
    (Microsoft's page says; not tried, since `dotnet test` alone finds the one here).
  The packages `xunit.runner.visualstudio` and `Microsoft.NET.Test.Sdk` are for the
  old way and are not taken.
- **WPF builds a window by first building a copy of the project under a made-up
  name.** A name for the assembly worked out from the project's name, in
  `Directory.Build.props`, came out otherwise for the copy, and the window could not
  find a view of its own project: "The tag 'NotBuiltYet' does not exist in XML
  namespace". Hence a name in each project.
- **A project that uses WPF is not given `System.IO` for nothing**, as others are:
  `File`, `Path` and `Directory` want their `using`.
- **A backslash, a "u" and four digits, typed into a file by an agent, arrived as
  the character itself**: twice, in a test, for the two characters that cannot be
  seen. It is what the Mac's notes warn of. They are written as numbers now
  (`(char)0x200B`). After writing anything about those characters, look:
  `grep -rlP '[^\x00-\x7F]' windows --include=*.cs --exclude-dir=old`.
- **The app as it is given out is one file of 135 MB**, most of it .NET. Its first
  start on this PC took 13 seconds, and its second under one: **a guess** that the
  first is the virus scanner reading a new program of that size, and WPF's five files
  being unpacked into the temporary folder. It uses some 55 MB of memory with its one
  window. Both are M10's to weigh (it can be made smaller by packing, at the cost of
  a slower start).
- **A window's words can be read from outside it**, by the system's own way for
  programs that read screens aloud (UI Automation, from PowerShell). That is how the
  published app was seen to show its two lines, at 1280 by 800, and to close when
  asked, with nobody looking. It may serve again for what a picture cannot tell.
- **The reader works from inside a program published as one file**: a throwaway
  program, outside the repository, read two test files with it and wrote them back
  the same. It was worth trying apart, since the app itself reads no file until M3.
- **`protoc` warns twenty-six times on a build from clean** that an import is unused,
  and not at all on a build after that. It is the Mac's finding too, and harmless.
  They are the only warnings there are.
- **The protoc inside Grpc.Tools 2.84.0 is 35.1.** ProPresenter 22's own program
  folder has Google.Protobuf 3.35.1 in it, which is the same.
- **This PC has ProPresenter 22.0** (22.0.0.110, built 2026-10-01). The descriptions
  are of 21.4, and the repository they come from had no newer set on 2026-10-11,
  though its job runs each night. So the first real files this app reads may well be
  a version ahead of what it has names for. That is what keeping the unknown is for,
  and M2's proofs on copies of real files will show whether any field has no name.
  **Nothing of the maintainer's has been read yet.**
- **Line ends.** Git on this PC changes line ends as it checks out
  (`core.autocrlf` is true). `testdata/` comes through to the byte all the same,
  because `.gitattributes` says it is not text; looked at with
  `git ls-files --eol testdata`. The one test song with Windows line ends still has
  them, and the others still have none.
- **`tools/look.py` read nothing from a song on Windows, and does now.** It said of
  Amazing Grace, which has forty chords, that it had no text boxes, no chords and no
  key. On Windows `protoc` ends each line of its text with a carriage return and a
  line feed, and `tools/proread.py` found its way about that text by line feeds
  alone. `proread.decode` now takes the one for the other, which changes nothing
  where there are none (the Mac, Linux). With it, **every one of the twelve test
  files is read on this PC exactly as `testdata/files/expected` says**, to the last
  chord's range: held against each other by a throwaway script, the one key apart
  that another tool adds (`folder`). Only the reading was tried. `make-fixtures.py`
  and `standins.py` write into `testdata/` and were not run here: whether they work
  on Windows is **not known**.
- **Python 3.14 and `protoc` 36.0 are on this PC** (the maintainer's, by `winget`,
  2026-10-12), and a new terminal finds both: Windows has Python's folder before the
  placeholder of its own that opens the Store. A session started before they were
  installed does not find them by name, but can run them where they are:

  ```
  PB="/c/Users/greys/AppData/Local/Microsoft/WinGet/Packages/Google.Protobuf_Microsoft.Winget.Source_8wekyb3d8bbwe/bin"
  PATH="$PB:$PATH" "/c/Users/greys/AppData/Local/Programs/Python/Python314/python.exe" tools/look.py --chords "Some Song.pro"
  ```

  (in Git Bash, from the top of the repository; `look.py` runs `protoc` by name, so
  its folder has to be on the path for that one command).
- **Pushing is over SSH here too**, and GitHub knows this PC's key. `gh` is signed
  in; its token may not change a workflow, which matters only over https
  (`mac/NOTES.md` has the same).
- **About this PC, for M3**, seen only as far as whether a thing is there:
  Documents is inside OneDrive (`spec/06`, rule 4); and
  `%APPDATA%\RenewedVision\ProPresenter\PathSettings.proPaths` is there and
  `ProPresenter` in Documents is not, so ProPresenter's folder has been moved (rule
  2). Both rules will be met for real. Whether ProPresenter 22 on Windows has
  workspaces (rule 3) is still to be found out, by reading only, with the
  maintainer's leave.
- **The workflow passed on GitHub's Windows machine at the first push** (2026-10-12,
  `windows-latest`, a minute): the forty-nine tests, the picture among them, and the
  app published as its one file of the same size to the byte as here. So a view is
  drawn into a picture on a machine with no screen. That was the last of M0's "done
  when". To see how a run went: `gh run list`, and `gh run view <number> --log`.

### At M1

- **Every case of every vector file passed the first time the tests were run**, the
  rules having been ported function by function from `chords.cpp`, and the reader of
  RTF from the Mac's port of `rtf.cpp`. That proves little, so:
- **How the tests were shown to bite.** Each rule was broken in one small way in
  turn, a hundred and sixty-two ways in all, in a clone made for it in a temporary
  folder, and the tests run each time (a throwaway script: a list of pieces of the
  source and what each is changed to, one at a time, the file put back after).
  - **129 were caught by a vector.**
  - **25 were caught only by a case of our own.** One is the Mac's "still wanted"
    (chords moving with trimmed words). Five are what white space, a letter and a
    number are, which `TextTests` holds over every one of the 65,536 units.
    **Nineteen are rules no vector holds**, found here: each has a case in
    `NotHeldByVectorsTests` now, its answer read from the reference's code, and
    `testdata/README.md` ("Still wanted") lists them for whoever next makes the
    vectors. They are corners: a letter from outside ASCII in a chord; a gap inside a
    part a comment named; a file with carriage returns alone; a picture and a table
    of lists in an RTF; the exact edges of "four chords", "three quarters", "four
    more" and "a quarter more" in the two new rules.
  - **8 cannot be told from the rule by any input** (worked through by hand; each is
    a second lock on a door another check already holds, in the reference too): a
    delta named among a chord's signs, which is a letter anyway; "or the part has no
    name" for an empty line, a marked part always having one; "or it has chords" for
    a line with no words, which is never blank once it has its stand-ins; not
    trimming a line of stand-ins, which trimming would not change; bringing a
    negative number round before it is made a unit, which making it a unit does;
    taking the bass off a chord before reading its kind, which is read from the
    other end (twice); and "on or before the first line break", no chord ever being
    put on one.
- **The six rules about text met real code and stopped nothing that was right.** One
  thing learnt: asked for the first place of a single character (`IndexOf('/')`),
  the build wants the comparison said, where for the last place (`LastIndexOf('/')`)
  it does not.
- **.NET rounds a number written to four places as Python's `round` does**, a tie
  going to the even digit by the exact value held (`0.03125` is `0.0312`). The page
  warns that not every way of rounding does; this one is held by a test of the ties
  there can be.
- **Where the Mac's port and the reference part, in corners no song will meet.** Seen
  while reading the Mac's `ChordProSong.swift`: it makes a directive's name small
  with Swift's `lowercased()` and asks `hasPrefix`, which go by whole characters
  (`spec/02`, "To the letter", has everything done on units). A name such as
  `start_of_` followed by an accent laid over the underline is a `start_of_` name to
  the reference and here, and not on the Mac. Nothing writes such a thing. **For the
  maintainer to pass on, or not.**

### At M2

- **Every test of the song passed the first time it was run**, the song having been
  ported from the Mac's line by line. So, as at M1:
- **How the proofs were shown to bite.** Ninety breakages of `Reading.cs`,
  `Writing.cs` and `OpenSong.cs`, one at a time, in a clone made for it (one of the
  ninety changed nothing, to see the count tell). Sixty-nine were caught at once.
  **Fourteen were rules no test held**, and each has a song made for it now
  (`NotHeldByTheTestFilesTests`): two cues with one id; a box of spaces and an em quad
  with no chords; a text box with no settings; chords asked for a shape; a line with
  both kinds of stand-in; a chord on the line break that ends a line of chords alone;
  a slide with nothing written of itself; what a box given to a slide is named,
  cleared of and put on top of; which box a blank slide's is modelled on. Five cannot
  be told from the rule by any input (a file's size, when its bytes are held against
  what was read anyway; looking at a box's other lines before a change of mind;
  whether a first change is marked as joined; and each of two checks that a key is
  already what it is asked to be). **One is held by nothing**: that the file is read
  back and held against what was written. A test would need a disk that does not
  keep what it is given.
- **A case can be written for a breakage and not catch it.** The first case for a
  chord on a line's own break put a new chord on the line: the chord wrongly kept
  landed on the same stand-in and gave way to the new one, and the file came out
  right for the wrong reason. Each new case was run against the breakage it was
  written for, which is how that showed.
- **On Windows two more things can stop a write than on the Mac**: a file marked
  read-only, and a file another program holds open (ProPresenter itself may). Both
  come out as the page's "could not be saved" with the system's own words after it,
  the file untouched and what was being written beside it cleared away; both are
  tests. The swap is `File.Replace`, as `spec/01` asks.
- **Where the Mac's port writes two bytes more than this one.** A text box with no
  settings written at all, asked to have no chords: the Mac's port gives it an empty
  set of settings (Swift makes the part by reaching into it), which reads the same
  and is a change saved; this one leaves it, since the request changes nothing
  (`Writing.cs`). No file to hand has such a text box. **For the maintainer to pass
  on, or not.**
- **Eleven of the twenty-seven cases of `ranges.json`** have the words of a text box
  in a clean test file, and are tried on the file itself (`spec/05`, the fifth
  proof).
- **A heredoc run from this session's shell can lose one of two backslashes, and can
  trip over an apostrophe.** Seen when a throwaway script was to find a line of C#
  with a backslash and an "n" in it. Edit such lines with the editing tool, or from a
  script kept in a file.

### At M3

- **ProPresenter 22 on this PC keeps no `.pro` files.** It has a section of its own,
  near the head of these notes, because it is the maintainer's to decide what
  follows from it.
- **On this PC the app finds no place.** There is no `ProPresenter` in Documents and
  ProPresenter's file of paths is empty. The app says so and offers Choose Folder,
  as `spec/06`, rule 6, has it. (The first editor, by its code, would look here for
  `Libraries` beside itself, say it was not found, and quit: it takes an empty file
  of paths for a folder named nothing. It was not started to see.)
- **The real `.pro` files on this PC are all in OneDrive and "only in the cloud"**:
  some eighty, among them a ProPresenter folder of the old kind and a folder of
  songs with chords. (Which folders, the maintainer was told, and they are not
  written here: these notes are open to anyone.) Reading one fetches it. The look at their RTF that `spec/01` asks of this session was begun
  and **stopped by this session's own permissions**, which would not have the files
  copied to where they could be looked at. So it is still not done, and is the
  maintainer's to allow or to do ("Left rough").
- **A folder in OneDrive that is chosen will have every song fetched**, the first
  time its marks are worked out: a mark needs the file read. Nothing is done about
  that. It is each song once, and the songs are small.
- **A freshly built app takes some eighteen seconds to show its window the first
  time, and one second every time after.** Seen of two builds, 17 and 19 seconds,
  each with this PC busy at other work; the second and third start of the same
  file took one second. Why is A GUESS: it is one file of 141 megabytes that nobody
  has signed, which Windows' own guard reads through before it lets it start, and
  the app unpacks WPF's libraries the first time. For M10, where the file's size
  and what the person is told are to be seen to: someone who has just downloaded
  it may wait so, once, looking at nothing.
- **WPF's look of Windows 11 draws off the screen**, lists, fields and buttons and
  all, dark and light, with no window opened and no app made. It is what the
  pictures are. Three things it needed:
  - an address that begins `pack:` is not known to .NET until WPF has been asked
    something. A test may ask for the look first, so `Look.Dictionary` asks;
  - the dictionary named `Fluent.xaml` has the styles and **no colours**: those are
    in `Fluent.Light.xaml` and `Fluent.Dark.xaml`, which are whole. A view under
    the first alone draws nothing where it asks for a colour, and says nothing;
  - plain words (`TextBlock`) are given no colour by the look, only by the window
    they are in. A view drawn with no window says the colour itself (`LibraryView`).
- **Two threads that each make a view at the same moment trip over each other
  inside WPF**, where a view's description is read out of the program
  (`PackagePart.GetStream`, with nothing of ours in the way). The tests of three
  classes run side by side, and the third time all of them were run one failed
  there. `Pictures.OnAThreadOfItsOwn` now lets one such thread run at a time, and
  six runs of everything since passed. If a test of a view ever fails with nothing
  wrong in it, look there first.
- **The look's own controls are shaped by resources it asks for by name**
  (`ListBoxItemPadding` and many more), which is how the lists' rows are made less
  tall with no style of our own. A list of what it sets on a control can be had by
  loading the dictionary and reading the control's style: that is how these were
  found.
- **What the pictures showed, and what was done.** All eleven were opened, in both
  looks where there are two. Right at the first drawing: the two greys and the bar,
  the two small pictures, the marks, a song's lines with its chords in orange, the
  band, each sentence in place of a list or a song. **Put right**: at its smallest
  the window cut the song's header short (decision 24); and there the middle of
  the three choices over the songs did not fit its words, so each is now as wide
  as its words want.
- **Every test of the library and of the window's model passed the first time it was
  run.** So, as before, **how the proofs were shown to bite**: each rule broken in
  one small way in turn, in two clones made for it, with the tests run after each.
  - Of the library's plain code (`Places`, `Listing`, `Marks`, `AppData`,
    `SongText`, `ProPresenterApp`): a hundred breakages, one of which changed
    nothing, to see the count tell. Eighty-eight were caught at once. **Eight were
    rules no test held**: a folder named as nothing in ProPresenter's file of
    paths; a song whose ending is in capitals; a folder that is one library, called
    by its own name (the case for it had chosen a folder named as the breakage named
    it); that the first editor's copy is a name that ENDS " (Chords)", so spelt
    (three ways of getting that wrong); that one chord is chords; and that what the
    app keeps of its own can be kept a second time. Two cannot be told from the rule
    by any input. One cannot be reached: what is done when the system will not say
    whether ProPresenter is running.
  - Of the window's model, the two lists' arithmetic, the look and the small
    pictures: ninety-seven, one of which changed nothing. Ninety were caught at
    once. **Five were rules no test held**: that a second start does nothing (the
    case for it could not tell); a song's own key and the key it is shown in, told
    apart; a song's slides where it has more text boxes than slides; and a folder
    chosen twice, kept once. One wastes work and breaks nothing ("Left rough").
  Each of the thirteen has a case now, and each case was run against the breakage
  it was written for.
- **`Get-ChildItem -Filter *.pro` in Windows PowerShell also lists `.proPref` and
  `.proPaths`.** It made ProPresenter's settings folder look as if it had fifteen
  songs in it. The app tells a song's ending by itself (`Listing.cs`).

## Left rough, and what to ask

- **Nobody has seen the window.** Every picture is of the view the window holds,
  drawn with no window. What only a hand and an eye at the PC can tell, and the
  maintainer is asked to try:
  - that it opens, in the dark look or the light as Windows is set, its title bar
    with it, and turns over when Windows is switched while it is open (decision 22
    hangs on a switch .NET marks as on trial);
  - that the bar between the two lists drags up and down, stops before either list
    is gone, goes back at two clicks, and is where it was left at the next start;
    and that the edge between the lists and the song drags sideways;
  - that **Choose Folder...** opens the system's own chooser, and that a folder
    chosen is listed, and is there again at the next start;
  - that the band comes within a couple of seconds of ProPresenter starting, and
    goes within a couple of its quitting, with this app in front all the while;
  - that a library of some hundreds of songs lists at once and its marks come in
    after, without the list jumping; the arrow keys in the lists; the tips;
  - that words are sharp on a screen set to another size than the first one's.
- **In Visual Studio, only the listing of the tests has been seen.** The maintainer
  opened the solution in Visual Studio 2026 and its Test Explorer listed the
  forty-nine there then were (2026-10-12), without the two packages named at the
  foot of `Directory.Packages.props`. Opening it changed no file of the repository.
  That the tests pass when run from there, and that F5 starts the app (`App` as the
  startup project), nobody has said. And Visual Studio may one day write the
  solution file again without its comment.
- **Neither the rules nor the song have been tried on a real song, and the RTF that
  ProPresenter on Windows writes has still not been looked at.** `spec/01`, "Not
  known", makes that look the first job of this session, `spec/11` asks it of the
  maintainer before M3, and the Mac's M7 leans on the answer (`mac/NOTES.md`, "RTF
  as ProPresenter on Windows writes it is still not known"): if Windows writes a
  character to stand in after each numbered one, a line of chords alone written
  there cannot yet be given a chord more or fewer. Three checks are wanted, on
  copies, outside the repository, printing numbers and the RTF's own words of
  command and never a word of a song:
  - what each file says wrote it (Windows or a Mac, and which version), and what
    its RTF is written with;
  - every text box read with `RtfWords` and with `tools/look.py`'s reader, which is
    another, and the two set side by side;
  - every box given the chords it already has, a chord more, none, and everything
    undone: every file to come back to the byte. (The Mac's session did this of
    425 songs: `mac/NOTES.md`, "At M2" and "At M7".)
  **Where the files are**: some eighty real `.pro` files in the maintainer's
  OneDrive, all "only in the cloud", in folders the maintainer was told of.
  **Why it is not done**: this session copied nothing of them. Its own
  permissions stopped the copying, as the handling of other people's things, and
  it did not go round that. It wants the maintainer to say it may, or to put
  copies in a folder outside OneDrive and say where.
- **A file that could not be fetched is marked as one that cannot be read, and the
  mark is kept** until the file's size or time changes (`Marks.Read`, as on the
  Mac). A song in OneDrive that is only in the cloud, opened with no network, would
  be so marked and stay so. Not seen; it follows from the code.
- **Where the window is, and how big, is not remembered.** The page says the system
  keeps the window's size for the app, which is true of a Mac and not of Windows:
  here the app would have to. It opens at 1280 by 800 in the middle of the screen
  every time. Small, and not built: for the maintainer to want or not.
- **There is no menu yet.** `spec/10` asks for the window's own menu with every
  command in it. There is one command so far, Choose Folder, and it is a button.
  The menu comes with the commands, from M4.
- **If the system will not say whether ProPresenter is running, it is taken as
  running** (decision 21). No test can make the system refuse, so no test holds it.
- **Marks being worked out for a library that has been left are let go** after the
  few in hand. Taking that out wastes work and breaks nothing, so no test tells.
- **Putting a copy back (`PutBack`) is not in `OpenSong` yet**: it is M4's, with the
  copies.
- **The app has no version number and no icon.** The first is wanted at M4 (a
  copy's index records it); the second at M10.
- **`old/publish.py` must not be run**: it commits, pushes and tags. Its README says
  so now.
- **Two lines of `reference/README.md` are no longer true**, under "Where this and
  `spec/` differ": that the apps leave out a slide with no text box, and that they
  leave a blank slide's box empty when its chords are taken off again. The
  maintainer decided otherwise on 2026-10-11 and the Mac's M7 built both. The first
  also names a heading `spec/01` no longer has. Told to the maintainer; it is the
  Mac session's or the Linux one's to put right, not this one's.
