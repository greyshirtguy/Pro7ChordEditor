// A place for a test to change songs in: a folder of its own under the system's
// temporary folder, holding copies of files from testdata/files, and safeguards that
// the test can set.
//
// Tests never run on a real ProPresenter library, and never change testdata/ itself
// (AGENTS.md, "The person's files"). Every test that saves anything saves it here.

import Foundation
import ProFormat
import Song
import TestData

final class Workbench {
    static let prefix = "Pro7ChordEditorTests-"

    let folder: URL
    /// What the safeguards answer. A test sets these to try a refusal.
    var proPresenterIsRunning = false
    var copyCannotBeKept = false
    /// Every copy the song asked to have kept: the file's bytes at that moment, and why.
    private(set) var copiesKept: [Data] = []
    private(set) var reasons: [CopyReason] = []

    init() throws {
        folder = FileManager.default.temporaryDirectory.appendingPathComponent(Self.prefix + UUID().uuidString, isDirectory: true)
        try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
    }

    deinit {
        // Only ever the folder made above, and only if it is named as one of these.
        if folder.lastPathComponent.hasPrefix(Self.prefix) {
            try? FileManager.default.removeItem(at: folder)
        }
    }

    struct NoCopy: Error, LocalizedError {
        var errorDescription: String? { "The test said no copy could be kept." }
    }

    var safeguards: Safeguards {
        Safeguards(
            proPresenterIsRunning: { [unowned self] in proPresenterIsRunning },
            keepCopy: { [unowned self] url, why in
                if copyCannotBeKept { throw NoCopy() }
                copiesKept.append(try Data(contentsOf: url))
                reasons.append(why)
            })
    }

    /// A copy of a file of testdata/files, by its path from there, in this folder.
    func copy(of path: String) throws -> URL {
        try put(TestData.file(path), named: String(path.split(separator: "/").last!))
    }

    /// A file of these bytes in this folder.
    func put(_ bytes: Data, named name: String) throws -> URL {
        let url = folder.appendingPathComponent(name)
        try bytes.write(to: url)
        return url
    }

    /// A copy of a test file, opened so that it can be changed.
    func open(_ path: String) throws -> OpenSong {
        try OpenSong(opening: copy(of: path), safeguards: safeguards)
    }

    /// The names of everything in the folder: to see that a save leaves nothing behind.
    func names() throws -> [String] {
        try FileManager.default.contentsOfDirectory(atPath: folder.path).sorted()
    }
}

/// The message a file holds.
func message(at url: URL) throws -> RVData_Presentation {
    try RVData_Presentation(serializedBytes: Data(contentsOf: url))
}

typealias Entry = RVData_Graphics.Text.Attributes.CustomAttribute

func isChord(_ entry: Entry) -> Bool {
    if case .chord? = entry.attribute { return true }
    return false
}

/// Where a text box is in a message, by its id.
struct BoxPlace {
    var cue: Int, action: Int, element: Int
}

func place(of element: String, in presentation: RVData_Presentation) -> BoxPlace? {
    for (c, cue) in presentation.cues.enumerated() {
        for (a, action) in cue.actions.enumerated() {
            for (e, candidate) in action.slide.presentation.baseSlide.elements.enumerated() where candidate.element.uuid.string == element {
                return BoxPlace(cue: c, action: a, element: e)
            }
        }
    }
    return nil
}

func entries(at place: BoxPlace, in presentation: RVData_Presentation) -> [Entry] {
    presentation.cues[place.cue].actions[place.action].slide.presentation.baseSlide.elements[place.element].element.text.attributes.customAttributes
}

func setEntries(_ entries: [Entry], at place: BoxPlace, in presentation: inout RVData_Presentation) {
    presentation.cues[place.cue].actions[place.action].slide.presentation.baseSlide.elements[place.element].element.text.attributes.customAttributes = entries
}

/// A chord entry as "name start-end", to compare and to read in a failure.
func spell(_ entries: [Entry]) -> [String] {
    entries.filter(isChord).map { "\($0.chord) \($0.range.start)-\($0.range.end)" }
}
