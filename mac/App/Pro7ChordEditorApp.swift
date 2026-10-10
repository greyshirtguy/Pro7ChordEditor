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
// So far (milestone M4) the window finds ProPresenter's libraries, lists their songs and
// shows one to be looked at; and the copies the app keeps of a file before changing it
// can be seen, in the song's Earlier Versions and in the settings, and one put back.
// There is no way yet to change a chord.
//
// One window and not a new one at every Command+N: there is one library to look at.
// Whether a song should open in a window of its own is a question for when there is a
// song to change.

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
        .defaultSize(width: 1100, height: 680)

        // Command+comma: the copies kept, all of them (spec/07-backups.md, rule 18).
        Settings {
            AllCopiesView(model: copies)
        }
    }
}
