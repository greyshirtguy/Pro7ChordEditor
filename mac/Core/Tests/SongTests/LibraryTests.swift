// Finding places, the lists, the marks and the app's own folder: spec/06-library.md,
// rules 1 to 11 and "What the app keeps of its own". All on folders and settings made
// up for the test, in a temporary folder: no test asks this Mac's ProPresenter anything.
//
// NEW: nothing here is in Simple Presenter, so there is no vector for any of it. What a
// song's marks should be is held to as-read-by-simple-presenter.json all the same, which
// says how many chords each test file has.

import ChordRules
import Foundation
import Song
import Testing
import TestData

@Suite("The library")
struct LibraryTests {
    /// A place laid out as ProPresenter lays one out, in the bench's folder: two
    /// libraries of copies of the test files, and beside them what is not a library.
    private func madeUpPlace(_ bench: Workbench, named name: String = "ProPresenter") throws -> URL {
        let place = bench.folder.appendingPathComponent(name, isDirectory: true)
        let songs = place.appendingPathComponent("Libraries/Songs", isDirectory: true)
        let copies = place.appendingPathComponent("Libraries/Old copies", isDirectory: true)
        let empty = place.appendingPathComponent("Libraries/Empty", isDirectory: true)
        for folder in [songs, copies, empty, place.appendingPathComponent("Media"), place.appendingPathComponent("Libraries/.hidden")] {
            try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        }
        for path in try TestData.presentationPaths() {
            let name = String(path.split(separator: "/").last!)
            try TestData.file(path).write(to: (path.hasPrefix("as-the-old") ? copies : songs).appendingPathComponent(name))
        }
        // Things in `Libraries` and in a library that are not libraries and not songs.
        try Data("not a folder".utf8).write(to: place.appendingPathComponent("Libraries/LibraryData"))
        try Data("an older kind of file".utf8).write(to: songs.appendingPathComponent("Doxology.pro6"))
        try Data("This is not a song.".utf8).write(to: songs.appendingPathComponent("Nonsense.pro"))
        try Data().write(to: songs.appendingPathComponent(".Hidden.pro"))
        try FileManager.default.createDirectory(at: songs.appendingPathComponent("A folder.pro"), withIntermediateDirectories: true)
        return place
    }

    // ---- Places

    @Test("Workspaces are found in ProPresenter's settings, the one in use first, each once, and only if it is there")
    func workspaces() throws {
        let bench = try Workbench()
        let first = try madeUpPlace(bench, named: "First")
        let second = try madeUpPlace(bench, named: "Second")
        let listed: [[String: Any]] = [
            ["name": "First", "url": first.absoluteString, "isActive": false, "somethingElse": "that is not looked at"],
            ["name": "The one in use", "url": second.absoluteString, "isActive": true],
            ["name": "Gone", "url": bench.folder.appendingPathComponent("Gone").absoluteString, "isActive": false],
            ["name": "First again", "url": first.path],
            ["name": "No place"],
        ]
        let json = try JSONSerialization.data(withJSONObject: listed)
        // As ProPresenter keeps the list (JSON, as data), and the two other ways it might.
        for kept in [json as Any, String(decoding: json, as: UTF8.self) as Any, listed as Any] {
            let finder = PlaceFinder(proPresentersSetting: { $0 == "userWorkspaces" ? kept : nil }, documents: nil)
            let places = finder.places()
            #expect(places.map(\.name) == ["The one in use", "First"])
            #expect(places.map(\.kind) == [.workspace, .workspace])
            #expect(places.map(\.isInUse) == [true, false])
            #expect(places.map(\.id) == [Place(kind: .workspace, name: "", folder: second).id, Place(kind: .workspace, name: "", folder: first).id])
        }
    }

    @Test("The folder the settings name, Documents, and folders chosen by hand are places too")
    func theOtherPlaces() throws {
        let bench = try Workbench()
        let named = try madeUpPlace(bench, named: "Moved")
        let documents = bench.folder.appendingPathComponent("Documents", isDirectory: true)
        let inDocuments = try madeUpPlace(bench, named: "Documents/ProPresenter")
        let chosen = bench.folder.appendingPathComponent("Somewhere else", isDirectory: true)
        try FileManager.default.createDirectory(at: chosen, withIntermediateDirectories: true)

        let finder = PlaceFinder(proPresentersSetting: { $0 == "applicationShowDirectory" ? named.path : nil }, documents: documents,
                                 chosen: [chosen, inDocuments, bench.folder.appendingPathComponent("Not there")])
        let places = finder.places()
        #expect(places.map(\.kind) == [.namedInSettings, .documents, .chosen])
        #expect(places.map(\.name) == ["Moved", "Documents", "Somewhere else"])
        #expect(places.map(\.folder.lastPathComponent) == ["Moved", "ProPresenter", "Somewhere else"])

        // A folder the settings name that has no `Libraries` in it is no place.
        let bare = PlaceFinder(proPresentersSetting: { $0 == "applicationShowDirectory" ? chosen.path : nil }, documents: nil)
        #expect(bare.places().isEmpty)
        // And with nothing anywhere, there is none, which is not an error.
        #expect(PlaceFinder(proPresentersSetting: { _ in nil }, documents: bench.folder.appendingPathComponent("No documents")).places().isEmpty)
    }

    // ---- The lists

    @Test("A place's libraries are the folders of its Libraries, by name; a library's songs are its .pro files, by name")
    func libraries() throws {
        let bench = try Workbench()
        let place = Place(kind: .workspace, name: "Made up", folder: try madeUpPlace(bench))
        let libraries = Listing.libraries(of: place)
        // Not the file beside them, nor the hidden folder; the empty one is a library still.
        #expect(libraries.map(\.name) == ["Empty", "Old copies", "Songs"])

        let songs = Listing.songs(of: libraries[2])
        #expect(songs.map(\.name) == ["A Song - In A Minor Key", "Amazing Grace", "Blank Slides", "Holy Holy Holy", "It Is Well", "Nonsense", "Plain Song", "Rock Of Ages (Toplady)"])
        #expect(songs.allSatisfy { $0.size > 0 && $0.modified > .distantPast })
        #expect(Listing.songs(of: libraries[0]).isEmpty)
        #expect(Listing.songs(of: libraries[1]).count == 4)
    }

    @Test("Names are sorted as the system sorts them")
    func sortedAsTheSystemSorts() throws {
        let bench = try Workbench()
        let folder = bench.folder.appendingPathComponent("Numbers", isDirectory: true)
        try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        for name in ["Song 10", "song 2", "Song 1", "\u{C9}t\u{E9}", "Zion", "apple"] {
            try Data().write(to: folder.appendingPathComponent(name + ".pro"))
        }
        #expect(Listing.songs(of: Library(name: "Numbers", folder: folder)).map(\.name) == ["apple", "\u{C9}t\u{E9}", "Song 1", "song 2", "Song 10", "Zion"])
    }

    @Test("A folder chosen by hand is taken for what it has")
    func aChosenFolder() throws {
        let bench = try Workbench()
        let whole = try madeUpPlace(bench)
        // The ProPresenter folder itself, its Libraries folder, and one library.
        #expect(Listing.libraries(of: Place(kind: .chosen, name: "", folder: whole)).map(\.name) == ["Empty", "Old copies", "Songs"])
        // (Chosen so, a folder with no songs in it is not offered as a library.)
        #expect(Listing.libraries(of: Place(kind: .chosen, name: "", folder: whole.appendingPathComponent("Libraries"))).map(\.name) == ["Old copies", "Songs"])
        let one = Listing.libraries(of: Place(kind: .chosen, name: "", folder: whole.appendingPathComponent("Libraries/Songs")))
        #expect(one.map(\.name) == ["Songs"])
        #expect(Listing.songs(of: one[0]).count == 8)
        #expect(Listing.libraries(of: Place(kind: .chosen, name: "", folder: whole.appendingPathComponent("Media"))).isEmpty)
    }

    @Test("The list is narrowed by what is typed, capitals and accents apart")
    func narrowed() throws {
        let songs = ["Amazing Grace", "This Is Amazing Grace", "\u{C9}t\u{E9}", "Holy Holy Holy"].map {
            ListedSong(name: $0, url: URL(fileURLWithPath: "/nowhere/\($0).pro"), size: 1, modified: .distantPast)
        }
        #expect(Listing.narrowed(songs, to: "").count == 4)
        #expect(Listing.narrowed(songs, to: "  ").count == 4)
        #expect(Listing.narrowed(songs, to: "grace").map(\.name) == ["Amazing Grace", "This Is Amazing Grace"])
        #expect(Listing.narrowed(songs, to: " AMAZING g").map(\.name) == ["Amazing Grace", "This Is Amazing Grace"])
        #expect(Listing.narrowed(songs, to: "ete").map(\.name) == ["\u{C9}t\u{E9}"])
        #expect(Listing.narrowed(songs, to: "zzz").isEmpty)
    }

    // ---- The marks

    struct AsRead: Decodable {
        struct File: Decodable {
            struct Box: Decodable {
                struct Chord: Decodable {}
                let chords: [Chord]
            }
            let file: String
            let textBoxes: [Box]
        }
        let files: [File]
    }

    @Test("A song's marks: how many chords it has, whether it is one of the old editor's copies, whether it can be read")
    func marks() throws {
        let bench = try Workbench()
        let place = Place(kind: .workspace, name: "Made up", folder: try madeUpPlace(bench))
        let libraries = Listing.libraries(of: place)
        let asRead = try JSONDecoder().decode(AsRead.self, from: TestData.file("expected/as-read-by-simple-presenter.json"))
        var seen = 0
        for library in libraries {
            for song in Listing.songs(of: library) {
                let marks = Marks.read(song)
                #expect(marks.isOldEditorsCopy == song.name.hasSuffix(" (Chords)"))
                if song.name == "Nonsense" {
                    #expect(marks.cannotBeRead && !marks.hasChords)
                } else {
                    // As many chords as Simple Presenter's editor is handed for that file.
                    let expected = try #require(asRead.files.first { $0.file == song.name + ".pro" }, "\(song.name) is not in as-read-by-simple-presenter.json")
                    seen += 1
                    let chords = expected.textBoxes.reduce(0) { $0 + $1.chords.count }
                    #expect(marks.chords == chords, "\(song.name)")
                    #expect(marks.hasChords == (chords > 0) && !marks.cannotBeRead)
                }
            }
        }
        #expect(seen == 11)
        // (The song ProPresenter wrote is the one with none.)
        #expect(Marks.read(Listing.songs(of: libraries[2]).first { $0.name == "Rock Of Ages (Toplady)" }!) == Marks())
    }

    @Test("Marks worked out before are known again only while the file is as it was")
    func keptMarks() throws {
        let song = ListedSong(name: "A", url: URL(fileURLWithPath: "/nowhere/A.pro"), size: 100, modified: Date(timeIntervalSince1970: 1_000))
        var kept = KeptMarks()
        #expect(kept.marks(for: song) == nil)
        kept.keep(Marks(chords: 12), for: song)
        #expect(kept.marks(for: song) == Marks(chords: 12))
        var bigger = song
        bigger.size = 101
        #expect(kept.marks(for: bigger) == nil)
        var later = song
        later.modified = Date(timeIntervalSince1970: 1_001)
        #expect(kept.marks(for: later) == nil)
        var elsewhere = song
        elsewhere.url = URL(fileURLWithPath: "/elsewhere/A.pro")
        #expect(kept.marks(for: elsewhere) == nil)
    }

    @Test("The list can be narrowed to songs with chords, or without")
    func markFilter() {
        let with = Marks(chords: 3), without = Marks(), broken = Marks(cannotBeRead: true)
        #expect(MarkFilter.all.lets(with) && MarkFilter.all.lets(without) && MarkFilter.all.lets(nil) && MarkFilter.all.lets(broken))
        #expect(MarkFilter.withChords.lets(with) && !MarkFilter.withChords.lets(without) && !MarkFilter.withChords.lets(nil))
        // A song that cannot be read is not said to be without chords: nobody knows.
        #expect(MarkFilter.withoutChords.lets(without) && !MarkFilter.withoutChords.lets(with) && !MarkFilter.withoutChords.lets(nil) && !MarkFilter.withoutChords.lets(broken))
    }

    // ---- The app's own folder

    @Test("The app's settings and marks are kept in its own folder, and read back")
    func appData() throws {
        let bench = try Workbench()
        let data = AppData(folder: bench.folder.appendingPathComponent("Own/Pro7ChordEditor"))
        // Nothing kept yet is not an error, and makes no folder.
        #expect(data.settings == AppData.Settings())
        #expect(data.marks == KeptMarks())
        #expect(!FileManager.default.fileExists(atPath: data.folder.path))

        var settings = AppData.Settings()
        settings.chosenFolders = ["/Somewhere/Songs"]
        settings.lastPlace = "/Somewhere"
        settings.lastLibrary = "/Somewhere/Songs"
        data.settings = settings
        var kept = KeptMarks()
        kept.keep(Marks(chords: 4, isOldEditorsCopy: true), for: ListedSong(name: "A", url: URL(fileURLWithPath: "/nowhere/A.pro"), size: 9, modified: Date(timeIntervalSince1970: 5)))
        data.marks = kept

        let again = AppData(folder: data.folder)
        #expect(again.settings == settings)
        #expect(again.marks == kept)
        #expect(try bench.names().contains("Own"))

        // A settings file that cannot be read is taken as not there.
        try Data("{ not json".utf8).write(to: data.folder.appendingPathComponent("settings.json"))
        #expect(again.settings == AppData.Settings())
    }

    @Test("The log has a line for each thing, with its time, and is begun again when it has grown long")
    func theLog() throws {
        let bench = try Workbench()
        let data = AppData(folder: bench.folder.appendingPathComponent("Own"))
        data.log("Started.", at: Date(timeIntervalSince1970: 0))
        data.log("Opened Plain Song.pro\nin Songs.", at: Date(timeIntervalSince1970: 61))
        let log = try String(contentsOf: data.folder.appendingPathComponent("log.txt"), encoding: .utf8)
        let lines = log.split(separator: "\n")
        #expect(lines.count == 2)
        #expect(lines[0].hasSuffix("  Started."))
        #expect(lines[1].hasSuffix("  Opened Plain Song.pro in Songs."))
        #expect(lines[0].hasPrefix("19") && lines[0].contains("-01-01T"))

        // Past a megabyte, the next line begins a new log and the old one is put aside.
        try Data(count: 1_000_001).write(to: data.folder.appendingPathComponent("log.txt"))
        data.log("After.")
        #expect(try String(contentsOf: data.folder.appendingPathComponent("log.txt"), encoding: .utf8).hasSuffix("  After.\n"))
        #expect(try Data(contentsOf: data.folder.appendingPathComponent("log-before.txt")).count == 1_000_001)
    }

    // ---- A song as lines of text

    @Test("A song as lines: a gap before each slide but the first, a group's name before its first slide, then each line of each box", arguments: (try? TestData.presentationPaths()) ?? [])
    func songLines(path: String) throws {
        let song = try OpenSong(opening: TestData.root.appendingPathComponent("files").appendingPathComponent(path))
        let boxes = song.textBoxes
        let lines = SongText.lines(of: boxes)

        // The gaps: one fewer than the slides that have a box.
        #expect(lines.filter { $0.kind == .gap }.count == Set(boxes.map(\.slide)).count - 1)
        #expect(lines.first?.kind != .gap && lines.last?.kind != .gap)
        // The groups: one line for each slide that starts a group with a name.
        let starts = boxes.filter { $0.startsItsGroup && !$0.group.isEmpty }
        #expect(lines.filter { $0.kind == .group }.map(\.text) == Set(starts.map(\.slide)).sorted().map { slide in "{c: \(starts.first { $0.slide == slide }!.group)}" })
        // Each box's lines, put together again, are the box as `toChordPro` writes it
        // (which chordpro.json holds), and each line knows where it is in its box.
        for box in boxes {
            // A blank slide is one line, and that line is empty.
            if box.blank {
                let own = lines.filter { $0.box == boxes.firstIndex(of: box) }
                #expect(own.map(\.kind) == [.blankSlide] && own.first?.text == "" && own.first?.slide == box.slide && own.first?.element == box.element)
                continue
            }
            let own = lines.filter { $0.box == boxes.firstIndex(of: box) && ($0.kind == .words || $0.kind == .chordsAlone) }
            #expect(own.map(\.text).joined(separator: "\n") == Chords.toChordPro(box.words, box.chords))
            #expect(own.map(\.words) == box.words.lines)
            #expect(own.map(\.line) == Array(own.indices))
            #expect(own.allSatisfy { $0.slide == box.slide && Array(box.words.units[$0.start..<($0.start + $0.words.length)]) == $0.words.units })
            #expect(own.allSatisfy { ($0.kind == .chordsAlone) == Chords.isPlaceholders($0.words) })
        }
        #expect(SongText.text(of: boxes) == lines.map(\.text).joined(separator: "\n"))
    }

    @Test("Plain Song, as its lines, is the ChordPro file it was made from")
    func plainSong() throws {
        let song = try OpenSong(opening: TestData.root.appendingPathComponent("files/made-by-simple-presenter/Plain Song.pro"))
        // testdata/songs/plain-song.cho, without what says what the song is, and with its
        // verse as the two slides it was dealt into, two lines to a slide.
        #expect(SongText.text(of: song.textBoxes) == """
            {c: Verse 1}
            [G]One two three [D]four
            Five six [Em]seven eight

            [C]Nine ten e[G/B]leven twelve
            Thirteen

            {c: Chorus}
            [C]Sing it [G]out
            [D]Sing it loud

            {c: Outro}
            [G] [D/F#] [Em]
            """)
    }
}
