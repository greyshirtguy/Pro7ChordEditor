// The song that is open: what it hands the editors, what they may ask of it, and undo.
// spec/05-song-and-saving.md is this file's specification, rule for rule; the reference
// is `PresentationEditor` in reference/simple-presenter/src/presentationeditor.cpp
// (`song`, `usedChords`, `setChords`, `setChordsAlone`, `key`, `setKey`, `change`,
// `undo`, `redo`), which also edits slides, props and stage layouts, none of which is
// wanted here.
//
// How a change goes. Everything that may be asked comes down to `save`: a new state of
// the whole message is made beside the one in memory, the safeguards are asked, the
// file is written, and only when it has read back as written does the new state take
// the old one's place. So a refusal, or a failure at any point, leaves the song in
// memory exactly as it was, with nothing to put back.
//
// Where this parts from the reference, each said at the place:
//
//   - The key is a step of undo like any other (rule 8 and after). There it is not.
//   - Setting the key leaves the key ProPresenter shows the song in alone, if someone
//     had chosen another (rule 6). There the two are always set together.
//   - There is a second request, for the key ProPresenter shows it in (rule 7).
//   - A save is refused while ProPresenter is running, and when the file on disk is no
//     longer the one that was read (rule 10). There neither is asked.
//
// Not safe to use from two threads at once, and not meant to be: the app keeps a song
// where its window is.

import ChordRules
import Foundation
import ProFormat

public final class OpenSong {
    /// How many steps of undo are kept, as the reference keeps.
    static let undoLimit = 200

    public let url: URL

    private var presentation: RVData_Presentation
    /// The file's bytes as they were last read from it or written to it, and what the
    /// file system said of it then. A save is refused if the disk no longer agrees.
    private var bytesOnDisk: Data
    private var stamp: SongFile.Stamp
    /// The slides. They do not change while the song is open, but when a copy is put
    /// back: nothing else here adds a slide, takes one away or moves one.
    private var rows: [SlideRow]
    private let safeguards: Safeguards?
    /// Whether the copy that is kept before a first change has been kept.
    private var copyKept = false
    private var undoSteps: [Step] = []
    private var redoSteps: [Step] = []

    /// The part of the file a change changed, as it was before and as it was after
    /// (rule 13). A change to chords is within one cue; a change of key is `music`,
    /// which a song may not have at all.
    private enum Unit {
        case cue(index: Int, before: RVData_Cue, after: RVData_Cue)
        case music(before: RVData_Presentation.Music?, after: RVData_Presentation.Music?)
    }

    private struct Step {
        var unit: Unit
        /// One step of undo with the change before it.
        var joined: Bool
    }

    /// Opens a song's file. With no safeguards it can be read and nothing else: every
    /// request to change it is refused.
    public init(opening url: URL, safeguards: Safeguards? = nil) throws {
        let bytes: Data
        do {
            bytes = try Data(contentsOf: url)
            self.stamp = try SongFile.stamp(of: url)
        } catch {
            // (The system says why in its own words: there is no such file, or it may
            // not be read.)
            throw Refusal.cannotBeRead(url.lastPathComponent, error.localizedDescription)
        }
        do {
            self.presentation = try RVData_Presentation(serializedBytes: bytes)
        } catch {
            throw Refusal.cannotBeRead(url.lastPathComponent, "it is not a presentation, or it is damaged.")
        }
        self.url = url
        self.bytesOnDisk = bytes
        self.rows = slideRows(of: presentation)
        self.safeguards = safeguards
    }

    // ---- What the editors are handed

    /// The song's name, and its id, as they are inside the file.
    public var name: String { presentation.name }
    public var id: String { presentation.uuid.string }

    /// One entry for each text box that holds the song's words, in the order of the
    /// slides and, on a slide, of its boxes.
    public var textBoxes: [TextBox] {
        readTextBoxes(of: presentation, rows: rows)
    }

    /// Every chord's name in song order, one for each use. It is what the typing
    /// bubble's offers go by.
    public var usedChords: [String] {
        textBoxes.flatMap { $0.chords.map(\.name) }
    }

    /// How many slides the song has, in the order they are shown.
    public var slideCount: Int { rows.count }

    /// The key the chords in the file are written in, as a musician writes it, or ""
    /// if the song states none.
    public var key: String {
        presentation.hasMusic ? keyName(presentation.music.original) : ""
    }

    /// The key ProPresenter is to show the chords in, or "" if the song states no key.
    /// The editors never draw chords in it: they show what is in the file.
    public var shownKey: String {
        presentation.hasMusic ? keyName(presentation.music.user) : ""
    }

    public var canUndo: Bool { !undoSteps.isEmpty }
    public var canRedo: Bool { !redoSteps.isEmpty }

    /// The file's bytes as they were last read from it or written to it: what the song
    /// is, as a file. (What Revert remembers of a song when it is opened, and holds
    /// the song against afterwards.)
    public var bytes: Data { bytesOnDisk }

    /// Whether the file on disk is still the one that was last read here or written
    /// from here: in its size, its time of last change and its bytes. If it is not,
    /// something else has changed it, and nothing will be saved over it.
    public var fileIsAsLastRead: Bool {
        guard let now = try? SongFile.stamp(of: url), now == stamp, let onDisk = try? Data(contentsOf: url) else { return false }
        return onDisk == bytesOnDisk
    }

    // ---- What the editors may ask
    //
    // Each changes the song and saves it at once, or throws a Refusal and changes
    // nothing. `joined` makes it one step of undo with the change before it. A request
    // that would change nothing saves nothing and is no step of undo.

    /// "This text box now has these chords": the whole list, each place counted along
    /// the whole box.
    public func setChords(slide: Int, element: String, _ chords: [Chord], joined: Bool = false) throws {
        try changeText(slide: slide, element: element, joined: joined) { text in
            write(chords: chords.map { Chord(at: $0.at, name: Chords.tidied($0.name)) }, into: &text)
        }
    }

    /// "This line of chords alone now has these": the names in order.
    ///
    /// SO FAR only where the line has as many stand-ins as there are names: its chords
    /// can be renamed and put in another order. One more or one fewer means writing the
    /// line's stand-ins into the box's RTF, which is milestone M7, as is the first chord
    /// on a blank slide (and the text box it needs, if the slide has none); until then
    /// both are refused.
    public func setChordsAlone(slide: Int, element: String, line: Int, names: [String], joined: Bool = false) throws {
        guard rows.indices.contains(slide) else { throw Refusal.slideGone }
        // A blank slide: its box, or no box at all (an empty `element`). No chords for
        // one that has none is nothing to do, and the box is left exactly as it is.
        if let blank = readTextBoxes(ofSlide: slide, of: presentation, rows: rows).first, blank.blank, blank.element == element {
            if names.isEmpty { return }
            throw Refusal.notBuiltYet
        }
        try changeText(slide: slide, element: element, joined: joined) { text in
            let lines = readWords(text).lines
            guard lines.indices.contains(line) else { throw Refusal.lineGone }
            let old = lines[line]
            // The stand-in the line has now: a plain space if it has spaces and no
            // zero-width space, else a zero-width space.
            let spaces = !old.contains(0x200B) && old.contains(0x0020)
            guard Chords.placeholders(names.count, standIn: spaces ? 0x0020 : 0x200B) == old else {
                throw Refusal.notBuiltYet
            }
            // The chords of the other lines keep their places. This line's go on its
            // stand-ins, every other place.
            let start = lines[..<line].reduce(0) { $0 + $1.length + 1 }
            var kept = readChords(text).filter { $0.at < start || $0.at > start + old.length }
            for (n, name) in names.enumerated() {
                kept.append(Chord(at: start + n * 2, name: Chords.tidied(name)))
            }
            write(chords: kept, into: &text)
        }
    }

    /// Sets the key the chords are written in. It changes no chord: it says what key
    /// the chords that are there are in.
    ///
    /// The key ProPresenter shows the song in is set to the same, IF the song had no
    /// key or its two keys were the same. If they differed, someone chose to show the
    /// song in another key, and that is left alone. (A song whose `music` has no `user`
    /// part is taken as one whose two keys are the same: nobody chose another. That is
    /// a GUESS at what ProPresenter means by leaving it out; no file to hand does.)
    public func setKey(_ key: String, joined: Bool = false) throws {
        let number = Chords.keyNumber(key)
        guard number >= 0 else { throw Refusal.notAKey(key) }
        if Array(key.utf16) == Array(self.key.utf16) { return }
        try changeMusic(joined: joined) { music, hadMusic in
            let together = !hadMusic || !music.hasUser || music.user == music.original
            Self.set(&music.original, to: number, minor: Chords.keyIsMinor(key))
            if together {
                Self.set(&music.user, to: number, minor: Chords.keyIsMinor(key))
            }
        }
    }

    /// NEW (rule 7): sets the key ProPresenter is to show the song in, and that alone.
    /// A song that states no key cannot be shown in another, and that is refused: a
    /// `music` made with only this part in it would read as a song in A flat.
    public func setShownKey(_ key: String, joined: Bool = false) throws {
        let number = Chords.keyNumber(key)
        guard number >= 0 else { throw Refusal.notAKey(key) }
        guard presentation.hasMusic else { throw Refusal.noKeyToShowAnother }
        if Array(key.utf16) == Array(shownKey.utf16) { return }
        try changeMusic(joined: joined) { music, _ in
            Self.set(&music.user, to: number, minor: Chords.keyIsMinor(key))
        }
    }

    /// Puts the last change back as it was before, and saves. A step that was joined to
    /// the one before it is undone with it, in one go.
    public func undo() throws {
        try restore(forwards: false)
    }

    /// Does again what was last undone.
    public func redo() throws {
        try restore(forwards: true)
    }

    /// "Put This One Back" (spec/07-backups.md, rules 19 to 22): makes the song's file
    /// these bytes again, which are a copy of it kept earlier.
    ///
    /// It is a save, and so is refused while ProPresenter is running and unless the
    /// file on disk is the one last read. First the file as it is now is copied, so that
    /// putting back can itself be taken back: undoing it is only putting back that
    /// copy, and there is no second mechanism. Then the bytes are written beside the
    /// file and swapped in, exactly as they are and not as a song read and written
    /// again, and read back. The song then starts afresh from the file: its history of
    /// undo is empty, and what was put back is where it starts from.
    ///
    /// Bytes that are what the file is already are nothing to do.
    public func putBack(_ bytes: Data) throws {
        guard bytes != bytesOnDisk else { return }
        let safeguards = try maySave()
        // What is put back must be a song: a copy that cannot be read is not swapped in.
        guard let copy = try? RVData_Presentation(serializedBytes: bytes) else {
            throw Refusal.cannotBeRead("That copy", "it is not a presentation, or it is damaged.")
        }
        do {
            try safeguards.keepCopy(url, .beforePuttingBack)
        } catch {
            throw Refusal.copyNotKept("\(error.localizedDescription)")
        }
        do {
            try SongFile.write(bytes, over: url)
        } catch {
            throw Refusal.notSaved("\(error)")
        }
        presentation = copy
        bytesOnDisk = bytes
        stamp = (try? SongFile.stamp(of: url)) ?? stamp
        rows = slideRows(of: copy)
        undoSteps.removeAll()
        redoSteps.removeAll()
        // (A new sitting with the song: its next change asks for a copy again, and none
        // is made if the newest kept is already what the file is.)
        copyKept = false
    }

    // ---- How a change is made

    private static func set(_ key: inout RVData_MusicKeyScale, to number: Int, minor: Bool) {
        key.musicKey = RVData_MusicKeyScale.MusicKey(rawValue: number) ?? .aFlat
        key.musicScale = minor ? .minor : .major
    }

    private func changeText(slide: Int, element: String, joined: Bool, _ apply: (inout RVData_Graphics.Text) throws -> Void) throws {
        guard rows.indices.contains(slide) else { throw Refusal.slideGone }
        let row = rows[slide]
        let before = presentation.cues[row.cue]
        var after = before
        var elements = after.actions[row.action].slide.presentation.baseSlide.elements
        // (Only a box that has text: chords are not put on a shape.)
        guard let index = elements.firstIndex(where: { $0.element.uuid.string == element && $0.element.hasText }) else {
            throw Refusal.textBoxGone
        }
        try apply(&elements[index].element.text)
        after.actions[row.action].slide.presentation.baseSlide.elements = elements
        guard after != before else { return }
        var changed = presentation
        changed.cues[row.cue] = after
        try save(changed)
        record(.cue(index: row.cue, before: before, after: after), joined: joined)
    }

    private func changeMusic(joined: Bool, _ apply: (inout RVData_Presentation.Music, _ hadMusic: Bool) -> Void) throws {
        let before = presentation.hasMusic ? presentation.music : nil
        var after = presentation.music
        apply(&after, presentation.hasMusic)
        guard after != before else { return }
        var changed = presentation
        changed.music = after
        try save(changed)
        record(.music(before: before, after: after), joined: joined)
    }

    /// A new change is a step of undo, and throws away what could have been redone.
    private func record(_ unit: Unit, joined: Bool) {
        undoSteps.append(Step(unit: unit, joined: joined && !undoSteps.isEmpty))
        if undoSteps.count > Self.undoLimit {
            undoSteps.removeFirst()
        }
        redoSteps.removeAll()
    }

    /// Takes the last step off one list, puts its part of the file as it was at the
    /// other end of that step, saves, and puts the step on the other list.
    private func restore(forwards: Bool) throws {
        guard let step = (forwards ? redoSteps : undoSteps).last else { return }
        var changed = presentation
        switch step.unit {
        case .cue(let index, let before, let after):
            changed.cues[index] = forwards ? after : before
        case .music(let before, let after):
            if let music = forwards ? after : before {
                changed.music = music
            } else {
                changed.clearMusic()
            }
        }
        try save(changed)
        if forwards {
            redoSteps.removeLast()
            undoSteps.append(step)
        } else {
            undoSteps.removeLast()
            redoSteps.append(step)
        }
        // A step joined to the one before it goes with it: back, the one before
        // follows; forwards, the one that was joined to this follows.
        if let next = (forwards ? redoSteps : undoSteps).last, forwards ? next.joined : step.joined {
            try restore(forwards: forwards)
        }
    }

    /// The first three things asked before anything is saved, in the order below (1 to
    /// 3). Answers with the safeguards, or throws the refusal.
    private func maySave() throws -> Safeguards {
        guard let safeguards else { throw Refusal.onlyForReading }
        if safeguards.proPresenterIsRunning() { throw Refusal.proPresenterIsRunning }
        guard fileIsAsLastRead else { throw Refusal.changedByAnother }
        return safeguards
    }

    /// Saves a new state of the song, and only then makes it the song. In order:
    ///
    ///   1. A song with no safeguards saves nothing.
    ///   2. Nothing is saved while ProPresenter is running.
    ///   3. Nothing is saved over a file that something else has changed: the file must
    ///      be, in its size, its time of last change AND its bytes, what was last read
    ///      from it or written to it here. (The page asks for the first two. The bytes
    ///      are asked as well because a file brought down by a syncing service can come
    ///      with its old time on it; and writing begins by reading the file anyway.)
    ///   4. Before the first save, a copy of the file is kept. If it cannot be, nothing
    ///      is saved.
    ///   5. The whole message is written beside the file and put in its place, and read
    ///      back.
    private func save(_ changed: RVData_Presentation) throws {
        let safeguards = try maySave()
        if !copyKept {
            do {
                try safeguards.keepCopy(url, .beforeAChange)
            } catch {
                throw Refusal.copyNotKept("\(error.localizedDescription)")
            }
            copyKept = true
        }
        let bytes: Data
        do {
            bytes = try changed.serializedData()
            try SongFile.write(bytes, over: url)
        } catch {
            throw Refusal.notSaved("\(error)")
        }
        presentation = changed
        bytesOnDisk = bytes
        // (If the file's stamp cannot be read now, the next save will be refused, which
        // is right: something is wrong with the file.)
        stamp = (try? SongFile.stamp(of: url)) ?? stamp
    }
}
