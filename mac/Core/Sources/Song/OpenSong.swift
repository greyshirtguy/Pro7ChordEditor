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
//   - A line of chords alone is given more or fewer stand-ins by changing the bytes of
//     that line in the box's RTF and no others (spec/01, "Chords with no words").
//     There the whole RTF is read into a model and written again.
//   - A blank slide's box is given its stand-ins after the opening of a neighbour's
//     RTF, and a slide with no box is given a copy of a neighbour's and never one of
//     this app's devising (spec/01, "Chords onto a blank slide").
//   - Only a line of chords alone is ever made stand-ins. There, any line asked for
//     is, its words with it; nothing there asks for one.
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
    /// What was made for the chords of blank slides while the song has been open here,
    /// so that taking the chords off again leaves no trace (spec/01, "A change of
    /// mind"). It is forgotten when the song is closed, and when a copy is put back.
    private var madeForChords: [Made.Of: Made] = [:]

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

    /// What a blank slide's first chords had made for them.
    private enum Made {
        /// Which text box: its cue, and its id on that cue's slide.
        struct Of: Hashable {
            var cue: Int
            var element: String
        }

        /// The slide's own box was given words: this is its `text`, whole, as the file
        /// had it before.
        case words(was: RVData_Graphics.Text)
        /// The slide had no box and was given one: the cue as the file had it before,
        /// and as it was just after, less the box. (While the second is what the cue
        /// still is without the box, nothing else in it has changed, and the first can
        /// be put back whole: the file is then what it was to the last byte, down to
        /// which of its empty parts were written and which were left out.)
        case box(cueBefore: RVData_Cue, cueWithoutIt: RVData_Cue)
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

    /// "This line of chords alone now has these": the names in order. The line's text
    /// becomes that many stand-ins and its chords go on them, every other place; the
    /// chords of the lines after it move with their words.
    ///
    ///   - **With no names the line is one zero-width space**, whatever it hung its
    ///     chords on, and is a line of chords alone still, so that a chord can be put
    ///     on it again. (Decided by the maintainer on 2026-10-12. The reference leaves
    ///     such a line with no stand-in at all, which in a box with other lines is an
    ///     empty line that takes nothing. And not the plain space the line may have
    ///     had: a text box left holding one plain space and no chord is, as a song is
    ///     read, a stray space and nothing more, and the line would be lost.)
    ///   - Where the line has those stand-ins already (its chords renamed, or put in
    ///     another order), only the chord entries change.
    ///   - Otherwise the line's characters are changed in the box's RTF, in place, and
    ///     not another byte of it (RTFWords). This is the one case in which the app
    ///     writes words.
    ///   - A blank slide's line: its first chords give its box words, or give the slide
    ///     a box if it has none (an empty `element`). No chords for a blank slide is
    ///     nothing to do, and nothing is made for it.
    ///   - The last chord taken off a blank slide that was given its chords while the
    ///     song has been open here unmakes what was made for them: the file is then
    ///     byte for byte what it was before the first.
    ///
    /// Only a line of chords alone is ever made stand-ins: a line that has words, or is
    /// empty, is refused, whatever asks. (The words of a song are not this app's to
    /// change. The reference would make stand-ins of any line it was asked for; nothing
    /// there asks.) A name that is nothing, or nothing but spaces, is left out, as it
    /// would be when the box's chords are written.
    public func setChordsAlone(slide: Int, element: String, line: Int, names: [String], joined: Bool = false) throws {
        guard rows.indices.contains(slide) else { throw Refusal.slideGone }
        let names = names.map(Chords.tidied).filter { !$0.utf16.isEmpty }
        // A blank slide is handed over as one box with no words: its own, or one with
        // no id if it has none.
        let handed = readTextBoxes(ofSlide: slide, of: presentation, rows: rows)
        let blank = handed.count == 1 && handed[0].blank ? handed[0] : nil
        if element.isEmpty {
            if names.isEmpty { return }
            guard let blank, blank.element.isEmpty else { throw Refusal.noLongerBlank }
            try giveABox(toSlide: slide, names: names, joined: joined)
        } else if let blank, blank.element == element {
            if names.isEmpty { return }
            try giveWords(toBlankBox: element, slide: slide, names: names, joined: joined)
        } else {
            try change(line: line, ofBox: element, slide: slide, names: names, joined: joined)
        }
    }

    /// A line of chords alone that has words already, of a kind: its stand-ins.
    private func change(line: Int, ofBox element: String, slide: Int, names: [String], joined: Bool) throws {
        let made = Made.Of(cue: rows[slide].cue, element: element)
        try changeCue(slide: slide, joined: joined) { cue, action in
            var elements = cue.actions[action].slide.presentation.baseSlide.elements
            let index = try Self.index(ofBox: element, in: elements)
            var text = elements[index].element.text
            let words = readWords(text)
            let lines = words.lines
            guard lines.indices.contains(line) else { throw Refusal.lineGone }
            let old = lines[line]
            guard Chords.isPlaceholders(old) else { throw Refusal.notChordsAlone }
            // In the character the line hangs its chords on now: a plain space if it
            // has spaces and no zero-width space, else a zero-width space. One for
            // each chord; and where there is no chord, one zero-width space, which
            // is what keeps the line a line of chords alone.
            let spaces = !old.contains(0x200B) && old.contains(0x0020)
            let new = names.isEmpty ? Chords.placeholders(1) : Chords.placeholders(names.count, standIn: spaces ? 0x0020 : 0x200B)
            // The chords of the other lines keep their characters; those after this
            // line move by as much as it grows or shrinks. This line's are replaced.
            let start = lines[..<line].reduce(0) { $0 + $1.length + 1 }
            var kept: [Chord] = []
            for chord in readChords(text) {
                if chord.at < start {
                    kept.append(chord)
                } else if chord.at > start + old.length {
                    kept.append(Chord(at: chord.at + new.length - old.length, name: chord.name))
                }
            }

            // A change of mind: the last chord taken off a blank slide that was given
            // its chords here, whose box has nothing in it but this line's stand-ins.
            // What was made for them is unmade.
            if names.isEmpty, kept.isEmpty, let was = madeForChords[made] {
                var without = lines
                without[line] = Words()
                if without.allSatisfy({ $0.isBlank && !$0.contains(0x200B) && !$0.contains(0x2001) }) {
                    switch was {
                    case .words(let whole):
                        elements[index].element.text = whole
                        cue.actions[action].slide.presentation.baseSlide.elements = elements
                    case .box(let cueBefore, let cueWithoutIt):
                        elements.remove(at: index)
                        cue.actions[action].slide.presentation.baseSlide.elements = elements
                        if cue == cueWithoutIt { cue = cueBefore }
                    }
                    return
                }
            }

            if new != old {
                guard let rtf = RTFWords.rtf(text.rtfData, withLine: line, become: new) else { throw Refusal.cannotBeWritten }
                text.rtfData = Data(rtf)
                // What the box says of a range of its words ("these are in capitals")
                // goes with the words, as the chords of the lines below do.
                moveOtherRanges(of: &text, lineStart: start, was: old.length, now: new.length)
            }
            for (n, name) in names.enumerated() {
                kept.append(Chord(at: start + n * 2, name: name))
            }
            write(chords: kept, into: &text)
            elements[index].element.text = text
            cue.actions[action].slide.presentation.baseSlide.elements = elements
        }
    }

    /// The first chords of a blank slide that has a text box: the box is given words,
    /// one stand-in for each chord. Only its RTF and its chord entries change.
    ///
    /// The RTF is made, not changed in place: as Apple's text system writes a text box
    /// with no text, there is no font in it, no size and no colour for a stand-in to
    /// be in. So it is the opening of the nearest text box of the song that has
    /// words, and the stand-ins after it. Zero-width spaces, whatever stray space the
    /// box held.
    private func giveWords(toBlankBox element: String, slide: Int, names: [String], joined: Bool) throws {
        let made = Made.Of(cue: rows[slide].cue, element: element)
        var was = RVData_Graphics.Text()
        let changed = try changeCue(slide: slide, joined: joined) { cue, action in
            var elements = cue.actions[action].slide.presentation.baseSlide.elements
            let index = try Self.index(ofBox: element, in: elements)
            was = elements[index].element.text
            guard let model = boxToModelOn(near: slide) else { throw Refusal.noBoxToModelOn }
            var text = was
            try Self.put(names, into: &text, afterTheOpeningOf: model.element.text)
            elements[index].element.text = text
            cue.actions[action].slide.presentation.baseSlide.elements = elements
        }
        // (Remembered once it has been saved, and only the first time: what the file
        // had before its first chord here is what is to be gone back to.)
        if changed && madeForChords[made] == nil {
            madeForChords[made] = .words(was: was)
        }
    }

    /// The first chords of a blank slide that has no text box: it is given one. NEW to
    /// these apps as decided on 2026-10-11, and as the reference does it but for one
    /// thing: the box is always a copy of the song's own, the nearest that has words,
    /// placed, sized and set as that is. If the song has none, the change is refused;
    /// the apps do not devise a text box.
    ///
    /// Of the copy: its id is a new one; its name is the copied name, or that with a
    /// number after it until no other element of the slide has it; what tied the other
    /// box to things on its own slide is cleared (how it comes on and goes off, and its
    /// links); its chords are taken off; and its words are the stand-ins. It goes after
    /// the slide's other elements, so that it is drawn over them.
    ///
    /// A GUESS, until it has been seen in ProPresenter: that the settings the copied
    /// box keeps for stretches of its words (how they are capitalised, a first font),
    /// whose stretches were over words that are not in the copy, do no harm left as
    /// they are (spec/01, "A change of mind", the last paragraph).
    private func giveABox(toSlide slide: Int, names: [String], joined: Bool) throws {
        var id = ""
        var cueBefore = RVData_Cue()
        var cueWithoutIt = RVData_Cue()
        let changed = try changeCue(slide: slide, joined: joined) { cue, action in
            guard let model = boxToModelOn(near: slide) else { throw Refusal.noBoxToModelOn }
            var elements = cue.actions[action].slide.presentation.baseSlide.elements
            var added = model
            id = Self.newID(writtenAs: model.element.uuid.string)
            added.element.uuid.string = id
            added.element.name = Self.name(model.element.name, notAmong: elements.map(\.element.name))
            added.clearBuildIn()
            added.clearBuildOut()
            added.childBuilds = []
            added.dataLinks = []
            try Self.put(names, into: &added.element.text, afterTheOpeningOf: model.element.text)
            cueBefore = cue
            // (As the cue will be with the box taken off again: made the way it will
            // be made then, so that the two can be held against each other.)
            cue.actions[action].slide.presentation.baseSlide.elements = elements
            cueWithoutIt = cue
            elements.append(added)
            cue.actions[action].slide.presentation.baseSlide.elements = elements
        }
        if changed {
            madeForChords[Made.Of(cue: rows[slide].cue, element: id)] = .box(cueBefore: cueBefore, cueWithoutIt: cueWithoutIt)
        }
    }

    /// Makes a text's words one stand-in for each of these chords, after the opening of
    /// another text's RTF, and puts the chords on them.
    private static func put(_ names: [String], into text: inout RVData_Graphics.Text, afterTheOpeningOf model: RVData_Graphics.Text) throws {
        guard let rtf = RTFWords.rtf(withTheOpeningOf: model.rtfData, then: Chords.placeholders(names.count)) else {
            throw Refusal.cannotBeWritten
        }
        text.rtfData = Data(rtf)
        write(chords: names.enumerated().map { Chord(at: $0.offset * 2, name: $0.element) }, into: &text)
    }

    /// The text box a blank slide's is modelled on: the nearest of the song that is
    /// shown and is not blank, on a slide before this one if there is one (the nearest
    /// first), else on one after. Its first such box, if a slide has two.
    ///
    /// (A box that is shown for its chords and has no character at all is passed over:
    /// it has no words to say where words begin, and so no opening to give.)
    private func boxToModelOn(near slide: Int) -> RVData_Slide.Element? {
        let others = Array((0..<slide).reversed()) + Array((slide + 1)..<max(slide + 1, rows.count))
        for other in others where rows.indices.contains(other) {
            let elements = Song.slide(of: rows[other], in: presentation).elements
            for box in readTextBoxes(ofSlide: other, of: presentation, rows: rows) where !box.blank && !box.words.isEmpty {
                if let found = elements.first(where: { $0.element.uuid.string == box.element && $0.element.hasText }) {
                    return found
                }
            }
        }
        return nil
    }

    /// (Only a box that has text: chords are not put on a shape.)
    private static func index(ofBox element: String, in elements: [RVData_Slide.Element]) throws -> Int {
        guard let index = elements.firstIndex(where: { $0.element.uuid.string == element && $0.element.hasText }) else {
            throw Refusal.textBoxGone
        }
        return index
    }

    /// A new id, written as the file's others are: in capitals, as ProPresenter on the
    /// Mac writes them, unless the id it is modelled on has a small letter in it.
    static func newID(writtenAs model: String) -> String {
        let id = UUID().uuidString
        return model.contains(where: { ("a"..."f").contains($0) }) ? id.lowercased() : id
    }

    /// A name for an element that no other element of the slide has: the name, or the
    /// name with " 2", " 3" ... after it.
    static func name(_ base: String, notAmong taken: [String]) -> String {
        var name = base
        var n = 2
        while taken.contains(where: { Array($0.utf16) == Array(name.utf16) }) {
            name = "\(base) \(n)"
            n += 1
        }
        return name
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
        // (And nothing is remembered of what was made for a blank slide's chords: that
        // was of the file as it was, and this is another.)
        madeForChords.removeAll()
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
        try changeCue(slide: slide, joined: joined) { cue, action in
            var elements = cue.actions[action].slide.presentation.baseSlide.elements
            let index = try Self.index(ofBox: element, in: elements)
            try apply(&elements[index].element.text)
            cue.actions[action].slide.presentation.baseSlide.elements = elements
        }
    }

    /// Changes a slide's cue: what is handed over is the cue, and which of its actions
    /// is the slide. Answers whether anything was changed, and so saved.
    @discardableResult
    private func changeCue(slide: Int, joined: Bool, _ apply: (inout RVData_Cue, _ action: Int) throws -> Void) throws -> Bool {
        guard rows.indices.contains(slide) else { throw Refusal.slideGone }
        let row = rows[slide]
        let before = presentation.cues[row.cue]
        var after = before
        try apply(&after, row.action)
        guard after != before else { return false }
        var changed = presentation
        changed.cues[row.cue] = after
        try save(changed)
        record(.cue(index: row.cue, before: before, after: after), joined: joined)
        return true
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
