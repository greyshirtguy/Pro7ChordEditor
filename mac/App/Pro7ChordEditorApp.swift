// The Mac app of Pro7 Chord Editor: where it starts.
//
// The app is the thin thing on top (spec/10-platforms.md). Everything it shows and does
// is in the package beside this folder (../Core): the rules of chords, the song, and the
// window itself, which is the library AppViews so that `swift test` can draw it into
// pictures. What is here is only what an app must have and a package cannot: the place
// it starts, and (in Info.plist) what it says of itself to macOS.
//
// Keep it so. A rule that finds its way into this folder cannot be proved by
// `swift test`, and will be built a second time, differently, on Windows.
//
// So far (milestone M5) the window finds ProPresenter's libraries, lists their songs,
// and shows one as ChordPro text in which chords can be typed, each change saved a
// moment after it is typed, with a copy of the file as it was kept first. The copies can
// be seen, in the song's Earlier Versions and in the settings, and one put back.
//
// One window and not a new one at every Command+N: there is one library to look at.

import AppViews
import SwiftUI

@main
struct Pro7ChordEditorApp: App {
    @State private var model = LibraryModel.onThisMac()
    @State private var copies = AllCopiesModel.onThisMac()

    var body: some Scene {
        Window("Pro7 Chord Editor", id: "main") {
            LibraryWindow(model: model)
        }
        .defaultSize(width: 1280, height: 800)
        .commands {
            // File > Save, for the hand that presses Command+S out of habit: nothing is
            // saved by hand here, and the window says so. And Revert, beside it, as in
            // the window (spec/06-library.md, rule 12a).
            CommandGroup(replacing: .saveItem) {
                Button("Save") { model.saveNow() }
                    .keyboardShortcut("s", modifiers: .command)
                Button("Revert to as Opened") { model.revert() }
                    .disabled(!model.canRevert)
            }
        }

        // Command+comma: the settings, in sections. The chords' colours, and the copies
        // kept, all of them (spec/06-library.md, "Settings"; spec/07-backups.md, rule 18).
        Settings {
            SettingsWindow(model: model, copies: copies)
        }
    }
}
