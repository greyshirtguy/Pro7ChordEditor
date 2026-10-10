// What the window shows, and what a click in it does, with no drawing in it: the places
// found, the libraries of the one chosen, the songs of the library chosen, the song that
// is open. spec/06-library.md, "The window".
//
// The views (LibraryWindow.swift) only show what is here and pass on what the person
// does. Everything that is a rule is in the layers below (Song: Places, Listing, Marks,
// SongText), which this calls; so that the rules are proved by `swift test` with no
// window, and are the same in the Windows app.
//
// So far (milestone M4) there is one thing here that changes a song's file: putting an
// earlier version of it back (spec/07-backups.md). For that the song is opened with its
// real safeguards: nothing is saved while ProPresenter runs, nor before a copy is kept.
// There is no way yet to change a chord.
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

    /// The copies of the song that is open, and the one picked to be looked at.
    public struct EarlierVersions: Equatable, Sendable {
        public var copies: [KeptCopy]
        public var pickedID: KeptCopy.ID?
        /// The copy picked, as it was: its lines, to look at and not to change.
        public var picked: ShownSong?
        /// One line that sets it against the song as it is now: how many chords each
        /// has, and their keys.
        public var comparison = ""
        /// If putting the picked one back wants asking first (rule 23): the question.
        public var question: String?
    }

    /// What was just put back, for the few seconds in which the line under the song
    /// offers to undo it.
    public struct PutBack: Equatable, Sendable {
        /// "Put back the version of 4 Oct."
        public var sentence: String
        /// The copy that was made just before: putting THAT back is the undoing.
        var before: KeptCopy
    }

    private let appData: AppData
    private let copies: Copies
    private var openSong: OpenSong?
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
    /// The names of the songs of the library that have copies kept.
    public private(set) var songsWithCopies: Set<String> = []
    /// The panel of earlier versions, while it is open.
    public private(set) var earlier: EarlierVersions?
    public private(set) var justPutBack: PutBack?
    /// What went wrong with the last thing asked, as a sentence; nothing if nothing did.
    public private(set) var refusal: String?

    /// What is typed in the search box, and what the list is narrowed to by mark.
    public var typed = ""
    public var filter: MarkFilter = .all

    public init(appData: AppData, appVersion: String = "", finder: @escaping ([URL]) -> PlaceFinder, proPresenterIsRunning: @escaping () -> Bool) {
        self.appData = appData
        self.copies = appData.copies(appVersion: appVersion)
        self.finder = finder
        self.askWhetherProPresenterIsRunning = proPresenterIsRunning
    }

    public static func onThisMac() -> LibraryModel {
        LibraryModel(appData: .onThisMac(), appVersion: Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? "",
                     finder: { PlaceFinder.onThisMac(chosen: $0) }, proPresenterIsRunning: ProPresenterApp.isRunning)
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
        if let refusal { return refusal }
        if let justPutBack { return justPutBack.sentence }
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
        // Copies that are kept no longer go now, when the app starts (spec/07, rule 12).
        let thrownAway = copies.thin()
        if thrownAway > 0 {
            appData.log("\(thrownAway) old copies were thrown away.")
        }
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
        // (Not while the panel of earlier versions is open over it: that is put away
        // by the person, not by the app coming to the front.)
        if let song, songs.contains(where: { $0.id == song }), earlier == nil {
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
        earlier = nil
        justPutBack = nil
        refusal = nil
        openSong = nil
        guard let song = songs.first(where: { $0.id == id }), let kept = keptSong(song) else {
            shown = nil
            return
        }
        do {
            // With its safeguards: ProPresenter is asked after before every save, and a
            // copy is kept before the first.
            let copies = copies
            let opened = try OpenSong(opening: song.url, safeguards: Safeguards(
                proPresenterIsRunning: askWhetherProPresenterIsRunning,
                keepCopy: { file, why in try copies.keep(file, of: kept, why: why) }))
            openSong = opened
            shown = Self.show(opened, named: song.name)
            appData.log("Opened \(song.url.lastPathComponent).")
        } catch {
            shown = ShownSong(name: song.name, problem: (error as? Refusal)?.sentence ?? "\(song.url.lastPathComponent) could not be read.")
            appData.log("Could not read \(song.url.lastPathComponent).")
        }
    }

    // ---- Earlier versions (spec/07-backups.md, rules 14 to 23)

    /// Whether the song that is open has copies kept.
    public var hasEarlierVersions: Bool {
        songs.first { $0.id == songID }.map { songsWithCopies.contains($0.name) } ?? false
    }

    /// Opens the panel, with the newest copy picked.
    public func showEarlierVersions() {
        guard let song = songs.first(where: { $0.id == songID }), let kept = keptSong(song) else { return }
        let all = copies.copies(of: kept)
        guard !all.isEmpty else { return }
        earlier = EarlierVersions(copies: all)
        pick(copy: all.first?.id)
    }

    /// When a copy was made, as the list says it: "Today 09:14", "Sunday 4 Oct".
    public func when(_ copy: KeptCopy) -> String {
        copies.when(copy.made)
    }

    /// Esc, or a click outside, puts the panel away.
    public func closeEarlierVersions() {
        earlier = nil
    }

    /// Picking one shows it as it was, and one line that sets it against the song now.
    public func pick(copy id: KeptCopy.ID?) {
        guard var panel = earlier, let copy = panel.copies.first(where: { $0.id == id }) else { return }
        panel.pickedID = copy.id
        panel.question = nil
        if let then = try? OpenSong(opening: copy.url) {
            panel.picked = Self.show(then, named: copy.songName)
        } else {
            panel.picked = ShownSong(name: copy.songName, problem: "This copy could not be read as a ProPresenter presentation.")
        }
        let now = shown.map { Self.count($0.chords, key: $0.key) } ?? ""
        panel.comparison = "\(Self.count(copy.chords, key: copy.key)).  Now: \(now)."
        earlier = panel
    }

    /// "Put This One Back". A copy that belongs to another song than the one now at
    /// that place (another id inside) asks first, saying both names; `sure` is the yes.
    public func putBackPicked(sure: Bool = false) {
        guard var panel = earlier, let copy = panel.copies.first(where: { $0.id == panel.pickedID }), let song = openSong else { return }
        if !sure && !copy.songID.isEmpty && copy.songID != song.id {
            panel.question = "This copy is of \u{201C}\(copy.songName)\u{201D}, and the song here now is \u{201C}\(song.name)\u{201D}: another song under the same name. Put the copy in its place?"
            earlier = panel
            return
        }
        putBack(copy, undoing: false)
    }

    /// Undoes the putting back, which is only putting back the copy made just before it.
    public func takeBack() {
        guard let before = justPutBack?.before else { return }
        putBack(before, undoing: true)
    }

    /// The offer to undo goes after a few seconds; the copy is in the list all the same.
    public func forgetWhatWasPutBack() {
        justPutBack = nil
    }

    private func putBack(_ copy: KeptCopy, undoing: Bool) {
        guard let song = openSong, let listed = songs.first(where: { $0.id == songID }), let kept = keptSong(listed) else { return }
        refusal = nil
        do {
            try song.putBack(try Data(contentsOf: copy.url))
        } catch {
            refusal = (error as? Refusal)?.sentence ?? "The copy could not be read."
            appData.log("An earlier version of \(listed.url.lastPathComponent) was not put back.")
            earlier?.question = nil
            return
        }
        // The song starts afresh from its file. The newest copy now is the file as it
        // was just before: the one to put back, to take this back.
        shown = Self.show(song, named: listed.name)
        earlier = nil
        let when = copies.when(copy.made)
        if undoing {
            justPutBack = nil
            appData.log("Putting back was undone for \(listed.url.lastPathComponent).")
        } else if let before = copies.copies(of: kept).first {
            justPutBack = PutBack(sentence: "Put back the version of \(when).", before: before)
            appData.log("Put back an earlier version of \(listed.url.lastPathComponent), the one of \(when).")
        }
        noteCopies()
    }

    private static func count(_ chords: Int, key: String) -> String {
        (chords == 1 ? "1 chord" : "\(chords) chords") + (key.isEmpty ? ", no key" : ", in \(key)")
    }

    private static func show(_ song: OpenSong, named name: String) -> ShownSong {
        let boxes = song.textBoxes
        return ShownSong(name: name, key: song.key, shownKey: song.shownKey, lines: SongText.lines(of: boxes),
                         slides: song.slideCount, chords: boxes.reduce(0) { $0 + $1.chords.count })
    }

    /// A song as its copies know it: by its place, its library and its name.
    private func keptSong(_ song: ListedSong) -> KeptSong? {
        guard let place, let library else { return nil }
        return KeptSong(place: place.name, library: library.name, name: song.name)
    }

    private func noteCopies() {
        songsWithCopies = place.flatMap { place in library.map { copies.namesWithCopies(place: place.name, library: $0.name) } } ?? []
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
            openSong = nil
            earlier = nil
        }
        noteCopies()
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
