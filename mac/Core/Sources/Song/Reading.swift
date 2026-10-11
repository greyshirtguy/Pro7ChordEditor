// Reading a presentation: which of its cues are slides and in what order, and which of
// a slide's boxes hold the song's words. spec/01-file-format.md, "From the presentation
// down to a text box", is the rule; reference/simple-presenter/src/presentationeditor.cpp
// (`open`, `song`) and proconvert.cpp (`readChords`) are what it was taken from; and
// testdata/files/expected/as-read-by-simple-presenter.json has what every test file
// reads as.
//
// Nothing here changes anything.

import ChordRules
import ProFormat

/// One slide of the song: where it is in the message, and what its group is.
struct SlideRow {
    /// Which of the presentation's `cues` it is.
    var cue: Int
    /// Which of that cue's `actions` is its slide.
    var action: Int
    var group: String
    var groupColour: Colour?
    var startsItsGroup: Bool
}

/// The first of a cue's actions that is switched on and is a presentation's slide.
/// (The page says "one of its actions is a slide"; that the action must itself be
/// enabled is from the reference, `slideAction`.)
func slideAction(of cue: RVData_Cue) -> Int? {
    cue.actions.firstIndex { action in
        guard action.isEnabled, case .presentation? = action.slide.slide else { return false }
        return true
    }
}

/// The slides, in the order ProPresenter shows them: the order of the groups, and inside
/// each group the order of its `cue_identifiers`; after those, any cue that no group
/// names, in the order of `cues`. A cue is left out if it is switched off or has no
/// slide, and a cue named twice is taken the first time.
///
/// (A presentation also has `arrangements`, which play the groups in another order.
/// They are not gone by here: the sheet is the song as it is laid down, each slide once.)
func slideRows(of presentation: RVData_Presentation) -> [SlideRow] {
    // (Two cues with one id: the later is the one a group's naming of that id finds, as
    // in the reference, which keeps them in a table that the later overwrites.)
    var cueOfID: [String: Int] = [:]
    for (index, cue) in presentation.cues.enumerated() {
        cueOfID[cue.uuid.string] = index
    }
    var rows: [SlideRow] = []
    var placed: Set<Int> = []
    func place(_ cue: Int, group: String, colour: Colour?, starts: Bool) -> Bool {
        let candidate = presentation.cues[cue]
        guard !placed.contains(cue), candidate.isEnabled, let action = slideAction(of: candidate) else { return false }
        placed.insert(cue)
        rows.append(SlideRow(cue: cue, action: action, group: group, groupColour: colour, startsItsGroup: starts))
        return true
    }
    for group in presentation.cueGroups {
        // A group has a colour of its own if someone set it in ProPresenter. Most have
        // none (or one that cannot be seen), and ProPresenter then colours the group by
        // its name, from a list it keeps for the whole library.
        let colour = group.group.color
        let own = group.group.hasColor && colour.alpha > 0
            ? Colour(red: Double(colour.red), green: Double(colour.green), blue: Double(colour.blue), alpha: Double(colour.alpha))
            : nil
        var first = true
        for id in group.cueIdentifiers {
            if let cue = cueOfID[id.string], place(cue, group: group.group.name, colour: own, starts: first) {
                first = false
            }
        }
    }
    for cue in presentation.cues.indices {
        _ = place(cue, group: "", colour: nil, starts: false)
    }
    return rows
}

/// The slide of a row.
func slide(of row: SlideRow, in presentation: RVData_Presentation) -> RVData_Slide {
    presentation.cues[row.cue].actions[row.action].slide.presentation.baseSlide
}

/// Whether a box shows words of its own. A box with a link shows something else: another
/// element's words, a timer, a clock, the next slide's words. The one kind of link that
/// leaves a box its own words is one that only says when the box is to be seen.
///
/// (The page says "no data_links". The reference is finer, and is followed: of the links
/// it knows it counts every kind but that one. A link of a kind the descriptions of the
/// format have no name for reads here as no kind at all, and so as none.)
func showsItsOwnWords(_ element: RVData_Slide.Element) -> Bool {
    element.dataLinks.allSatisfy { link in
        switch link.propertyType {
        case nil, .visibilityLink?: return true
        default: return false
        }
    }
}

/// A box's chords as the file has them: every custom attribute that is a chord (`chord`
/// is the one of its kinds that is set), as its range's start and its name, sorted by
/// start with the file's order kept among equals. The range's end is not believed
/// (spec/01-file-format.md, "Chords").
func readChords(_ text: RVData_Graphics.Text) -> [Chord] {
    var found: [Chord] = []
    for attribute in text.attributes.customAttributes {
        if case .chord(let name)? = attribute.attribute {
            found.append(Chord(at: Int(attribute.range.start), name: name))
        }
    }
    return found.enumerated()
        .sorted { ($0.element.at, $0.offset) < ($1.element.at, $1.offset) }
        .map(\.element)
}

/// The words of a box: the plain text of its RTF.
func readWords(_ text: RVData_Graphics.Text) -> Words {
    RTFWords.words(ofRTF: text.rtfData)
}

/// What the editors are handed.
func readTextBoxes(of presentation: RVData_Presentation, rows: [SlideRow]) -> [TextBox] {
    rows.indices.flatMap { readTextBoxes(ofSlide: $0, of: presentation, rows: rows) }
}

/// What the editors are handed of one slide: one entry at least.
func readTextBoxes(ofSlide number: Int, of presentation: RVData_Presentation, rows: [SlideRow]) -> [TextBox] {
    let row = rows[number]
    let elements = slide(of: row, in: presentation).elements
    func box(_ element: String, words: Words, chords: [Chord], blank: Bool) -> TextBox {
        TextBox(slide: number, group: row.group, groupColour: row.groupColour, startsItsGroup: row.startsItsGroup,
                element: element, words: words, chords: chords, blank: blank)
    }
    var shown: [TextBox] = []
    for element in elements where showsItsOwnWords(element) {
        let boxWords = readWords(element.element.text)
        let boxChords = readChords(element.element.text)
        // A box with no words is left out, unless it is a line of chords alone: it
        // has chords, or the characters Multitracks hangs them on. (A box with only
        // a stray space in it, which files are full of, is neither.)
        if boxWords.isBlank && boxChords.isEmpty && !boxWords.contains(0x200B) && !boxWords.contains(0x2001) {
            continue
        }
        shown.append(box(element.element.uuid.string, words: boxWords, chords: boxChords, blank: false))
    }
    // NEW (spec/01, "A blank slide"): a slide none of whose boxes is shown is handed
    // over all the same, as one box with no words, so that chords can be put on an
    // intro that someone made as an empty slide.
    //   - If it has a text box to put chords in, that is the box: its first that shows
    //     its own words, whatever stray space that may hold.
    //   - If it has none, the box has no id yet. Nothing is made for it here: a text
    //     box is added to the slide only when a chord is put on it
    //     (OpenSong.setChordsAlone).
    if shown.isEmpty {
        let empty = elements.first { $0.element.hasText && showsItsOwnWords($0) }
        shown.append(box(empty?.element.uuid.string ?? "", words: Words(), chords: [], blank: true))
    }
    return shown
}

/// A key as a musician writes it, from the file's number for its note and its scale.
func keyName(_ key: RVData_MusicKeyScale) -> String {
    Chords.keyName(key.musicKey.rawValue, minor: key.musicScale == .minor)
}
