# Pro7 Chord Editor for the Mac

The Mac app, being built first of the two, to the plan in
[`spec/11-plan.md`](../spec/11-plan.md). So far it is set up and empty: it builds,
opens one window that says there is nothing to open yet, and has tests that read the
shared test data. There is nothing to download.

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
| `Core/` | A Swift package: the rules of chords (`ChordRules`), the reader of ProPresenter's files (`ProFormat`, generated), and the song (`Song`). No window in any of it |
| `Core/generate-proto.sh` | Makes `ProFormat` again from the `proto/` submodule. Needs `brew install protobuf swift-protobuf` |
| `App/` | The app itself, SwiftUI, thin |
| `Pro7ChordEditor.xcodeproj` | Builds the app |
| [`NOTES.md`](NOTES.md) | What was decided and found out on the way, and what is left rough |

If you are an AI coding agent: [`AGENTS.md`](../AGENTS.md) first, then the plan, then
[`NOTES.md`](NOTES.md).
