// The Mac app of Pro7 Chord Editor: where it starts.
//
// The app is the thin thing on top (spec/10-platforms.md). What a click does to a song
// is worked out in the package beside this folder (../Core: ChordRules, ProFormat,
// Song), in plain code that the tests call with no window. What is here only shows
// what that code says and passes on what the person does. Keep it so: a rule that
// finds its way into a view cannot be proved by `swift test`, and will be built a
// second time, differently, on Windows.
//
// So far (milestone M0) there is one window with nothing to do in it. The library is
// M3, the editors M5 and M6 (spec/11-plan.md).
//
// One window and not a new one at every Command+N: there is one library to look at.
// Whether a song should open in a window of its own is a question for when there is a
// song to open.

import SwiftUI

@main
struct Pro7ChordEditorApp: App {
    var body: some Scene {
        Window("Pro7 Chord Editor", id: "main") {
            NothingYetView()
        }
    }
}
