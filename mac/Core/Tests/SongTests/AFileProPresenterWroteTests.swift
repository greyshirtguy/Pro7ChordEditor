// The one test file that ProPresenter itself wrote: Rock of Ages, from the maintainer's
// own library, as ProPresenter 7.9 on a Mac saved it (testdata/README.md says why this
// song may be here: its words are of 1776).
//
// Every other test file was written by Simple Presenter, or made from one that was. So
// this is the only one that shows what a real file is like, and it happens to be unlike
// them in two ways that matter:
//
//   - Its cues are not in the order its groups name them. The slides are in the hymn's
//     order only if the groups are gone by.
//   - Every one of its text boxes has a link: one that says when the box is to be seen.
//     Under the page's first wording, "a box with a link is not shown", this song would
//     have had no words at all. A box keeps its own words under such a link, and the
//     page says so now.
//
// What is expected here is not a vector, and was not worked out from the code either:
// it is the hymn, which says what order its lines come in.

import ChordRules
import Foundation
import ProFormat
import Song
import Testing
import TestData

@Suite("A file ProPresenter wrote")
struct AFileProPresenterWroteTests {
    static let path = "made-by-propresenter/Rock Of Ages (Toplady).pro"

    /// The first line of each slide, as the hymn has them: four verses of three slides.
    static let firstLines = [
        "Rock of Ages cleft for me", "Let the water and the blood", "Be of sin the double cure",
        "Not the labors of my hands", "Could my zeal no respite know", "All for sin could not atone",
        "Nothing in my hand I bring", "Naked come to thee for dress", "Foul I to the fountain fly",
        "While I draw this fleeting breath", "When I soar to worlds unknown", "Rock of Ages cleft for me",
    ]

    @Test("It is read in the hymn's order, by its groups, with every box its own words")
    func readsInTheHymnsOrder() throws {
        let song = try OpenSong(opening: TestData.root.appendingPathComponent("files").appendingPathComponent(Self.path))
        let boxes = song.textBoxes
        #expect(song.name == "Rock Of Ages (Toplady)")
        #expect(song.slideCount == 12)
        #expect(boxes.map { $0.words.lines.first?.string ?? "" } == Self.firstLines)
        #expect(boxes.map(\.slide) == Array(0..<12))
        #expect(boxes.map(\.group) == ["Verse 1", "Verse 2", "Verse 3", "Verse 4"].flatMap { [$0, $0, $0] })
        #expect(boxes.map(\.startsItsGroup) == [true, false, false, true, false, false, true, false, false, true, false, false])
        // Two lines to a slide, no chords yet, no blank slides, and no key.
        #expect(boxes.allSatisfy { $0.words.lines.count == 2 && $0.chords.isEmpty && !$0.blank })
        #expect(song.key == "" && song.shownKey == "" && song.usedChords.isEmpty)
        // ProPresenter gave each group a colour that can be seen.
        #expect(boxes.allSatisfy { ($0.groupColour?.alpha ?? 0) > 0 })
    }

    @Test("In the file, the cues are in another order, and every box has a link")
    func whatMakesItUnlikeTheOthers() throws {
        let presentation = try RVData_Presentation(serializedBytes: TestData.file(Self.path))
        // As the cues lie in the file, the first slide is from the third verse.
        let asTheyLie = presentation.cues.compactMap { cue in
            cue.actions.first?.slide.presentation.baseSlide.elements.first.map { RTFWords.words(ofRTF: $0.element.text.rtfData).lines.first?.string ?? "" }
        }
        #expect(asTheyLie.count == 12)
        #expect(asTheyLie != Self.firstLines)
        #expect(asTheyLie.sorted() == Self.firstLines.sorted())
        // One text box to a slide, each with one link, of the kind that only says when
        // the box is to be seen.
        let elements = presentation.cues.flatMap { $0.actions.flatMap { $0.slide.presentation.baseSlide.elements } }
        #expect(elements.count == 12)
        #expect(elements.allSatisfy { element in
            element.dataLinks.count == 1 && element.dataLinks.allSatisfy { if case .visibilityLink? = $0.propertyType { return true } else { return false } }
        })
    }

    @Test("Chords and a key are put on it, and taken off again, and it is the file ProPresenter wrote")
    func changedAndUndone() throws {
        let bench = try Workbench()
        let song = try bench.open(Self.path)
        let original = try Data(contentsOf: song.url)
        let boxes = song.textBoxes

        //   Rock of Ages cleft for me \n Let me hide myself in thee      (52 places)
        //   0    5  8    13    19  23 25 26  30 33   38     45 48
        try song.setChords(slide: 0, element: boxes[0].element, [Chord(at: 0, name: "Bb"), Chord(at: 13, name: "Eb"), Chord(at: 26, name: "F"), Chord(at: 33, name: "Bb")])
        try song.setKey("Bb")
        #expect(bench.copiesKept == [original])

        let again = try OpenSong(opening: song.url)
        #expect(again.key == "Bb" && again.shownKey == "Bb")
        #expect(again.textBoxes.first?.chords.map(\.name) == ["Bb", "Eb", "F", "Bb"])
        #expect(again.textBoxes.dropFirst() == boxes.dropFirst())
        // Written as the rules say: each to the next chord or the end of its line.
        let saved = try message(at: song.url)
        let here = try #require(place(of: boxes[0].element, in: saved))
        #expect(spell(entries(at: here, in: saved)) == ["Bb 0-13", "Eb 13-25", "F 26-33", "Bb 33-52"])
        // The box had entries that are not chords, and has them still, first.
        let before = try RVData_Presentation(serializedBytes: original)
        #expect(entries(at: here, in: saved).filter { !isChord($0) } == entries(at: here, in: before))

        try song.undo()
        try song.undo()
        #expect(try Data(contentsOf: song.url) == original)
    }
}
