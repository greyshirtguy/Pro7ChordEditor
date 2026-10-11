# Notes on the Windows app

For the next session, which will remember nothing of the ones before: what was decided
on the way and why, what was found out on Windows, and what is left rough. Read it
after `AGENTS.md`, `spec/11-plan.md` and `mac/NOTES.md`: the Mac app was built first,
and most of what it found out is true here too. Add to it at the end of every
milestone; put right what has become untrue.

## Where things stand

| | | |
| --- | --- | --- |
| **M0** Setting up | Built, 2026-10-12. **Not done until the workflow has run on GitHub's machine**, which wants a push, and nothing has been pushed | The four layers and the app, empty; the reader of a `.pro` made at each build; forty-nine tests; one picture; the app published as one file, and started; the workflow, written and never run. The first editor moved to `old/` |
| **M1** The rules | Next | Every case of every vector file but `sheet-model.json` and `layout.json`. `mac/NOTES.md`, "The rules (M1)" and "At M1", says what the Mac port had to pin down; `spec/02`, "To the letter", has it for both apps |

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
11. **The commits are on `master`**, as the Mac's are, and are not pushed.

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

## Left rough, and what to ask

- **The workflow has never run.** It is written from the Mac's and from what worked
  here, and GitHub's machine may differ: it has no screen, and whether a view draws
  into a picture there is not known until it is tried. Ask for a push; then read the
  run (`gh run list`), and put right what it shows.
- **In Visual Studio, only the listing of the tests has been seen.** The maintainer
  opened the solution in Visual Studio 2026 and its Test Explorer listed the
  forty-nine (2026-10-12), without the two packages named at the foot of
  `Directory.Packages.props`. Opening it changed no file of the repository. That the
  tests pass when run from there, and that F5 starts the app (`App` as the startup
  project), nobody has said. And Visual Studio may one day write the solution file
  again without its comment.
- **The six rules about text have been tried on a scratch file only.** M1 is where
  they meet real code. If one proves wrong for a case, say why where it is switched
  off, and not for the whole project.
- **Pictures are of a view that only has to be laid out.** A view that waits for
  something (a list filled a moment later, a text box that wants the keyboard) will
  need the thread's queue of work run before it is drawn. `Pictures.cs` does not do
  that yet: M3's to add.
- **The app has no version number, no icon, and says nothing to Windows of how it
  wants to be drawn on a fine screen.** The first is wanted at M4 (a copy's index
  records it); the rest at M3 and M10.
- **`old/publish.py` must not be run**: it commits, pushes and tags. Its README says
  so now.
- **Asked of the maintainer before M3** (`spec/11`): a look at the RTF of a song made
  in ProPresenter on Windows. `spec/01`, "Not known", makes looking the first job of
  this session, and the Mac's M7 leans on the answer (`mac/NOTES.md`, "RTF as
  ProPresenter on Windows writes it is still not known"): if Windows writes a
  character to stand in after each numbered one, a line of chords alone written there
  cannot yet be given a chord more or fewer. `tools/look.py` is the tool, and works
  here now ("What was found out"). Reading the maintainer's own library is theirs to
  allow, and where it is, theirs to say.
- **Two lines of `reference/README.md` are no longer true**, under "Where this and
  `spec/` differ": that the apps leave out a slide with no text box, and that they
  leave a blank slide's box empty when its chords are taken off again. The
  maintainer decided otherwise on 2026-10-11 and the Mac's M7 built both. The first
  also names a heading `spec/01` no longer has. Told to the maintainer; it is the
  Mac session's or the Linux one's to put right, not this one's.
