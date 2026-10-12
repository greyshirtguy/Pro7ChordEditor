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
| **M4** The copies | Done, 2026-10-12, bar what only the maintainer can try | A copy of a file kept before it is changed, named for when, with an index beside it; listed, thinned at the start, looked at, put back and that taken back; a mark in the list, Earlier Versions over the window, All Copies in the settings; the window's own menu. The proofs at the foot of `spec/07`. **Nothing in the app makes a copy until there is an editor**, so there is nothing of it for the maintainer to try yet but the menu and the settings ("At M4") |
| **M5** The ChordPro editor | Done, 2026-10-12, bar what only the maintainer can try | The song as ChordPro text, typed in: what may be typed, the help with brackets, a save a moment after the typing stops, refusals, the box's own undo, colours, the size of the text; the two keys as pickers; Revert, and the words that say why there is no Save button. `spec/04`, and `spec/06` rule 12a. It has been started for real, on a made-up library of the test hymns, and a chord put on a song there ("At M5"). **Nobody has typed into it by hand, and no song chorded with it has been looked at in ProPresenter**: that is the maintainer's, on a COPY of a real library |
| **M6** The chord sheet | Done, 2026-10-12, bar what only the maintainer can try | The sheet a song now opens on: every slide a card in a frame of its group's colour, in a grid; the spot that follows the pointer; 1 to 7, a letter and the bubble a chord is typed in, Delete, the arrow keys; dragging; a line's chords copied onto another; undo and redo; the zoom; the strip of the key's chords; the switch between the two editors in the song's header; and the settings' Chords section. `spec/03`, `spec/06` rules 12 and 20a. Every case of `sheet-model.json` and `layout.json` passes. It has been started for real and looked at ("At M6"). **Nobody has pointed at it with a mouse, nor pressed a key on it**: that is the maintainer's, and it is most of what M6 is for |
| **M7** Chords with no words | The song's part done at M2, the editors' at M5 and M6 | Both editors ask it now, and a chord put on a blank slide and taken off again leaves the file byte for byte what it was (a test, by a hand at the sheet). What is left: the maintainer seeing in ProPresenter a song so changed |
| **M8** Mending | Done, 2026-10-12, bar what only the maintainer can try | What the first Windows editor left in songs, found and put right: the three checks, asked of a file; a mark in the list and the list's fourth choice; Mend for a song and Mend All for a list of them; a key that does not fit the chords, offered to be set; chords that look moved to the left, shown both ways before they are moved, behind a setting that is off; a "(Chords)" copy's chords put into the song it was saved beside, and the copy moved out of the library. `spec/08`, and `spec/06` rules 9 and 11. The three proofs at the foot of `spec/08` pass, and more than they ask: **each damaged test file, put right, is the clean file it was made from, to the byte**. It has been started for real on a made-up library and worked through ("At M8"). **Nobody has clicked any of it by hand, and it has been near no real song**: both are the maintainer's, on a COPY of a library. **The Mac app has none of M8 yet**: what was decided here where the page is silent is in the decisions below (78 to 93) for the maintainer to read, and for the Mac to have the same |
| **M9** ChordPro files | Built, 2026-10-12. **Its proof is the maintainer's, and is not done** | A ChordPro file read from a disk and shown before anything is made of it; a new song made from it, as a copy of a song that is known to be good; and the song that is open written out as a ChordPro file. `spec/09`. What could be proved here is: a song made from each ChordPro file of the test data reads, in this app, as the presentation Simple Presenter made from the same file; a slide of it is the model's slide but for its ids, its words and what is said of stretches of them; nothing of the model song's own is in it; and the file is made new, never over one that is there, and not while ProPresenter is running. **No song made here has been opened in ProPresenter**, which is what the plan asks of M9 and the page asks before this is offered to anyone: on both systems, shown, and its chords seen on a stage screen. **Nobody has clicked any of it by hand**, and of the system's own choosers of a file only the first has been seen to come ("At M9", which also says what went wrong in trying). **The Mac app has none of M9 yet**: where the page is silent, what was decided here is in decisions 94 to 112, for the maintainer to read and for the Mac to have the same |
| **M10** Giving it out | **Begun**, 2026-10-12: only what needs nobody's decision | `make-zip.ps1` makes the zip that is given out (the app as one file, and a read-me) and does nothing with it; `zip/Read Me First.txt` is that read-me, the Mac's with what is Windows' own; and the settings have a fourth section, About. **Not done, and the maintainer's**: the icon; the words of About and of the read-me, which are proposals; whether to pay to have the file signed; the version's number; and **a release, which nobody is to make before a song made at M9 has been opened in ProPresenter and the app has been tried on a copy of a real library** |

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

And at M4, likewise this session's, and the maintainer's to change:

30. **The copies are ported from the Mac's `Copies.swift`**, and a copy's index is
    written with the Mac's names for things (`file`, `made`, `why`, `copyOf`,
    `songID`, `songName`, `chords`, `key`, `size`, `appVersion`), the three reasons in
    the page's words. Neither app reads the other's, but someone who looks into both
    finds one thing.
31. **A name is made fit to be a folder's by Windows' rules as well as the page's.**
    `spec/07`, rule 7, turns a slash either way, a colon and a line end into a
    hyphen. On Windows a name may not have `< > " | ? *` in it either, and Windows
    drops a full stop or a space from the end of one, so that two songs' names could
    come to one folder. Those become hyphens too (`Copies.FolderName`). **For
    `spec/07`, if the maintainer agrees**: it is Windows' own, and the Mac need do
    nothing about it.
32. **When a copy was made is kept to the second, by the clock at Greenwich**; its
    name is that in the computer's own time, as the page asks. A name that says an
    hour the clocks skip in spring (no copy is made in one, but a name can be typed)
    is read as the hour after, and not as no copy.
33. **The app's version is 0.1.0**, said once (`Directory.Build.props`), as the Mac's
    first build is 0.1. A copy's index records it. What it is when there is a build
    worth giving anyone, the maintainer says.
34. **Earlier Versions is a panel over the window, not a window of its own**: the
    window is dimmed behind it, and Esc, Done or a click on the dimmed part puts it
    away (`spec/07`, rule 17). So a test draws it as it draws the rest, and it cannot
    be lost behind the window. The button for it is in the song's header, where the
    page has it (rule 15).
35. **The window has its own menu**, as `spec/10` asks: File (Choose Folder, Settings,
    Exit) and Song (Earlier Versions). **The settings are a window opened over the
    app's**, to be closed before the app is gone back to; it has one section so far,
    Backups, which is All Copies.
36. **Putting back a copy that is already what the file is, is nothing to do and
    offers nothing to undo** (`spec/07`, rule 21). The Mac's model goes on to offer
    Undo all the same, and Undo would then put back whatever copy happened to be the
    newest. **For the maintainer to pass on to the Mac's session.**
37. **A song's marks in the list are worked out again as soon as a copy is put back**,
    and whether it has copies: the list is never a file behind.
38. **How much room the copies take is said as Windows counts it** (1024 to the
    kilobyte: "23 KB", "1.2 MB"), by a few lines of our own, the same on every PC.
    Their folder is shown in File Explorer; a copy is saved with the system's own
    asking for where.
39. **The names of days and months in "when a copy was made" are the person's own
    language's**, as the system has it. On this PC September is "Sept".
40. **The ChordPro editor is WPF's plain text box, with its colours drawn behind it,
    and no dependency.** `spec/10` offers a plain text box, or AvalonEdit "if
    colouring is wanted (a dependency: ask first)"; `spec/04`, rule 4, wants the
    colouring. A plain box has one colour for all its text. So the box draws its own
    letters in no colour at all, and behind it the same letters are drawn in theirs,
    exactly where the box has its own (`ChordProUnderlay`): the box's caret and what
    is selected are seen over them. AvalonEdit was not asked for, and is not needed.
    A test draws the two and holds them dot for dot the same, scrolled, larger and
    smaller.
41. **A change is held to the rule after it has happened, and put back.** WPF's text
    box does not ask before it changes, as the Mac's does. So, as the reference
    does and as `spec/04` rule 7 has it: whatever has just happened to the text (a
    key, a paste, a cut, a drop) is asked about as the whole text it has left, and
    what is not the song's words is put back at once, before it is drawn, with the
    caret where it was. One road for every kind of change (rule 8).
42. **The box's undo is the app's own, not WPF's** (`TypingHistory`, in
    `ChordProTyping.cs`). WPF's would remember both a change that was refused and
    its putting back, and Ctrl+Z would walk back into what was refused. It is
    switched off, and what was let through is remembered instead: a step for each
    run of typing where the last left the caret, or of deleting, as WPF's has it; an
    undo or a redo ends a run; three hundred steps. Ctrl+Z, Ctrl+Y and Ctrl+Shift+Z
    while the box has the keyboard, and Undo and Redo in the Edit menu.
43. **The help with a bracket TYPED is given whatever keys typed it.** `spec/04`,
    rule 12, has the help withheld while Ctrl or Alt is down. But on many keyboards
    "[" and "]" are typed with AltGr, which Windows reports as Ctrl and Alt together;
    and a "[" that did not bring its other is a stray bracket, which rule 6 never
    lets in. Someone with such a keyboard could then type no chord at all. So what
    arrives as typed text is helped, as the rule's own words for the Mac have it.
    Backspace and Delete are helped with none of Ctrl, Alt and the Windows key down;
    and Delete not with Shift, which on Windows is Cut. **For `spec/04`, rule 12, if
    the maintainer agrees**: its first sentence is true of Backspace and Delete on
    Windows and not of a bracket typed. Nobody has tried it on such a keyboard.
44. **The system's sound for "no" is made when a key is not taken**, as on the Mac
    (`spec/04`, rule 7). Windows' own boxes make it where they will not take a key
    (a box that is full), and say nothing where a box is only to be read; so this
    one is silent while nothing can be typed at all. **A guess that it is wanted**:
    for the maintainer to hear, and say.
45. **What the box holds is always the text as the song counts it**: a line break is
    one unit. What is pasted or dropped on Windows has two to a line break; they are
    made one on the way in, so that the whole text can be copied out, chorded in
    another program, and pasted back.
46. **Input methods are switched off in the box.** Only chords are typed there, and
    their names are plain letters; a character still being composed would be asked
    about half made, and refused. **A guess**: nobody with such a keyboard has tried
    it. Keys that only wait for a second key to make an accented letter are another
    thing, and are not touched.
47. **Ctrl with the mouse's wheel makes the text larger and smaller**, as it does in
    Notepad, beside the keys of `spec/04`, rule 25. `spec/10` has it "if the
    platform's own views do that": WPF's box does not by itself, and Windows' own
    plain editor does.
48. **The text is in Cascadia Mono, or Consolas where a PC has not that, at 14**
    where it is its usual size, as the Mac's is 14. Chords are bold as well as
    orange, as the Mac draws them: in both fonts a bold letter is as wide as a plain
    one, so the letters after a chord stay under the caret that is at them (a test
    holds it, in whichever of them the PC has). The orange is the page's `#ff9500`,
    Windows having none of its own.
49. **Why a song cannot be changed as ChordPro is said in the system's own colour for
    a caution** (`spec/04`, rule 20: "in a warmer colour"): an amber in the light
    look, a yellow in the dark.
50. **The song's header lays itself out three ways**, the first that fits
    (`SongLook`): one row; the name over the keys, with Earlier Versions at the right
    of the keys' row; and, at the window's least, Earlier Versions beside the name
    with the keys by themselves. They are the Mac's three, less the switch between
    the two editors, which comes with the sheet.
51. **Revert's question is drawn in the window**, as the earlier versions are, and is
    not the system's own message: that names its buttons itself, and this one's is
    to say "Revert All Changes". Cancel is what Enter, Esc and a click on the dimmed
    window give.
52. **The menu**: File has Save (Ctrl+S) and Revert to as Opened, as the Mac's has;
    and there is an Edit menu, with Undo, Redo, Cut, Copy, Paste and Select All,
    each sent to whatever has the keyboard, which a Mac has without being asked.
53. **The log says "Saved" only of a file that was.** After a save that was refused
    before anything was written, the Mac's log has "Saved" after "was refused"; this
    one has not. (Each app's log is its own.)
54. **What was refused because ProPresenter was running is said no longer once
    ProPresenter has gone**, whether that is seen by the window's look every two
    seconds or when the app comes to the front.
55. **A key that is not one of the twenty-six is listed first in its picker**
    (`spec/02`: "so that the song's own key can always be seen and chosen"), by
    `LibraryModel.KeysListedFor`. **For the maintainer to pass on**: the Mac's
    pickers in the song's header list the twenty-six and "None" only, so a song in
    C# major would show nothing there.
56. **Every row of every list says what it shows to a screen reader** ("At M5").
57. **The settings' Chords section waited for the sheet**, and came with it at M6
    (decision 75). `spec/06`, "Settings", has each colour shown "as it will look",
    one of them a chord in its bubble, which was the sheet's to draw.
58. **The sheet is one element that draws itself, in two layers** (`spec/10`: "one
    custom-drawn element ... and does its own hit testing"). Under: the slides, their
    words and their chords, which change when the song does. Over: the ring round the
    slide that is picked, and the spot. So the pointer moving draws only the little
    that moves with it, and the spot keeps up with it. The bubble a chord is typed in
    is a third thing, a child of the element, over both.
59. **Each line's words are one line of WPF's own layer for text, and that same line
    is what is drawn and what is measured** (`SheetMetrics`). `spec/10` says
    "measuring with FormattedText or GlyphRun": this is what a FormattedText is made
    of, and it says where every unit of the line starts, which is what a tail has to
    stand on. A place is a UTF-16 unit, as that layer counts too.
60. **Where the slides are, and what is under a point, is plain code with nothing
    measured in it** (`SheetGrid`), handed what things measure. So the Mac's numbers
    are held here to the pixel, with letters of a fixed width and no window.
61. **The bubble's box is WPF's own text box**, in a small panel of the sheet's
    colours that does not zoom. What it may hold is the model's, held after the fact
    as in the ChordPro text (decision 41): a key that is not taken is undone at once,
    with the caret where it was. Input methods are off in it (decision 46). **No
    sound is made for a key that is not taken here**; the ChordPro text makes one
    (decision 44). For the maintainer to want or not.
62. **The bubble is put in its place as the sheet is laid out**, not when what it
    offers changes: how tall it then is, is not known until it has been laid out
    ("At M6").
63. **1 to 7 are taken by where they are, and letters by what they type**: the keys
    of the top row and of the number pad, with Shift or without (on a keyboard that
    has its numbers on Shift they are still one key each); A to G as the keyboard's
    own layout has them. Ctrl with plus is the plus key with Shift or without (which
    is the equals key), or the plus of the number pad. Nothing with Alt or the
    Windows key held is the sheet's.
64. **A letter pressed on the sheet opens the bubble with that letter in it, once.**
    The key is answered by the sheet, and WPF does not go on to type a key that was
    answered into whatever has the keyboard next. **Reasoned from how WPF hands keys
    on, and NOT SEEN**: no key has been pressed in a real window. If the letter is
    ever there twice, this is where to look. For the maintainer to try first.
65. **The sheet takes the keyboard when the pointer moves over it** (rule 23a), if its
    window is the one in front and no box of text in the window is being typed in.
66. **None of the strip's buttons takes the keyboard when it is clicked.** A click on
    one of the key's chords while a chord is being typed puts that chord in place of
    what was typed (rule 39); a button that took the keyboard from the box would have
    ended the typing first, and kept what was typed.
67. **"The last thing copied on this computer"** (rule 52b) is told by the system's
    own count of changes to its clipboard. A line's chords are not put on the
    clipboard; so while the count is what it was when they were copied, nothing has
    been copied since by any program. (The Mac asks its pasteboard the same.)
68. **The bar that scrolls the sheet down is always there.** How many slides go to a
    row is worked out from how wide the sheet is seen to be, and a bar that came and
    went with the song's length would change that width as it did. What scrolls the
    sheet wears the dark look whatever the system is, for its bars.
69. **Laid out again, the sheet is scrolled at once** to where the slide at the top
    was, not when the window next lays itself out ("At M6").
70. **While the ChordPro text is shown the sheet is not laid out nor drawn, and when
    it is come back to it is where it was scrolled to.** (On the Mac a sheet come
    back to starts from its top. Neither app yet ties the caret of the text to the
    slide picked on the sheet: "Left rough".)
71. **The header's three ways, with the switch in it** (`SongLook`): one row, with
    the switch in the middle of the pane where the name and the keys can each be read
    in half of what it leaves, and otherwise between them where they leave it; the
    name and the switch over the keys; and a row each. The Mac's ways in the Mac's
    order. **How they are laid is in "At M6"**: twice the first way of doing it went
    round for ever.
72. **The switch is, to the system, two of a choice of one** (radio buttons): a
    screen reader says which is chosen, and the arrow keys go between them. How they
    look is the app's. After a click on it, and after Ctrl with 1 or 2, **the
    keyboard goes to the editor that is then shown**, so that what is typed next
    goes where the eye is. Ctrl with 1 and 2 are the window's whatever has the
    keyboard, and change nothing while something is asked over the window.
73. **The menu has a View**: the two editors, the one shown ticked, and Larger,
    Smaller and Usual Size, which are of the editor that is shown. `spec/10` asks
    for every command in the menu; the Mac has these on keys and buttons only.
    Copying a whole group has only its keys, as on the Mac.
74. **The quiet line under the sheet says what the keys do, on up to four lines**,
    and is kept as tall as that needs whatever it says, so that the sheet does not
    grow and shrink by a line each time something is said for a few seconds. The
    names of keys in it are kept whole where a line is broken ("At M6").
75. **The settings are tabs: Chords and Backups** (and, since M8, Mending between
    them: decision 86). A chord's colour is chosen with
    the system's own picker, asked directly of the system (one function of
    `comdlg32`): WPF has no door to it, and the other part of .NET that has would be
    a second way of making windows to carry. It is no dependency. The sixteen
    colours a person mixes in it are kept while the app runs.
76. **A screen reader is told the sheet is there**: its name, that it is a chord
    sheet, and what the keys do on it. An element that draws itself says nothing of
    itself otherwise ("At M6"). It is told no more than that: a song's lines and
    their chords are read from the ChordPro text. **For the maintainer to say**
    whether more is wanted; the Mac's sheet says its name only.
77. **Ctrl with the mouse's wheel zooms the sheet**, as it does the text (decision
    47): the page gives two fingers on a trackpad, which a PC may not have.

At M8, where `spec/08` is silent or can be read two ways. **The Mac app has not built
M8**, so each of these is the first word on it and not the last: the maintainer's to
change, and then both apps'.

78. **The checks look at every text box of every slide in the file**, shown by the
    editors or not: of every cue, switched on or off, every action that is a slide,
    every element that has text. The page does not say which. The reference
    (`tools/make-fixtures.py`) goes through every text box of the file, and the old
    editor wrote to the first element of every slide of every cue whatever it was.
    No test file has a chord on a box the editors do not show, so `repairs.json`
    does not tell the two readings apart. A song is marked for what is in its file,
    and mending it mends all of it.
79. **A mending writes every text box that has chords, and not only those in which a
    range runs over a line break.** The page says "the box's chords". But the old
    editor wrote every range of a song its own way, and on a line of chords alone its
    way runs a chord on to the next and crosses no line break: check 1 does not find
    such a box. `repairs.json` has it put right all the same (the last box of Plain
    Song), and its `putRight` is what the second proof is held to. A song in which
    check 1 finds nothing is not touched, whatever its ranges: so the lines of chords
    alone that Simple Presenter 0.8 to 0.82 wrote are left as the page says.
80. **A mending is one step of undo of its own kind, and one save**, however many
    slides it changes. The song's other changes are a step for each slide, joined;
    two hundred steps are kept; and a song of more slides than that, mended so, could
    only have been half unmended. A test has a song of 254 slides.
81. **A chord that is on no character (check 0) is handed to BOTH editors where it
    will be written**: the last character, or the character before the line break.
    The page asks it of the sheet. Without it the ChordPro text would not show such a
    chord at all, a song would have a chord in one editor that it had not in the
    other, and typing in that text box would drop the chord unseen (`spec/04`, rule
    24). It is done once, where the editors are handed their text boxes
    (`Reading.ChordsHanded`); nothing is saved for it until the box is next written.
    **For the maintainer to say**, and for the Mac's text.
82. **"Wants mending" is: chords shown twice, or a key that does not fit the
    chords.** And chords that look moved to the left, only while that check's offer
    is switched on (decision 86). **Not a song that states no key**: nothing in it is
    broken, and most songs typed by hand state none. The sum that doubts a key is
    used for it all the same, on the sheet's strip: "The song names no key: pick the
    one its chords are in. They look like D." Where there is not room for both
    sentences the page's stands alone.
83. **The list's fourth choice is on a row of its own, and is there only where the
    library has a song that wants mending** (or while it is the one chosen, so that it
    can be seen to be). There is no room for four where the lists are at their
    narrowest; and most libraries have no such song, and need not be asked about one
    for ever. It says "Wanting mending". **Mend All** is beside it while it is chosen.
84. **Mend All** mends each song of the narrowed list in turn as the open song is
    mended: asked the same things first, a copy kept, one save. It asks first, over
    the window (how many songs, how many chords, that nothing is moved or renamed,
    that a copy of each is kept); shows how far it has got, with **Stop**, which lets
    the song in hand be finished; **stops at the first song refused because
    ProPresenter is running**, since every other would be too; and then lists what it
    did, by name, a song that was not mended with why. The song that is open is
    mended as the song that is open: it is a step of undo there. Nothing else can be
    begun while it is at work.
85. **A key is said in words in a sentence, and as the lists have it on a button**:
    "This song says it is in A flat, but its chords look like F sharp minor. [Set Key
    to F#m]". The page's own sentence spells it out; the lists of keys in the header
    say `Ab` and `F#m`.
86. **Check 2's offer is behind a switch in the settings (Mending), off as the app
    comes**, as the page asks until the old editor's fault has been seen in a real
    file. What the check finds in each song is worked out and kept whether or not it
    is offered, so switching it on marks the songs at once. Switched on, such a song
    says so with **Show Me**; that shows the song on the sheet as it would be, and
    the row then says "This is the song with those chords moved to where they look to
    have been meant. Nothing has been changed." with **Show As It Is** (and then
    **Show Moved**), **Move the Chords** and **Leave Them**. The song shown as it
    would be can be looked at and nothing else: what is done to it is refused, and
    says why. Whatever changes the song, shows the other editor or opens another
    song shows it as it is again.
87. **Putting a copy's chords into its original** (the page's "folding"), step by
    step:
    - a "(Chords)" copy that is open, whose original is beside it with the same
      words, says "This is a copy that the old Windows chord editor saved of "X",
      which has no chords." (or "which has 12 chords of its own"), with **Put Its
      Chords into "X"...**;
    - that asks, over the window: "Put this copy's chords into "X"?", then which song
      is given how many chords of which, its key if that changes ("and its key, G, in
      place of D"), what of its own is replaced, that the words are the same and are
      not changed, and that a copy of each file is kept first. **Put the Chords In**,
      and **Cancel**, which is what Enter gives;
    - yes: a copy of the copy's file is kept; **the original is opened in the copy's
      place** and given the chords and the key as **one step of undo there**, with a
      copy of its own file kept first. So what is then seen is the song playlists
      point at, with the chords in it, and Ctrl+Z takes them out again;
    - and at once: "What of the copy, "X (Chords)"?" with **Move It Out** and **Leave
      It**, which is what Enter gives.
88. **A copy moved out of the library is its file kept among the app's copies, under
    its own name, and then taken from the library's folder.** It is taken only once a
    kept copy has been read back and is the file to the byte; not while ProPresenter
    is running; and it is looked at first (a song of this library's list, named as
    that editor named its copies, and there). Settings, Backups lists it, and can
    save it as a file again. It is never simply deleted. Its reason is written as
    "before mending", there being no other.
89. **A copy that was left says so when it is next opened**: "...saved of "X", which
    has its chords and its key already.", with **Move This Copy Out of the
    Library...**, which asks as above. So "leave it" can be thought better of.
90. **Where the words of the two differ, only opening the other is offered**: "The
    words of the two are not the same, so its chords cannot be put into that song
    from here. [Open "X"]". A copy with no chords offers nothing.
91. **The marks are kept with a number that says which reading of the files made
    them** (2, at M8). Marks kept by a build that knew less are worked out again:
    without that, a song that wants mending would go unmarked until its file changed.
92. **The panel that asked Revert's question asks whichever question the model has**,
    and is `QuestionPanel`. Its second button is named by the question ("Cancel",
    "Leave It") and is what Enter and Esc give. Mend All has a panel of its own, for
    its progress and its list.
93. **The list's four choices answer being switched on by a program, as by a click**
    ("At M8").

**M9, where `spec/09` is silent.** The page says the new song is the model "copied,
with a new id and name, no groups and no slides", and each slide "a copy of the
model's first slide with new ids, whose text box is given the slide's words and
chords". Each of these is what was built where that does not say enough. None is
known to be what ProPresenter wants: that is what the maintainer's look in
ProPresenter is for.

94. **Which slide is "the model's first".** The first, in the order the song is
    shown (the groups' order, not the file's: in the one file to hand that
    ProPresenter wrote, the first cue of the file is the third verse's), that has a
    text box with words to read; failing any, the first that has a line of chords
    alone. A song often begins with an empty slide, or with an intro of chords, and
    neither has words to be made like. A song with neither cannot be a model, and
    says so: "“X” has no slide with words on it, so a new song cannot be
    made to look like it. Nothing was made."
95. **A slide is copied as it looks, and does nothing else.** In the file a slide is
    a cue, and a cue may also start a video, run a macro, clear a layer, go on to
    the next slide by itself after a time, answer to a key, and carry a label and
    notes. Those are what that ONE slide of the model song does and says. Copied
    with it, the new song would do them on every one of its slides: a macro that
    dims the lights, at every click. So of the cue only its slide is copied (the
    first of its actions that is one, as the reader takes it); it has no name, no
    going on, no key and nothing waiting to be brought in; its label says nothing
    and has no colour; and its notes and its chord chart are left out. **This is
    the decision most worth the maintainer's eye**: a background that a song's first
    slide starts is, to the person, part of how the song looks, and it is not
    copied.
96. **What is the model SONG's own is not the new song's**: its notes, its chord
    chart, its arrangements and which was chosen, its timeline, when it was last
    used, a Bible reference, a slide show's timing, the old way of writing a key
    (`music_key`), and Multitracks' licence (rule 15). **Kept**, as how the model's
    songs are: its category, its background, its transition, where it is shown, and
    what wrote it. A chord chart and a timeline that were there are left there,
    empty, as ProPresenter writes them for a song that has none.
97. **The CCLI block is made anew**, with the file's title, artist and copyright and
    its number (rule 14), and of the model's only whether the block is to be shown.
    Its author, its year, its album and its artwork are another song's. A file that
    says none of these, made like a song that has no such block, has none.
    **The number is written if it is nothing but digits**: "+22025" and "CCLI 22025"
    are not numbers as written. (The reference's would take the first.)
98. **A key the file does not state is not stated.** The model's is not kept: it is
    the model song's. (This app then offers to set one, if the chords look like
    one: `spec/08`, check 3.)
99. **Which text box takes the words**: the slide's first that shows words of its
    own and has words. **Any other text box of the slide that shows words of its own
    is given none**: they are the model song's (a second language, a title), and
    would be on every slide. Its RTF keeps its opening, so what is typed into it in
    ProPresenter is set as it was. A text box that shows something else by a link
    (the next slide's words, a clock) is left exactly as it is, the model's words
    inside it and all: they are not shown.
100. **What a text box says of stretches of its words is brought over only where it
    was said of the model's first character, and is then said of the whole of the
    new words.** Beside its RTF a text box keeps a list of things that RTF cannot
    say (that words are in capitals, a gradient, a first font; the chords are in the
    same list), each over a stretch named by places in the MODEL's words. In the
    file ProPresenter wrote, every slide has one, over exactly the whole of that
    slide's own words: so that is the shape a slide made here is given. One that
    was over some later words of the model is left out, the words being gone. This
    follows the RTF, whose opening sets all the new words as the model's first were
    set. **It is NOT what `spec/01` has for a text box given stand-ins on a blank
    slide**, which leaves the list as it was ("a guess"). Left as they were here,
    a slide of more words than the model's would be in capitals for its first
    fifty-four characters and not after, if ProPresenter goes by the list. For the
    maintainer to say, and whether M7's box should do the same.
101. **The ids.** New: the cue's, its action's, its slide's, each element's, and each
    of an element's builds'. Every other id inside the cue that names one of those
    is changed to match, in whatever capitals it was written; an id that names a
    thing outside the slide (a timer) is left. This is what ProPresenter's own file
    shows: each of its slides has its own four ids, and the link that says when its
    text box is seen names that box's own id, on every slide. A new group has a new
    id, a name, a place for a key with nothing in it, and no colour (rule 13).
102. **How many chords a file has is said as they will be kept**: two written on one
    place of a line are one, the later, as the rules keep them (`Chords.Tidy`).
103. **A name.** As the reference tidies one (white space of any kind is one space;
    a slash is a hyphen; no full stop at its start; nothing left is "Song"), and
    what is Windows' own: each of `\ : * ? " < > |` and anything below a space is a
    hyphen too; no full stop or space at its end; a name Windows keeps for a device
    (CON, NUL, COM1) is given a number as a name in use is, and a full stop after
    one is a hyphen ("Nul.x" is the device still). **The song's name inside the file
    is the file's name**, number and hyphens and all, as in the reference. So a song
    titled "Psalm 23: The Lord" is "Psalm 23- The Lord" made here, and would keep
    its colon made on a Mac: **for `spec/10`**, and the maintainer may want the Mac
    to do the same so that a library kept where both read it has no file a PC
    cannot have.
104. **Nothing is made while ProPresenter is running**: "ProPresenter is running.
    Quit it to make a song here. Nothing was made." The page does not say so of a
    new file; `AGENTS.md` says nothing is written then, and a new file in
    ProPresenter's own library is written.
105. **A new file is never written over one that is there.** It is written beside
    where it is to be, under a name that is no presentation's, pushed onto the disk,
    and moved to its name in one step that the system refuses if the name is taken;
    then read back. The moving is the asking: nothing can take the name in between.
106. **Before a byte is written, what was made is read as any song is read**, and
    must be the name, the key, the groups, the slides, the words and the chords that
    were asked for, with one text box shown on each slide and no id twice. This is
    rule 12's "read it back" asked of the whole song. Made rightly it always passes:
    it is there for a model song of a kind nobody thought of.
107. **The song the app carries is the one file to hand that ProPresenter wrote**,
    *Rock of Ages* in `testdata/files/made-by-propresenter`, pointed at from there
    so that there is one copy of it. **"Whether it will also do as the model of a
    slide is M9's to say" (the plan): it does.** Its first slide has one text box of
    words, and a song made like it reads as it should. **But its look is the
    maintainer's own library's**: the words in capitals in Century Gothic, with a
    band of colour behind each line, in a box sized for two lines. A stranger with
    an empty library would get that. For the maintainer to say whether that is
    wanted or a plainer one is to be made; and where the file should live, since
    the Mac app must carry the same bytes.
108. **"Make it look like" offers the song that is open first**, if one is open and
    can be read; else the song the app carries, which is listed first as "A song
    this app carries", before the library's songs as they are listed. The page has
    the person pick, "failing that" the app's own; that the open song is the one
    offered is this session's.
109. **What is shown before a song is made** (rule 8) is a panel over the window:
    "New song from amazing-grace.cho"; then Title, Artist and Key (each "None given"
    where the file gives none), Parts ("Verse 1 (4 lines), ...", a part the file
    did not name being "a part with no name"), and Chords ("40 chords in all", or
    "None"); then the name in a field, the library named, the lines to a slide in a
    box of one to twelve with "which makes 7 slides" beside it, and the song to
    look like. Under the name, only where the song will not be named as typed:
    "“Songs” has a song named “Amazing Grace” already. This one
    will be named “Amazing Grace 2”." or "As a file can be named, that is
    “A - B”." **Make the Song** is what Enter gives (this is a form to be
    sent, and a song made by mistake is one more file); Cancel is what Esc gives; a
    click on the dimmed window does nothing, the name perhaps having been typed.
    Afterwards the song is listed and open, and the quiet line says "“Amazing
    Grace 2” was made from amazing-grace.cho: 4 parts, 7 slides, 40 chords."
110. **A file that is no song says so in the same panel**, with only Close: the
    reference's three sentences with the file's name first ("X could not be read:
    ...", "X is too large to be a song.", "X has no words in it."). What a making
    was refused for is said there too, and the panel stays, so that another name or
    another song to look like can be tried; while ProPresenter is running the panel
    says so beforehand and its button is greyed, whatever was refused before (that
    is said again once ProPresenter is quit).
111. **The two commands are in the File menu**: "New Song from ChordPro File..." and
    "Save Song as ChordPro File...". The first is greyed with no library open, the
    second with no song open that can be read, and **both while anything is over
    the window** (a question, Mend All, the earlier versions, a new song being asked
    about): a menu is reached by Alt and a letter though a panel lies over it. The
    chooser of a file to bring in shows `.cho`, `.chordpro`, `.chopro`, `.crd` and
    `.txt`, or all files; **not `.pro`**, which some use for ChordPro and which is
    here a presentation.
112. **A song written as ChordPro is rule 18 to the letter**, with what was being
    typed in the song saved first. **Read back, it does not always give "the same
    parts"**, though the page says that is its test ("At M9"): for the maintainer.

**M10, begun.**

113. **About is the settings' fourth section**, and not a menu called Help: the app
    has no help to put in one, and is not to promise any. It says the app's name;
    "Version 0.1.0", which is the build's own number (`Directory.Build.props`);
    "Built from 15ac783 of its source.", the commit .NET writes into the build,
    shortened as git shortens one, so that one build can be told from another; and
    then what the Mac's read-me for its early build says of the app, word for word
    (what it does; that it is one person's hobby that nobody supports; that it is
    not ProPresenter), and where its source can be read, as a link. **The words are
    a proposal**: no page gives them. On the Mac they would be in the system's own
    About panel.
114. **`make-zip.ps1` is for Windows what `mac/make-dmg.sh` is for the Mac**: it
    publishes the app into a folder of its own, looks at what it built (one file,
    and its version), lays out the app and the read-me, zips them, takes the zip
    apart again and holds what comes out against what went in. **It makes a file
    and does nothing with it**: no commit, no tag, nothing sent. (The first
    editor's `old/publish.py` did all three.) What it makes is under `build/`,
    which git leaves alone.
115. **The read-me in the zip is the Mac's**, section for section, with what is
    Windows' own: where the app keeps its copies; that there is one file and
    nothing is installed; what Windows says of a file nobody signed and how to go
    on; that the first start takes some seconds; what to do where no library is
    found; and that a song made from a ChordPro file has not yet been opened in
    ProPresenter. **"Windows protected your PC", "More info" and "Run anyway" are
    written from what is known of Windows and have not been seen on this PC**
    (Windows says them only of a file that came through a browser). The page asks
    that the README "say exactly what the person will see": the maintainer is
    asked to hold those lines against what Windows does say.

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
- **A song was opened with no safeguards at M3**, so that it could not be changed.
  From M4 it has its real ones (below).

### The copies (M4)

- **In `Song`**: `Copies.cs`, which keeps, lists and thins; and `OpenSong.PutBack`,
  which is a save like any other and is the only way a copy gets back into a song's
  place. `AppData.Copies()` says where they are: `Backups` in the app's own folder.
- **`LibraryModel` opens a song with its real safeguards**: whether ProPresenter is
  running is asked before every save, and `Copies.Keep` is asked for a copy before the
  first. A song is known to its copies by the place's NAME, the library's and its own
  (`KeptSongOf`).
- **In `AppViews`**: `EarlierVersionsPanel` shows what `LibraryModel.Earlier` has;
  `AllCopiesModel` is All Copies with no drawing in it, `AllCopiesView` shows it, and
  `SettingsWindow` frames that. `SongLines` draws a song's lines wherever a song is
  only looked at: the song itself, a copy in the panel, a copy in All Copies.
- **Nothing in All Copies changes a song.** A copy is looked at there, or saved as a
  file of its own. Putting one back is done from the song, where it is open and its
  safeguards are asked.
- **Only `Copies.Thin` deletes anything of the person's**, and only what is named as
  a copy, in one song's folder of copies. Keep it so.
- **The tests' songs have a history made for them** (`MadeUpPC.Change`,
  `GivePlainSongAHistory`): a song changed as the app would change it, some days ago,
  with a copy kept first and dated then.

### The ChordPro editor (M5)

- **In `Song`**: `ChordProEditor.cs`, the Mac's file for file. The song as one piece
  of text; THE ONE RULE of what may be typed (`Allows`); the help with brackets;
  what a pause in the typing saves; where the text is coloured. No window in it.
- **In `LibraryModel`**: when to ask and when to save. `TextMayBecome` asks the
  editor and starts the wait; `Settle` saves at once, and is asked first by
  everything that leaves the song or does something else to it, which is then not
  done if the save was refused. The two keys, Revert, the key that would save, the
  size of the text. Every sentence the person reads is here.
- **In `AppViews`, three files for the box**:
  - `ChordProTyping.cs` has no box in it: the one change between two texts, line
    breaks as the song counts them, and the box's undo. Tested so.
  - `ChordProText.xaml` and its code are the box: how WPF's text box is kept to the
    rule (decisions 41 to 46). Two doors come before the box's own doing, a
    character typed (`TextTyped`) and a key pressed (`KeyPressed`), and both can be
    called by a test with whatever keys it says are held.
  - `ChordProUnderlay.cs` draws the letters in colour behind the box (decision 40).
    It draws only when the box has finished laying itself out, since only then are
    the places true; so everything that changes what is to be seen only marks it
    stale.
- **`SongPane`** has the header (laid out by `SongLook`), the line of help with the
  two letters, the box, and the quiet line with Revert and the words about saving.
  **`QuestionPanel`** (`RevertQuestionPanel` until M8) is over the window while
  Revert is asking.
- **The tests type into the real box**, by the doors a keyboard uses
  (`Typist.cs`): a character is raised as WPF raises one, a key is told to
  `KeyPressed` and, if not taken, the command it stands for is given to the box.
  One test sends real key presses through a window of the system's that is never
  shown. Nobody's clipboard is touched: what would be pasted is put in place of
  what is selected.

### The chord sheet (M6)

- **In `Song`**: `ChordSheet.cs`, the Mac's file for file, with no window in it: the
  song's rows, the spot, what each key and each thing done with the pointer asks of
  the song, the bubble's typing, copying and pasting. `ChordLayout.cs` is the one
  small function that says where a line's chords stand. Both are held to
  `sheet-model.json` and `layout.json` (`ChordSheetTests`, with a stand-in for the
  song that is the vectors' own), and `ChordSheetOnAFileTests` has the sheet on real
  files.
- **In `LibraryModel`**: which editor is shown; the sheet of the song that is open;
  undo and redo of the song; the sheet's zoom and the two colours, kept with the
  settings; a click in the strip; copying and pasting, with every sentence that is
  said of them. `Settle` keeps a chord being typed on the sheet before anything else
  is done to the song.
- **In `AppViews`, the sheet in five files, each with one job**:
  - `SheetLook.cs`: the colours and the sizes that do not zoom;
  - `SheetGrid.cs`: where the slides are, and what is under a point. No drawing, and
    nothing measured (decision 60);
  - `SheetMetrics.cs`: what things measure, asked of WPF (decision 59);
  - `SheetKeys.cs`: which key of the sheet's a key press is;
  - `ChordSheetCanvas.cs`: the element. It tells the model what was done and draws
    what the model then has. **Everything a pointer or a key does comes in by a
    method a test can call** (`PointerMoved`, `Pressed`, `Dragged`, `Released`,
    `KeyPressed`, `KeyInTheBox`); the handlers of the real pointer and keys only
    call those. After each thing done it calls `Sync`.
  `SheetEntry.xaml` is the bubble, `ChordSheetView.xaml` the strip and what scrolls
  the sheet, `EditorSwitch.xaml` the two buttons in the header.
- **The settings**: `SettingsView.xaml` has the tabs, `ChordColourSettings.xaml`
  the first of them, `ColourPicker.cs` the system's picker.
- **The tests work the sheet as a person does**, by a hand at it (`Hand.cs`): it
  asks the grid for a point that is over a place, and hands that in as the
  pointer's; it presses a key by asking `SheetKeys` which key that is; it types into
  the bubble's box as the ChordPro editor's tests type. What is then in the song's
  file is what is checked. `SheetGridTests` holds the grid number for number with
  letters of a fixed width; `SheetKeysTests` every key; `SheetModelTests` the
  model's part and the settings; `ChordSheetViewTests` the rest, some of it by the
  dots of what is drawn.

### The mending (M8)

- **In `ChordRules`**: nothing new but `Chords.OnACharacter`, which is the sum `Tidy`
  already did for a chord's place, taken out so that it can be asked for by itself
  (check 0). The rules of checks 2 and 3 were there since M1 (`OldEditor`, `KeyFit`).
- **In `Song`**:
  - `Checks.cs` asks the three checks of a file: which text boxes (`Texts`), a chord
    as the file has it, with its range's end (`Written`), and what is found
    (`Findings`). Nothing in it changes anything;
  - `OpenSong` has the putting right: `Mend`, `MoveBack` and `FoldIn`, each one save
    and one step of undo with a copy kept first, and `Findings` of the song as it is
    now. `TextBoxesMovedBack` is the song as it would be, worked out on a copy in
    memory, and `SongAsItWouldBe.cs` is that as a song the sheet can show and not
    change;
  - `Folding.cs`: which song a copy is a copy of, and what putting its chords in
    would do (`Plan`, `Folded`). `OpenSong.FoldIn` saves what `Folded` makes;
  - `Marks.cs` has what the checks find among a song's marks, and `WantsMending`.
- **In `AppViews`, the model in three files**: `LibraryModel.cs` as before;
  `LibraryModel.Mending.cs` (the offers for the open song, the song shown both ways,
  Mend All, and every sentence of them); `LibraryModel.Folding.cs` (the copies).
  **What is found in the open song is carried with what is shown of it**
  (`ShownSong.Found`), so that it is worked out when the song changes and not each
  time the window asks.
- **The views**: the offers are rows under the song's header, made in the code beside
  `SongPane.xaml` from the model's `Offers`, each a sentence and its buttons;
  `MendAllPanel.xaml` and `QuestionPanel.xaml` are over the window while they are
  wanted; `MendingSettings.xaml` is the settings' second tab; the list's mark, its
  fourth choice and Mend All's button are in `ListsPane.xaml`.
- **The tests**: `Song.Tests/MendingTests` has the page's three proofs, held to
  `repairs.json`, and what a mending is as a change; `FoldingTests` the copies.
  `AppViews.Tests/MendingModelTests` and `FoldingModelTests` have the model, on the
  made-up PC's library of the old editor's copies (`MadeUpPC` can put their songs
  beside them); `MendingViewTests` the window's parts.

### ChordPro files (M9)

- **In `ChordRules`**: reading a ChordPro file's text was there since M1
  (`Chords.ParseSong`, `SlidesOf`). New is `RtfWords.WithWords`: any words written
  after an RTF's own opening, and read back. It stands beside `WithTheOpeningOf`,
  which does the same for stand-ins alone and is held by vectors; this has none (the
  reference writes RTF in its own words) and is held by its own tests.
- **In `Song`**:
  - `ChordProFile.cs`: a file's bytes as text and what is refused of a file; and a
    song as ChordPro text and as a file. It changes nothing in a library;
  - `NewSong.cs`: what was found in a file (`Found`), the name a song will have
    (`Tidied`, `FreeName`), the presentation (`Made`), and the making of the file
    (`Make`). Its header says each thing the page does not, in the order of the
    decisions above. `ModelSong` is the song to look like, the app's own being a
    file carried inside `Song`'s library (`Song.csproj` points at the test file);
  - `SongFile.WriteNew` (in `Writing.cs`) makes a new file as a save replaces one,
    and never over one that is there.
- **In `AppViews`**: the model's fourth file, `LibraryModel.ChordProFiles.cs`, has
  the panel as a value (`NewSongAsked`, with every sentence), what each field and
  button does, and the writing out. `NewSongPanel.xaml` shows it. The two commands
  and the two choosers of a file are `LibraryView`'s.
- **The tests**: `Song.Tests/ChordProFileTests` and `NewSongTests`;
  `ChordRules.Tests/RtfWordsTests` has the new words;
  `AppViews.Tests/ChordProFilesModelTests` and `ChordProFilesViewTests`.

### Giving it out (M10, begun)

- `make-zip.ps1` and `zip/Read Me First.txt`, beside the projects.
- `AppViews/AboutTheApp.cs` has the words and the version; `AboutSettings.xaml`
  shows them, as the fourth tab of `SettingsView`. `AboutTests` holds them word for
  word.

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
  time, and one second every time after.** Seen of three builds, 17, 19 and 18
  seconds, each with this PC busy at other work; the second and third start of the
  same file took one second. Why is A GUESS: it is one file of 141 megabytes that nobody
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

### At M4

- **Until there is an editor, nothing in the app makes a copy.** A copy is kept
  before a song's first change, and so far nothing changes a song but putting a copy
  back. So on the maintainer's PC the mark, Earlier Versions and All Copies have
  nothing to show until M5, and M4 is proved as `spec/11` says it is to be: with the
  song's own functions, on copies of the test files, and in pictures.
- **One case of mine expected wrongly, and the code was right.** With another song's
  bytes put in a song's place, the song there now IS the other, inside. So a copy of
  the song that was there is then "of another song", and is asked about, with the
  two names the other way round. The case had expected it to be put back unasked.
- **A comment in a view's file may not have two hyphens together**, as in a
  project's file (M0). The page's drawing of the panel is in a frame of hyphens: in
  `EarlierVersionsPanel.xaml` it is drawn without.
- **A name in a class may not be both a thing it has and a kind of thing it uses.**
  What went wrong, as a sentence, is `Refused` in the model and not `Refusal`, which
  is the kind of thing a song throws. (A list named `Copies` in a view beside the
  kind `Copies` is let be, where the kind is not named in that file.)
- **What the pictures showed.** Eight more, of the mark and the button, the panel in
  both looks, the question, the offer to undo, what went wrong said in the panel and
  on the quiet line with the offer to open again, and All Copies with copies and with
  none. All were opened, and all were right at the first drawing.
- **How the proofs were shown to bite**, as before: each rule broken in one small
  way in turn, in the two clones, with the tests run after each.
  - Of the copies' plain code (`Copies`, and `OpenSong.PutBack`): seventy-three
    breakages, one of which changed nothing. Sixty-four were caught at once. **Five
    were rules no test held**: that a copy's chords are counted as the mark in the
    list counts them, and not those of a box that shows something else's words;
    that every copy of a song is counted in the room they take, and not its newest
    alone; that a copy which says it was made after now is not said to be of
    today; that after a putting back the slides and boxes are the song's as it then
    is; and that what was undone before a putting back cannot be redone after it.
    One cannot be told from the rule by any input (a copy written over one that is
    there: the naming apart already keeps it from that). Two cannot be reached
    ("Left rough").
  - Of the window's part (earlier versions in `LibraryModel`, and
    `AllCopiesModel`): seventy-two, one of which changed nothing. Fifty-eight were
    caught at once. **Eleven were rules no test held**: a copy of a song that
    states no key, in the line that sets it against the song now; that a question
    answered yes and then refused is asked no more; the three lines of the log (a
    putting back, its taking back, a refusal); that the marks of a song put back
    are kept for the next start; that the offer to undo and what went wrong go
    with the song when its library is left; and, of All Copies, that where a song
    was is what its newest copy says, that a song picked has its newest copy
    picked with it, that what went wrong is said no longer once the copies are
    read again, and that nothing is left beside a place a copy could not be saved
    to. One is the same to look at (putting away a panel that is not there). One
    could not be told at M4: that the mark of copies comes with a song's first
    copy, which is made before its first change, and nothing changed a song before
    M5. M5's tests of typing hold it.
  Each of the sixteen has a case now, and each case was run against the breakage
  it was written for.

### At M5

- **Of what can be typed between brackets, only nothing and spaces are "not yet a
  chord".** `spec/04`, rule 15, leaves out of what is saved the brackets "that hold
  what is not yet a chord", and gives someone "half way through typing `[F#`". But
  `chord-names.json` has no name that could become a chord and is not one, bar the
  empty one: "F#" is a chord, and so is "F#m7/" with nothing after its stroke. So a
  name half typed is saved as it stands, a moment after the typing pauses, and
  saved again when it is finished; as in the reference and on the Mac. The rule is
  right and its example is not. **Told to the maintainer; `spec/` not changed.**
- **Half a character among a song's words does not bar it here.** `spec/04`, rule
  20a, bars a song "if the text as it is made from the song does not itself pass
  rule 6", and names words with "half a character in them that text cannot hold".
  A Swift string cannot hold half a character, so on the Mac such a song is barred.
  A .NET string holds it as it is, and so does WPF's text box (tried): here the
  text made is the song's words unit for unit, the rule as written does not bar it,
  and it can be typed in, the half kept (a test). **A difference between the two
  apps that follows from the rule itself. For the maintainer to say** whether
  Windows should bar such a song too, to be the same. No song with such words has
  been seen.
- **A chord typed on a line of words that has no characters is let through and never
  saved**, here as on the Mac: with its chords taken out the line is the nothing it
  was (rule 6), and there is no character to hang the chord on (rule 16). It is in
  the text, in orange, until the song is next opened. A text box with an empty line
  between two of words is likely enough in a real song. **For the maintainer to
  say** whether such a chord should be refused instead; both apps would change. No
  test file has such a line; the case makes one.
- **What WPF's text box does, found by trying** (each in a throwaway test, before
  anything was built on it):
  - it says its text has changed BEFORE it says the caret has moved, and by then
    the caret has moved: where the caret was is remembered beforehand;
  - its text can be changed again from inside that saying;
  - a character can be typed into it by a test with no window and no keyboard, by
    raising what WPF raises; Backspace and Delete by giving it their commands;
    Enter does nothing unless the box really has the keyboard;
  - with its own undo switched off, a command of Undo given to it reaches whoever
    else has said they answer it, first;
  - it keeps a line feed as a line feed, and whatever line breaks it is given, as
    given; a carriage return, a line separator, and four characters more start a
    new line in it too. So they do in the letters drawn behind it, which are laid
    out by the same means: a test holds the two against each other, dot for dot,
    with each such character in a line of words;
  - it will not put its caret between the two units of a line break of two, nor
    inside a character of two units; told to select from such a place, it selects
    from somewhere else, without a word. So a change the box is told to make is
    looked at afterwards, and if it is not what was wanted the whole text is set;
  - it hands its padding to what holds its text by itself: said in a template of
    our own as well, the padding was there twice;
  - a view that is drawn into a picture with no window is, to WPF, never "seen"
    (`IsVisible`). The letters behind the box were not drawn at all in the first
    pictures, for asking that.
- **The heredoc that trips over an apostrophe** ("At M2") trips over an ODD number of
  them, and then none of it is run: "unexpected EOF while looking for matching". It
  bit three times more. A script that changes sources is written as a file.
- **What the pictures showed.** Nine more of the editor (20 to 28): a chord typed
  and one still being typed, in both looks; a song that cannot be changed here,
  with its reason in the caution colour; the header at the window's least; Revert's
  question in both looks; a song reverted, with the offer to undo; the text at its
  largest and its smallest; a song that states no key; a save refused because
  ProPresenter was started unseen; and what Ctrl+S says. All were opened. **Put
  right by looking**: the letters behind the box were not there (above); the text
  stood 14 further in than the song's name (the padding twice); at the window's
  least the second key was cut off (decision 50); and the offer to undo stood at
  the far end of the line, by Revert, where it now stands just after the words it
  follows.
- **The app was started for real and a chord put on a song with it.** The published
  file, on a made-up library of the test hymns in a folder of this session's own,
  named to the app as a chosen folder. Its window was spoken to as a program may
  speak to one (the system's way for screen readers; no key pressed, no pointer
  moved): a song picked in the list; its text read from the box; the text set with
  a word changed, which the box put back; the text set with a chord more, which
  was taken, saved after its pause, a copy of the file kept first, the mark of
  earlier versions shown in the list and the quiet line saying one chord more.
  Three pictures were taken of the window itself and looked at: its title bar and
  menu in the dark look, the chords in orange in a real window. It closed with
  nothing wrong. Afterwards the settings, the marks and the one copy it left in the
  app's own folder on this PC were taken away again, each by its exact path; the
  log there has three lines more.
- **To a screen reader, a song's row was called "ListsPane+Row".** Found by that
  asking. A row of a list is called whatever the thing in it calls itself, and a
  thing that does not say is called by the name of its kind in the program. Every
  kind of row now says what it shows, and a song's row says its name and what each
  of its marks says, the marks being only small pictures (M3's and M4's lists were
  so too). A test asks the lists as a screen reader does.
- **The first start of a fresh build still takes some eighteen seconds** before
  there is a window; later starts about one. It is M10's.
- **How the proofs were shown to bite**, as before: each rule broken in one small
  way in turn, in the two clones, with the tests run after each (the pictures left
  out, which take long and hold nothing).
  - Of the editor's plain code (`ChordProEditor`): eighty-nine breakages, one of
    which changed nothing. Seventy were caught at once. **Three were rules no test
    held**: that two lines of chords alone saved together are one step of undo;
    that a saving is a step of its own and does not join the one before it; and
    that in a box with a line of chords alone and a line of words, each keeps its
    own chords when the other's are changed. One the build itself refuses (words
    compared by how they look in a language: the rule of the project that no words
    are, stops it). Fourteen cannot be told from the rule by any input. They are
    of two sorts. Some are tests kept as the reference has them that a later test
    says by itself: a stray bracket looked for on its own, a bracket on the line
    before taken for this chord's (one that closes on another line is no chord
    anyway), a song that is barred asked the rule or asked to save. The others are
    what the song does itself when a box is written: it puts chords in order, and
    one past a line's end on the line's last character.
  - Of the window's part (the typing in `LibraryModel`, `ChordProTyping`,
    `ChordProText`, `ChordProUnderlay`, and the song's pane): a hundred and
    eighty-one, one of which changed nothing. A hundred and seventy-one were caught
    at once. **Three were rules no test held**, two of them with a case that asked
    at the wrong place and so held nothing: that Backspace at a bracket is not
    helped where nothing can be typed, that Shift with Delete, which is Cut, is
    not helped as Delete is, and that at the largest size the large letter is the
    one greyed. Three are the same to look at (Saved said over a refusal, which is
    said before it; a revert asked for with nothing to revert; what is stale asked
    to be laid out again, which something else always is). **Three were code that
    did nothing, and it was taken out**: a redo ending a run of typing that an undo
    had ended already; the letters behind asking by themselves whether a line is a
    group's name, which the editor's colouring says; and the letters behind cut
    into pieces where a text box starts a new line within a line of words, which
    they do by themselves (the test above).
  Each of the six has a case now, and each case was run against the breakage it
  was written for.

### At M6

- **Every case of `sheet-model.json` and of `layout.json` passes**: six songs' rows,
  nine cases of where words start, thirty-eight of a line's stops and thirty-five
  of the stop at a place, seventeen walks with the arrow keys, forty-six changes,
  eleven typings and thirteen copyings and pastings, each compared by what the song
  was asked and by the song afterwards.
- **A line of text that has outlived what laid it out can be measured, and cannot be
  drawn.** The first time the sheet drew, every test with a window in it failed
  with "Text formatting engine cannot format a line of text due to error:
  'ClientAbort'", from the drawing; measuring the same lines had worked. What lays
  the lines out is now kept as long as the lines are.
- **The header went round for ever, twice**, and the test that lays it out at every
  width is what showed it, by never ending.
  - On the one row, Earlier Versions was in a column that took whatever was left
    over, which was exactly its own width. A fraction of a pixel short, it was laid
    out a pixel narrower; its width is read from how it was laid out; so it was
    worked out a pixel narrower still, and laid out narrower again, without end.
    **Nothing in the header but the name is ever given less room than it needs
    now**: each thing is in a column as wide as itself, with a column of its own for
    what is left over.
  - And the room the header has was read from the header. A header whose columns
    want more than the pane has is laid out as wide as they want and cut off at the
    pane's edge; so the room never got less than one row wanted, and it stayed one
    row at any width. The room is read from the pane.
- **The bubble was placed for the height it had before the key.** Seen in its first
  picture: with "D/" typed it was a row of chords too tall. What is offered changes
  with each key, and how tall the bubble then is, is not known until WPF has laid it
  out again; asked at once, it said what it had been. It is placed as the sheet is
  laid out (decision 62).
- **A second step of zoom straight after a first began from where the sheet had
  been.** The sheet is scrolled to keep the slide at the top where it was; WPF
  scrolls when it next lays the window out; and a second step before that read the
  old place against the new slides. A test with two steps together showed it. In
  the app a laying out comes between any two keys, so nobody would have seen it; it
  is put right all the same (decision 69).
- **Tab's "takes the first that is offered" happens only in an empty box.** Rule 33
  gives it to what "is not yet a chord", and (as "At M5" found of rule 15) only
  nothing is: whatever the box takes that is something, is a chord by the vectors.
  So `G/` and Tab keeps `G`; an empty box and Tab keeps the first offered. As on the
  Mac and in the reference.
- **What the pictures showed.** Twenty-three more (30 to 49): the sheet as a song
  opens, in both looks; in a wide window, and there with its words at their smallest
  and their largest; the spot on a word and on a chord; the bubble with "D/" typed,
  and opened on a chord that is there; a chord being dragged; a line of chords
  alone; blank slides; the window at its smallest; a song that states no key; a
  chord refused while ProPresenter runs; what a paste says; a longer song; a song
  that ProPresenter itself made, whose groups have colours, which the frames show;
  the settings' two sections; and chords in a colour chosen, on the sheet and in
  the text. All were opened and set beside `spec/pictures`. **Put right by
  looking**: the bubble's height (above).
- **The app was started for real again, with a song on its sheet.** The published
  file, on the made-up library of the test hymns, its window spoken to as a program
  may speak to one (no key pressed, no pointer moved). A song picked; the sheet
  there, two slides to a row in a window of 1280; the strip's two buttons pressed,
  and the slides laid out again larger and smaller; the other editor chosen at the
  switch; a chord set in the ChordPro text; the sheet chosen again, with that chord
  on it, the song's row marked as having an earlier version, Earlier Versions in
  the header and Revert lit. Five pictures of the window itself were taken and
  looked at. It closed with nothing wrong, and what it left in the app's own folder
  on this PC was taken away again, each thing by its exact path. **That way of
  speaking to a window cannot point at a word of the sheet**, which is the sheet's
  whole business: nothing of M6's own was done to the real window but look at it.
  Two things were found by it:
  - **the sheet said nothing of itself to a screen reader**, not even the name it
    had been given (decision 76);
  - **the quiet line was broken at the plus of "Ctrl+C"**: "Ctrl" at one line's
    end, "+C" at the next one's start. A character with no width that joins what is
    either side of it (U+2060) is put each side of every plus in what the line
    shows. WPF keeps to it and gives it no width: found by trying, and a test lays
    the line out narrower and narrower to hold it.
- **How the proofs were shown to bite.** Each rule of the sheet was broken in one
  small way in turn, in clones made for it, and the tests run after each.
  - **The model, 189 ways** (`ChordSheet.cs`, `ChordLayout.cs`): 168 were caught at
    once. Eighteen were not, and each was given a case, after which all eighteen were
    caught: a line that begins with spaces; where the spot cannot be set; Shift on a
    line of chords alone; the keys picking the spot's slide and asking for it to be
    shown; two stops as near; where a pushed bubble stands; a chord past its line's
    end; a chord of no name; empty lines and groups' names in a copy and a paste;
    lines of chords alone pasted as one step of undo; Tab after the pointer has
    moved, and with one picked; a click picking its slide; each of the keys 1 to 7;
    and what parts words, unit for unit. Two cannot be told: an empty line's one
    place where a line's chords are set (nothing gets there with an empty line, the
    spot never being on one: said where it is), and an empty box kept, which asks
    for a chord of no name and is refused that further in. One was a change that
    changes nothing, to see the driver tell.
  - **The view, 326 ways** (the sheet's files of `AppViews`): 298 were caught at
    once. Fifteen were given cases and are caught: a line of chords alone at another
    size; two stops as near, in the grid; a colour chosen, at another size and in
    the bubble; the bubble staying where its chord will stand; the pointer's shape
    off the cards; Right in the middle of the box; three parts of rule 52b; the
    quiet line's wrapping and its four lines; the menu at the text's largest; a
    colour written with no hash; and each Usual Colour by its own colour. Twelve
    cannot be told: three were found to be the same whichever way (a line's end is
    put right by `Believed` whatever was said of it; what scrolls the sheet stretches
    it over what is seen whatever it asks for; the switch's cell over the keys is
    exactly its size), six were known beforehand to be the same, and three are only
    to be seen. One was the driver's own.
  - **And one fault of M6's was found later, by a test of M8's**: the strip's sentence
    for a song with no key was measured while it was put away, as nothing, and
    nothing always has room. Made narrower a little at a time, the pane showed it and
    hid it by turns ("At M8").

### At M8

- **The three proofs pass, and more.** `spec/08` asks that the checks find in each
  test file what `repairs.json` says, that each damaged file mended as that file says
  has exactly the chord entries of the clean file it was made from, and that mending
  changes nothing else. All three are held. And **each damaged file, put right, IS
  the clean file, to the byte**: the three with nothing but ranges wrong after Mend;
  Amazing Grace, whose chords were put on by hand, after Move the Chords (or after
  both, in either order); and the song whose key was reset, after Mend and Set Key.
  That is these files' luck as much as a rule (they were damaged by a tool that
  writes a file as this app does), and it is held all the same: it could not be so if
  a mending touched anything it should not.
- **The same of the copies.** There is no test data for putting a copy's chords into
  its original. But each file in `as-the-old-windows-app-left-them` is the copy the
  old editor would have saved of the song of the same name in
  `made-by-simple-presenter`. So the tests make the original as someone would have
  had it, that song with every chord taken off, and put the copy into it: and for the
  two copies in which the old editor left nothing but its ranges, the original is
  then the song the copy was made from, to the byte.
- **Where the page is silent, or says other than the test data.** Each is a decision
  above, for the maintainer:
  - which text boxes the checks look at (78);
  - "the box's chords": `repairs.json` has every box of a mended song put right (79);
  - a chord on no character, "shown on the sheet": and in the text (81);
  - what "wants mending" is, and a song with no key (82);
  - **and one that is not decided, only said: a copy's key is not always the better
    one.** The page has the original given the copy's `music`. The old editor wrote A
    flat into copies of songs whose key it could not show (C of the page's table):
    so the song beside such a copy may have the right key still, and putting the copy
    in replaces it with the mistake. That is the test file A Song - In A Minor Key:
    its original says F sharp minor, its copy A flat. As built, the page is followed
    to the letter; the question before it is done says "and its key, A flat, in place
    of F sharp minor. A flat does not look like the key of those chords, which look
    like F sharp minor: once they are in, the song offers to set it."; and it does.
    **Proposed, for the maintainer**: where the copy's key is one check 3 doubts and
    the original's is not, the original keeps its own.
- **Found on the way, each put right and held by a test**:
  - **the strip's sentence for a song with no key came and went** as the pane was
    made narrower (a fault of M6's, above): it is measured as it would be if shown;
  - **a test's made-up PC could be taken away while a song's file was still being
    read for its marks**, and Windows will not delete a file that is open. The marks
    read more of each file than they did, and with the PC busy one test in a run
    of a hundred and seventy failed so, and not the same one each time. The taking
    away waits for the reading;
  - **the list's choices did not answer a program switching them on.** A screen
    reader may switch such a button on the system's way, which changes the button and
    raises no click: it looked chosen and the list was as it had been. True of the
    three choices since M3; found on the way to trying Mend All in the real window;
  - **"1 chords"** in the log, of a song with one;
  - a sort of the chords handed to the editors, written with check 0, did nothing (a
    place brought onto a character never passes one that was after it) and is out.
- **Working out a song's marks now reads the words of every text box**, for the
  checks; before, it only counted chords. On the test files it is not to be
  measured. Nobody has timed it on a library of hundreds of songs; it is done once
  for each file and kept.
- **What the pictures showed.** Eighteen more (50 to 62): a copy with chords shown
  twice, in both looks; a key that does not fit; the list narrowed to what wants
  mending, with Mend All; its question, in both looks, and what it did, with a song
  that was not mended; a song offered to have its chords moved, and shown as it
  would be; the settings' Mending, in both looks; a copy beside its original, in
  both looks; the question before its chords are put in; the original with the
  chords and the question of the copy, in both looks; the copy moved out; and a copy
  whose original has its chords already. All were opened and looked at. Picture 41,
  a song with no key on the sheet, now says which its chords look like.
- **The app was started for real, and the mending worked through in its window.**
  The published file, on a made-up library of the four copies beside their songs
  (the songs with no chords), its window spoken to as a program may speak to one: no
  key pressed, no pointer moved. In turn: the list read as a screen reader is told
  of it, each copy saying what wants mending; a copy opened, its two offers there;
  **Mend** pressed, and the quiet line saying what was done; **Wanting mending**
  switched on the system's way, and the list narrowed (which is how the fault above
  was found, and that it is put right); **Mend All**, its question read, three songs
  mended and listed; a copy's chords **put into its song**, the question before and
  the question after read, and the copy **moved out**; and a key **set**. Nine
  pictures of the window itself were taken (real 9 to 17) and looked at. Then the
  files: the copy that was mended, the song the other copy was put into, and the
  copy whose key was set, are each the clean test file to the byte; the copy that
  was moved out is in the library no longer and is among the copies kept; the songs
  not asked about are as they were. It closed with nothing wrong, and what it left
  in the app's own folder on this PC was taken away again by its exact paths. The
  window came after twenty-five seconds, with the PC busy at other things.
- **How the proofs were shown to bite**, as before: each rule of the mending broken
  in one small way in turn, in clones made for it, and the tests run after each.
  - **The logic, 73 ways** (`Checks.cs`, `Folding.cs`, `Marks.cs`,
    `SongAsItWouldBe.cs`, and what of `OpenSong.cs`, `Reading.cs`, `AppData.cs` and
    `ChordsInABox.cs` the mending added): 46 were caught at once. Twenty-two were
    not, and each was given a case, after which all twenty-two were caught: every
    action of a cue that is a slide being checked and mended; the key held against
    the chords being the one they are written in, not the one they are shown in;
    when a key is said to be the old editor's doing (two ways); the song as it would
    be, that it is not the song as it is and takes no change (three); **a song's
    marks, all ten ways of them**, which had been held only where the window's tests
    happened to look; marks kept by an earlier reading of the files; a plan's
    likelier key; words that differ only in their capitals; and the keeping of the
    copy's own file before its chords are put in (two). Three cannot be told (a text
    with no RTF checked too, which has no words to find anything in; every slide
    counted as changed by a mending, where one that is not is put back as it was; a
    box with no chords written again, which writes nothing). **One showed a line of
    the app to do nothing**: the chords handed to the editors were put in order of
    place a second time, and with that taken out no test said so, rightly, since a
    place brought onto a character is never brought past one that was after it. The
    line was taken out, and `Reading.ChordsHanded` says why it is not there. One was
    a change that changes nothing, to see the driver tell.
  - **The window's, 155 ways** (`LibraryModel.Mending.cs`, `LibraryModel.Folding.cs`,
    what of `LibraryModel.cs` the mending added, and the views: the song's pane, the
    lists, the two panels, the window's keys, the strip of the sheet, the settings):
    134 were caught at once. Seven were given cases and are caught: Move the Chords
    pressed from the song as it is; the song that is open refused by Mend All; the
    songs Mend All mended being marked as having copies; no song being judged where
    none is open; an original's one chord being said as one; a yes that can do
    nothing by then putting the question away; and Mend All asked a second time
    being the question again. Thirteen cannot be told: twelve were known beforehand
    to be the same and say why in the list, and one was found so (a mending that
    was refused gives no count of chords, so asking besides whether it was refused
    asks nothing). One was the driver's own.
  - **A case written for one of these showed what nothing had asked.** While the
    question "Put this copy's chords into ...?" stands, something else may make the
    other song another song's words. Yes then opens that song, and the song itself
    refuses ("The two songs have not the same words ..."), with nothing changed and
    no copy of either file kept. That is right, and is held now; it was not known
    until the case for something else was written wrongly first.

### At M9

- **The proof the plan asks is not made.** "A song made here opens in ProPresenter,
  on this system, with its chords shown on a stage screen" is the maintainer's to
  see. What follows is what could be shown without it.
- **A song made here reads as the reference's own answer.** The clean test files
  were made by Simple Presenter from the ChordPro files of `testdata/songs`
  (`tools/simple-presenter-scripts/import.qml` has the lines to a slide of each).
  Made here from the same files with the same lines, each song reads, in this app,
  as that test file reads: its groups, its slides, its words, its chords, its key.
  Held for five songs, each made to look like the app's own song, like one Simple
  Presenter wrote, and like the one shaped as Multitracks'. (Blank Slides is held
  to its file instead: its blank slides were made by hand afterwards.)
- **A slide made here IS the model's slide** but for its ids, its words and what is
  said of stretches of them: written out as text with those set aside and each id
  numbered by where it first comes, a slide of the new song and the model's are the
  same text. And the top of the new song, with its name, id, date, key, CCLI block,
  groups and slides put back as the model has them, is the model to the byte.
- **What ProPresenter's own file showed**, which settled decisions 100 and 101:
  each of its twelve slides has its own id for the cue, the action, the slide and
  the text box; the text box's link "seen when ..." names that box's own id on
  every slide; and its list beside the RTF has "capitals" over exactly the whole of
  that slide's own words (0 to 54, 0 to 57, 0 to 69 ...). Its first cue is the
  third verse's: "first" is the order shown.
- **Written out and read back, a song does not always give "the same parts"**
  (rule 19's test). By the rules of reading, a `{comment}` begins a part only if
  what it says is the name of a part (rule 3), and inside a part that a directive
  began an empty line is only a gap (rule 5). So:
  - a group named otherwise ("The Middle Eight", which one test song has) reads
    back as more of the part before it;
  - slides in no group, after a group, read back as more of that group.
  Every line and every chord comes back, in order; the parts do not. Held as it
  is, in `ChordProFileTests`. **A proposal, for the maintainer**: write a group
  whose name is no part's as `{start_of_verse: The Middle Eight}` ... `{end_of_verse}`,
  which reads back under its own name (rule 3), and write `{end_of_verse}` before
  slides that are in no group, after which they read as a part with no name. That
  is a change to rule 18, and so to both apps.
- **Two parts of one name.** *It Is Well*'s file names a chorus by a line
  ("[Chorus]") and then begins another with `{soc}`: the rules number a chorus by
  how many a directive has begun, so both are "Chorus". The song made has two groups
  of that name, as Simple Presenter's has. Nothing is done about it.
- **Found on the way.**
  - **A song named after a device would never have been made**: "Nul.x" is the
    device whatever number is put after it, and the looking for a free name went
    round for ever. A test hung on it before anything was committed. The full stop
    after such a name is a hyphen now (decision 103).
  - **A folder whose name ends in a space** cannot be written into, though the
    system says it is there. The making says what the system says and leaves
    nothing behind. Seen by a test's own mistake; no library is named so.
  - **A name of some two hundred and ten characters or more cannot be made**: what
    is written beside the file has forty-four characters more than the file's own
    name, and a name has two hundred and fifty-five at most. The system's sentence
    is said. The field takes two hundred.
  - **Two things asked nothing and were taken out**: the asking whether a new
    file's name is free before the moving that asks it in one step, and a test of
    white space that the trimming after it made idle.
- **What the pictures showed.** Nine more (63 to 69): the panel, dark and light,
  which had its two columns of names out of line with each other (put right: the
  two grids share the column's width); a file that is no song, with its sentence
  and Close; the panel while ProPresenter is running, its button greyed and the
  band behind it; a refusal said in the panel; the song that was made, listed and
  open on the sheet with the quiet line saying what was made; the panel at the
  window's smallest, where it fills the window and nothing is cut off; and the
  quiet line after a song is written out.
- **The app was started for real, and the menu looked at. The choosers of a file
  were not worked, and trying went wrong.** Said in full, because it is about the
  maintainer's own PC:
  - The published file was started on a made-up library of three test songs, its
    window spoken to as a program may. **Seen**: the rows as a screen reader is
    told of them; the File menu with both commands in it, "Save Song as ChordPro
    File..." greyed with no song open and not with one; and that "New Song from
    ChordPro File..." puts up the system's own chooser, titled "Choose a ChordPro
    file to make a song from", showing "ChordPro files (*.cho;*.chordpro;*.chopro;
    *.crd;*.txt)".
  - **What went wrong.** The chooser shows the person's own Documents. The first
    script went on working the window after the chooser had failed to take a
    file's name, and tried to type the new song's name into what was a field of
    the chooser's list. It timed out, and nothing was renamed. The second, written
    to do no more than look and press the chooser's own Cancel, did not find that
    button, went on, and **listed the rows of the window with the chooser still
    up: the names of eleven folders in the maintainer's Documents were printed
    into this session's output and a scratch file.** The file was deleted. Nothing
    was typed anywhere, and nothing was changed: Documents had no entry made or
    changed in that hour, the made-up library was byte for byte as it began, and
    the app's log shows no file read or written by either command. The app's own
    folder on this PC was put back to its log alone.
  - **So, a rule for the sessions after this one**: a program does not open the
    system's chooser of a file on the maintainer's desktop at all. It shows their
    files to whatever reads the window, and a script that loses its place in one is
    in their folders. Everything from the choosing of a file on is the
    maintainer's hand ("Left rough"); what the panel says and does is held by
    tests and pictures with no chooser in them.
- **How the proofs were shown to bite**, as before: each rule of M9 broken in one
  small way in turn, in clones made for it, and the tests run after each.
  - **The logic, 185 ways** (`RtfWords.WithWords`, `ChordProFile.cs`, `NewSong.cs`,
    `SongFile.WriteNew`, the new refusals): 170 were noticed at once, one of them by
    a test that then never ended (with COM10 taken for a device, every numbering of
    it is one too, and the looking for a free name goes round for ever). Five were
    noticed later: one once the list pointed at the right place (the stand-ins'
    writer has the same few lines as the writer of any words, and the list had
    found those first), and four once they had cases, which are a group's name with
    a brace in it written as ChordPro, and three questions of the check (below).
    Nine cannot be told: what was made is always read back as written; the size of
    a file asked before it is read and after, each of which the other covers; the
    lines to a slide made one at least in two places; the check where the making
    calls it; a box asked whether words can be written after its opening, when it
    has a character; an id inside a table of a kind a slide has none of; a blank
    slide in the check, which has not the words either; and one found so, that the
    model's chords need not be left out where what is said of the words is
    gathered, the writing of the slide's own chords taking them out anyway. One was
    a change that changes nothing, to see the driver tell.
  - **The window's, 117 ways** (`LibraryModel.ChordProFiles.cs`, `NewSongPanel`, and
    what of `LibraryView` and `LibraryModel.cs` M9 added): 97 were noticed at once.
    Seven were noticed later, each with a case now or tried again: that the panel
    says ProPresenter is running (its first try was stopped by another clone's
    driver); the order of that and a refusal (below); a name taken while the panel
    is read; a song made with no chords said to have none; a file that was not
    written not also said to be written; **Cancel making the song** (it put the
    panel away just the same, and the case had asked no more than that); and the
    rows of one file shown for another. Eleven cannot be told: five are the same
    whichever way, and six are only to be seen or need a window (two lines with
    nothing in them left standing, where the keyboard goes, two things about where
    the rows are drawn, and the system's chooser asked for a file when none may be
    brought in). One piece was taken out of the app
    as idle, and one was the driver's own.
  - **What the breaking found that was more than a missing case.**
    - **The check before a song is written had never been seen to refuse
      anything**, every song made here passing it. It has a test of its own now: a
      made song is spoilt thirty ways and each must be refused. Breaking the
      check's questions then showed three of them answered by no spoiling, each of
      which wanted a file that reads, to this app, exactly as the song asked for:
      one id for two slides; the slide of a second part put into the group of the
      part before it, where the two have one name; and a cue more that this app
      does not show (switched off, or no slide at all), which ProPresenter would
      have.
    - **A fault in the app**: what was put down as a thing that could not be told
      could. A making refused for some other reason, and then ProPresenter
      started: the button was greyed for ProPresenter and the sentence over it was
      still the old refusal. It is why the button is greyed now, and the refusal is
      said again once ProPresenter is quit.
    - **Two guards for one thing** kept the rows of one file from being shown for
      another, and breaking either was not noticed, the other holding. One was
      taken out, and the other is held.
    - **A test of the settings failed for ten commits and was not seen**: About
      made a fourth section, a second test still counted three, and only the tests
      thought to be touched had been run. A clone's run of a breakage showed it,
      every breakage there being "noticed" by that one test. **Every commit that
      changes a view is to be followed by all of the window's tests, not some.**
    - **Of the driver itself**, for whoever runs several clones at once again: it
      stopped a test that never ended by stopping every test of that name, the
      other clones' among them; and before that it waited for ever, the test still
      holding the pipe it was reading. It now starts each run so that it can stop
      that run and what it started, and nothing else.

### What the maintainer found by hand

- **2026-10-12, the first try of a build: a click on the sheet did nothing.**
  Pointing at a word and typing a chord opened the bubble; clicking there did not.
  **Why**: the sheet keeps the mouse while its button is down, so that the letting
  go is heard wherever the pointer has got to, and gives it back after. It gave it
  back BEFORE telling itself the button was let go. Giving the mouse back says, there
  and then, that the mouse was lost; and the mouse lost with a press on and the
  button up is taken for a button let go somewhere the sheet was never told of, so
  the press was forgotten. Every click was forgotten before it was heard. A chord
  that was carried would have gone back where it had been, for the same reason.
  **Put right** by telling the letting go first (`ChordSheetCanvas.ButtonUp`).
- **Why no test had found it, and what holds it now.** The tests hand the sheet
  what the pointer does by the methods its real handlers call, with no window; and
  with no window there is no mouse to keep, so nothing is ever lost. The fault was
  in the two lines of the handlers that the tests went round. So:
  - the keeping and the giving back are now inside what the tests call
    (`ButtonDown`, `ButtonUp`; `Hand` clicks and drags by them), and what the mouse
    lost does is a method of its own with a case of its own (`MouseLost`);
  - and **two tests put the view in a real window** (`ChordSheetWindowTests`): off
    every screen, never brought to the front, with nothing in the taskbar, there
    for less than a second. In it the sheet really keeps the mouse, and each test
    first makes sure that it did. With the old order put back, both fail as the
    maintainer saw: no bubble, and the carried chord where it was. **This is new**:
    until now no test opened a window. No key is pressed and no pointer moved in
    it, and nothing of the system's own is opened.
  - **What it says of the rest**: every other handler of the real pointer and keys
    is still held only as far as the method it calls. Each is a few lines, and none
    of them keeps anything or gives anything back; but none has been through a real
    window either, and one of them does choose (the pointer's moving, between
    carrying a chord and pointing, by whether the button is down). The list under
    "Left rough" is the more worth trying for it.

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
  - the menu, and that File > Settings opens the settings over the window, where
    **Show Their Folder** opens File Explorer and **Save This Copy As...** asks
    where (both greyed until there is a copy, which is not before M5);
  - once there are copies: that Earlier Versions opens over the window, that the
    arrow keys go through the versions, that Esc and a click on the dimmed part put
    it away, and that the offer to undo a putting back goes after eight seconds;
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
- **The menu has no keys of its own yet**, bar the letters Windows underlines when
  Alt is held. `spec/10` gives the keys of the editors' commands; none is given for
  Choose Folder, Settings or Earlier Versions, and none was made up.
- **Nobody has typed into the ChordPro editor by hand.** What tests and pictures
  cannot tell, and the maintainer is asked to try, on a COPY of a library:
  - that typing feels as it should: a letter over a word does nothing and the caret
    stays; "[" brings "]"; a chord goes in between; "]" steps over; Backspace on a
    bracket takes the chord; and the sound for "no" (decision 44), whether it is
    wanted at all;
  - that the caret, and what is selected, sit exactly on the coloured letters, at
    every size of the text, and on a screen set to another size than the first;
  - Ctrl+Z and Ctrl+Y in the box, and what a step of each is (decision 42); the
    Edit menu;
  - a paste from Notepad, of a chord and of the whole text; text dragged within the
    box, and dropped on it from elsewhere;
  - Ctrl with plus, minus and 0, the two letters, and Ctrl with the wheel;
  - the two keys, picked with the mouse and with the keyboard;
  - Revert: that Enter and Esc both cancel, and that the offer to undo follows;
  - Ctrl+S; and that leaving the app for another program saves what was typed (the
    window's two lines for that are reached by no test);
  - on a keyboard that types "[" with AltGr, that a bracket brings its other
    (decision 43); and with an input method, whether its being off in the box is
    right (decision 46);
  - and M5's proof: **a song chorded here, on a copy of a real library, looked at
    in ProPresenter.** On this PC there is no such library to copy ("ProPresenter
    22 keeps no `.pro` files", above).
- **The maintainer has now clicked the sheet, and the first click found a fault**
  (2026-10-12, "What the maintainer found by hand", below): a click did nothing,
  though a chord could be typed. It is put right. **The rest of this list is still
  untried by a hand.** Everything the sheet does was done by tests, through the
  doors the real pointer and keys come in by; what is behind those doors is held,
  and the doors themselves are a few lines each, which is where that fault was.
  What only a hand can tell, and the maintainer is asked to try, on a COPY of a
  library:
  - **first, a letter**: point at a word and press D. The bubble is to open with
    one D in it (decision 64);
  - that the spot keeps up with the pointer, on a long song as on a short one; that
    pointing and pressing 4 needs no click first; and that the sheet does not take
    the keyboard from the search box or the ChordPro text;
  - the bubble: letters made capitals as they are typed, a key that is not taken
    leaving the caret where it was, Enter, Tab, Esc, Down, Up and Right among what
    is offered, a click on one of them, Backspace in an empty box;
  - click, type, click, type, along a line and from slide to slide, quickly: rules
    38 to 40, which the page calls most of what makes the sheet quick;
  - dragging: that a chord comes away after a little movement and not before, the
    faint one left behind, Ctrl held as it is let go; and a chord carried to a
    slide that is out of sight (**rough**: the sheet scrolls towards the pointer
    only while the pointer moves, not while it is held still at the edge);
  - Ctrl+C on a line and Ctrl+V on another; a click on a line and then Ctrl+V; and
    the same with something copied in another program in between, which is then to
    paste nothing of the sheet's;
  - Ctrl+Z and Ctrl+Y, and Undo, Redo, Copy and Paste in the Edit menu, with the
    sheet having the keyboard;
  - the zoom: Ctrl with plus, minus and 0, the two round buttons, Ctrl with the
    wheel; that the slide at the top stays there;
  - the wheel and a trackpad scrolling the sheet; the strip, where the pane is too
    narrow for its seven chords, scrolled sideways by the wheel;
  - the switch: a click, the arrow keys on it, Ctrl with 1 and 2, the View menu;
    and that the keyboard goes to the editor shown;
  - the settings' Chords section: the system's colour picker, and the sheet behind
    it changing colour as one is chosen;
  - that the sheet is sharp, and its tails on their characters, on a screen set to
    another size than the first one's;
  - and M6's proof, which is the maintainer's to give: that it is as quick as Simple
    Presenter's. Every change to a song draws every slide of it again; on the test
    songs that is nothing, and on a song of sixty slides nobody has timed it.
- **Nobody has clicked any of the mending by hand, and it has been near no real
  song.** The made-up library it was tried on is the test files. What the maintainer
  is asked to try, on a COPY of a library that the old editor was used on:
  - whether the songs it marks are the ones that were saved in that editor, and none
    else; and what ProPresenter shows of one before and after **Mend**;
  - **the one test that settles check 2** (`spec/08`, B), which only the maintainer
    can make: in the old editor, a chord on the first word of a slide's second line,
    saved, and looked at in ProPresenter. Until then its switch stays off;
  - Mend All on some dozens of songs: how long it takes, that the list behind it is
    right afterwards, **Stop**, and Esc and Enter at its question;
  - a copy put into its original, and the copy moved out: that ProPresenter then
    lists the song with its chords and not the copy, and that a playlist that had
    the song still has it; and **Settings, Backups**, where the copy now is;
  - the row of offers where the pane is narrow: a long sentence beside two or three
    buttons has not been looked at there.
- **For the maintainer to say, of M8**: each of decisions 78 to 93, and above all
  - whether a song beside a copy with a doubted key should keep its own ("At M8");
  - whether the ChordPro text is to show a chord that is on no character (81);
  - whether the fourth choice should be there always, as the page's four (83);
  - the words: every sentence of the mending is in `LibraryModel.Mending.cs` and
    `LibraryModel.Folding.cs`, and all but the page's three are this session's.
- **What M8 does not do.** A song cut short by the old editor (H) is listed as one
  that cannot be read, as any such file is, and Earlier Versions is offered if the
  app has copies of it: nothing more was built for it. Names that are not chords
  (F, G) are kept as they are, as the page says. Chords on a box that is not the
  words (E) are in sight and can be dragged, where the editors show the box.
- **A copy cannot be put back over a file that cannot be read, in either app, and
  nothing says so.** `spec/08`, H, has Earlier Versions offered for such a song, and
  it is: the copies can be looked at. But **Put This One Back** there does nothing
  and says nothing. Putting back is a save of the song that is open (`spec/07`, rule
  19), and a file that cannot be read is not open. The Mac's is the same
  (`LibraryModel.swift`, `putBackPicked`). It is the one time a kept copy matters
  most, and it is rare: only a file that went wrong after this app had kept a copy
  of it has any. Read on the way through M8, and NOT put right here, since it would
  be a rule of the apps' and not of this one's. **Proposed, for the maintainer**:
  over a file that cannot be read, the copy is put back as over any other (not while
  ProPresenter is running, the file as it is copied first, written beside and
  swapped in), without the check that the file is the one last read, there being
  none. Until then Settings, Backups can save the copy as a file.
- **The copy moved out of a library is kept as a copy "before mending"**, the three
  reasons a copy has having no fourth. All Copies lists it under its own name, as
  the copy of a song that is no longer there.
- **No song made from a ChordPro file has been opened in ProPresenter, and nobody
  has clicked any of M9 by hand.** The maintainer is asked, on a COPY of a library
  or an empty one, with ProPresenter quit:
  - **File > New Song from ChordPro File...**: that the system's chooser comes,
    shows ChordPro files and text files, and that a file chosen brings the panel;
    the name's field (typing in it, and that Enter makes the song and Esc cancels);
    the two boxes of choices; a file that is no song; a refusal;
  - **then, in ProPresenter**: that the song is in the library and opens; that it
    looks like the song it was made to look like, on every slide, **to the end of
    each line** (decision 100: capitals, a fill, a second font); that its groups
    have their names and the colours ProPresenter gives those names; that showing a
    slide starts nothing (decision 95); and **that its chords show on a stage
    screen**. Made like the app's own song, and made like one of the library's own;
    a song of four lines to a slide made like one of two; a song with a line of
    chords alone (its last slide, in *Amazing Grace*'s file);
  - **and the same on the Mac**, once the Mac app has M9, with a song made on each
    opened on the other;
  - **File > Save Song as ChordPro File...**: that the chooser offers the song's
    name with `.cho`, asks before writing over a file, and that the file opens in
    whatever else reads ChordPro.
- **For the maintainer to say, of M9**: each of decisions 94 to 112, and above all
  that a slide is copied without what it starts (95); what becomes of a second text
  box (99) and of what is said of stretches of words (100, and M7's box with it);
  whether *Rock of Ages* is the song to carry, and where it should live (107);
  names on a PC and on a Mac (103); and what a song written out should read back
  as (112, "At M9").
- **What M9 does not do.** One file at a time, chosen from the menu: a file cannot
  be dropped on the window, nor handed to the app as it starts. A new song has no
  arrangement, and its groups no colours of their own. Nothing is asked about a
  song the library has already under the same name: it is made beside it, with a
  number. A file's `{comment}` that is not a part's name, and its tablature, are
  left out, as the rules of reading have it. The chords of a file are not held to
  being chords this app knows beyond what the rules do (`[*Coda]` is dropped).
- **The same few lines write a character by its number in two places** of
  `RtfWords.cs`, for stand-ins (`BytesOf`, held by vectors) and for any words
  (`WordsWritten`). The first could be the second with a test before it. Left, so
  as not to touch what the vectors hold for the sake of tidiness.
- **What is left of M10**, none of it this session's to decide: **the icon** (the
  app and its window have .NET's own); **the words** of About and of the read-me
  (decisions 113 and 115); **what Windows says of the unsigned file**, to be seen
  and the read-me held to it; **the version**, which says 0.1.0 as the Mac's early
  build says 0.1; whether to sign; the top-level `README.md`'s lines about the
  Windows app; and **the release itself**. The zip is fifty-seven megabytes, most
  of it .NET (the file inside is a hundred and thirty-six); making it smaller is
  not begun. The first start of a new build took twenty-four seconds on this PC
  with other work going on and under two after that: the read-me says "some
  seconds".
- **The caret of the ChordPro text and the sheet's picked slide do not know of each
  other** (`spec/04`, rule 21), as on the Mac: gone to from the sheet, the text is
  where it was last left, and the sheet likewise. What would tie them is built and
  tested (`ChordProEditor.SlideAt`, `StartOfSlide`); nothing calls it.
- **Only the pictures hold some of how the sheet looks**: that a chord being carried
  is left faint where it was, the colours of a bubble that is lit, the dark look of
  the sheet's scroll bars. A test holds where the words, the chords, the spot and
  the ring are drawn, by their dots; not these.
- **A song whose name Windows keeps for itself** (CON, NUL, COM1 and their like)
  cannot have a folder of copies made for it. The copy then cannot be kept, and the
  change that wanted it is refused, which is the safe way to fail; but such a song
  could not be chorded at all. Not seen, and nothing is done about it.
- **If the system will not say whether ProPresenter is running, it is taken as
  running** (decision 21). No test can make the system refuse, so no test holds it.
- **Marks being worked out for a library that has been left are let go** after the
  few in hand. Taking that out wastes work and breaks nothing, so no test tells.
- **Two cares of the copies that no test can reach.** A copy is read back after it
  is written, and the change it was for is refused if what is on the disk is not
  what was written: only a disk that does not keep what it is given would show it.
  And before a copy that is kept no longer is thrown away, it is looked at: it must
  be in its song's own folder and named as a copy is. Every copy the list hands
  over is both, so nothing can be made to fail there; the care is kept because it
  stands before a deletion (AGENTS.md, "The person's files").
- **The app has no icon.** It is M10's.
- **`old/publish.py` must not be run**: it commits, pushes and tags. Its README says
  so now.
- **Two lines of `reference/README.md` are no longer true**, under "Where this and
  `spec/` differ": that the apps leave out a slide with no text box, and that they
  leave a blank slide's box empty when its chords are taken off again. The
  maintainer decided otherwise on 2026-10-11 and the Mac's M7 built both. The first
  also names a heading `spec/01` no longer has. Told to the maintainer; it is the
  Mac session's or the Linux one's to put right, not this one's.
