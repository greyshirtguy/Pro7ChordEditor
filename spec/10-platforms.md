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
| One of the key's chords carried from the strip ([03](03-chord-sheet.md), rule 46b) | The system's drag-and-drop: the button's picture goes with the pointer, with the system's green plus beside the pointer wherever letting go would put the chord on, and slides back to the strip when it is let go anywhere else | The system's drag-and-drop, with its own pointers for "this can be put here" and "it cannot" |
| Zooming the sheet, and the size of the ChordPro text | Command with minus, plus (or equals) and 0; for the sheet, a pinch on the trackpad too | Ctrl with the same keys; Ctrl with the wheel, if the platform's own views do that |
| Showing the sheet, and the ChordPro text | Command+1, Command+2 | Ctrl+1, Ctrl+2 |
| The key that would save | Command+S. It is in no menu ([06](06-library.md), rule 12a) | Ctrl+S |
| The names of the keys in the line of hints ([03](03-chord-sheet.md), rule 53e), and in what a copy says | Their signs: ⌫ for Delete, ↩ for Return, ⇥ for Tab, ⌥ for Option, ⌘C, ⌘V. The key that brings up the copying is ⌘, and no other. What is said of dragging a chord names the key that copies in a word: "Hold Option to drag a copy" | In words: Delete, Enter, Tab, Ctrl+C, Ctrl+V. The key that brings up the copying is Ctrl. "Hold Ctrl to drag a copy" |
| The edge between the lists and the song ([06](06-library.md), rule 6a) | The app's own, and not the system's split view, which starts the lists at their widest whatever it is asked, and changes their width with the window's. A bar eight points wide, which the system is told is a place where the pointer is the one for dragging sideways | Whatever of the platform's starts where it is told to and keeps the lists' width |
| Deleting | Either delete key | Delete or Backspace |
| Open on what is at the spot | Return | Enter, F2 |
| Redo | Shift+Command+Z | Ctrl+Y, Ctrl+Shift+Z |
| Menus | A menu bar kept to what the app has a use for. About, which shows the app's icon. File: Close (Command+W), and Revert. No View menu, there being nothing to put in it. Edit: Undo, Redo, Cut, Copy, Paste and Select All, and nothing else: not Delete, which would act on a spot the pointer has left by the time it is in the menu, and not what macOS adds to every Edit menu (Writing Tools, AutoFill, Start Dictation, Emoji & Symbols). No Save. Help: the guide. (The maintainer, 2026-10-13: "lets clean up the menus".) The keys of the sheet are in no menu: the line of hints says them | The same commands in the window's own menu |
| Closing the window | Quits the app, as for any Mac app of one window. What was being typed is saved first. (The maintainer, 2026-10-13.) The guide's window is closed without that | Ends the program, as it always has |
| The guide ([13](13-guide.md)), asked for from the keys | Command+?, which is the Mac's key for help. It is Help > Pro7 Chord Editor Guide, where macOS would otherwise say the app has no help | F1, which is Windows'. The same under Help in the window's own menu |
| The guide's pictures, and its names for the keys | Of the Mac app's sheet, with the Mac's arrow drawn in for the pointer. Command, Option, Return | Of the Windows app's sheet, with Windows' arrow. Ctrl, Enter |
| Settings | A Settings window (Command+comma) | A settings page or dialog |
| The app's icon | `mac/App/ChordEditor.icns`, in the app as it is and named as its icon in the app's `Info.plist` | `windows/App/ChordEditor.ico`: the program's own icon, and its window's |
| The app's own data | `~/Library/Application Support/Pro7ChordEditor` | `%LOCALAPPDATA%\Pro7ChordEditor` |
| Looks | The system's controls, fonts and colours for lists, buttons and fields. Dark and light as the system is | The same, in the system's own style |

The icon is one picture on both, the maintainer's own (2026-10-13), each app having
it in its platform's kind of file, as it was given. `pictures/ChordEditor.png` at the
top of the repository is the same picture, for the README. Neither app makes the icon
from anything: a new one is three new files.

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
  (As built: the window itself is a library of the same package too, with no `@main`
  in it, so that the command-line tests can draw it into pictures; and the app is a
  few lines that show it.)
- Files are read with [swift-protobuf](https://github.com/apple/swift-protobuf), the
  Swift code generated from `proto/autogen-proto` by its plug-in at build time, or
  generated once and kept in the repository with the command that made it written
  beside it. [01-file-format.md](01-file-format.md) says which files of that folder,
  and why that folder. Do not write a reader by hand.
  (As built: kept in the repository. swift-protobuf's plug-in takes only `.proto`
  files inside its own package, and these are in a submodule outside it, shared with
  the Windows app. So the Mac app builds from a clone with no submodule and no
  `protoc`, on GitHub's machine too; `mac/Core/generate-proto.sh` is the command;
  the code says which version of the descriptions it was made from; and a test
  holds it to the submodule wherever the submodule is checked out. The Windows app
  generates at every build, as below, and its clones and its workflow need the
  submodule for it. **The two ways are each platform's own** and change nothing a
  person sees: what the two apps must share is the commit of `proto/` they are made
  from, and moving it on is one change to both.)
- **The sheet is one custom-drawn view**, not a stack of SwiftUI texts and shapes: it
  has thousands of small things in it, follows the pointer at every move, and must
  know exactly where every character is. Draw it with `Canvas`, or as an `NSView`
  wrapped for SwiftUI, measuring text with Core Text so that the place under the
  pointer and the place a bubble is drawn come from the same numbers. The typing
  bubble is a real text field laid over it. The ChordPro editor is `NSTextView`
  wrapped: SwiftUI's own text editor cannot colour as it is typed in or refuse a
  change.
- Keys: the sheet must take the keyboard without a text field in it. That is fiddly
  in SwiftUI; an `NSView` that accepts first responder is the sure way. (As built: it
  is, and it answers Command with C, V and Z in `performKeyEquivalent`, which a
  window's views are asked before its menus are.)
- **Found, measuring with Core Text**: for a zero-width space that ends a line,
  `CTLineGetOffsetForStringIndex` says nought, the line's left edge. Real songs have
  such lines. A chord there would be drawn at the start of its line, and the search
  for the character under the pointer, which counts on places never going backwards,
  goes wrong for the letters before it. So the places of a line are asked for once,
  and one that is left of an earlier one is put where the next believable one is.
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
  generated reader of the file format is in the repository twice and was made from
  descriptions older than any now in the submodule: generate anew from
  `proto/autogen-proto`. The new code is in another namespace than the old one's
  `Rv.Data` ([01-file-format.md](01-file-format.md)).
- **The rules as a class library** with no WPF in it (`windows/ChordRules`), tested
  with xUnit by `dotnet test`. The song, the copies and the checks are a second
  library on top. The app is a thin third project.
- Files are read with `Google.Protobuf`, the C# generated from `proto/autogen-proto`
  by `Grpc.Tools` at build time.
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
  scale, and write a PNG into a folder the test names. (**Found**: the second, on an
  `NSHostingView` in a window that is never shown, draws the system's lists, fields
  and buttons from a command-line test. It does not draw the see-through material of
  a sidebar, which comes out blank, so a pane is given a plain ground. `ImageRenderer`
  does not draw the system's lists at all.)
- **Windows**: `RenderTargetBitmap` of the element after `Measure` and `Arrange`, to
  a PNG. WPF needs a thread in single-threaded apartment for this; give the test one.

Draw at least: the sheet of `Amazing Grace.pro` as opened; in a wide window, and
there with the words small and with them large; with the spot on a word;
with the typing bubble open and "D/" typed; with a chord being dragged; a song with a
line of chords alone; the ChordPro editor. Then **open the PNGs and look at them**
beside `spec/pictures`. Do this after every change to how something is drawn, and say
in your report that you did and what you saw. Pictures do not go in the repository
unless the maintainer wants some for the README; **but for the guide's**, which are
part of the app, are kept in its folder, and are held by a test to what the app draws
([13-guide.md](13-guide.md), rules 13 to 15).

What a picture cannot tell (does the spot keep up with the pointer, does a drag feel
right, is typing quick) only a person can. Say plainly which things you have only
seen in a picture and ask the maintainer to try them.

## Getting it to other people

- **Mac**: a build for anyone else must be signed with a Developer ID and notarized
  by Apple, or macOS will refuse to open it. That needs the maintainer's paid Apple
  developer account, which is being renewed. Nothing before that step needs it: Xcode
  builds and runs an unsigned or locally signed app on their own Mac. Give it out as a
  zipped `.app` or a `.dmg` on the Releases page.
  (As built, 2026-10-12, for an early build the maintainer asked for before the
  account was renewed: `mac/make-dmg.sh` makes the `.dmg`, of a Release for both
  kinds of Mac, signed only "to run locally". "Refuse" is then not quite the last
  word: macOS will not open such an app at a double-click once it has come through a
  browser, and the person can allow it, once, in System Settings under Privacy &
  Security. The read-me in the image says how, and that it is to be done only for
  something whose maker one has reason to trust. Such a build is put on the Releases
  page as a pre-release, so that the page's "latest" stays the one most people came
  for.)
- **Windows**: a self-contained `.exe` in a `.zip` on the Releases page, as the old
  editor was.
- **The README at the top of the repository says where each app is downloaded**, in
  the first thing a visitor reads after what the app is: a section, "Try it", with a
  part headed for the Mac and a part headed for Windows. Each has a link that fetches
  the newest file of that app straight away and, under it, a picture of that app
  (`pictures/screenshot-mac.png`, `pictures/screenshot-windows.png`), as wide as the
  page. GitHub's own "Latest" can name only one release, and while the new
  apps are given out as pre-releases it names the old Windows editor; so the README,
  not that label, is the way in. **Whoever publishes a release of an app brings that
  app's part of "Try it" up to date in the same push**: the link, the version, the
  size. (The maintainer, 2026-10-12: "an obvious answer to someone who wants to try
  it out: links to both releases". And on 2026-10-14, of the two pictures, which
  stood side by side under the page's first sentence, over a table of two rows:
  "not working well visually"; "move the screenshot down in the readme to the try it
  section", "no longer side by side - but in a Mac and Windows section".)
- **One release holds both apps, from 0.3 on.** A release is one tag, `v` and the
  number (`v0.3`), with two files in it, each named for its app and that number:
  `Pro7ChordEditor-mac-0.3.dmg` and `Pro7ChordEditor-windows-0.3.zip`. The number
  has three parts from 1.0.0 on, and is above the first editor's 0.9.5, so that the
  newest is not also the lowest. So the two apps go by one number
  ([06-library.md](06-library.md), rule 6d), and a number is used up once either app
  has been given out under it. (The maintainer, 2026-10-13, of the Windows build: it
  "will get added to the new release". Until then each app had releases of its own
  and a number of its own: `mac-v0.2`, `windows-v0.2`.)
- **What a visitor is to find is one newest release with both files in it**: the
  release GitHub calls "Latest" always has the Mac's image and Windows' zip. (The
  maintainer, 2026-10-14: "I want the user experience to see a single latest release
  with both builds attached"; and 2026-10-13: "release to 1.0.0 (so these new
  versions become latest release in the repo)".) The two apps are worked on in
  sessions of their own, and neither waits for the other ("I'll be making updates on
  separate sessions mac/windows - bring in the new builds after one has made the new
  tag/release"). **So a new number goes out in two halves**, each on the
  maintainer's word to the session that does it:

  1. **The app that comes first to the number** is built under it, pushed, and both
     workflows seen to pass. The tag goes on the commit its file was built from, and
     the release is made with that one file **as a pre-release, and not as
     "Latest"** (`gh release create v1.0.4 <file> --prerelease --latest=false
     --verify-tag`). Its text says that the other app's is to come. "Latest" stays
     where it was, on the release before, which has both files.
  2. **The app that comes second** is built under the same number, whether or not
     anything of its own has changed since its last: the number is used up, and the
     release is not whole without it. It is pushed, and both workflows seen to
     pass. Then, in this order: its file is added (`gh release upload v1.0.4
     <file>`); its part is put into the text, the other app's being left as it came
     but for the words that said this one was to come; **the tag is moved** to the
     commit its file was built from (below); and, both files being there, **the
     release is made no pre-release and "Latest"** (`gh release edit v1.0.4
     --prerelease=false --latest`).

  In either half the file is fetched back from GitHub and held against the one that
  was sent, and the app's own part of "Try it" is brought up to date, with the
  sentence over the two parts if it no longer holds. (A file in a pre-release is
  fetched by its link like any other, so each part of "Try it" gives the newest of
  its app whichever half has been done.) **A session that finds a pre-release with
  the other app's file alone in it** has found a number that waits for its own app,
  and says so to the maintainer when it starts.

  **The tag is moved each time**, to the commit the second file was built from, so
  that the source at the tag is what both files were built from. (The maintainer,
  2026-10-14, of 1.0.3, whose tag was on the Mac's commit when the Windows file was
  added from a later one: "move the tag to make the most sense"; and of every time
  after: "yes move the tag each time".) Look first that nothing the first app is
  built from differs between the two commits (`git diff --stat <first> <second>`,
  of that app's folder and of `proto`; its `NOTES.md` and its `README.md` are not
  what it is built from). If something does, the tag would name a source the first file was
  not built from: leave it where it is and tell the maintainer. It is moved by one
  forced push (`git tag -f v1.0.4 <commit>`, then `git push --force origin
  refs/tags/v1.0.4`) and never by deleting it first: the release is held by the
  tag's name, and stayed as it was, published and with its files, when 1.0.3's was
  moved so. **A clone that had the tag where it was keeps it there**, an ordinary
  fetch leaving a tag it already has alone: so a session starts with `git fetch
  --tags --force`, and never pushes its tags all together.

  (Until 2026-10-14 the first half was made "Latest" at once, so that 1.0.0, and
  1.0.3 for a day, were GitHub's newest with the Mac's file alone. The maintainer,
  of making it a pre-release until the second file is in: "use the pre-release
  idea".)
- **The read-me in each download still opens "An early build"**, at 1.0 too: the
  Mac's (`mac/dmg/Read Me First.txt`) and Windows' (`windows/zip/Read Me First.txt`).
  It is left so until the maintainer says otherwise, and is not asked again at each
  release. (The maintainer, 2026-10-14, asked whether a 1.0 is still to say that of
  itself: "I'll leave 1.0 as "an early build" (not best practice - but a choice I'll
  keep until I've used it for a while and fixe a few bugs)".)
- **Every push** should build both apps and run both sets of tests on GitHub's own
  Mac and Windows machines (`.github/workflows`). That is the referee the two apps
  share: neither can drift from the vectors without it showing. Set it up in the
  first milestone of each app.

## One thing only the maintainer can find out, on each

- **Mac**: nothing now. How ProPresenter 21 records its workspaces was found by
  reading its settings on the maintainer's Mac ([06-library.md](06-library.md),
  rule 3), and its bundle identifier is rule 17 of the same page.
- **Windows**: what the RTF of a song made in ProPresenter for Windows looks like
  ([01-file-format.md](01-file-format.md), Not known), and whether the old editor
  really did save hand-placed chords short ([08-repair.md](08-repair.md), B).
