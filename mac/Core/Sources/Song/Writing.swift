// Writing: a box's chords into the message, and the message into the song's file.
// spec/01-file-format.md, "Chords" (writing a box's chords) and "Writing".
//
// This is the part of the app that can hurt somebody's library, so it does as little as
// can be done and checks what it did:
//
//   - A box's chord entries are replaced and nothing else in the box is touched. Every
//     custom attribute that is not a chord stays, in its order; the chords go after
//     them, in order of place, each with the range the rules give it (Chords.ranges).
//     In every song from Multitracks looked at, that is the order the file has them in
//     already, so a box given the chords it had is, entry for entry, what it was.
//   - The one thing done to an entry that is not a chord: where a line of chords alone
//     is made longer or shorter, the ends of its range go with the words
//     (`moveOtherRanges`), as Simple Presenter moves them.
//   - The file is never half written. The whole message is written to a new file beside
//     the song's own, pushed out of the system's keeping onto the disk, and put in the
//     place of the old one in one step. Then it is read back, and must be byte for byte
//     what was meant.
//
// The RTF of a box is not written here. The one case in which it is written at all,
// the stand-ins of chords that have no words, is OpenSong's to ask and RTFWords' to do
// (spec/01-file-format.md, "Chords with no words"): bytes changed in place, and read
// back before anything is saved.

import ChordRules
import Foundation
import ProFormat

/// Replaces a box's chord entries with these chords.
func write(chords: [Chord], into text: inout RVData_Graphics.Text) {
    var entries = text.attributes.customAttributes.filter { attribute in
        if case .chord? = attribute.attribute { return false }
        return true
    }
    for range in Chords.ranges(readWords(text), chords) {
        var entry = RVData_Graphics.Text.Attributes.CustomAttribute()
        entry.range.start = Int32(range.start)
        entry.range.end = Int32(range.end)
        entry.chord = range.name
        entries.append(entry)
    }
    text.attributes.customAttributes = entries
}

/// What a box says of a range of its words, and is not a chord, goes with the words
/// when a line of chords alone in it is made longer or shorter: `lineStart` is where
/// that line began, `was` how long it was and `now` how long it is.
///
/// A text box has custom attributes that are no chords: "these are in capitals", a
/// font's first size, a gradient. Most boxes that hold a line of chords alone, as
/// Multitracks makes them, say of all their words that they are in capitals. Left as it
/// was, that range would stop short of a stand-in put in, and with a chord taken off
/// would end past the last of the words. So each end of each such range is moved as
/// RTFWords.placeAfter says, which is what Simple Presenter does and A GUESS at what
/// ProPresenter does (decided by the maintainer on 2026-10-13: "let's copy simple
/// presenter for now").
///
/// Nothing else of such an entry changes, none is added, taken away or put in another
/// place in the list, and one that has no range is not given one. An end that is where
/// it was is not written again. (The chords' own ranges are written afresh by
/// `write(chords:into:)`, after this.)
func moveOtherRanges(of text: inout RVData_Graphics.Text, lineStart: Int, was: Int, now: Int) {
    guard was != now else { return }
    for index in text.attributes.customAttributes.indices {
        let entry = text.attributes.customAttributes[index]
        if case .chord? = entry.attribute { continue }
        guard entry.hasRange else { continue }
        let start = RTFWords.placeAfter(Int(entry.range.start), lineStart: lineStart, was: was, now: now)
        let end = RTFWords.placeAfter(Int(entry.range.end), lineStart: lineStart, was: was, now: now)
        if start != Int(entry.range.start) {
            text.attributes.customAttributes[index].range.start = Int32(start)
        }
        if end != Int(entry.range.end) {
            text.attributes.customAttributes[index].range.end = Int32(end)
        }
    }
}

/// A song's file on disk.
enum SongFile {
    /// What tells one state of a file from another without reading it: its size and its
    /// time of last change, to the nanosecond the file system keeps.
    struct Stamp: Equatable {
        var size: Int64
        var seconds: Int
        var nanoseconds: Int
    }

    struct Failure: Error, CustomStringConvertible {
        var description: String
    }

    static func stamp(of url: URL) throws -> Stamp {
        var info = stat()
        let found = url.withUnsafeFileSystemRepresentation { path in
            path.map { stat($0, &info) } ?? -1
        }
        guard found == 0 else {
            throw Failure(description: String(cString: strerror(errno)))
        }
        return Stamp(size: Int64(info.st_size), seconds: info.st_mtimespec.tv_sec, nanoseconds: info.st_mtimespec.tv_nsec)
    }

    /// Puts these bytes in the place of the file, whole or not at all.
    static func write(_ data: Data, over url: URL) throws {
        // Beside the song, so that the swap is within one disk and is one step. The name
        // starts with a dot so that it is not seen for the moment it is there, and no
        // two saves can pick the same one.
        let beside = url.deletingLastPathComponent()
            .appendingPathComponent(".\(url.lastPathComponent).\(UUID().uuidString).saving")
        guard FileManager.default.createFile(atPath: beside.path, contents: nil) else {
            throw Failure(description: "a file could not be made in the song's folder.")
        }
        do {
            let handle = try FileHandle(forWritingTo: beside)
            try handle.write(contentsOf: data)
            // Onto the disk itself, before it takes the song's place: after a power cut
            // the song is then the old one or the new one, and never an empty file.
            try handle.synchronize()
            try handle.close()
            // (This keeps the old file's permissions and its date of making.)
            _ = try FileManager.default.replaceItemAt(url, withItemAt: beside)
        } catch {
            // What was being written is of no use now. The song's own file was not touched.
            try? FileManager.default.removeItem(at: beside)
            throw Failure(description: error.localizedDescription)
        }
        guard let back = try? Data(contentsOf: url), back == data else {
            throw Failure(description: "the file did not read back as it was written.")
        }
    }
}
