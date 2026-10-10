// The copies kept before a change, and putting one back. spec/07-backups.md, and the
// proofs at its foot. On copies of the test files, with the copies themselves kept in
// the test's own temporary folder.
//
// NEW: nothing here is in Simple Presenter, so no vector holds any of it.

import ChordRules
import Foundation
import ProFormat
import Song
import Testing
import TestData

@Suite("The copies kept")
struct CopiesTests {
    static let plainSong = "made-by-simple-presenter/Plain Song.pro"
    static let utc = TimeZone(identifier: "UTC")!
    static let kept = KeptSong(place: "Sunday", library: "Songs", name: "Plain Song")

    /// 2026-10-11 09:14:03 in UTC, and so many days and seconds from it.
    private func time(days: Double = 0, seconds: Double = 0) -> Date {
        var parts = DateComponents()
        (parts.year, parts.month, parts.day, parts.hour, parts.minute, parts.second) = (2026, 10, 11, 9, 14, 3)
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = Self.utc
        return calendar.date(from: parts)!.addingTimeInterval(days * 86_400 + seconds)
    }

    private func copies(_ bench: Workbench) -> Copies {
        Copies(folder: bench.folder.appendingPathComponent("Own/Backups", isDirectory: true), appVersion: "0.1", timeZone: Self.utc)
    }

    /// A song opened so that its copies are really kept, by `Copies`, at a time the
    /// test may move on.
    private func open(_ bench: Workbench, _ url: URL, at clock: @escaping () -> Date) throws -> OpenSong {
        let copies = copies(bench)
        return try OpenSong(opening: url, safeguards: Safeguards(
            proPresenterIsRunning: { [unowned bench] in bench.proPresenterIsRunning },
            keepCopy: { file, why in try copies.keep(file, of: Self.kept, why: why, at: clock()) }))
    }

    // ---- Keeping

    @Test("A change makes one copy, the same bytes as the file was; a second change in the same sitting makes none; another sitting makes one")
    func oneCopyForASitting() throws {
        let bench = try Workbench()
        let url = try bench.copy(of: Self.plainSong)
        let original = try Data(contentsOf: url)
        var now = time()
        let song = try open(bench, url, at: { now })
        #expect(copies(bench).copies(of: Self.kept).isEmpty)

        try song.setKey("E")
        var kept = copies(bench).copies(of: Self.kept)
        #expect(kept.count == 1)
        #expect(try Data(contentsOf: kept[0].url) == original)
        #expect(kept[0].url.lastPathComponent == "2026-10-11 09.14.03.pro")
        #expect(kept[0].url.deletingLastPathComponent().path.hasSuffix("Own/Backups/Sunday/Songs/Plain Song"))

        now = time(seconds: 60)
        try song.setKey("D")
        try song.undo()
        #expect(copies(bench).copies(of: Self.kept).count == 1)

        // Opened again and changed: a copy of the file as it is now, which is in E.
        let inE = try Data(contentsOf: url)
        now = time(seconds: 3600)
        try open(bench, url, at: { now }).setKey("A")
        kept = copies(bench).copies(of: Self.kept)
        #expect(kept.map(\.url.lastPathComponent) == ["2026-10-11 10.14.03.pro", "2026-10-11 09.14.03.pro"])
        #expect(try Data(contentsOf: kept[0].url) == inE)
        #expect(kept.map(\.isTheFirst) == [false, true])
    }

    @Test("What the index says of a copy: why, of what, the song's id and name, its chords, its key, its size")
    func theIndex() throws {
        let bench = try Workbench()
        let url = try bench.copy(of: Self.plainSong)
        let song = try open(bench, url, at: { time() })
        try song.setKey("E")
        let copy = try #require(copies(bench).copies(of: Self.kept).first)
        #expect(copy.made == time() && copy.why == .beforeAChange)
        #expect(copy.copyOf == url.standardizedFileURL.path)
        #expect(copy.songName == "Plain Song" && copy.songID == song.id && !copy.songID.isEmpty)
        #expect(copy.chords == 11 && copy.key == "G")
        #expect(copy.size == (try TestData.file(Self.plainSong)).count)

        // The index is a file of its own beside the copies, and says the app's version.
        let index = try String(contentsOf: copy.url.deletingLastPathComponent().appendingPathComponent("index.json"), encoding: .utf8)
        #expect(index.contains("\"why\" : \"before a change\"") && index.contains("\"appVersion\" : \"0.1\""))
        // It has not a word of the song in it, nor a chord's name.
        #expect(!index.contains("One two three") && !index.contains("D/F#"))
    }

    @Test("With the index gone, the copies are still listed, from the folder and from themselves")
    func withTheIndexGone() throws {
        let bench = try Workbench()
        let url = try bench.copy(of: Self.plainSong)
        try open(bench, url, at: { time() }).setKey("E")
        try open(bench, url, at: { time(days: 1) }).setKey("D")
        let home = copies(bench).folder(of: Self.kept)
        try FileManager.default.removeItem(at: home.appendingPathComponent("index.json"))

        let kept = copies(bench).copies(of: Self.kept)
        #expect(kept.map(\.url.lastPathComponent) == ["2026-10-12 09.14.03.pro", "2026-10-11 09.14.03.pro"])
        // When, from its name; what is in it, from itself; why, and of which file, not known.
        #expect(kept.map(\.made) == [time(days: 1), time()])
        #expect(kept.map(\.key) == ["E", "G"] && kept.allSatisfy { $0.chords == 11 && $0.songName == "Plain Song" })
        #expect(kept.allSatisfy { $0.why == nil && $0.copyOf.isEmpty })

        // An index that is damaged is the same as none; and a file that is not named as
        // a copy is no copy.
        try Data("{ not json".utf8).write(to: home.appendingPathComponent("index.json"))
        try Data("a note".utf8).write(to: home.appendingPathComponent("Read me.txt"))
        try Data("a song".utf8).write(to: home.appendingPathComponent("Plain Song.pro"))
        #expect(copies(bench).copies(of: Self.kept).count == 2)
    }

    @Test("No second copy is made of bytes that the newest copy already is; another made in the same second is named apart")
    func noSecondCopyOfTheSame() throws {
        let bench = try Workbench()
        let url = try bench.copy(of: Self.plainSong)
        let keeper = copies(bench)
        #expect(try keeper.keep(url, of: Self.kept, why: .beforeAChange, at: time()) != nil)
        #expect(try keeper.keep(url, of: Self.kept, why: .beforeMending, at: time(seconds: 5)) == nil)
        #expect(keeper.copies(of: Self.kept).count == 1)

        // The file changes, and is copied twice more in one second.
        try OpenSong(opening: url, safeguards: bench.safeguards).setKey("E")
        #expect(try keeper.keep(url, of: Self.kept, why: .beforePuttingBack, at: time()) != nil)
        try OpenSong(opening: url, safeguards: bench.safeguards).setKey("D")
        #expect(try keeper.keep(url, of: Self.kept, why: .beforeMending, at: time()) != nil)
        let kept = keeper.copies(of: Self.kept)
        #expect(kept.map(\.url.lastPathComponent) == ["2026-10-11 09.14.03 3.pro", "2026-10-11 09.14.03 2.pro", "2026-10-11 09.14.03.pro"])
        #expect(kept.map(\.why) == [.beforeMending, .beforePuttingBack, .beforeAChange])
        #expect(kept.map(\.key) == ["D", "E", "G"])
    }

    @Test("A copy that cannot be made refuses the change and leaves the file alone")
    func aCopyThatCannotBeMade() throws {
        let bench = try Workbench()
        let url = try bench.copy(of: Self.plainSong)
        let original = try Data(contentsOf: url)
        // Where the folder of copies would be, there is a file: nothing can be kept there.
        try FileManager.default.createDirectory(at: bench.folder.appendingPathComponent("Own"), withIntermediateDirectories: true)
        try Data("in the way".utf8).write(to: bench.folder.appendingPathComponent("Own/Backups"))

        let song = try open(bench, url, at: { time() })
        #expect { try song.setKey("E") } throws: { ($0 as? Refusal)?.reason == .copyNotKept }
        #expect { try song.setChords(slide: 0, element: song.textBoxes[0].element, []) } throws: { ($0 as? Refusal)?.reason == .copyNotKept }
        #expect(try Data(contentsOf: url) == original)
        #expect(song.key == "G" && !song.canUndo)
    }

    // ---- Putting one back

    @Test("Put back, the file is byte for byte the copy; taken back, byte for byte what it was")
    func putBackAndTakenBack() throws {
        let bench = try Workbench()
        let url = try bench.copy(of: Self.plainSong)
        let original = try Data(contentsOf: url)
        var now = time()
        let song = try open(bench, url, at: { now })
        let box = try #require(song.textBoxes.first)
        try song.setChords(slide: box.slide, element: box.element, [Chord(at: 0, name: "Em")])
        try song.setKey("E")
        let changed = try Data(contentsOf: url)
        #expect(song.canUndo)

        // The one copy there is, is the file as it was. Put back:
        now = time(seconds: 600)
        let first = try #require(copies(bench).copies(of: Self.kept).first)
        try song.putBack(try Data(contentsOf: first.url))
        #expect(try Data(contentsOf: url) == original)
        // The song starts afresh from it: what it shows is the file, and undo is empty.
        #expect(song.key == "G" && song.textBoxes.first?.chords == box.chords)
        #expect(!song.canUndo && !song.canRedo)

        // Before it was put back, the file as it was then was copied, and says why.
        var kept = copies(bench).copies(of: Self.kept)
        #expect(kept.count == 2 && kept[0].why == .beforePuttingBack && kept[0].made == now)
        #expect(try Data(contentsOf: kept[0].url) == changed)

        // Taking it back is putting that copy back. There is no second mechanism.
        now = time(seconds: 660)
        try song.putBack(try Data(contentsOf: kept[0].url))
        #expect(try Data(contentsOf: url) == changed)
        #expect(song.key == "E" && song.textBoxes.first?.chords == [Chord(at: 0, name: "Em")])
        // That was a putting back too, and so the file as it was before it was copied:
        // the same bytes as the first copy, but the first is not the newest, and it is
        // the newest that a new copy is held against.
        kept = copies(bench).copies(of: Self.kept)
        #expect(kept.count == 3 && kept[0].why == .beforePuttingBack)
        #expect(try Data(contentsOf: kept[0].url) == original)

        // The song goes on from there as from a new sitting: its next change keeps a copy.
        now = time(seconds: 720)
        try song.setKey("D")
        kept = copies(bench).copies(of: Self.kept)
        #expect(kept.count == 4 && kept[0].why == .beforeAChange)
        #expect(try Data(contentsOf: kept[0].url) == changed)
        try song.undo()
        #expect(try Data(contentsOf: url) == changed)
    }

    @Test("Putting back is a save: refused while ProPresenter runs, over a file something else changed, and without its own copy first")
    func puttingBackIsASave() throws {
        let bench = try Workbench()
        let url = try bench.copy(of: Self.plainSong)
        let original = try Data(contentsOf: url)
        let song = try open(bench, url, at: { time() })
        try song.setKey("E")
        let changed = try Data(contentsOf: url)

        bench.proPresenterIsRunning = true
        #expect { try song.putBack(original) } throws: { ($0 as? Refusal)?.reason == .proPresenterIsRunning }
        bench.proPresenterIsRunning = false

        // A copy that is no song is not put in a song's place.
        #expect { try song.putBack(Data("not a song".utf8)) } throws: { ($0 as? Refusal)?.reason == .cannotBeRead }
        // Bytes that are the file already are nothing to do.
        try song.putBack(changed)
        #expect(try Data(contentsOf: url) == changed && song.canUndo)
        #expect(copies(bench).copies(of: Self.kept).count == 1)

        // A song opened only to be looked at puts nothing back.
        #expect { try OpenSong(opening: url).putBack(original) } throws: { ($0 as? Refusal)?.reason == .onlyForReading }

        // Something else changes the file.
        try OpenSong(opening: url, safeguards: bench.safeguards).setKey("A")
        let byAnother = try Data(contentsOf: url)
        #expect { try song.putBack(original) } throws: { ($0 as? Refusal)?.reason == .changedByAnother }
        #expect(try Data(contentsOf: url) == byAnother)
        #expect(song.key == "E")
    }

    // ---- How long

    @Test("Of old copies the first is never thrown away, nor the twenty newest, nor any of the last thirty days")
    func thinning() throws {
        let bench = try Workbench()
        let url = try bench.copy(of: Self.plainSong)
        let keeper = copies(bench)
        // Thirty copies, one every ten days back from now, each of other bytes.
        var keys = Chords.majorKeys() + Chords.minorKeys() + ["A#", "B#", "Cb", "C#"]
        for n in (0..<30).reversed() {
            try OpenSong(opening: url, safeguards: bench.safeguards).setKey(keys.removeLast())
            #expect(try keeper.keep(url, of: Self.kept, why: .beforeAChange, at: time(days: -10.0 * Double(n))) != nil)
        }
        let home = keeper.folder(of: Self.kept)
        try Data("a note".utf8).write(to: home.appendingPathComponent("Read me.txt"))
        #expect(keeper.copies(of: Self.kept).count == 30)

        // Now: copies 0 to 29, of ages 0, 10, 20 ... 290 days. The twenty newest stay
        // (0 to 190 days), and of the ten older only the very first (290 days).
        #expect(keeper.thin(asOf: time()) == 9)
        let left = keeper.copies(of: Self.kept)
        #expect(left.count == 21)
        #expect(left.last?.made == time(days: -290) && left.last?.isTheFirst == true)
        #expect(left.dropLast().map(\.made) == (0..<20).map { time(days: -10.0 * Double($0)) })
        // The index follows: it has an entry for each copy that is left and for no
        // other. And what is not a copy is not touched.
        let index = try JSONSerialization.jsonObject(with: Data(contentsOf: home.appendingPathComponent("index.json"))) as? [[String: Any]]
        #expect(Set(index?.compactMap { $0["file"] as? String } ?? []) == Set(left.map(\.id)))
        #expect(FileManager.default.fileExists(atPath: home.appendingPathComponent("Read me.txt").path))
        #expect(left.allSatisfy { $0.why == .beforeAChange })
        // Again, and nothing more goes.
        #expect(keeper.thin(asOf: time()) == 0)

        // A year on, all are old: the twenty newest and the first are there still.
        #expect(keeper.thin(asOf: time(days: 365)) == 0)
        #expect(keeper.copies(of: Self.kept).count == 21)
    }

    @Test("Everything of the last thirty days is kept, however many")
    func thinningKeepsTheLastThirtyDays() throws {
        let bench = try Workbench()
        let url = try bench.copy(of: Self.plainSong)
        let keeper = copies(bench)
        var keys = Chords.majorKeys() + Chords.minorKeys()
        // Twenty-five copies in the last twenty-five days.
        for n in (0..<25).reversed() {
            try OpenSong(opening: url, safeguards: bench.safeguards).setKey(keys.removeLast())
            try keeper.keep(url, of: Self.kept, why: .beforeAChange, at: time(days: -Double(n)))
        }
        #expect(keeper.thin(asOf: time()) == 0)
        #expect(keeper.copies(of: Self.kept).count == 25)
        // Sixty days on they are all old, and all but the twenty newest and the first go.
        #expect(keeper.thin(asOf: time(days: 60)) == 4)
        #expect(keeper.copies(of: Self.kept).count == 21)
    }

    // ---- All of them

    @Test("Every song that has copies is found, by place and library; and how much room they take")
    func allCopies() throws {
        let bench = try Workbench()
        let keeper = copies(bench)
        let plain = try bench.copy(of: Self.plainSong)
        let grace = try bench.copy(of: "made-by-simple-presenter/Amazing Grace.pro")
        try keeper.keep(plain, of: Self.kept, why: .beforeAChange, at: time())
        try keeper.keep(grace, of: KeptSong(place: "Sunday", library: "Songs", name: "Amazing Grace"), why: .beforeAChange, at: time())
        // A library of the same name in another place is another library.
        try keeper.keep(plain, of: KeptSong(place: "Documents", library: "Songs", name: "Plain Song"), why: .beforeAChange, at: time())
        // A name that is not fit to be a folder's is made fit.
        try keeper.keep(plain, of: KeptSong(place: "Sunday / AM: one", library: "Songs", name: "Plain Song"), why: .beforeAChange, at: time())

        #expect(keeper.songs().map(\.id) == ["Documents/Songs/Plain Song", "Sunday/Songs/Amazing Grace", "Sunday/Songs/Plain Song", "Sunday - AM- one/Songs/Plain Song"])
        #expect(keeper.namesWithCopies(place: "Sunday", library: "Songs") == ["Amazing Grace", "Plain Song"])
        #expect(keeper.namesWithCopies(place: "Documents", library: "Songs") == ["Plain Song"])
        #expect(keeper.namesWithCopies(place: "Sunday", library: "Kids").isEmpty)
        #expect(keeper.size == (try Data(contentsOf: plain)).count * 3 + (try Data(contentsOf: grace)).count)
        // Nothing kept yet is nothing found, and no folder made to look.
        let none = Copies(folder: bench.folder.appendingPathComponent("Nowhere/Backups"))
        #expect(none.songs().isEmpty && none.size == 0 && none.thin() == 0)
        #expect(!FileManager.default.fileExists(atPath: bench.folder.appendingPathComponent("Nowhere").path))
    }

    @Test("When a copy was made, as the list says it")
    func when() {
        let keeper = Copies(folder: URL(fileURLWithPath: "/nowhere"), timeZone: Self.utc)
        let british = Locale(identifier: "en_GB")
        // Now is Sunday 11 October 2026, 09:14.
        #expect(keeper.when(time(seconds: -3600), asOf: time(), locale: british) == "Today 08:14")
        #expect(keeper.when(time(days: -1), asOf: time(), locale: british) == "Saturday 10 Oct")
        #expect(keeper.when(time(days: -6), asOf: time(), locale: british) == "Monday 5 Oct")
        #expect(keeper.when(time(days: -7), asOf: time(), locale: british) == "4 Oct")
        #expect(keeper.when(time(days: -70), asOf: time(), locale: british) == "2 Aug")
        #expect(keeper.when(time(days: -300), asOf: time(), locale: british) == "15 Dec 2025")
    }
}
