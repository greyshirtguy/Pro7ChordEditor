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
| **M6** The chord sheet | Next | `spec/03`. Its model first, held to `sheet-model.json` and `layout.json` |
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
57. **The settings' Chords section waits for the sheet** (`spec/06`, "Settings": the
    colour of a chord on the sheet and in the ChordPro text). It shows each "as it
    will look", one of them a chord in its bubble, which is M6's to draw. The colour
    of a chord in the text is one place in the code until then.

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
  **`RevertQuestionPanel`** is over the window while Revert is asking.
- **The tests type into the real box**, by the doors a keyboard uses
  (`Typist.cs`): a character is raised as WPF raises one, a key is told to
  `KeyPressed` and, if not taken, the command it stands for is given to the box.
  One test sends real key presses through a window of the system's that is never
  shown. Nobody's clipboard is touched: what would be pasted is put in place of
  what is selected.

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
    new line in it too (`ChordProTyping.BreaksALineInATextBox`);
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
- **The settings have no Chords section yet** (decision 57), so the colour of a chord
  in the text cannot be chosen until M6.
- **A slide picked from elsewhere** (`spec/04`, rule 21) has nowhere to be picked
  from until there is a sheet. What it will ask is built and tested
  (`ChordProEditor.SlideAt`, `StartOfSlide`); nothing calls it yet.
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
