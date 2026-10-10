# Pro7 Chord Editor for the Mac

The Mac app, being built first of the two, to the plan in
[`spec/11-plan.md`](../spec/11-plan.md). So far it finds ProPresenter's libraries on
the Mac, lists their songs, and shows a song with its chords to be looked at. Under
that, and with no window yet, are the rules of chords and the song itself: setting its
chords and its key, saving safely, undo. The copies it keeps of a file before changing
it can be seen and put back. **No chord can be changed from the window yet**, and
there is nothing to download.

For macOS 15 and 26, built with Xcode 26.

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
| [`NOTES.md`](NOTES.md) | What was decided and found out on the way, and what is left rough |

If you are an AI coding agent: [`AGENTS.md`](../AGENTS.md) first, then the plan, then
[`NOTES.md`](NOTES.md).
