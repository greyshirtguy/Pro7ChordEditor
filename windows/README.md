# Pro7 Chord Editor for Windows

The Windows app, being built second of the two, to the plan in
[`spec/11-plan.md`](../spec/11-plan.md). So far it finds the songs on a PC, lists them
by library, and opens one on its **chord sheet**: every slide of the song with its
words, and each chord in a bubble over the word it is played on. **Chords are put on
there with the mouse and the keys**: point at a word and press 1 to 7 for the chords
of the song's key, or drag one of them down onto the word, or type one; drag a chord
to move it; copy a line's chords onto another line. A line under the sheet says what
the keys and the mouse do where the pointer is, and a one-page guide to all of it
opens the first time the app is started and is under Help after that. The same song
can be had as ChordPro text, each chord in square brackets where it is played, and
chords typed in there. The words cannot be changed in either.
Every change is saved as it is made, with a copy of the song's file as it was kept
first, and a song's earlier versions can be looked at and put back.

**It knows a song that the first Windows editor saved**, by what that editor left in
the file: chords written so that ProPresenter shows them twice, and sometimes the
wrong key. Such a song is marked in the list and says what was found when it is
opened, with a button that puts it right; a whole list of them can be mended at
once. And where that editor saved its copy, "Song (Chords)", beside a song, the
copy's chords can be put into the song itself.

**A new song can be made from a ChordPro file**, the plain-text form that SongSelect
and others give songs out in: the file is read and what was found in it shown, and
the song is then made to look like a song the library already has, by copying one of
that song's slides for each of the new one's. A song can be written out as a ChordPro
file too. **No song made this way has yet been opened in ProPresenter**
([`NOTES.md`](NOTES.md)).

**Version 1.0.3 is on the [Releases](https://github.com/greyshirtguy/Pro7ChordEditor/releases) page**, in the one
release that holds both apps (`v1.0.3`): a zip with the app as one file, and a read-me.
**It has been tried by hand only briefly.** Keep a backup of your library, quit ProPresenter first, and try it on a
copy of a library before your own: the read-me says how.

Under what is seen it has the rules of chords, held to the same answers as the Mac
app's, and the song itself: a file opened, its chords and its keys changed, each change
saved safely and undone.

**It reads songs that are kept as files**, as every ProPresenter 7 looked at up to
version 21 keeps them. The one copy of version 22 looked at, on Windows, keeps its
songs in a database and not as `.pro` files, and there the app finds no library; what
is to be done about that is not decided ([`NOTES.md`](NOTES.md)). A folder of `.pro`
files, wherever it is, can always be chosen by hand.

The first Windows editor, version 0.9.5, is in [`old/`](old/README.md) as it was, and
its [last release](https://github.com/greyshirtguy/Pro7ChordEditor/releases/tag/v0.9.5)
still works as it did. This app is to replace it, and finds and mends what it left in
files ([`spec/08-repair.md`](../spec/08-repair.md)).

Built with .NET 10 and WPF. So far it has been run on Windows 11 and nowhere else.

## To build it and run its tests

Two things are wanted first.

- **The .NET 10 SDK.** Visual Studio 2026 brings it with its ".NET desktop development"
  workload; or `winget install Microsoft.DotNet.SDK.10`.
- **The `proto/` folder filled in.** It holds the descriptions of ProPresenter's file
  format, from which the app's reader of those files is made at each build. It is a
  git submodule, a pointer to another repository, and an ordinary `git clone` leaves
  it empty. Either clone with it,

  ```
  git clone --recurse-submodules https://github.com/greyshirtguy/Pro7ChordEditor.git
  ```

  or fill it afterwards, in a clone already made:

  ```
  git submodule update --init
  ```

  A build with it empty stops and says so.

Then, **in this folder**:

```
cd windows
dotnet test                  # every test
dotnet run --project App     # the app
dotnet publish App           # the app as the one file it is given out as
```

`dotnet publish App` leaves `Pro7ChordEditor.exe`, alone, in
`App\bin\Release\net10.0-windows\win-x64\publish`. It needs nothing installed to run.
The first time a newly built one is started it may be some seconds before its window
shows; after that, one.

The app keeps its own things (what was chosen, a log of what it did, the copies of
songs' files) in `%LOCALAPPDATA%\Pro7ChordEditor`, and nothing anywhere else.

To see the window without starting the app, some tests draw it into pictures (in
PowerShell):

```
$env:PRO7_PICTURES = "C:\some\folder"
dotnet test --project Tests/AppViews.Tests --filter-class "*PictureTests"
```

Run from anywhere else, `dotnet test` stops with "Testing with VSTest target is no
longer supported": `global.json` in this folder tells it how these tests are run, and
it is only found from here or below.

In Visual Studio: open `Pro7ChordEditor.slnx`, and make `App` the startup project.

Building from clean, `protoc` warns some two dozen times that an import is unused. That
is nothing ([`ProFormat/ProFormat.csproj`](ProFormat/ProFormat.csproj) says why).

## What is where

| | |
| --- | --- |
| `ChordRules/` | The rules of chords, and the words of an RTF. Plain .NET, with no file and no window in it |
| `ProFormat/` | The reader and writer of ProPresenter's files, made at each build from `proto/autogen-proto`. One file of it is written by hand |
| `Song/` | The song as the editors see it; the library: where songs are, the lists, the marks, the app's own settings; the copies kept before a change; the checks for what the first editor left in a file, with the mending of it; and ChordPro files, read, written, and made into a new song. No window in it |
| `AppViews/` | The window: what it shows and what a click does. A library, so that the tests can draw it into pictures |
| `App/` | The app itself: the few lines that start it, and how it is built to be given out |
| `Tests/` | The tests of each, held to [`testdata/`](../testdata/README.md) |
| `Pro7ChordEditor.slnx` | All of the above as one, for `dotnet` and Visual Studio |
| `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `global.json` | What all the projects have in common; every package they take, and its version; rules for the code, six of which stop a build; and how tests are run. Each says at its top what it is |
| `make-zip.ps1`, `zip/` | What makes the zip that is given out, and the read-me that goes in it. It makes a file and does nothing with it |
| [`NOTES.md`](NOTES.md) | What was decided and found out on the way, and what is left rough |
| [`old/`](old/README.md) | The first editor, as it was |

If you are an AI coding agent: [`AGENTS.md`](../AGENTS.md) first, then the plan, then
[`mac/NOTES.md`](../mac/NOTES.md) (the Mac app was built first, and what it found out
is there), then [`NOTES.md`](NOTES.md).
