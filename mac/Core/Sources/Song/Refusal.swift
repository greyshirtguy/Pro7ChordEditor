// Why a thing was not done, as a sentence for the person.
//
// Everything the song may be asked either is done and saved, or is refused and changes
// nothing: not the file, and not the song in memory. A refusal says why in words that
// can be shown as they are. They say plainly what happened and what, if anything, the
// person can do; they promise nothing (AGENTS.md).
//
// The sentences are listed in spec/05-song-and-saving.md ("What a refusal says"), so
// that the Windows app says the same.

import Foundation

public struct Refusal: Error, Equatable, Sendable, CustomStringConvertible {
    public enum Reason: Equatable, Sendable {
        /// The file could not be read as a presentation.
        case cannotBeRead
        /// The song was opened to be looked at, and was given no way to save.
        case onlyForReading
        /// There is no slide of that number.
        case slideGone
        /// The slide has no text box of that id.
        case textBoxGone
        /// The box has no line of that number.
        case lineGone
        /// What was given is no key.
        case notAKey
        /// The song states no key, so it cannot be shown in another.
        case noKeyToShowAnother
        /// The line asked for is not a line of chords alone: it has words, or is empty.
        case notChordsAlone
        /// The stand-ins a chord with no words hangs on could not be written into the
        /// box's RTF and read back as meant.
        case cannotBeWritten
        /// A blank slide was to be given chords, and the song has no text box with
        /// words to model its own on.
        case noBoxToModelOn
        /// A slide that was to be given a text box for its chords has one by now.
        case noLongerBlank
        case proPresenterIsRunning
        /// The file on disk is no longer the one that was read.
        case changedByAnother
        /// The copy that is kept before a first change could not be made.
        case copyNotKept
        /// The file could not be written, or did not read back as written.
        case notSaved
    }

    public let reason: Reason
    /// The sentence to show.
    public let sentence: String

    public var description: String { sentence }

    init(_ reason: Reason, _ sentence: String) {
        self.reason = reason
        self.sentence = sentence
    }

    static func cannotBeRead(_ name: String, _ why: String) -> Refusal {
        Refusal(.cannotBeRead, "\(name) could not be read as a ProPresenter presentation: \(why)")
    }
    static let onlyForReading = Refusal(.onlyForReading, "This song was opened only to be looked at. Nothing was changed.")
    static let slideGone = Refusal(.slideGone, "That slide is no longer in the song. Nothing was changed.")
    static let textBoxGone = Refusal(.textBoxGone, "That text box is no longer on the slide. Nothing was changed.")
    static let lineGone = Refusal(.lineGone, "The text box has no such line. Nothing was changed.")
    static func notAKey(_ key: String) -> Refusal {
        Refusal(.notAKey, "\u{201C}\(key)\u{201D} is not a key. Nothing was changed.")
    }
    static let noKeyToShowAnother = Refusal(.noKeyToShowAnother,
        "This song does not say what key it is in. Set its key first, and then the key ProPresenter is to show it in.")
    static let notChordsAlone = Refusal(.notChordsAlone, "That is not a line of chords with no words. Nothing was changed.")
    static let cannotBeWritten = Refusal(.cannotBeWritten,
        "A chord could not be added to this line, or taken from it: its text box is written in a way that cannot be changed here safely. Nothing was changed.")
    static let noBoxToModelOn = Refusal(.noBoxToModelOn,
        "This song has no text box with words in it to model one on, so chords cannot be put on a slide that has none. Nothing was changed.")
    static let noLongerBlank = Refusal(.noLongerBlank, "That slide is no longer blank. Nothing was changed.")
    static let proPresenterIsRunning = Refusal(.proPresenterIsRunning, "ProPresenter is running. Quit it to change chords here. Nothing was changed.")
    static let changedByAnother = Refusal(.changedByAnother,
        "This song's file has been changed by something else since it was opened here. Nothing was changed. Open the song again to see it as it is now.")
    static func copyNotKept(_ why: String) -> Refusal {
        Refusal(.copyNotKept, "A copy of the song's file could not be kept first, so the song was not changed: \(why)")
    }
    static func notSaved(_ why: String) -> Refusal {
        Refusal(.notSaved, "The song could not be saved, and is as it was: \(why)")
    }
}
