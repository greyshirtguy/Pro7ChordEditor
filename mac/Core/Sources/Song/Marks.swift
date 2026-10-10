// The marks beside a song's name in the list: what can be said of the song at a glance.
// spec/06-library.md, rules 9 to 11.
//
// So far two of the four the page names: that it has chords, and that it is one of the
// old Windows editor's "(Chords)" copies. That copies of it are kept is milestone M4,
// and that something in it wants mending is M8.
//
// A mark needs the file to be read, which for a big library takes a while. So the names
// are listed at once, the marks are worked out afterwards and off the thread that draws,
// and they are kept (by AppData) under the file's path, with its size and time of last
// change: the next start has them at once, and a file that has changed since is read
// again.
//
// Reading a file for its marks is only reading. `read` opens nothing for change.

import Foundation
import ProFormat

public struct Marks: Codable, Hashable, Sendable {
    /// How many chords the song has, on the slides and boxes the editors would show.
    public var chords: Int
    /// Whether its name ends " (Chords)", as the old Windows editor named the copies it
    /// saved beside the songs it was given (spec/08-repair.md, D).
    public var isOldEditorsCopy: Bool
    /// Whether the file could not be read as a presentation.
    public var cannotBeRead: Bool

    public var hasChords: Bool { chords > 0 }

    public init(chords: Int = 0, isOldEditorsCopy: Bool = false, cannotBeRead: Bool = false) {
        self.chords = chords
        self.isOldEditorsCopy = isOldEditorsCopy
        self.cannotBeRead = cannotBeRead
    }

    /// Reads a song's file and says what its marks are.
    public static func read(_ file: ListedSong) -> Marks {
        var marks = Marks(isOldEditorsCopy: file.name.hasSuffix(" (Chords)"))
        guard let bytes = try? Data(contentsOf: file.url), let presentation = try? RVData_Presentation(serializedBytes: bytes) else {
            marks.cannotBeRead = true
            return marks
        }
        for row in slideRows(of: presentation) {
            for element in slide(of: row, in: presentation).elements where showsItsOwnWords(element) {
                marks.chords += readChords(element.element.text).count
            }
        }
        return marks
    }
}

/// The marks worked out before, each with the size and time its file had then.
public struct KeptMarks: Codable, Equatable, Sendable {
    struct Entry: Codable, Equatable, Sendable {
        var size: Int
        var modified: Date
        var marks: Marks
    }

    var entries: [String: Entry] = [:]

    public init() {}

    /// What was worked out for this file, if the file is still as it was then.
    public func marks(for file: ListedSong) -> Marks? {
        guard let entry = entries[file.id], entry.size == file.size, entry.modified == file.modified else { return nil }
        return entry.marks
    }

    public mutating func keep(_ marks: Marks, for file: ListedSong) {
        entries[file.id] = Entry(size: file.size, modified: file.modified, marks: marks)
    }

    public var count: Int { entries.count }
}

/// What the list can be narrowed to (rule 11).
public enum MarkFilter: String, CaseIterable, Sendable {
    case all
    case withChords
    case withoutChords

    /// Whether a song with these marks is let through. One whose marks are not worked
    /// out yet is let through only by `all`.
    public func lets(_ marks: Marks?) -> Bool {
        switch self {
        case .all: return true
        case .withChords: return marks?.hasChords == true
        case .withoutChords: return marks.map { !$0.hasChords && !$0.cannotBeRead } ?? false
        }
    }
}
