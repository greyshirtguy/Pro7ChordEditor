// When a save is refused, and that a refusal leaves everything as it was: the file, and
// the song in memory. spec/05-song-and-saving.md, rules 10 to 12, and the last of its
// proofs.

import ChordRules
import Foundation
import ProFormat
import Song
import Testing
import TestData

@Suite("Refusals")
struct RefusalsTests {
    static let plainSong = "made-by-simple-presenter/Plain Song.pro"

    /// Asks for a change, expects it to be refused for this reason, and sees that the
    /// file and the song are what they were.
    private func refused(_ song: OpenSong, for reason: Refusal.Reason, sourceLocation: SourceLocation = #_sourceLocation) throws {
        let bytes = try Data(contentsOf: song.url)
        let boxes = song.textBoxes
        let box = try #require(boxes.first)
        #expect(sourceLocation: sourceLocation) {
            try song.setChords(slide: box.slide, element: box.element, [Chord(at: 0, name: "Em")])
        } throws: { ($0 as? Refusal)?.reason == reason }
        #expect(sourceLocation: sourceLocation) { try song.setKey("E") } throws: { ($0 as? Refusal)?.reason == reason }
        #expect(try Data(contentsOf: song.url) == bytes, sourceLocation: sourceLocation)
        #expect(song.textBoxes == boxes, sourceLocation: sourceLocation)
        #expect(song.key == "G", sourceLocation: sourceLocation)
        #expect(!song.canUndo, sourceLocation: sourceLocation)
    }

    @Test("A song opened with no safeguards can be read and nothing else")
    func onlyForReading() throws {
        let bench = try Workbench()
        let song = try OpenSong(opening: bench.copy(of: Self.plainSong))
        try refused(song, for: .onlyForReading)
    }

    @Test("Nothing is saved while ProPresenter is running, and no copy is kept for it")
    func whileProPresenterIsRunning() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.plainSong)
        bench.proPresenterIsRunning = true
        try refused(song, for: .proPresenterIsRunning)
        #expect(bench.copiesKept.isEmpty)

        // When it has quit, the same change is made.
        bench.proPresenterIsRunning = false
        try song.setKey("E")
        #expect(song.key == "E")
        // And it is asked before every save, undo among them.
        bench.proPresenterIsRunning = true
        #expect { try song.undo() } throws: { ($0 as? Refusal)?.reason == .proPresenterIsRunning }
        #expect(song.key == "E" && song.canUndo)
    }

    @Test("Nothing is saved over a file that something else has changed")
    func changedByAnother() throws {
        let bench = try Workbench()
        // Another song open on the same file changes it.
        let song = try bench.open(Self.plainSong)
        let other = try OpenSong(opening: song.url, safeguards: bench.safeguards)
        try other.setKey("D")
        try refused(song, for: .changedByAnother)
        #expect(try OpenSong(opening: song.url).key == "D")
    }

    @Test("Nor over one whose time of last change has moved, though its bytes have not")
    func touched() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.plainSong)
        try FileManager.default.setAttributes([.modificationDate: Date(timeIntervalSinceNow: -3600)], ofItemAtPath: song.url.path)
        try refused(song, for: .changedByAnother)
    }

    @Test("Nor over one changed with its size and its time left as they were")
    func changedBehindItsStamp() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.plainSong)
        let attributes = try FileManager.default.attributesOfItem(atPath: song.url.path)
        // The same song with another name of the same length inside it: another file of
        // the same size. Then its time is put back to the nanosecond.
        var stat0 = stat()
        #expect(stat(song.url.path, &stat0) == 0)
        var other = try message(at: song.url)
        other.name = String(other.name.reversed())
        try other.serializedData().write(to: song.url)
        var times = [stat0.st_atimespec, stat0.st_mtimespec]
        #expect(utimensat(AT_FDCWD, song.url.path, &times, 0) == 0)
        var stat1 = stat()
        #expect(stat(song.url.path, &stat1) == 0)
        #expect(stat1.st_size == stat0.st_size && stat1.st_mtimespec.tv_sec == stat0.st_mtimespec.tv_sec && stat1.st_mtimespec.tv_nsec == stat0.st_mtimespec.tv_nsec)
        #expect(attributes[.size] as? Int == (try FileManager.default.attributesOfItem(atPath: song.url.path))[.size] as? Int)

        try refused(song, for: .changedByAnother)
    }

    @Test("Nothing is saved if a copy cannot be kept first; and the copy is asked for again next time")
    func noCopyNoChange() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.plainSong)
        let original = try Data(contentsOf: song.url)
        bench.copyCannotBeKept = true
        try refused(song, for: .copyNotKept)

        bench.copyCannotBeKept = false
        try song.setKey("E")
        try song.setKey("D")
        try song.undo()
        // One copy for a sitting with a song, not one for each change: of the file as it
        // was before the first of them.
        #expect(bench.copiesKept == [original])
    }

    @Test("If the file cannot be written, the song is as it was")
    func cannotBeWritten() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.plainSong)
        // A folder that cannot be written in: nothing can be put beside the song.
        try FileManager.default.setAttributes([.posixPermissions: 0o555], ofItemAtPath: bench.folder.path)
        defer { try? FileManager.default.setAttributes([.posixPermissions: 0o755], ofItemAtPath: bench.folder.path) }
        try refused(song, for: .notSaved)
        #expect(try bench.names() == [song.url.lastPathComponent])
    }

    @Test("If the new file cannot be put in the song's place, the song is as it was and nothing is left beside it")
    func cannotBeSwappedIn() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.plainSong)
        // A file that may not be changed or replaced, in a folder that can be written in:
        // the new file is written beside it, and then cannot take its place.
        try FileManager.default.setAttributes([.immutable: true], ofItemAtPath: song.url.path)
        defer { try? FileManager.default.setAttributes([.immutable: false], ofItemAtPath: song.url.path) }
        try refused(song, for: .notSaved)
        #expect(try bench.names() == [song.url.lastPathComponent])
    }

    @Test("A file that is no presentation is not opened, and says so")
    func notAPresentation() throws {
        let bench = try Workbench()
        let nonsense = try bench.put(Data("This is not a song.".utf8), named: "Nonsense.pro")
        #expect { _ = try OpenSong(opening: nonsense) } throws: { ($0 as? Refusal)?.reason == .cannotBeRead }
        #expect { _ = try OpenSong(opening: bench.folder.appendingPathComponent("Not there.pro")) } throws: { ($0 as? Refusal)?.reason == .cannotBeRead }
    }

    @Test("A saved file keeps who may read it; and every refusal is a sentence that ends properly")
    func permissionsAndSentences() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.plainSong)
        try FileManager.default.setAttributes([.posixPermissions: 0o600], ofItemAtPath: song.url.path)
        // (Changing who may read a file does not change its time of last change.)
        try song.setKey("E")
        #expect(try FileManager.default.attributesOfItem(atPath: song.url.path)[.posixPermissions] as? Int == 0o600)

        bench.proPresenterIsRunning = true
        do {
            try song.setKey("D")
            Issue.record("it was not refused")
        } catch let refusal as Refusal {
            #expect(refusal.sentence == "ProPresenter is running. Quit it to change chords here. Nothing was changed.")
            #expect("\(refusal)" == refusal.sentence)
        }
    }
}
