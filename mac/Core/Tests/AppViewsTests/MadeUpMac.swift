// What the tests of the window stand on: a made-up Mac, in a folder of its own under
// the system's temporary folder. It has a place laid out as ProPresenter lays one out,
// with copies of the test files for songs; a folder for the app's own things; and a
// ProPresenter that is running or not as the test says.
//
// No test of the window asks this Mac's ProPresenter anything, reads a real library, or
// writes outside its own folder (AGENTS.md, "The person's files").

import AppViews
import Foundation
import Song
import TestData

@MainActor
final class MadeUpMac {
    nonisolated static let prefix = "Pro7ChordEditorViewTests-"

    let folder: URL
    let place: URL
    var proPresenterIsRunning = false
    /// Whether ProPresenter's settings list the place as a workspace. If not, there is
    /// no place to be found at all.
    var hasAWorkspace = true

    init() throws {
        folder = FileManager.default.temporaryDirectory.appendingPathComponent(Self.prefix + UUID().uuidString, isDirectory: true)
        place = folder.appendingPathComponent("Sunday", isDirectory: true)
        let songs = place.appendingPathComponent("Libraries/Songs", isDirectory: true)
        let copies = place.appendingPathComponent("Libraries/Old copies", isDirectory: true)
        let kids = place.appendingPathComponent("Libraries/Kids", isDirectory: true)
        for library in [songs, copies, kids] {
            try FileManager.default.createDirectory(at: library, withIntermediateDirectories: true)
        }
        for path in try TestData.presentationPaths() {
            let name = String(path.split(separator: "/").last!)
            try TestData.file(path).write(to: (path.hasPrefix("as-the-old") ? copies : songs).appendingPathComponent(name))
        }
        try Data("This is not a song.".utf8).write(to: songs.appendingPathComponent("Nonsense.pro"))
    }

    deinit {
        // Only ever the folder made above, and only if it is named as one of these.
        if folder.lastPathComponent.hasPrefix(Self.prefix) {
            try? FileManager.default.removeItem(at: folder)
        }
    }

    var appData: AppData { AppData(folder: folder.appendingPathComponent("Own", isDirectory: true)) }

    func model() -> LibraryModel {
        LibraryModel(
            appData: appData,
            finder: { [unowned self] chosen in
                let listed: [[String: Any]] = hasAWorkspace ? [["name": "Sunday", "url": place.absoluteString, "isActive": true]] : []
                return PlaceFinder(proPresentersSetting: { $0 == "userWorkspaces" ? listed : nil }, documents: nil, chosen: chosen)
            },
            proPresenterIsRunning: { [unowned self] in proPresenterIsRunning })
    }

    func library(_ name: String) -> Library.ID {
        Library(name: name, folder: place.appendingPathComponent("Libraries/\(name)", isDirectory: true)).id
    }

    func song(_ name: String, in library: String = "Songs") -> ListedSong.ID {
        ListedSong(name: name, url: place.appendingPathComponent("Libraries/\(library)/\(name).pro"), size: 0, modified: .distantPast).id
    }
}
