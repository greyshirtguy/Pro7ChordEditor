# The two platforms

What is the Mac's own way and what is Windows', how each project is laid out, how an
agent sees what it has drawn, and what getting an app to other people takes.

The rule from `AGENTS.md`: the two apps behave the same and write the same files.
Where a platform has its own way of doing a thing, follow the platform. This page is
the list of those things. Anything not on it is to be the same.

## Where each follows its platform

| | Mac | Windows |
| --- | --- | --- |
| The key for shortcuts | Command, where these pages say Ctrl | Ctrl |
| Copying a chord while dragging | Option held (the Mac's key for "copy as you drag") | Ctrl held |
| Deleting | Either delete key | Delete or Backspace |
| Open on what is at the spot | Return | Enter, F2 |
| Redo | Shift+Command+Z | Ctrl+Y, Ctrl+Shift+Z |
| Menus | A menu bar with every command in it, its shortcut beside it | The same commands in the window's own menu |
| Settings | A Settings window (Command+comma) | A settings page or dialog |
| The app's own data | `~/Library/Application Support/Pro7ChordEditor` | `%LOCALAPPDATA%\Pro7ChordEditor` |
| Looks | The system's controls, fonts and colours for lists, buttons and fields. Dark and light as the system is | The same, in the system's own style |

The sheet itself is drawn by the app and looks the same on both: dark, with the
colours of [03-chord-sheet.md](03-chord-sheet.md). (Whether it should have a light
form for a light system is the maintainer's to say once it exists.)

## Counting characters

A place in a song's words is a number of **UTF-16 units**
([01-file-format.md](01-file-format.md)).

- **C#**: `string` is UTF-16. An index is a place. Nothing to do.
- **Swift**: `String` counts in whole characters, so that an emoji, or a letter with a
  combining accent, is one where the file counts two or more. Never use `count`,
  `String.Index` distances or `prefix(n)` for a place. Keep a text box's words as
  `[UInt16]`, or go through `text.utf16` every time, and put that behind one small
  type so that the rest of the code cannot get it wrong. The vectors give lengths and
  units for this reason (`rtf-text.json`).

## The Mac app

- An ordinary Xcode project in `mac/`, SwiftUI, for the newest macOS and the one
  before it. Not sandboxed, not for the App Store: it reads and writes a folder in
  Documents that belongs to another app.
- **The rules as a Swift package** inside it (`mac/ChordRules`, say), with no AppKit or
  SwiftUI in it, tested with `swift test` from the command line. The song
  ([05-song-and-saving.md](05-song-and-saving.md)), the copies and the checks are a
  second package or target on top of it, also without a window. The app is a thin
  third thing. An agent can build and prove the first two without ever opening Xcode.
- Files are read with [swift-protobuf](https://github.com/apple/swift-protobuf), the
  Swift code generated from `proto/proto` by its plug-in at build time, or generated
  once and kept in the repository with the command that made it written beside it.
  Do not write a reader by hand.
- **The sheet is one custom-drawn view**, not a stack of SwiftUI texts and shapes: it
  has thousands of small things in it, follows the pointer at every move, and must
  know exactly where every character is. Draw it with `Canvas`, or as an `NSView`
  wrapped for SwiftUI, measuring text with Core Text so that the place under the
  pointer and the place a bubble is drawn come from the same numbers. The typing
  bubble is a real text field laid over it. The ChordPro editor is `NSTextView`
  wrapped: SwiftUI's own text editor cannot colour as it is typed in or refuse a
  change.
- Keys: the sheet must take the keyboard without a text field in it. That is fiddly
  in SwiftUI; an `NSView` that accepts first responder is the sure way.
- macOS will ask the person, the first time, whether the app may read Documents. Say
  why in `Info.plist` (`NSDocumentsFolderUsageDescription`): "to open the songs in
  your ProPresenter library".
- The maintainer has said a private API is not ruled out if it is steady and buys
  something large. Nothing here needs one. Ask before reaching for one.

## The Windows app

- .NET, WPF, in `windows/`. The newest .NET with long support, as a self-contained
  single-file build so that nothing has to be installed first.
- WPF is kept because the app is already WPF, it is still looked after, and drawing
  and measuring text for a view like the sheet is solid in it. If a real reason to
  move turns up, it is the maintainer's choice; say what the reason is.
- **The old app is replaced, not mended.** All of it is one code-behind file with the
  rules in the window, and its faults are what [08-repair.md](08-repair.md) is about.
  Keep what it knew (where ProPresenter's folder is, the process to look for), which
  is written down in [06-library.md](06-library.md), and start the code afresh. Its
  generated reader of the file format is in the repository twice and is three years
  old: generate anew from `proto/proto`.
- **The rules as a class library** with no WPF in it (`windows/ChordRules`), tested
  with xUnit by `dotnet test`. The song, the copies and the checks are a second
  library on top. The app is a thin third project.
- Files are read with `Google.Protobuf`, the C# generated from `proto/proto` by
  `Grpc.Tools` at build time.
- **The sheet is one custom-drawn element**: a `FrameworkElement` that overrides
  `OnRender`, measuring with `FormattedText` or `GlyphRun`, and does its own hit
  testing. Not a `RichTextBox`: that is what the old app was, and where its trouble
  with places came from. The ChordPro editor is a plain `TextBox`, or AvalonEdit if
  colouring is wanted (a dependency: ask first).
- An `.exe` that nobody signed is stopped by Windows the first time ("Windows
  protected your PC"), and someone on the old editor's issue 11 could not get past it.
  Signing costs money each year; until the maintainer decides, the README must say
  exactly what the person will see and how to go on.

## Seeing what you have drawn

An agent cannot look at a window. It can look at a picture file. So each app has
tests that **draw into pictures**:

- **Mac**: render the view off screen (`ImageRenderer` for a SwiftUI view, or
  `NSView.bitmapImageRepForCachingDisplay` and `cacheDisplay`) at a fixed size and
  scale, and write a PNG into a folder the test names.
- **Windows**: `RenderTargetBitmap` of the element after `Measure` and `Arrange`, to
  a PNG. WPF needs a thread in single-threaded apartment for this; give the test one.

Draw at least: the sheet of `Amazing Grace.pro` as opened; with the spot on a word;
with the typing bubble open and "D/" typed; with a chord being dragged; a song with a
line of chords alone; the ChordPro editor. Then **open the PNGs and look at them**
beside `spec/pictures`. Do this after every change to how something is drawn, and say
in your report that you did and what you saw. Pictures do not go in the repository
unless the maintainer wants some for the README.

What a picture cannot tell (does the spot keep up with the pointer, does a drag feel
right, is typing quick) only a person can. Say plainly which things you have only
seen in a picture and ask the maintainer to try them.

## Getting it to other people

- **Mac**: a build for anyone else must be signed with a Developer ID and notarized
  by Apple, or macOS will refuse to open it. That needs the maintainer's paid Apple
  developer account, which he is renewing. Nothing before that step needs it: Xcode
  builds and runs an unsigned or locally signed app on his own Mac. Give it out as a
  zipped `.app` or a `.dmg` on the Releases page.
- **Windows**: a self-contained `.exe` in a `.zip` on the Releases page, as the old
  editor was.
- **Every push** should build both apps and run both sets of tests on GitHub's own
  Mac and Windows machines (`.github/workflows`). That is the referee the two apps
  share: neither can drift from the vectors without it showing. Set it up in the
  first milestone of each app.

## One thing only the maintainer can find out, on each

- **Mac**: where `PathSettings.proPaths` is, and ProPresenter's bundle identifier
  ([06-library.md](06-library.md), rules 3 and 17).
- **Windows**: what the RTF of a song made in ProPresenter for Windows looks like
  ([01-file-format.md](01-file-format.md), Not known), and whether the old editor
  really did save hand-placed chords short ([08-repair.md](08-repair.md), B).
