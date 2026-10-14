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
key ProPresenter shows it in. Sharps and flats are drawn as ♯ and ♭ on the sheet (a
setting turns that off); the song is saved with `#` and `b` either way. Nothing is saved while ProPresenter is running. A guide
to all of it, one page with pictures of the app's own sheet, opens the first time the
app is started, and is under Help ever after.

Not yet: mending songs the old Windows editor damaged; ChordPro files.

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

Since 0.3 a release is one for both apps, `v` and the number (`v1.0.0`), with the
Mac's image and Windows' zip as its two files, and the release GitHub calls "Latest"
always has both. If the Windows app has made the release of a number already, as a
pre-release, the image is added to it, the tag is moved to the commit the image was
built from, and the release is then made no pre-release and "Latest". If not, the
release is made with the image, as a pre-release, and its text says that Windows' is
to come. Either way the Mac's part of "Try it" in
[the README at the top](../README.md) is brought up to date in the same push: the
link, the version and the size ([`spec/10-platforms.md`](../spec/10-platforms.md),
"Getting it to other people", which has the steps). A release is made, changed or
taken down on the maintainer's word and not otherwise.

A build can be asked to write out its menus as it starts, for checking one with no
hand on a mouse: start the program inside the app with `PRO7_SAY_MENUS` set, and it
prints them (`AppMenus.swift`).

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

Two things the tests do when they are asked, and not otherwise:

```
PRO7_PICTURES=/some/folder swift test --package-path mac/Core    # the window drawn into pictures there, to be looked at
PRO7_SPEED=1 swift test --package-path mac/Core --filter SpeedTests    # how long the window keeps the main thread
```

The second times the window on a long song: the pointer moved across the sheet, a
chord dragged, and the edge between the lists and the song moved. It fails for
nothing and says what it found, to be set beside the numbers in
[`NOTES.md`](NOTES.md), "What made the window slow". Run it before and after any
change to how the window is laid out or drawn. (`PRO7_SPEED=2` is the same as a
screen of two dots to the point would have it.)

## What is where

| | |
| --- | --- |
| `Core/` | A Swift package: the rules of chords (`ChordRules`), the reader of ProPresenter's files (`ProFormat`, generated), the song and the library (`Song`), and the window (`AppViews`) |
| `Core/generate-proto.sh` | Makes `ProFormat` again from the `proto/` submodule. Needs `brew install protobuf swift-protobuf` |
| `Core/Sources/AppViews` | The window: what it shows and what a click does. A library too, so that the tests can draw it into pictures |
| `Core/Sources/AppViews/Guide` | The guide's words, a copy of [`spec/guide.json`](../spec/guide.json), and its pictures, which a test draws and holds to what the app draws now ([`spec/13-guide.md`](../spec/13-guide.md)) |
| `App/` | The app itself: the few lines that start it, what it says of itself to macOS, and its icon (`ChordEditor.icns`, the maintainer's own picture) |
| `Pro7ChordEditor.xcodeproj` | Builds the app |
| `make-dmg.sh`, `dmg/` | Makes the disk image the app is given out in, and the read-me that goes in it |
| [`NOTES.md`](NOTES.md) | What was decided and found out on the way, and what is left rough |

If you are an AI coding agent: [`AGENTS.md`](../AGENTS.md) first, then the plan, then
[`NOTES.md`](NOTES.md).
