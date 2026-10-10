// The order of a song's slides, and of a box's chords, where the file has them in
// another order than they are to be read in. spec/01-file-format.md, "The order of the
// slides" and "Reading a box's chords".
//
// Mine, not vectors. In every test file the cues are already in the order their groups
// name them, and every box's chords are already in order of place, so
// as-read-by-simple-presenter.json would pass if neither rule were kept. These cases
// are made here from Plain Song, and what is expected was read from the reference's code
// (`PresentationEditor::open`, `proconvert::readChords`), not got from it.

import ChordRules
import Foundation
import ProFormat
import Song
import Testing
import TestData

@Suite("Mine, not vectors: slides and chords that are out of order in the file")
struct SlidesInOrderTests {
    private func plainSong() throws -> (RVData_Presentation, [TextBox]) {
        let bytes = try TestData.file("made-by-simple-presenter/Plain Song.pro")
        let boxes = try OpenSong(opening: TestData.root.appendingPathComponent("files/made-by-simple-presenter/Plain Song.pro")).textBoxes
        return (try RVData_Presentation(serializedBytes: bytes), boxes)
    }

    @Test("Slides go by their groups, not by where their cues are in the file")
    func byGroups() throws {
        let bench = try Workbench()
        var (made, boxes) = try plainSong()
        #expect(made.cues.count == 4 && boxes.count == 4)
        // The cue of each box, as the file has them.
        let cues = try boxes.map { try #require(place(of: $0.element, in: made)).cue }
        func id(_ n: Int) -> RVData_UUID { made.cues[cues[n]].uuid }
        var nobody = RVData_UUID()
        nobody.string = "00000000-0000-0000-0000-000000000000"

        // Two groups: "Last First" names the fourth slide, then a cue that is not there,
        // then the first. "Again" names the first once more, then the second. The third
        // is in no group.
        var lastFirst = RVData_Presentation.CueGroup()
        lastFirst.group.name = "Last First"
        lastFirst.group.color.red = 1
        lastFirst.group.color.alpha = 1
        lastFirst.cueIdentifiers = [id(3), nobody, id(0)]
        // (A colour that cannot be seen is no colour: this one is all there but for
        // how solid it is, which is nought.)
        var again = RVData_Presentation.CueGroup()
        again.group.name = "Again"
        again.group.color.green = 1
        again.cueIdentifiers = [id(0), id(1)]
        made.cueGroups = [lastFirst, again]

        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Groups.pro"))
        let now = song.textBoxes
        #expect(now.map(\.element) == [boxes[3].element, boxes[0].element, boxes[1].element, boxes[2].element])
        #expect(now.map(\.slide) == [0, 1, 2, 3])
        #expect(now.map(\.group) == ["Last First", "Last First", "Again", ""])
        // A cue named twice is taken the first time, so the first slide of "Again" that
        // is shown is its second name.
        #expect(now.map(\.startsItsGroup) == [true, false, true, false])
        #expect(now[0].groupColour == Colour(red: 1, green: 0, blue: 0, alpha: 1))
        #expect(now[2].groupColour == nil && now[3].groupColour == nil)
    }

    @Test("A cue whose slide's action is switched off is not a slide")
    func actionSwitchedOff() throws {
        let bench = try Workbench()
        var (made, boxes) = try plainSong()
        let second = try #require(place(of: boxes[1].element, in: made))
        made.cues[second.cue].actions[second.action].isEnabled = false
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Action.pro"))
        #expect(song.textBoxes.map(\.element) == [boxes[0].element, boxes[2].element, boxes[3].element])
    }

    @Test("A box's chords are read in order of place, the file's order kept among equals")
    func chordsInOrderOfPlace() throws {
        let bench = try Workbench()
        var (made, boxes) = try plainSong()
        let here = try #require(place(of: boxes[0].element, in: made))
        func entry(_ start: Int32, _ end: Int32, _ name: String) -> Entry {
            var entry = Entry()
            entry.range.start = start
            entry.range.end = end
            entry.chord = name
            return entry
        }
        // Out of order, two in one place, one with no name, and ranges that say nothing true.
        setEntries([entry(14, 99, "D"), entry(0, 0, "G"), entry(14, 15, "Em"), entry(4, 2, ""), entry(8, 9, "C")], at: here, in: &made)
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Chords.pro"), safeguards: bench.safeguards)
        // As the file has them: nothing is tidied on the way in, and only the start is believed.
        #expect(song.textBoxes[0].chords == [
            Chord(at: 0, name: "G"), Chord(at: 4, name: ""), Chord(at: 8, name: "C"), Chord(at: 14, name: "D"), Chord(at: 14, name: "Em"),
        ])
        // Put back as they were read, they are tidied and written as the rules say.
        try song.setChords(slide: 0, element: boxes[0].element, song.textBoxes[0].chords)
        #expect(spell(entries(at: here, in: try message(at: song.url))) == ["G 0-8", "C 8-14", "Em 14-18"])
    }
}
