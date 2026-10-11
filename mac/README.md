# Pro7 Chord Editor for the Mac

The Mac app, being built first of the two, to the plan in
[`spec/11-plan.md`](../spec/11-plan.md). So far it finds ProPresenter's libraries on
the Mac, lists their songs, and shows a song in either of two ways. As a chord sheet:
all its slides on one sheet, as a grid like ProPresenter's own but of words at a size
to read, with the chords in bubbles over them, to be put on, changed, moved and taken
off with the mouse and the keys; the sheet zooms, to show more of a song at once. Or
as ChordPro text, its chords in square brackets among its words, to be typed in.
Chords can be put where there are no words: on an intro's line of chords, and on a
slide that is blank. Each change is saved as it is made, with a copy of the file as
it was kept first, which can be seen and put back. A song's key can be set, and the
key ProPresenter shows it in. Nothing is saved while ProPresenter is running.

Not yet: mending songs the old Windows editor damaged; ChordPro files; an icon.

**It changes the files of a ProPresenter library. Try it on a copy of one first**
(copy a folder of `Libraries` somewhere, and open the copy with Choose Folder…).

For macOS 15 and 26, built with Xcode 26.

## To give it to someone else

```
mac/make-dmg.sh        # builds the app as a Release and writes mac/build/Pro7ChordEditor-mac-<version>.dmg
```

The image holds the app, a way to Applications to drag it onto, and
[a read-me](dmg/Read%20Me%20First.txt). The version is the app's own, in the Xcode
project.

**It is not yet signed with a Developer ID, nor checked by Apple** (notarized): that
waits on the maintainer's developer account. So on another Mac, once the image has
come through a browser, macOS will not open the app at a double-click; the person
has to allow it once in System Settings, under Privacy & Security, and the read-me
says how. The script's head says what changes when there is a Developer ID.

## To build it and run its tests

```
swift test --package-path mac/Core        # everything that has no window in it
open mac/Pro7ChordEditor.xcodeproj        # the app: Command+R to run, Command+U for the same tests
```

Nothing has to be installed first, and the `proto/` submodule is not needed to build.

## What is where

| | |
| --- | --- |
| `Core/` | A Swift package: the rules of chords (`ChordRules`), the reader of ProPresenter's files (`ProFormat`, generated), the song and the library (`Song`), and the window (`AppViews`) |
| `Core/generate-proto.sh` | Makes `ProFormat` again from the `proto/` submodule. Needs `brew install protobuf swift-protobuf` |
| `Core/Sources/AppViews` | The window: what it shows and what a click does. A library too, so that the tests can draw it into pictures |
| `App/` | The app itself: the few lines that start it |
| `Pro7ChordEditor.xcodeproj` | Builds the app |
| `make-dmg.sh`, `dmg/` | Makes the disk image the app is given out in, and the read-me that goes in it |
| [`NOTES.md`](NOTES.md) | What was decided and found out on the way, and what is left rough |

If you are an AI coding agent: [`AGENTS.md`](../AGENTS.md) first, then the plan, then
[`NOTES.md`](NOTES.md).
