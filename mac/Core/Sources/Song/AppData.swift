// What the app keeps of its own, and where. spec/06-library.md, "What the app keeps of
// its own".
//
// On the Mac it is all in one folder, `Pro7ChordEditor` in the person's Application
// Support. Nothing of the app's is ever put in ProPresenter's folders, or beside a song.
//
//   settings.json   the folders chosen by hand, and the place and library last open
//   marks.json      the marks worked out for each song (Marks.swift)
//   log.txt         a line for each thing the app did
//
//   Backups/        the copies of files kept before a change (Copies.swift)
//
// The log is for working out what happened when something has gone wrong. It has the
// names of files in it and NEVER the words of songs, nor the names of chords.
//
// Losing any of these loses nothing of anyone's: the settings are chosen again, the
// marks are worked out again. So a file here that cannot be read is taken as not there,
// and a failure to write one is not an error to trouble the person with.

import Foundation

public struct AppData: Sendable {
    public let folder: URL

    public init(folder: URL) {
        self.folder = folder
    }

    /// The app's own folder on this Mac, made if it is not there.
    public static func onThisMac() -> AppData {
        let support = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first
            ?? FileManager.default.temporaryDirectory
        return AppData(folder: support.appendingPathComponent("Pro7ChordEditor", isDirectory: true))
    }

    public struct Settings: Codable, Equatable, Sendable {
        /// The folders chosen by hand, as paths, in the order they were chosen.
        public var chosenFolders: [String] = []
        /// The place and the library last open, by their ids.
        public var lastPlace: String?
        public var lastLibrary: String?
        /// How many times its usual size the chord sheet is drawn; nothing if it was
        /// never changed.
        public var sheetZoom: Double?
        /// How many times its usual size the ChordPro text is; nothing if it was never
        /// changed.
        public var textZoom: Double?
        /// How much of the height of the two lists at the window's left the libraries
        /// have, from nought to one; nothing if the bar between them was never moved.
        public var listsSplit: Double?

        public init() {}
    }

    private func read<T: Decodable>(_ type: T.Type, from name: String) -> T? {
        guard let bytes = try? Data(contentsOf: folder.appendingPathComponent(name)) else { return nil }
        return try? JSONDecoder().decode(type, from: bytes)
    }

    private func write<T: Encodable>(_ value: T, to name: String) {
        let encoder = JSONEncoder()
        encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
        guard let bytes = try? encoder.encode(value) else { return }
        try? FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        try? bytes.write(to: folder.appendingPathComponent(name), options: .atomic)
    }

    public var settings: Settings {
        get { read(Settings.self, from: "settings.json") ?? Settings() }
        nonmutating set { write(newValue, to: "settings.json") }
    }

    /// The copies kept, which are in `Backups` here. `appVersion` is for their index.
    public func copies(appVersion: String = "") -> Copies {
        Copies(folder: folder.appendingPathComponent("Backups", isDirectory: true), appVersion: appVersion)
    }

    public var marks: KeptMarks {
        get { read(KeptMarks.self, from: "marks.json") ?? KeptMarks() }
        nonmutating set { write(newValue, to: "marks.json") }
    }

    /// Adds a line to the log, with the time before it. When the log has grown past a
    /// megabyte it is put aside as `log-before.txt`, in place of the one put aside
    /// before, and a new one begun.
    public func log(_ line: String, at time: Date = Date()) {
        let file = folder.appendingPathComponent("log.txt")
        try? FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        if let size = (try? FileManager.default.attributesOfItem(atPath: file.path))?[.size] as? Int, size > 1_000_000 {
            let before = folder.appendingPathComponent("log-before.txt")
            try? FileManager.default.removeItem(at: before)
            try? FileManager.default.moveItem(at: file, to: before)
        }
        let stamp = time.formatted(.iso8601.year().month().day().dateSeparator(.dash).time(includingFractionalSeconds: false).timeSeparator(.colon))
        let text = Data("\(stamp)  \(line.replacingOccurrences(of: "\n", with: " "))\n".utf8)
        if let handle = try? FileHandle(forWritingTo: file) {
            defer { try? handle.close() }
            _ = try? handle.seekToEnd()
            try? handle.write(contentsOf: text)
        } else {
            try? text.write(to: file)
        }
    }
}
