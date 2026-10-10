// The copies kept: before the app changes a file it keeps a copy of it as it was, out
// of sight, and can put any copy back. That is what makes saving every change at once a
// safe thing to do to somebody's Sunday library. spec/07-backups.md.
//
// NEW: Simple Presenter keeps a copy the first time its editor changes a file and has no
// way to look at them or put one back. This is that idea finished.
//
// Where. In the app's own folder (AppData), under `Backups`, in folders that follow
// where the file was: the place, then the library, then the file's name without its
// ending. So the copies of `Songs/Amazing Grace.pro` in the workspace "Sunday" are in
// `Backups/Sunday/Songs/Amazing Grace/`. (The page was written before there could be
// more than one place, and had no place in the path. Two places can each have a library
// called "Default", so the place is there now, and the page says so.) A song is known by
// where it is: renamed or moved in ProPresenter it starts afresh, and its earlier copies
// stay under its old name, where All Copies finds them.
//
// What. A copy is the file's bytes, unchanged. It is named for when it was made, to the
// second, in the computer's own time: `2026-10-11 09.14.03.pro`. Beside the copies is
// `index.json`, with an entry for each: why it was made, the whole path of the file it
// is a copy of, the song's id and name as they were inside it, how many chords it had,
// its key, its size, and the app's version.
//
// The index is a help, not the truth. If it is lost or damaged the copies are still
// there, named by their time, and are listed from the folder; what the index would have
// said of one is then read from the copy itself, or not known.
//
// What is thrown away, and how carefully. Only `thin` deletes anything, only inside one
// song's folder of copies, and only files that are named as copies are. The oldest copy
// of a file, the file as it was before this app ever touched it, is NEVER thrown away.

import Foundation
import ProFormat

/// Why a copy was made.
public enum CopyReason: String, Codable, Sendable {
    case beforeAChange = "before a change"
    case beforePuttingBack = "before putting back a copy"
    case beforeMending = "before mending"
}

/// A song that has copies kept, known by where it is: its place, its library and its
/// file's name without the ending.
public struct KeptSong: Hashable, Sendable, Identifiable {
    public var place: String
    public var library: String
    public var name: String

    public var id: String { "\(place)/\(library)/\(name)" }

    public init(place: String, library: String, name: String) {
        self.place = place
        self.library = library
        self.name = name
    }
}

/// One copy of a song's file.
public struct KeptCopy: Hashable, Sendable, Identifiable {
    /// Where the copy is.
    public var url: URL
    /// When it was made.
    public var made: Date
    /// Why. Not known if the index is lost.
    public var why: CopyReason?
    /// The whole path of the file it is a copy of. Empty if the index is lost.
    public var copyOf: String
    /// The song's id and name as they were inside it, how many chords it had, and its key.
    public var songID: String
    public var songName: String
    public var chords: Int
    public var key: String
    public var size: Int
    /// Whether it is the oldest copy there is of its file: the one never thrown away.
    public var isTheFirst = false

    public var id: String { url.lastPathComponent }
}

public struct Copies: Sendable {
    /// The `Backups` folder.
    public let folder: URL
    /// The app's version, for the index.
    public var appVersion: String
    /// The computer's own time zone. A test may give another.
    public var timeZone: TimeZone

    public init(folder: URL, appVersion: String = "", timeZone: TimeZone = .current) {
        self.folder = folder
        self.appVersion = appVersion
        self.timeZone = timeZone
    }

    struct CannotBeKept: Error, LocalizedError {
        var why: String
        var errorDescription: String? { why }
    }

    /// What the index says of one copy.
    struct Entry: Codable {
        var file: String
        var made: Date
        var why: CopyReason
        var copyOf: String
        var songID: String
        var songName: String
        var chords: Int
        var key: String
        var size: Int
        var appVersion: String
    }

    // ---- Where

    /// A name made fit to be one folder's name. (A library is a folder already, so its
    /// name is fit; a workspace's name is whatever someone typed.)
    static func folderName(_ name: String) -> String {
        let fit = name.map { "/:\\".contains($0) || $0.isNewline ? "-" : String($0) }.joined()
        return fit.isEmpty || fit == "." || fit == ".." ? "-" : fit
    }

    /// The folder a song's copies are kept in.
    public func folder(of song: KeptSong) -> URL {
        folder.appendingPathComponent(Self.folderName(song.place), isDirectory: true)
            .appendingPathComponent(Self.folderName(song.library), isDirectory: true)
            .appendingPathComponent(Self.folderName(song.name), isDirectory: true)
    }

    private func nameFormat() -> DateFormatter {
        let format = DateFormatter()
        format.locale = Locale(identifier: "en_US_POSIX")
        format.timeZone = timeZone
        format.dateFormat = "yyyy-MM-dd HH.mm.ss"
        return format
    }

    /// When a copy of this name was made, if it is named as a copy is:
    /// "2026-10-11 09.14.03.pro", or with " 2" before the ending for a second copy made
    /// in the same second.
    func time(ofCopyNamed name: String) -> Date? {
        timeAndTurn(ofCopyNamed: name)?.time
    }

    /// The same, and which copy of its second it is: 1 for the first, 2 for " 2".
    private func timeAndTurn(ofCopyNamed name: String) -> (time: Date, turn: Int)? {
        guard name.hasSuffix(".pro") else { return nil }
        var stem = String(name.dropLast(4))
        var turn = 1
        if let space = stem.lastIndex(of: " "), stem.distance(from: stem.startIndex, to: space) == 19,
           let again = Int(stem[stem.index(after: space)...]), again >= 2 {
            stem = String(stem[..<space])
            turn = again
        }
        guard stem.count == 19, let time = nameFormat().date(from: stem) else { return nil }
        return (time, turn)
    }

    // ---- Keeping

    /// Keeps a copy of a file as it is on disk now. Answers with the copy, or with
    /// nothing if the newest copy kept of that file is already byte for byte the same,
    /// in which case no second one is made. Throws if the copy cannot be made: the
    /// change that wanted it is then refused.
    @discardableResult
    public func keep(_ file: URL, of song: KeptSong, why: CopyReason, at time: Date = Date()) throws -> KeptCopy? {
        let bytes: Data
        do {
            bytes = try Data(contentsOf: file)
        } catch {
            throw CannotBeKept(why: error.localizedDescription)
        }
        let home = folder(of: song)
        if let newest = copies(of: song).first, (try? Data(contentsOf: newest.url)) == bytes {
            return nil
        }
        do {
            try FileManager.default.createDirectory(at: home, withIntermediateDirectories: true)
            // Named for the second it was made. Another of the same second, which can
            // only be other bytes, has " 2" after the time, and so on.
            let stem = nameFormat().string(from: time)
            var name = stem + ".pro"
            var again = 2
            while FileManager.default.fileExists(atPath: home.appendingPathComponent(name).path) {
                name = "\(stem) \(again).pro"
                again += 1
            }
            let url = home.appendingPathComponent(name)
            // The file's bytes, unchanged: not read as a song and written again. Never
            // over a copy that is there. One that did not come out whole is no copy, and
            // is cleared away: it is the file made on the line before, and no other.
            try bytes.write(to: url, options: .withoutOverwriting)
            guard (try? Data(contentsOf: url)) == bytes else {
                try? FileManager.default.removeItem(at: url)
                throw CannotBeKept(why: "the copy did not read back as it was written.")
            }
            var entry = Entry(file: name, made: time, why: why, copyOf: file.standardizedFileURL.path, songID: "", songName: "",
                              chords: 0, key: "", size: bytes.count, appVersion: appVersion)
            Self.describe(bytes, into: &entry)
            // (The index is a help. If it cannot be written, the copy is still kept.)
            var entries = index(in: home)
            entries.append(entry)
            writeIndex(entries, in: home)
            return copies(of: song).first { $0.id == name }
        } catch let error as CannotBeKept {
            throw error
        } catch {
            throw CannotBeKept(why: error.localizedDescription)
        }
    }

    /// What can be said of a song from its file's bytes, for the index.
    private static func describe(_ bytes: Data, into entry: inout Entry) {
        guard let presentation = try? RVData_Presentation(serializedBytes: bytes) else { return }
        entry.songID = presentation.uuid.string
        entry.songName = presentation.name
        entry.key = presentation.hasMusic ? keyName(presentation.music.original) : ""
        entry.chords = 0
        for row in slideRows(of: presentation) {
            for element in slide(of: row, in: presentation).elements where showsItsOwnWords(element) {
                entry.chords += readChords(element.element.text).count
            }
        }
    }

    private func index(in home: URL) -> [Entry] {
        guard let bytes = try? Data(contentsOf: home.appendingPathComponent("index.json")) else { return [] }
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        return (try? decoder.decode([Entry].self, from: bytes)) ?? []
    }

    private func writeIndex(_ entries: [Entry], in home: URL) {
        let encoder = JSONEncoder()
        encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
        encoder.dateEncodingStrategy = .iso8601
        guard let bytes = try? encoder.encode(entries) else { return }
        try? bytes.write(to: home.appendingPathComponent("index.json"), options: .atomic)
    }

    // ---- Listing

    /// The copies kept of a song, the newest first. Listed from the folder: every file
    /// in it that is named as a copy is one, whether or not the index knows of it.
    public func copies(of song: KeptSong) -> [KeptCopy] {
        let home = folder(of: song)
        let names = (try? FileManager.default.contentsOfDirectory(atPath: home.path)) ?? []
        let entries = Dictionary(index(in: home).map { ($0.file, $0) }, uniquingKeysWith: { _, later in later })
        var copies: [KeptCopy] = names.compactMap { name in
            guard let made = time(ofCopyNamed: name) else { return nil }
            let url = home.appendingPathComponent(name)
            if let entry = entries[name] {
                return KeptCopy(url: url, made: entry.made, why: entry.why, copyOf: entry.copyOf, songID: entry.songID, songName: entry.songName,
                                chords: entry.chords, key: entry.key, size: entry.size)
            }
            // Not in the index: what can be read from the copy itself, and no more.
            var entry = Entry(file: name, made: made, why: .beforeAChange, copyOf: "", songID: "", songName: "", chords: 0, key: "", size: 0, appVersion: "")
            if let bytes = try? Data(contentsOf: url) {
                entry.size = bytes.count
                Self.describe(bytes, into: &entry)
            }
            return KeptCopy(url: url, made: made, why: nil, copyOf: "", songID: entry.songID, songName: entry.songName,
                            chords: entry.chords, key: entry.key, size: entry.size)
        }
        // The newest first; of those made in one second, the later named first.
        copies.sort { a, b in
            if a.made != b.made { return a.made > b.made }
            return (timeAndTurn(ofCopyNamed: a.id)?.turn ?? 0) > (timeAndTurn(ofCopyNamed: b.id)?.turn ?? 0)
        }
        if !copies.isEmpty {
            copies[copies.count - 1].isTheFirst = true
        }
        return copies
    }

    private static func folders(in folder: URL) -> [URL] {
        ((try? FileManager.default.contentsOfDirectory(at: folder, includingPropertiesForKeys: [.isDirectoryKey], options: [.skipsHiddenFiles])) ?? [])
            .filter { (try? $0.resourceValues(forKeys: [.isDirectoryKey]).isDirectory) == true }
            .sorted { $0.lastPathComponent.localizedStandardCompare($1.lastPathComponent) == .orderedAscending }
    }

    /// Every song that has copies, by place and library: those whose song is no longer
    /// where it was included.
    public func songs() -> [KeptSong] {
        var found: [KeptSong] = []
        for place in Self.folders(in: folder) {
            for library in Self.folders(in: place) {
                for song in Self.folders(in: library) {
                    let kept = KeptSong(place: place.lastPathComponent, library: library.lastPathComponent, name: song.lastPathComponent)
                    if !copies(of: kept).isEmpty {
                        found.append(kept)
                    }
                }
            }
        }
        return found
    }

    /// The names of the songs of one library that have copies: for the mark in the list.
    public func namesWithCopies(place: String, library: String) -> Set<String> {
        let home = folder.appendingPathComponent(Self.folderName(place), isDirectory: true).appendingPathComponent(Self.folderName(library), isDirectory: true)
        return Set(Self.folders(in: home).map(\.lastPathComponent).filter {
            !copies(of: KeptSong(place: place, library: library, name: $0)).isEmpty
        })
    }

    /// How much room all the copies take, in bytes.
    public var size: Int {
        songs().reduce(0) { total, song in total + copies(of: song).reduce(0) { $0 + $1.size } }
    }

    // ---- How long

    /// Throws away the copies that are kept no longer. Of each song's copies: the oldest
    /// is never thrown away; everything from the last thirty days is kept; and the
    /// twenty newest, whatever their age. The rest go. Done when the app starts.
    @discardableResult
    public func thin(asOf now: Date = Date()) -> Int {
        var thrownAway = 0
        for song in songs() {
            let all = copies(of: song)
            let recent = now.addingTimeInterval(-30 * 24 * 60 * 60)
            let going = all.enumerated().filter { n, copy in
                !copy.isTheFirst && n >= 20 && copy.made < recent
            }.map(\.element)
            guard !going.isEmpty else { continue }
            let home = folder(of: song)
            for copy in going {
                // Only a file in this song's folder of copies, and named as a copy is.
                guard copy.url.deletingLastPathComponent().standardizedFileURL == home.standardizedFileURL,
                      time(ofCopyNamed: copy.url.lastPathComponent) != nil else { continue }
                if (try? FileManager.default.removeItem(at: copy.url)) != nil {
                    thrownAway += 1
                }
            }
            let left = Set(copies(of: song).map(\.id))
            writeIndex(index(in: home).filter { left.contains($0.file) }, in: home)
        }
        return thrownAway
    }

    // ---- Saying when

    /// When a copy was made, as the list of earlier versions says it: "Today 09:14",
    /// "Sunday 4 Oct" for the six days before, "12 Sep" earlier in the year, and
    /// "2 Aug 2025" before that.
    public func when(_ made: Date, asOf now: Date = Date(), locale: Locale = .current) -> String {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = timeZone
        func text(_ format: String) -> String {
            let formatter = DateFormatter()
            formatter.locale = locale
            formatter.timeZone = timeZone
            formatter.dateFormat = format
            return formatter.string(from: made)
        }
        let days = calendar.dateComponents([.day], from: calendar.startOfDay(for: made), to: calendar.startOfDay(for: now)).day ?? 0
        if days == 0 { return "Today " + text("HH:mm") }
        if days > 0 && days < 7 { return text("EEEE d MMM") }
        if calendar.component(.year, from: made) == calendar.component(.year, from: now) { return text("d MMM") }
        return text("d MMM yyyy")
    }
}
