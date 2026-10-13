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
// So far (milestone M7, a chord dragged out of the strip, and the guide) the window finds
// ProPresenter's libraries, lists their songs, and shows one as a sheet of its slides,
// on which chords are put, moved and taken off with the mouse and the keys, or as
// ChordPro text in which they are typed. Each change is saved as it is made, with a
// copy of the file as it was kept first. The copies can be seen, in the song's Earlier
// Versions and in the settings, and one put back. A guide to all of it opens once, and
// is under Help. mac/NOTES.md says where things stand.
//
// One window and not a new one at every Command+N: there is one library to look at.
// Closing it quits the app, as it does for any app of one window: there is nothing to
// do without it. And the menu bar is kept to what the app has a use for (AppMenus, in
// the package).

import AppViews
import SwiftUI

@main
struct Pro7ChordEditorApp: App {
    @State private var model = LibraryModel.onThisMac()
    @State private var copies = AllCopiesModel.onThisMac()

    init() {
        // Before any menu is made: what the system is not to put in them, and the
        // tidying of what it puts in all the same.
        AppMenus.atStart()
        AppMenus.watch()
    }

    var body: some Scene {
        // (Its title is the app's name and its version: AppTitle, in the package.)
        Window(AppTitle.ofThisApp, id: "main") {
            // (What was being typed is saved as the app goes: the window sees to
            // that when it is told the app is quitting.)
            LibraryWindow(model: model, whenClosed: { DispatchQueue.main.async { NSApp.terminate(nil) } })
        }
        .defaultSize(width: 1280, height: 800)
        .commands {
            // About, with the app's icon; and File > Revert, as in the window
            // (spec/06-library.md, rule 12a). No Save: nothing is saved by hand.
            AppCommands(model: model)
            // Help: the guide (spec/13-guide.md).
            GuideCommands()
        }

        // The guide: one page on how chords are put on, in a window of its own, so
        // that the app can be used while it is read. The app opens it once, at its
        // first start on this Mac; after that it is under Help and behind the ? at
        // the right of the sheet's strip. (One that was open when the app was quit is
        // not opened again by the system at the next start: it opens when asked for.)
        Window(GuideWindow.title, id: GuideWindow.id) {
            GuideWindow(model: model)
        }
        .defaultSize(width: 780, height: 820)
        // (At a side of the screen, so that less of the main window is under it.)
        .defaultPosition(.topTrailing)
        .restorationBehavior(.disabled)

        // Command+comma: the settings, in sections. The chords' colours, and the copies
        // kept, all of them (spec/06-library.md, "Settings"; spec/07-backups.md, rule 18).
        Settings {
            SettingsWindow(model: model, copies: copies)
        }
    }
}
