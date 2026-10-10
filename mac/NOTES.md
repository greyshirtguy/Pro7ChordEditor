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
| **M2** The song | Next | |

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
    the best source of what is right. `spec/01` and `spec/02` say so now. **The code
    does not yet**: `Chords.ranges` still answers as `ranges.json` has it, because a
    vector is not edited and that one has to be made again from a changed Simple
    Presenter. See "Left rough".
11. **Blank slides are to be shown, and to take chords.** Simple Presenter leaves a
    blank text box out, so an intro made as an empty slide can be given none. The
    design is in `spec/01` ("A blank slide", "Chords onto a blank slide"), with what
    it asks of the sheet, the ChordPro editor and the song in `spec/03`, `04` and
    `05`. Showing them is M2's (`blank`, in what the song hands over); giving them
    chords is M7's.

## How it is laid out, and why

```
mac/
  Pro7ChordEditor.xcodeproj   the app target, and nothing else
  App/                        the app: SwiftUI, thin
  Core/                       one Swift package: everything with no window in it
    Sources/ChordRules/         the rules (M1)                  no protobuf, no AppKit
    Sources/ProFormat/          the generated reader of a .pro  nothing by hand
    Sources/Song/               the song, the copies, the checks (M2 on)
    Tests/TestData/             how tests find testdata/
    Tests/ChordRulesTests/      held to testdata/vectors
    Tests/SongTests/            held to testdata/files
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
- **The descriptions are of ProPresenter 21.4; ProPresenter 22 is installed on this
  Mac.** No file tried was written by 22. When the submodule has 22's, move it on and
  generate again.
- **ProPresenter's bundle identifier is `com.renewedvision.propresenter`**, in every
  version installed here (7.9.2, 7.14.1, 18.3, 21.3, 22). Now in `spec/06`, rule 17.
- **ProPresenter's own settings** are in
  `~/Library/Application Support/RenewedVision/ProPresenter/`. Seen there, by name
  only: `Workspaces/workspace-v1.sqlite3` and a `UserWorkspaces` folder. Not opened
  yet: that is M3's.
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

### Rules the vectors do not hold

Found by breaking the rules (above). Each has a case in
`Core/Tests/ChordRulesTests/NotHeldByVectorsTests.swift`, whose expected answer was
READ from the reference's code, not got from it. The list of inputs to add to the
tools that make the vectors is in `testdata/README.md`, "To be made again, and cases
wanted", where the session that looks after Simple Presenter will find it. When the
vectors have them, the cases here that say the same can go.

## Left rough, and what to ask

- **`Chords.ranges` is to change when `ranges.json` has been made again** (decision
  10), and not before: the new rule fails the vector as it stands. The change is
  small. In `ChordsInABox.swift`, where a chord's `end` is worked out: if the chord's
  own line `isPlaceholders`, the end is `chord.at + 1`; otherwise as now. Then take
  the sentences that say "until it is made again" out of `spec/01` (Chords) and
  `spec/02` (`ranges`), and the same from here. **Nothing should write a file for
  anyone before this is done.** M2 can be built before it: its proofs are on
  `testdata/files`.
- **A type named `Song` cannot live comfortably in a module named `Song`** (Swift
  then cannot tell the two apart in code that imports it). The layer was named at M0.
  Settle at the start of M2: call the layer something else (`SongFile`?), or the type.
- **What the port of the RTF reader does not copy**, each said where it happens in
  `RTFWords.swift`: what Qt's reader of numbers makes of two characters after a
  backslash and an apostrophe that are not two hexadecimal digits. Nothing is known to
  write such a thing.
- **`mac/Core` is built and tested without Xcode; the app has not been touched at
  M1.** It still shows its one window.
- **No test draws a picture yet.** There is nothing to draw but a line of text. Where
  the views and their picture tests are to live is to be settled at M3, when there is
  a first real view. My proposal: a fourth library in `Core` for the views (SwiftUI
  and AppKit, no `@main`), so that `swift test` draws them and the Xcode target stays
  an `App` struct and an `Info.plist`. It bends "the app is a thin third thing" only
  by making it thinner. The maintainer's to say.
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
