// What the window shows, and what a click in it does, with no drawing in it: the places
// found, the libraries of the one chosen, the songs of the library chosen, the song that
// is open. spec/06-library.md, "The window".
//
// The views (LibraryWindow.swift) only show what is here and pass on what the person
// does. Everything that is a rule is in the layers below (Song: Places, Listing, Marks,
// SongText), which this calls; so that the rules are proved by `swift test` with no
// window, and are the same in the Windows app.
//
// So far (milestone M3) a song is opened only to be looked at: it is handed no
// safeguards, and so cannot be changed (Song/Safeguards.swift).
//
// It is handed what it would otherwise ask the Mac for (where the app keeps its own
// things, how to find places, whether ProPresenter is running), so that tests can hand
// it made-up ones. `onThisMac` hands it the real ones.

import ChordRules
import Foundation
import Observation
import Song

@MainActor
@Observable
public final class LibraryModel {
    /// A song as it is shown: its lines, or why it could not be read.
    public struct ShownSong: Equatable, Sendable {
        public var name: String
        /// The key its chords are written in, and the key ProPresenter shows it in; ""
        /// if it states none.
        public var key = ""
        public var shownKey = ""
        public var lines: [SongLine] = []
        public var slides = 0
        public var chords = 0
        /// If it could not be read, the sentence that says so. Then there are no lines.
        public var problem: String?
    }

    private let appData: AppData
    private let finder: ([URL]) -> PlaceFinder
    private let askWhetherProPresenterIsRunning: () -> Bool
    private var started = false
    private var keptMarks = KeptMarks()
    private var marking: Task<Void, Never>?
    private var chosenFolders: [URL] = []

    public private(set) var places: [Place] = []
    public private(set) var placeID: Place.ID?
    public private(set) var libraries: [Library] = []
    public private(set) var libraryID: Library.ID?
    public private(set) var songs: [ListedSong] = []
    public private(set) var marks: [ListedSong.ID: Marks] = [:]
    public private(set) var songID: ListedSong.ID?
    public private(set) var shown: ShownSong?
    public private(set) var proPresenterIsRunning = false

    /// What is typed in the search box, and what the list is narrowed to by mark.
    public var typed = ""
    public var filter: MarkFilter = .all

    public init(appData: AppData, finder: @escaping ([URL]) -> PlaceFinder, proPresenterIsRunning: @escaping () -> Bool) {
        self.appData = appData
        self.finder = finder
        self.askWhetherProPresenterIsRunning = proPresenterIsRunning
    }

    public static func onThisMac() -> LibraryModel {
        LibraryModel(appData: .onThisMac(), finder: { PlaceFinder.onThisMac(chosen: $0) }, proPresenterIsRunning: ProPresenterApp.isRunning)
    }

    // ---- What is shown

    public var place: Place? { places.first { $0.id == placeID } }
    public var library: Library? { libraries.first { $0.id == libraryID } }

    /// The songs of the library, narrowed by what is typed and by mark.
    public var listedSongs: [ListedSong] {
        Listing.narrowed(songs, to: typed).filter { filter.lets(marks[$0.id]) }
    }

    /// The quiet line under the song: what is there to be seen. Nothing here is a
    /// promise of what the app will do.
    public var notice: String {
        if let shown {
            if shown.problem != nil { return "Nothing was done to this file." }
            let slides = shown.slides == 1 ? "1 slide" : "\(shown.slides) slides"
            let chords = shown.chords == 1 ? "1 chord" : "\(shown.chords) chords"
            return "\(slides), \(chords). Nothing can be changed here yet."
        }
        if library != nil {
            let withChords = songs.filter { marks[$0.id]?.hasChords == true }.count
            return songs.count == 1 ? "1 song, \(withChords) with chords." : "\(songs.count) songs, \(withChords) with chords."
        }
        return ""
    }

    // ---- What the person does

    /// When the window first shows: what the app kept is read, the places are found, and
    /// the place and library last open are opened again. Asked a second time it does
    /// nothing.
    public func start() async {
        guard !started else { return }
        started = true
        let settings = appData.settings
        chosenFolders = settings.chosenFolders.map { URL(fileURLWithPath: $0, isDirectory: true) }
        keptMarks = appData.marks
        findPlaces(preferring: settings.lastPlace, library: settings.lastLibrary)
        proPresenterIsRunning = askWhetherProPresenterIsRunning()
        appData.log("Started. \(places.count) places found, \(libraries.count) libraries in the one open.")
        await marksAreWorkedOut()
    }

    /// When the app comes to the front: the lists are read again, since ProPresenter may
    /// have added songs meanwhile, and whether it is running is asked again.
    public func refresh() async {
        guard started else { return }
        let song = songID
        findPlaces(preferring: placeID, library: libraryID)
        proPresenterIsRunning = askWhetherProPresenterIsRunning()
        if let song, songs.contains(where: { $0.id == song }) {
            open(song: song)
        }
        await marksAreWorkedOut()
    }

    public func choose(place id: Place.ID?) {
        guard id != placeID else { return }
        placeID = id
        listLibraries(preferring: nil)
        remember()
    }

    public func choose(library id: Library.ID?) {
        guard id != libraryID else { return }
        libraryID = id
        listSongs()
        remember()
    }

    /// A click on a song opens it. One that cannot be read says so, and nothing is done
    /// to it.
    public func open(song id: ListedSong.ID?) {
        songID = id
        guard let song = songs.first(where: { $0.id == id }) else {
            shown = nil
            return
        }
        do {
            let opened = try OpenSong(opening: song.url)
            let boxes = opened.textBoxes
            shown = ShownSong(name: song.name, key: opened.key, shownKey: opened.shownKey, lines: SongText.lines(of: boxes),
                              slides: opened.slideCount, chords: boxes.reduce(0) { $0 + $1.chords.count })
            appData.log("Opened \(song.url.lastPathComponent) to be looked at.")
        } catch {
            shown = ShownSong(name: song.name, problem: (error as? Refusal)?.sentence ?? "\(song.url.lastPathComponent) could not be read.")
            appData.log("Could not read \(song.url.lastPathComponent).")
        }
    }

    /// A folder chosen by hand becomes a place, is remembered, and is opened.
    public func add(folder: URL) {
        if !chosenFolders.contains(where: { $0.standardizedFileURL == folder.standardizedFileURL }) {
            chosenFolders.append(folder)
        }
        findPlaces(preferring: Place(kind: .chosen, name: "", folder: folder).id, library: nil)
        remember()
        appData.log("A folder was chosen: \(folder.lastPathComponent).")
        Task { await marksAreWorkedOut() }
    }

    /// Waits until the marks of the songs listed have been worked out.
    public func marksAreWorkedOut() async {
        await marking?.value
    }

    // ---- How

    private func findPlaces(preferring wanted: Place.ID?, library: Library.ID?) {
        places = finder(chosenFolders).places()
        placeID = places.first { $0.id == wanted }?.id ?? places.first?.id
        listLibraries(preferring: library)
    }

    private func listLibraries(preferring wanted: Library.ID?) {
        libraries = place.map(Listing.libraries(of:)) ?? []
        libraryID = libraries.first { $0.id == wanted }?.id ?? libraries.first?.id
        listSongs()
    }

    private func listSongs() {
        songs = library.map(Listing.songs(of:)) ?? []
        if !songs.contains(where: { $0.id == songID }) {
            songID = nil
            shown = nil
        }
        // What was worked out before, for the files that are as they were then; the
        // rest in the background, a few at a time, so that the list is there at once.
        marks = [:]
        var unknown: [ListedSong] = []
        for song in songs {
            if let known = keptMarks.marks(for: song) {
                marks[song.id] = known
            } else {
                unknown.append(song)
            }
        }
        marking?.cancel()
        guard !unknown.isEmpty else { return }
        marking = Task { [weak self] in
            for start in stride(from: 0, to: unknown.count, by: 20) {
                let few = Array(unknown[start..<min(start + 20, unknown.count)])
                let found = await Task.detached(priority: .utility) { few.map { ($0, Marks.read($0)) } }.value
                guard let self, !Task.isCancelled else { return }
                for (song, marks) in found {
                    self.marks[song.id] = marks
                    self.keptMarks.keep(marks, for: song)
                }
            }
            guard let self, !Task.isCancelled else { return }
            self.appData.marks = self.keptMarks
        }
    }

    private func remember() {
        var settings = AppData.Settings()
        settings.chosenFolders = chosenFolders.map(\.path)
        settings.lastPlace = placeID
        settings.lastLibrary = libraryID
        appData.settings = settings
    }
}
