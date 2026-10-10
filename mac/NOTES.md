# Notes on the Mac app

For the next session, which will remember nothing of the ones before: what was decided
on the way and why, what was found out about ProPresenter on the Mac, and what is left
rough. Read it after `AGENTS.md` and `spec/11-plan.md`. Add to it at the end of every
milestone; put right what has become untrue.

## Where things stand

| | | |
| --- | --- | --- |
| **M0** Setting up | Done, 2026-10-10 | The package and its three layers, empty; the generated reader; nine tests; the Xcode project and an app with one window; the workflow |
| **M1** The rules | Done, 2026-10-10 | `ChordRules`: every rule of `spec/02`, the words of an RTF, the key fit and the old editor's shift. Every case of the fifteen vector files M1 is held to passes (some thirty thousand answers), with cases of our own for what no vector holds |
| **M2** The song | Done, 2026-10-10 | `Song`: a song opened and read (blank slides among its boxes), its chords and its two keys set, every change saved at once and safely, refusals, undo and redo. The proofs at the foot of `spec/05`, on copies of the test files |
| **M3** The library | Done, 2026-10-10 | Places found (workspaces from ProPresenter's settings, Documents, folders chosen), their libraries and songs listed, searched and narrowed by mark; a song shown to be looked at; whether ProPresenter is running; the app's own folder. The window, drawn into pictures by the tests. **The maintainer has not yet tried it** |
| **M4** The copies | Done, 2026-10-10 | A copy kept before a file's first change, listed (with or without its index), thinned at the start, put back and taken back. In the window: the mark, Earlier Versions on a song, All Copies in the settings. The proofs at the foot of `spec/07`. **Nothing in the window can make a copy yet**, so on a real Mac there are none to see until M5 |
| **M5** The ChordPro editor | Done, 2026-10-11 | A song is shown as ChordPro text and its chords typed in: the one rule of what may be typed, the help with brackets, a save a moment after the typing stops, all of one saving one step of undo. The two keys in the song's header. Refusals said under the song. **The first build that changes a chord from the window. The maintainer has not yet tried it**; it is to be tried on a COPY of a library first |
| **M6** The chord sheet | Next | |

## What the maintainer decided, 2026-10-10

1. **Both apps read files with code generated from `proto/autogen-proto`**, the set
   taken from ProPresenter itself, not `proto/proto` as the pages first said. The
   pages were put right the same day, in commits made beside this work
   (`spec/01-file-format.md` has the whole of it).
2. **"Everything else as it came" is said of the message, not of the bytes.** The
   maintainer's words: the rule "is about not disturbing/breaking ProPresenter's
   parsing of these protocol buffer files". `Core/Sources/ProFormat/ProFormat.swift`
   has the long of it.
3. **There is no `PathSettings.proPaths` on the Mac.** ProPresenter has workspaces,
   each with a folder of its own. The app is to show the workspaces it finds and let
   one be picked; anything else is opened with Choose Folder. How ProPresenter records
   them is to be found out at M3, by reading only (`spec/06-library.md`, rule 3).
4. **macOS 15 and 26** are "the newest macOS and the one before it".
5. **The ignore rules are one file for each app.** The Visual Studio list that was at
   the top of the repository would have left out any folder under `mac/` named
   `Packages`, `Debug` or `Backup…`. It is now `windows/.gitignore`; `mac/.gitignore`
   is the Mac's; the one at the top has only what is true of both.
6. **The maintainer has real songs on this Mac** that may be read, and checked
   against as copies outside the repository: some from Multitracks as they came, some
   changed by the old Windows editor. Ask where. Nothing of them comes into the
   repository (`AGENTS.md`, "The person's files").

And at M1, the same day:

7. **The words of an RTF are read by a port of the reference's small reader**, not by
   AppKit's (`NSAttributedString`). The page allowed either on the Mac. A port keeps
   AppKit out of the rules and gives both apps the same reader.
8. **`spec/02-chord-rules.md` has a section "To the letter"** with what the Mac port
   had to pin down (white space, what a letter is, comparing by units, a heading's
   words), so that the Windows app does the same.
9. **Two names are not the reference's**: its `Song` (a ChordPro file as read) is
   `Chords.ParsedSong`, and its `Range` is `ChordRange`. Swift has a `Range`, and
   `Song` is the layer above.

And after M1, on what its check of real songs found:

10. **A chord on a line of chords alone is written with a range one long**, as
    Multitracks writes it, and not run on to the next chord as Simple Presenter
    writes it. The maintainer's measure: the proto and the songs from Multitracks are
    the best source of what is right. `spec/01` and `spec/02` say so. At first the
    code waited for `ranges.json` to be made again from a changed Simple Presenter;
    then the maintainer said not to wait, and `Chords.ranges` has the rule. The vector
    is still not edited: see "Left rough" for the one allowance that makes.
11. **Blank slides are to be shown, and to take chords.** Simple Presenter leaves a
    blank text box out, so an intro made as an empty slide can be given none. The
    design is in `spec/01` ("A blank slide", "Chords onto a blank slide"), with what
    it asks of the sheet, the ChordPro editor and the song in `spec/03`, `04` and
    `05`. Showing them is M2's (`blank`, in what the song hands over); giving them
    chords is M7's.

And at M2, by saying go to its plan:

12. **The type is `OpenSong`**, in the layer still called `Song`: "the song that is
    open", as `spec/05` has it. A type named as its module is makes trouble in Swift.
13. **A song cannot save unless it is handed its safeguards**: a way to ask whether
    ProPresenter is running, and a way to keep a copy first. The real ones are M3's
    and M4's. One opened with none can be read and nothing else.
14. **The key is a step of undo**, as `spec/05` says; in the reference it is not.
15. **What a refusal says** is a list of sentences in `spec/05`, for both apps.

16. **A file ProPresenter wrote is in the test data**: *Rock of Ages*, from the
    maintainer's MainHall library. The maintainer said to use any file of it. One was
    chosen whose words are out of copyright, since `AGENTS.md` and `testdata/README.md`
    say nothing else may come in, and whose bytes hold nothing of anyone's: it was
    looked through first. `testdata/README.md` describes it.

And at M3:

17. **The window is a library of the package**, `AppViews`, with no `@main` in it,
    so that `swift test` draws it into pictures. The Xcode target is the few lines
    that show it.
18. **ProPresenter's settings are read at run time**, to find workspaces, and only
    read. Nothing is written in any folder of ProPresenter's.
19. **The places the app finds are those whose songs are `.pro` files**: workspaces
    kept as folders, `ProPresenter` in Documents, and folders chosen by hand.

And at M4:

20. **A copy's folder has the place in it**: `Backups/<place>/<library>/<song>/`. The
    page had none, from before there could be several places. `spec/07` says so now.
21. **A copy is shown as its lines**, as a song is in the window, until the chord
    sheet is built (M6).
22. **The song is opened with its real safeguards now** (`LibraryModel.open`): whether
    ProPresenter is running, and `Copies.keep`. M5 has nothing to add for a change to
    be safe; it only has to ask for one.

And at M5, 2026-10-11:

23. **A slide with no text box at all is a blank slide too**, handed over with no id,
    and is given a text box when a chord is put there, as Simple Presenter does it;
    and **chords taken off a blank slide again in the same sitting leave the file byte
    for byte what it was**. Both the maintainer's ("Let's adopt the ability to add/edit
    chords on blank slides! and yes do #2 as well (I will typically keep the simple
    presenter and stand alone editors identical)"). `spec/01` has both now. Reading
    hands such a slide over already; the writing of both is M7's.
24. **M5 was built on "and then proceed"**, with no plan shown first. The plan the
    page asks for was given with the report instead.
25. **The two keys are pickers in the song's header from M5**, not left for the sheet:
    without them a song cannot be told what key its chords are in, and ProPresenter
    is the one that moves them to another.
26. **Only the boxes that were typed in are saved** (`spec/04`, rule 24). The
    reference saves every box whose chords the text cannot show exactly, the first
    time anything is typed anywhere.
27. **A save that is refused makes the text again from the song** (`spec/04`, rule
    19a), so that what is seen is what its sentence says: nothing was changed. And
    if it was the save made on the way to another song, the song is not left, or the
    sentence would be swept away unread.
28. **Until M7** a line of chords alone keeps as many chords as it has and a blank
    slide takes none; what would change that is not let through, and says why
    (`spec/04`, "Until chords with no words are built").

## How it is laid out, and why

```
mac/
  Pro7ChordEditor.xcodeproj   the app target, and nothing else
  App/                        the app: SwiftUI, thin
  Core/                       one Swift package: everything with no window in it
    Sources/ChordRules/         the rules (M1)                  no protobuf, no AppKit
    Sources/ProFormat/          the generated reader of a .pro  nothing by hand
    Sources/Song/               the song, the library; the copies, the checks (M2 on)
    Sources/AppViews/           the window: its model and its views (M3 on)
    Tests/TestData/             how tests find testdata/
    Tests/ChordRulesTests/      held to testdata/vectors
    Tests/SongTests/            held to testdata/files
    Tests/AppViewsTests/        the window's model, and pictures of the window
    generate-proto.sh           what made Sources/ProFormat/Generated
```

- **One package, three libraries**, not three packages: one `swift test`, one
  `Package.resolved`, and a target still cannot import what it does not name.
- **The generated code is kept in the repository.** swift-protobuf's build plug-in
  only takes `.proto` files inside the package, and these are in a submodule outside
  it. Kept, the package builds from a clone with no submodule and no `protoc`.
  `Generated/MadeFrom.swift` says what it was made from. **Run
  `mac/Core/generate-proto.sh` again whenever the `proto/` submodule is moved on**;
  nothing checks that for you (see "Left rough").
- **Only what a presentation needs is generated**: `presentation.proto` and what it
  imports, 32 files of the 148. For another kind of file, add its `.proto` to `ROOTS`
  in the script.
- **Tests find `testdata/` by walking up from their own source file.** A package
  cannot take resources from outside itself.
- **The Xcode project was written by hand**, not made by a tool, so that there is no
  tool to depend on. Its `App` folder is one that follows the disk (a "synchronized"
  folder, Xcode 16 and later): a file added to `mac/App` is in the app without the
  project being touched. `Info.plist` is named in the project as not a resource.
- **The package is in the project as a folder**, not as a package the project refers
  to. Both build the app. Only the first lets the scheme run the package's tests
  (Command+U in Xcode): with a reference, `xcodebuild test` answered "There are no
  test bundles available to test".
- **The app target** is Swift 6, with the main actor as the default for its own code,
  as Xcode 26 sets a new app up. The package is plain Swift 6.
- **`com.greyshirtguy.Pro7ChordEditor`** is the app's bundle identifier. It is my
  choice, from the GitHub name, and is the maintainer's to change. Better changed
  before M3 than after: macOS keeps what the person has allowed the app, and the
  app's settings, under it.
- **The app is signed "to run locally"** (ad hoc), which needs no account. The
  hardened runtime is switched on in the project and Xcode leaves it off for an ad hoc
  signature, saying so in the build log; it comes on by itself when the app is signed
  with a Developer ID at M10.

### The rules (M1)

`Core/Sources/ChordRules/Chords.swift` is the way in and lists the files. What to know
before changing any of it:

- **The rules stand under `Chords`**, as the C++ has them under `chords::`, with the
  names they have there: `Chords.transposed("C#m", from: "E", to: "Db")`. Three things
  that are not from `chords.cpp` have names of their own: `RTFWords`, `KeyFit`,
  `OldEditor`.
- **Inside, everything is `[UInt16]`**: a String is turned into its UTF-16 units on the
  way in and back on the way out, and `Units.swift` has the few operations, each named
  for the QString function it stands in for. Nothing uses String's `count`, `hasPrefix`
  or `==` on text, nor Foundation's trimming or character sets. Keep it so.
- **`Words`** is the type for a text box's words, and a place is an `Int` into its
  `units`. Names of chords and keys, and ChordPro text as typed, are plain Strings.
- **Tests**: one file of tests for each vector file, reading every case
  (`Tests/ChordRulesTests/Vectors.swift` says how they compare and report). Tests
  marked "Mine, not a vector" are just that.

### The song (M2)

`Core/Sources/Song/Song.swift` is the way in and lists the files. What to know before
changing any of it:

- **Everything that may be asked ends in `OpenSong.save`.** A new state of the whole
  message is made beside the one in memory; the safeguards are asked; the file is
  written beside itself, pushed onto the disk, swapped in and read back; and only then
  does the new state become the song. So a refusal or a failure anywhere leaves the
  song in memory as it was, with nothing to put back. Keep it so: do not change
  `presentation` anywhere else.
- **The order in `save`** is the order in `spec/05`, rule 10: no safeguards; ProPresenter
  running; the file changed by something else (size, time AND bytes); the copy; the
  write. A refusal before the copy leaves no copy.
- **A step of undo** holds the part that changed, before and after: one cue, or
  `music` (which a song may not have at all). 200 steps are kept, as the reference keeps.
- **Slides do not change while a song is open**, so they are worked out once. The text
  boxes are read from the message each time they are asked for. If that is ever slow
  (it parses every box's RTF), keep them and throw them away in `save`.
- **Tests** work in `Workbench`: a folder of its own under the system's temporary
  folder, with copies of the test files and safeguards a test can set. Nothing is ever
  saved into `testdata/`.

### The library and the window (M3)

- **In `Song`, with no window**: `Places.swift` (finding), `Listing.swift` (libraries,
  songs, search), `Marks.swift`, `AppData.swift` (the app's own folder), `SongText.swift`
  (a song as lines of text: what is shown now, and what the ChordPro editor will work
  on at M5).
- **In `AppViews`**: `LibraryModel` is everything the window shows and does, with no
  drawing in it, and is tested so. The views only show it. `LibraryModel`, the finder
  and the song are each handed what they would otherwise ask the Mac for, so that
  tests hand them a made-up Mac (`Tests/AppViewsTests/MadeUpMac.swift`).
- **To see the window**: `PRO7_PICTURES=/some/folder swift test --package-path mac/Core
  --filter PictureTests`, then open the PNGs. Do this after any change to how
  something is drawn, and say what was seen. Without the variable they go to
  `Pro7ChordEditorPictures` in the system's temporary folder.
- **A song is opened with no safeguards at M3**, so it cannot be changed. M5 hands it
  real ones: `ProPresenterApp.isRunning`, and the copies of M4.

### The copies (M4)

- **In `Song`**: `Copies.swift` is all of it: keeping, listing, thinning, saying when.
  `OpenSong.putBack` is the putting back, and is a save like any other: it goes
  through the same three questions (`maySave`) and writes with the same `SongFile.write`.
- **In `AppViews`**: `LibraryModel` has the panel's state (`earlier`), `putBackPicked`
  and `takeBack`; `EarlierVersionsPanel` shows it. `AllCopiesModel` and `AllCopiesView`
  are the settings (Command+comma), wired in `App/Pro7ChordEditorApp.swift`.
- **Only `Copies.thin` deletes anything**, only files in a song's folder of copies that
  are named as copies, and never the oldest. If that is ever changed, look hard.
- **Tests make history** with `MadeUpMac.change(_:daysAgo:)`, which changes a song as
  the app would, with a copy kept first, dated then.

### The ChordPro editor (M5)

- **In `Song`, with no window**: `ChordProEditor.swift` is every rule of `spec/04`:
  what may be typed (`take`, `allows`), the help with brackets (`helpWith…`), what a
  pause saves (`save`), the colours, which slide a place is on. It works on the lines
  `SongText` makes, and counts in UTF-16 units throughout.
- **In `AppViews`**: `ChordProView.swift` has `ChordProText`, the system's text view
  and what stands between it and the model, made apart from SwiftUI so that a test
  can type into it; and `ChordProView`, the few lines that put it in the window.
  `LibraryModel` says when to save (`textMayBecome`, `settle`) and sets the keys.
- **How a key press goes**: the text view asks before any change
  (`shouldChangeTextIn`) -> `LibraryModel.textMayBecome` -> `ChordProEditor.take`. Yes,
  and the change happens and a save is set for 700 ms on; no, and it never happens.
  The save is `LibraryModel.settle` -> `ChordProEditor.save` -> `OpenSong`.
- **The text is set from the model only when `ChordProShown.version` goes up**: a
  song opened, an earlier version put back, a save refused. Never because of what was
  typed, or the caret would jump from under the person typing.
- **`settle()` is called before anything else is done to a song or the song is
  left**, and answers whether what was typed is saved. If a save was refused, the
  thing asked for is not done, so that the sentence is seen.
- **Tests type into the real text view**: `insertText`, `doCommand(by:)` for the two
  delete keys, `readSelection(from:)` with a pasteboard of the test's own (never the
  person's), and `undo(_:)`.
- **When M7 is built**: `ChordProEditor.canBeSavedYet` goes, with the one place that
  asks it and the one line in `save` marked UNTIL M7; and `OpenSong.setChordsAlone`
  loses its two refusals.
- **When M6 is built**: the sentence for a song whose words have brackets gets its
  last words back ("The chord sheet can still be used."), and `slide(at:)` and
  `start(ofSlide:)`, which nothing shows yet, are what ties the caret to the sheet.

## What was found out

- **A file read and written back unchanged is the same, byte for byte**, in every
  file tried: the nine of `testdata/files`, and copies of 22 real songs of the
  maintainer's (14 from Multitracks; last written by ProPresenter 7.8.0, 7.9.2,
  7.13.0, 7.16.2, 17.1.0, 18.1.0, 19.0.0 and 20.0.1 on the Mac and 7.14.0 on Windows;
  1,299 chords among them). This was a check made once, by hand, on copies in a
  temporary folder, with a throwaway program on `ProFormat`; it is not a test and is
  not relied on, since the rule is about the message (above). But it means that a
  change of chords should change the bytes of the chords and nothing else, which is
  worth a test at M2 on `testdata/files`.
- **`proto/autogen-proto` knows every field in those 22 files.** `protoc --decode`
  shows none without a name. Through the older `proto/proto` there are 123 without a
  name, in 4 of the files. That is the difference the maintainer's first decision
  makes: nothing is lost either way, but with the newer set there is nothing in
  those files the app is blind to.
- **The descriptions are of ProPresenter 21.4.** When the submodule is moved on to a
  later version's, generate again.
- **ProPresenter's bundle identifier is `com.renewedvision.propresenter`**, in every
  version looked at here, from 7.9.2 to 21.3. Now in `spec/06`, rule 17.
- **The test files say they were written by ProPresenter 7.16.2 on no platform**
  (`application_info`): Simple Presenter wrote them. A file ProPresenter wrote says
  `macos` or `windows`.
- **`protoc` warns "Import customOptions.proto is unused"** once for nearly every
  file when the script runs. It is true and harmless: the descriptions are taken from
  ProPresenter by a program, and it leaves those lines in.
- **Swift 6.1 at least** is needed: the generated code marks its types `nonisolated`.
  So Xcode 16.3 or later; the workflow uses GitHub's `macos-26`.
- **The workflow passes on GitHub's Mac** (first run, 2026-10-10, on `macos-26`, under
  two minutes): the tests, and the app built and signed to run locally, from a clone
  with no submodule and nothing installed. That was the last of M0's "done when".
- **Pushing is over SSH.** The key on this Mac is one GitHub knows. Over https, the
  token in the keychain may not change `.github/workflows`, and a push that touches a
  workflow is refused. `gh` is installed and not signed in; it is not needed to push,
  and the runs of a public repository can be read without it.

### At M1

- **Three things about Swift and Foundation that would have made the rules wrong for
  some songs**, all now in `spec/02`, "To the letter":
  - Foundation's white space has U+200B in it, and its trimming empties a line of
    stand-ins. (`spec/02` had said no library counts it so.)
  - Swift compares strings, and tests a prefix, by whole characters: "é" as one unit
    is the same to it as "e" and an accent.
  - Swift does not promise that its sort keeps equals in order.
- **A backslash, a "u" and four digits, typed into a file by an agent, can arrive as
  one Chinese character.** It is what RTF's numbered characters look like. It happened
  in a test sample here, and looked for all the world like a fault in the reader of
  RTF, which was right. `NotHeldByVectorsTests` builds its sample from pieces for that
  reason. After writing anything about RTF, look for characters that should not be
  there: `LC_ALL=C grep -rn -P '[^\x00-\x7F]' mac/Core`.
- **How the tests were shown to bite.** Every vector passed the first time the tests
  were run, which proves little. So each rule was broken in one small way in turn,
  seventy-nine ways in all, and the tests run each time. Fifty-eight were caught by a
  vector. Twenty-one were not. Two of those cannot be told apart from the rule by any
  input (worked through by hand). The other nineteen are rules no vector holds: see
  the next heading. Each now has a case in `NotHeldByVectorsTests`, and the breakages
  were run again to see those cases catch them.
- **On copies of the 22 real songs** (a check made once, outside the repository,
  printing numbers only):
  - 400 text boxes have RTF. The reader here and AppKit's give the same words, unit
    for unit, for all 400. None has a byte over 127 in its RTF.
  - All 1,299 chords stand on a place inside their words; none on a line break.
  - **Every chord on a line of chords alone has a range one character long** in those
    files: 290 of 290, its stand-in and no more (171 hang on U+200B, 119 on a space).
    `ranges()`, and `ranges.json`, run such a range on to the next chord, over the em
    quad between: (0,2) (2,4) (4,5) where Multitracks has (0,1) (2,3) (4,5). That is
    194 ranges that would be written otherwise than they were found. The maintainer
    has decided for Multitracks' way (decision 10). Looked at again for that
    decision: of the 1,009 chords on lines of words, 949 have exactly the range
    `ranges()` writes and the other 60 are the old editor's; and "one long on a line
    of chords alone, otherwise as now" gives all 1,239 undamaged ranges, with no
    exception. The chord entries are in order of place in every box, after every
    other entry, as `spec/01` has them written. Of the 82 boxes with a line of chords
    alone, 80 are that line and nothing else.
  - **Blank slides**: 27 slides of 383 have a blank text box (30 boxes; 14 share
    their slide with another text box, 5 have data links), and one slide has nothing
    on it. A blank box's RTF, as Apple's text system writes it (29 of the 30), is an
    empty document with an EMPTY font table: no font, size or colour to give a
    stand-in. That is why `spec/01` has the RTF of a blank slide made from a
    neighbour's opening and not changed in place. A box that is one stand-in with a
    chord on it, as Multitracks writes it, has the whole opening and then the
    stand-in: a space, or a numbered U+200B.
  - Away from such lines, every range in these songs is exactly what `ranges()`
    would write, but for two songs, in which 60 chords have a range that
    runs past the end of its line: check 1 of `spec/08`, the old Windows editor's
    mark. The other checks call nothing in any song doubtful.
  - The one song last written by ProPresenter on Windows has six text boxes and no
    line break in any of them. So how ProPresenter on Windows writes a line break is
    still not known (`spec/01`, "Not known"). Its RTF is not Apple's dialect (no
    `cocoartf`; it has a list table, margins and paper size) and reads the same here
    as in AppKit.
  - Two songs are shown in another key than their own (`user` is not `original`), one
    of them in C#, which is not among the twenty-six keys a picker offers.

### At M2

- **How the proofs were shown to bite**, as at M1: each rule of the song layer was
  broken in one small way in turn, forty-five ways, and its tests run each time.
  Forty-four were caught. The other no input can tell from the rule (a box that is
  only a zero-width space is not blank whether or not it is asked for by name, since
  a zero-width space is not white space).
- **Where the test files could not hold a rule**, cases were made from Plain Song in a
  temporary folder, and say they are mine: slides whose cues are not in the order
  their groups name them, a cue named twice, a cue in no group, an action switched
  off, chords out of order in the file, an entry that is not a chord among the chords,
  a blank slide, a box with a link. In every test file the cues are already in group
  order and the chords already in order of place, so `as-read-by-simple-presenter.json`
  would pass without either rule.
- **On copies of the 22 real songs** (once, outside the repository, numbers only):
  383 slides; 387 text boxes handed over, 17 of them blank slides; 1,299 chords. Each
  box was then given the chords it already has, and everything undone:
  - **all 22 files came back byte for byte what they were**;
  - for 236 boxes of 307 the file did not change at all when the box was given its own
    chords;
  - the 71 that changed are the 51 with a line of chords alone (the range rule that
    is to change: decision 10) and the 20 the old Windows editor had saved.
  Run again with the new range rule in: the 51 are none. 287 of 307 boxes are written
  exactly as they were, and the 20 that are not are the old editor's, in two songs.
- **On copies of the 309 songs of the maintainer's MainHall library** (the same way):
  every one opens; every one read and written unchanged is the same bytes; there is
  no field in any of them that the descriptions have no name for. Two have chords.
  One is from Multitracks, and all 121 of its chords are written as the rule writes
  them, the 56 on lines of chords alone among them. The other is not from
  Multitracks, states no key, and has 8 chords each with a range exactly 4 long, none
  of them on the start of a word: **who wrote those is not known**. It is not what
  Multitracks writes nor what the old Windows editor is said to have written
  (`spec/08`). Ask the maintainer before M8.
- **The file ProPresenter wrote** (`testdata/files/made-by-propresenter`) has its cues
  out of the order its groups name them, a colour on every group, and a link on every
  text box that only says when the box is to be seen. Under "a box with a link is not
  shown", as the page first had it, the song would have had no words at all.
- **No chord's name in those songs would be changed by tidying**, and none is empty.
  It matters because setting a box's chords tidies every name in the box, the ones
  nobody touched among them (`spec/05` now says so).
- **Every `music` in those songs has both its keys**; seven songs have none; two are
  shown in another key than their own.
- **A box with a link.** The page said "no data_links". The reference is finer: a box
  whose only link says when it is to be seen keeps its own words. Followed, and
  `spec/01` now says it.
- **`FileManager.replaceItemAt` keeps the old file's permissions**, and fails cleanly
  on a file that may not be changed (the new file is then cleared away). Both are
  tests.

### At M3

- **How ProPresenter 21 records its workspaces** is in `spec/06`, rule 3: a list in
  its own settings, under `userWorkspaces`. Read with `CFPreferencesCopyAppValue`,
  which an app that is not sandboxed may do for another's settings.
- **On this Mac the finder finds two places**: the workspace in use (1 library, 2
  songs) and `ProPresenter` in Documents (10 libraries, 422 songs, 19 of them with
  chords, 3 the old editor's copies, none that cannot be read). The folder the
  settings name is the workspace, so it is shown once.
- **The marks of all 422 songs are worked out in a quarter of a second** (an
  optimised build). They are still done in the background and kept, as the page
  asks: a library on a slow disk is another matter.
- **ProPresenter's helper programs go on running after it has quit.** They have
  bundle identifiers of their own, and asking for the app's own does not count them:
  checked with ProPresenter quit and two helpers running.
- **Drawing the window off the screen** (`spec/10`): `cacheDisplay` of an
  `NSHostingView` in a window never shown draws lists, fields and buttons. A
  sidebar's see-through material comes out blank, which is why the three panes are
  plain `HSplitView` panes with grounds of their own and not a `NavigationSplitView`.
- **A view that scrolls both ways centres what is smaller than itself.** The song is
  given the pane's size as its least size to sit at the top left. Seen in a picture,
  and put right.
- **The four things the first pictures showed wrong**: that; a quiet line too long
  for its pane; a raw error code in the sentence for a file that cannot be read; "no
  songs in this library" where there was no library.

### At M4

- **How the proofs were shown to bite**: twenty-six breakages, one at a time. All
  were caught but one, which was a rule with nothing to show it (after thinning, the
  index still listed copies that were gone). It has a check now, and is caught.
- **A put back and its taking back leave two copies more**, each "before putting back
  a copy": a copy is held only against the NEWEST kept (rule 4), so the second is
  made though an older copy has the same bytes. I had expected one; the rule is the
  rule, and the page now says why.
- **`Data.write` will not take "never write over a file" together with "atomically"**:
  it stops the program. A copy is written with the first alone, then read back, and
  cleared away if it did not come out whole.
- **Three pictures were looked at** (the panel, the window just after a putting back,
  All Copies) and nothing in them wanted changing.

### At M5

- **A text view does not answer the Edit menu's Undo itself**: it leaves `undo:` to
  its window, which uses the window's undo. Given an undo of its own by its delegate
  (which rule 23 wants, so that it can be forgotten when the text is set afresh), it
  must answer `undo:` and `redo:` itself, and say when each can be done.
  `ChordProText.View` does.
- **The help with brackets is given to what arrives as typed text**
  (`insertText`), not by looking at which keys are down: on the Mac a key pressed
  with Command or Control never arrives so, and several keyboards type `[` and `]`
  with Option held. What is pasted or dropped comes by another door
  (`readSelection`) and gets none.
- **Everything the system does to text as it is typed is switched off** in the text
  view (curly quotes, dashes, spelling put right, spaces added round a paste): each
  is a change to the words, and would be refused one key press after another.
- **Asked to bring the caret into view before it has a size, a text view scrolls its
  own margin away.** `ChordProText.show` puts the top left corner in view instead
  when the caret is at the start.
- **A window made in code lets go of itself when closed** unless told not to. A test
  that closes one it still holds brings the tests down a little later, in some other
  test. `isReleasedWhenClosed = false`.
- **A save that is waiting for the typing to stop can come after a test is over**,
  when its made-up Mac is gone. `MadeUpMac` is held weakly by what it hands out, and
  a test that types either waits (`whatWasTypedIsSaved`) or settles.
- **Swift calls two Strings equal that only look alike** (an accented letter as one
  character and as two). What is typed is held against the words as UTF-16 units,
  never as Strings; `Chords.withoutChords` and `chordsOf` were given forms that take
  and give `Words` for it.
- **Thirteen pictures were looked at.** Three things were wrong in the first of the
  editor and were put right: the song's name was cut to "A...ace" by the two keys
  beside it (they go under it now where the pane is narrow); the line of help was cut
  off; and the text's left margin was scrolled out of sight. The dark one, the one
  with ProPresenter running, a refusal with Open Again, a song whose words have
  brackets, and blank slides were then as meant.
- **How the proofs were shown to bite**: eighty-two breakages, one at a time, of the
  editor's rules, the reading of blank slides and the window. Seventy-seven were
  caught. Two that were not were holes in the tests and are caught now: text with a
  line more or a line fewer at its END (every line before it still being what it
  was), and a paste straight after typing. One was a guard nothing reached (a paste
  does not come in by `insertText`), and is taken out. The five still not caught are
  each a second lock on a door another check already holds, and say so where they
  stand: the reference's test for a stray bracket (the words not matching says it
  already); rule 16's "on the last character" (`Chords.tidy` does the same when the
  box is written); which side of a `]` Delete is on (it comes to the same chord);
  that a barred song saves nothing (nothing is ever taken from one); and asking
  about a change made from here before making it (it is asked about again after).
- **The tests hung once, and it could not be made to happen again.** In the first
  breakage round one run of the tests never came back (half an hour, until it was
  stopped). The same broken rule was run 43 times more, then the whole round again
  with a time limit on each, then all the tests but the pictures 80 times: no hang.
  The cause is NOT known. What was running were the tests that type into the real
  text view from a program with no window on the screen, and my guess is something
  in the system that such a program waits on now and then (the tests made the
  system's "no" sound then; they are silent now). If it comes back: `sample` the
  process named `swiftpm-testing-helper` before stopping it. The workflow has a time
  limit now so that a hang there cannot hold a machine for hours.
- **A check on copies of 331 real songs** (the maintainer's two sample libraries,
  copied outside the repository under numbers, read and then deleted; numbers only):
  all opened; one cannot be changed as ChordPro, having brackets in its words; 16,268
  lines of text, 130 of them lines of chords alone, and **1,173 blank slides**, five
  with no text box. For each of the other 330, nothing typed saved nothing and kept
  no copy. In the 281 that have a line of words not starting with a chord, a chord
  typed at the start of the first such line was taken, saved, and was then in the
  file at that place with the box's words as they were; the only thing in the whole
  message that differed was that one text box's chord entries; and undo left the
  file byte for byte what it was.
- **A text box's id is not its own in a song.** In one of those songs the boxes of 27
  slides share one id: slides made by duplicating one. My check tripped on it, not
  the app, which names a box by its slide and its id and changed the one box. But
  nothing must ever tell boxes apart by id alone: the editor goes by which box of the
  list it is (`SongLine.box`), and the sheet must too. `spec/05` says so now.
- **Blank slides are everywhere**: three and a half to a song in those libraries. In
  the ChordPro text each is an empty line after the gap's empty line, so most songs
  have runs of two empty lines in them. It reads well enough in the pictures; whether
  it does in the hand is for the maintainer to say (`spec/04`, rule 3).

### Rules the vectors did not hold, and what became of them

At M1, breaking the rules one at a time found nineteen that no vector held. Each was
given a case of the Mac app's own, its answer READ from the reference's code, and the
inputs were listed for the tools that make the vectors.

**On 2026-10-10 the vectors and test files were made again** from a Simple Presenter
that has the new range rule and shows blank slides, with nearly all of those inputs
in, a song for headings, a song with blank slides, and the file ProPresenter wrote
read into `as-read-by-simple-presenter.json`. What that showed:

- **The Mac app's rules passed every new case with not a line of them changed.** The
  reference answered as its code had been read.
- The allowance the range tests made for one stale case tripped, as it was built to,
  and is taken out.
- Of twenty breakages that only the Mac app's own cases caught before, the vectors
  now catch nineteen. The own cases that a vector now holds are gone from
  `NotHeldByVectorsTests`; one is left, and `testdata/README.md` ("Still wanted")
  says what would hold it.
- `as-read-by-simple-presenter.json` now has `blank` on each entry, and an entry with
  an empty `element` for a slide that has no text box at all. That last is Simple
  Presenter's own (it gives such a slide a text box when a chord is put there); this
  app hands nothing over for it, and `ReadingASongTests` leaves those entries out.

**Two things the reference now does that these apps do not, for the maintainer to
decide** (both in `spec/01`, both M7's): giving a slide with no text box one when a
chord is put there; and, while a song is open, putting a blank slide's file back byte
for byte as it was when its last chord is taken off again. The maintainer asked the
second of Simple Presenter in so many words.

## Left rough, and what to ask

- **Nobody has typed a chord into this app with a hand.** Everything of M5 has been
  proved by tests on made-up Macs and seen in pictures; the app itself has not been
  run by the session that built it, since on this Mac it would open the maintainer's
  own library with a build that can write. **To be tried, on a COPY of a library
  first**: that typing keeps up; that Command+Z and Shift+Command+Z take back and
  put again; pasting; Find (Command+F); a dead key (Option+E, then E) in a chord and
  in a word; the sound when a key is not taken, and whether it is welcome; that
  switching songs, leaving the app and quitting each save what was typed; the two
  keys; Earlier Versions and Put This One Back, which had no copies to show until
  now; and that a song so changed shows its chords in ProPresenter.
- **Two empty lines look alike** where a blank slide is (`spec/04`, rule 3): to be
  tried with M7, when one of them can take a chord.
- **A chord the file has on an empty line of a box** is shown on that line and is
  dropped if the box is saved, as in the reference (`spec/04`, rule 16 has no
  character to hang it on). Not seen in a real song.
- **All Copies reads every copy's folder each time the settings open.** Fine for
  hundreds of songs; if it is ever slow, read it off the thread that draws.
- **What only a hand on a mouse can tell, and nobody has yet** (M3): that the app
  opens and finds the library; what macOS asks the first time it reads Documents;
  that the lists keep up with typing in the search box and with clicks; that the
  band comes and goes as ProPresenter is started and quit; that the window's size is
  remembered; that Choose Folder does what it says. The pictures show the window as
  drawn, not as used.
- **Not in M3, on purpose**: the mark "wants mending" (M8); keeping a chord being typed when another song is clicked (rule 13: there is
  no typing yet); playlists, search by a song's words, dropping a file on the window
  (the page's "later").
- **The window's looks are a first go** at the page's proposal, to be tried and
  changed with the maintainer.
- **Nothing checks that `Generated/` matches the submodule.** The workflow could
  generate again and compare, but GitHub's Mac would then need the same `protoc` and
  `protoc-gen-swift` as made it, to the version. Not worth it yet.
- **A presentation that ProPresenter itself made, of words anyone may use**, is asked
  of the maintainer before M9 by the plan. It is wanted before M2: saving is proved
  there, and every test file so far was written by Simple Presenter. A few slides of
  Amazing Grace typed into ProPresenter would do.
- **From M3 on**, macOS may ask again for leave to read Documents each time the app
  is rebuilt, since an ad hoc signature is a new one each build. **A guess**, not yet
  seen. If it does, signing with the maintainer's own Apple Development certificate
  stops it.
