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
| **M4** The copies | Next | |

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

- **What only a hand on a mouse can tell, and nobody has yet** (M3): that the app
  opens and finds the library; what macOS asks the first time it reads Documents;
  that the lists keep up with typing in the search box and with clicks; that the
  band comes and goes as ProPresenter is started and quit; that the window's size is
  remembered; that Choose Folder does what it says. The pictures show the window as
  drawn, not as used.
- **Not in M3, on purpose**: the marks "copies are kept" (M4) and "wants mending"
  (M8); keeping a chord being typed when another song is clicked (rule 13: there is
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
