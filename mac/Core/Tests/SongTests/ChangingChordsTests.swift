// Changing a song's chords, on copies of the test files in a temporary folder: that a
// change is saved at once, that it changes nothing else in the file, that it can be
// undone and done again, and that what is written is what the rules say.
// spec/05-song-and-saving.md, "Proofs to write".

import ChordRules
import Foundation
import ProFormat
import Song
import Testing
import TestData

@Suite("Changing chords")
struct ChangingChordsTests {
    static let paths = (try? TestData.presentationPaths()) ?? []

    @Test("A change to one box's chords leaves every other field of the file as it was", arguments: paths)
    func changesOnlyThatBoxsChords(path: String) throws {
        let bench = try Workbench()
        let song = try bench.open(path)
        let boxes = song.textBoxes
        #expect(!boxes.isEmpty)
        // Each box of the song in turn, one change on top of another.
        for (n, box) in boxes.enumerated() where !box.words.isEmpty {
            let before = try message(at: song.url)
            // A different list each time: the box's own chords with its last one gone and
            // one put on the first place, or on the second if the first has one.
            var wanted = Array(box.chords.dropLast())
            let free = wanted.contains { $0.at == 0 } ? min(1, box.words.length - 1) : 0
            wanted.append(Chord(at: free, name: "Bb\(n)"))
            try song.setChords(slide: box.slide, element: box.element, wanted)

            let after = try message(at: song.url)
            let here = try #require(place(of: box.element, in: after))
            // What the file has now is what the rules say is written for those chords...
            let rule = Chords.ranges(box.words, wanted).map { "\($0.name) \($0.start)-\($0.end)" }
            #expect(spell(entries(at: here, in: after)) == rule)
            // ... and the file before, given this box's new chord entries after the
            // entries of its that are not chords, is the file after. Nothing else moved.
            var expected = before
            setEntries(entries(at: here, in: before).filter { !isChord($0) } + entries(at: here, in: after).filter(isChord), at: here, in: &expected)
            #expect(expected == after, "something besides the chords of \(box.element) changed")
            #expect(after != before)
        }
        // One copy was kept, before the first change, of the file as it was.
        #expect(bench.copiesKept == [try TestData.file(path)])
        // Nothing is left beside the song.
        #expect(try bench.names() == [song.url.lastPathComponent])
    }

    @Test("A change and its undo leave the same file; redo makes the change again", arguments: paths)
    func undoneIsTheSameFile(path: String) throws {
        let bench = try Workbench()
        let song = try bench.open(path)
        let original = try Data(contentsOf: song.url)
        let box = try #require(song.textBoxes.first { !$0.chords.isEmpty })

        #expect(!song.canUndo && !song.canRedo)
        try song.setChords(slide: box.slide, element: box.element, [Chord(at: 0, name: "Em")])
        let changed = try Data(contentsOf: song.url)
        #expect(changed != original)
        #expect(song.canUndo && !song.canRedo)
        #expect(song.textBoxes.first { $0.element == box.element }?.chords == [Chord(at: 0, name: "Em")])

        try song.undo()
        #expect(try Data(contentsOf: song.url) == original)
        #expect(song.textBoxes.first { $0.element == box.element }?.chords == box.chords)
        #expect(!song.canUndo && song.canRedo)

        try song.redo()
        #expect(try Data(contentsOf: song.url) == changed)
        #expect(song.canUndo && !song.canRedo)

        // Nothing to undo, or to redo, is nothing done.
        try song.redo()
        #expect(try Data(contentsOf: song.url) == changed)
        try song.undo()
        try song.undo()
        #expect(try Data(contentsOf: song.url) == original)
    }

    @Test("A step joined to the one before it is undone with it; a new change throws away what could be redone")
    func joinedSteps() throws {
        let bench = try Workbench()
        let song = try bench.open("made-by-simple-presenter/Amazing Grace.pro")
        let boxes = song.textBoxes
        let original = try Data(contentsOf: song.url)

        try song.setChords(slide: boxes[0].slide, element: boxes[0].element, [Chord(at: 0, name: "C")])
        let afterOne = try Data(contentsOf: song.url)
        // Two more that are one step: as when a chord is dragged from one box to another.
        try song.setChords(slide: boxes[1].slide, element: boxes[1].element, [])
        try song.setChords(slide: boxes[2].slide, element: boxes[2].element, [Chord(at: 3, name: "D")], joined: true)
        let afterThree = try Data(contentsOf: song.url)

        try song.undo()
        #expect(try Data(contentsOf: song.url) == afterOne)
        try song.redo()
        #expect(try Data(contentsOf: song.url) == afterThree)
        try song.undo()
        try song.undo()
        #expect(try Data(contentsOf: song.url) == original)
        #expect(!song.canUndo && song.canRedo)

        // A new change, and what was undone cannot be done again.
        try song.setChords(slide: boxes[0].slide, element: boxes[0].element, [Chord(at: 1, name: "G")])
        #expect(!song.canRedo)
        // (A first change marked as joined has nothing to be joined to.)
        try song.undo()
        #expect(try Data(contentsOf: song.url) == original)
    }

    @Test("A request that changes nothing saves nothing, and is no step of undo")
    func nothingChangedIsNothingSaved() throws {
        let bench = try Workbench()
        let song = try bench.open("made-by-simple-presenter/Amazing Grace.pro")
        let box = try #require(song.textBoxes.first)
        let stamp = try FileManager.default.attributesOfItem(atPath: song.url.path)[.modificationDate] as? Date

        try song.setChords(slide: box.slide, element: box.element, box.chords)
        #expect(!song.canUndo)
        #expect(bench.copiesKept.isEmpty)
        #expect(try FileManager.default.attributesOfItem(atPath: song.url.path)[.modificationDate] as? Date == stamp)
        #expect(try Data(contentsOf: song.url) == TestData.file("made-by-simple-presenter/Amazing Grace.pro"))
    }

    @Test("What is put on is tidied: names, places, and two in one place")
    func whatIsPutOnIsTidied() throws {
        let bench = try Workbench()
        let song = try bench.open("made-by-simple-presenter/Amazing Grace.pro")
        let box = try #require(song.textBoxes.first)
        //   "Amazing grace how sweet the sound\nThat saved a wretch like me": 61 places,
        //   the line break at 33.
        try song.setChords(slide: box.slide, element: box.element, [
            Chord(at: 500, name: " d/f# "), Chord(at: 8, name: "g7"), Chord(at: 8, name: "C"), Chord(at: -3, name: "f#M7/a"),
            Chord(at: 33, name: "Em"), Chord(at: 20, name: "  "),
        ])
        let here = try #require(place(of: box.element, in: try message(at: song.url)))
        #expect(spell(entries(at: here, in: try message(at: song.url))) == ["F#M7/A 0-8", "C 8-32", "Em 32-33", "D/F# 60-61"])
        // And a song opened afresh from the file hands over just those.
        let again = try OpenSong(opening: song.url)
        #expect(again.textBoxes.first?.chords == [Chord(at: 0, name: "F#M7/A"), Chord(at: 8, name: "C"), Chord(at: 32, name: "Em"), Chord(at: 60, name: "D/F#")])
    }

    @Test("Entries of a box that are not chords stay, in their order, before the chords")
    func otherEntriesStay() throws {
        // No test file has an entry that is not a chord, so one is made here: a copy of
        // Plain Song whose first box is given two, among its chords.
        let bench = try Workbench()
        var made = try RVData_Presentation(serializedBytes: TestData.file("made-by-simple-presenter/Plain Song.pro"))
        let first = try #require(try OpenSong(opening: TestData.root.appendingPathComponent("files/made-by-simple-presenter/Plain Song.pro")).textBoxes.first)
        let here = try #require(place(of: first.element, in: made))
        var size = Entry()
        size.range.end = 5
        size.originalFontSize = 96
        var capitals = Entry()
        capitals.range.start = 2
        capitals.range.end = 9
        capitals.capitalization = .allCaps
        let chords = entries(at: here, in: made)
        setEntries([chords[0], capitals] + chords.dropFirst() + [size], at: here, in: &made)
        let song = try OpenSong(opening: bench.put(made.serializedData(), named: "Made.pro"), safeguards: bench.safeguards)

        try song.setChords(slide: first.slide, element: first.element, [Chord(at: 4, name: "Am")])
        let now = entries(at: here, in: try message(at: song.url))
        #expect(now.count == 3)
        #expect(now[0] == capitals)
        #expect(now[1] == size)
        #expect(spell(now) == ["Am 4-18"])
    }

    @Test("A slide or a text box that is not there is refused, and nothing is changed")
    func whatIsNotThere() throws {
        let bench = try Workbench()
        let song = try bench.open("made-by-simple-presenter/Plain Song.pro")
        let original = try Data(contentsOf: song.url)
        let box = try #require(song.textBoxes.first)

        #expect { try song.setChords(slide: 99, element: box.element, []) } throws: { ($0 as? Refusal)?.reason == .slideGone }
        #expect { try song.setChords(slide: -1, element: box.element, []) } throws: { ($0 as? Refusal)?.reason == .slideGone }
        #expect { try song.setChords(slide: box.slide, element: "no such box", []) } throws: { ($0 as? Refusal)?.reason == .textBoxGone }
        // (A box of another slide is not on this one.)
        let other = try #require(song.textBoxes.first { $0.slide != box.slide })
        #expect { try song.setChords(slide: box.slide, element: other.element, []) } throws: { ($0 as? Refusal)?.reason == .textBoxGone }
        #expect(try Data(contentsOf: song.url) == original)
        #expect(!song.canUndo && bench.copiesKept.isEmpty)
    }

    // ---- What the vectors say is written

    struct Ranges: Decodable {
        struct Case: Decodable {
            struct Chord: Decodable {
                let at: Int
                let name: String
            }
            struct Range: Decodable {
                let start: Int
                let end: Int
                let name: String
            }
            let text: String
            let chords: [Chord]
            let out: [Range]
        }
        let cases: [Case]
    }

    /// Every box of every clean test file, by its words.
    private func boxesByWords(_ bench: Workbench) throws -> [[UInt16]: (song: OpenSong, box: TextBox)] {
        var found: [[UInt16]: (OpenSong, TextBox)] = [:]
        for path in Self.paths where path.hasPrefix("made-by-simple-presenter/") {
            let song = try bench.open(path)
            for box in song.textBoxes where found[box.words.units] == nil {
                found[box.words.units] = (song, box)
            }
        }
        return found
    }

    @Test("Chords put on a box give the ranges ranges.json says, in the file")
    func rangesAsTheVectorsSay() throws {
        let bench = try Workbench()
        let boxes = try boxesByWords(bench)
        let file = try JSONDecoder().decode(Ranges.self, from: TestData.vector("ranges.json"))
        var tried = 0
        for c in file.cases {
            // (Only the cases whose text is the words of a box in a test file can be
            // tried on a file: this layer cannot give a box other words.)
            guard let (song, box) = boxes[Array(c.text.utf16)] else { continue }
            tried += 1
            try song.setChords(slide: box.slide, element: box.element, c.chords.map { Chord(at: $0.at, name: $0.name) })
            let now = try message(at: song.url)
            let here = try #require(place(of: box.element, in: now))
            // (`ranges` writes a name as it is given. The song is asked for a box's
            // chords by an editor, and tidies each name first, as rule 1 of the page
            // says: so "g" in the vector is "G" in the file, and the rest is the vector's.)
            #expect(spell(entries(at: here, in: now)) == c.out.map { "\(Chords.tidied($0.name)) \($0.start)-\($0.end)" },
                    "ranges.json: \(c.chords.map { "\($0.name)@\($0.at)" })")
        }
        #expect(tried >= 10, "only \(tried) cases of ranges.json could be tried on a test file")
    }

    struct SheetModel: Decodable {
        struct Song: Decodable {
            struct Box: Decodable {
                let row: Int
                let element: String
                let text: String
            }
            let textBoxes: [Box]
        }
        struct Asked: Decodable {
            struct Chord: Decodable {
                let at: Int
                let name: String
            }
            let ask: String
            let row: Int
            let element: String
            let chords: [Chord]?
        }
        struct Edit: Decodable {
            let song: String
            let asked: [Asked]
        }
        let songs: [String: Song]
        let edits: [Edit]
    }

    @Test("The chords Simple Presenter's sheet asks for, put on as it asks, are written as the rules say")
    func asTheSheetAsks() throws {
        let bench = try Workbench()
        let boxes = try boxesByWords(bench)
        let model = try JSONDecoder().decode(SheetModel.self, from: TestData.vector("sheet-model.json"))
        var tried = 0
        for edit in model.edits {
            for asked in edit.asked where asked.ask == "setChords" {
                // The sheet's songs are made up for its vectors. Where a box of one has
                // the words of a box in a test file, what it asks is put on that box.
                guard let made = model.songs[edit.song]?.textBoxes.first(where: { $0.row == asked.row && $0.element == asked.element }),
                      let (song, box) = boxes[Array(made.text.utf16)], let chords = asked.chords else { continue }
                tried += 1
                // (As the sheet hands them over: not in order, the new one where it falls.)
                let wanted = chords.map { Chord(at: $0.at, name: $0.name) }
                try song.setChords(slide: box.slide, element: box.element, wanted)
                let now = try message(at: song.url)
                let here = try #require(place(of: box.element, in: now))
                #expect(spell(entries(at: here, in: now)) == Chords.ranges(box.words, wanted).map { "\($0.name) \($0.start)-\($0.end)" })
                #expect(song.textBoxes.first { $0.element == box.element }?.chords == Chords.tidy(box.words, wanted))
            }
        }
        #expect(tried >= 10, "only \(tried) of the sheet's requests could be tried on a test file")
    }

    // ---- A line of chords alone

    @Test("The chords of a line of chords alone can be renamed; one more or one fewer is refused until M7")
    func aLineOfChordsAlone() throws {
        let bench = try Workbench()
        let song = try bench.open("made-by-simple-presenter/Plain Song.pro")
        // Plain Song ends with a slide that is one line of three stand-ins.
        let box = try #require(song.textBoxes.first { $0.words.lines.contains { Chords.isPlaceholders($0) } })
        let line = try #require(box.words.lines.firstIndex { Chords.isPlaceholders($0) })
        #expect(box.chords.count == 3)
        let original = try Data(contentsOf: song.url)

        try song.setChordsAlone(slide: box.slide, element: box.element, line: line, names: ["am", "F", "c/e"])
        let renamed = try #require(song.textBoxes.first { $0.element == box.element })
        #expect(renamed.chords.map(\.name) == ["Am", "F", "C/E"])
        #expect(renamed.chords.map(\.at) == box.chords.map(\.at))
        // The words are not touched: the box's RTF is byte for byte what it was.
        let before = try RVData_Presentation(serializedBytes: original)
        let after = try message(at: song.url)
        let here = try #require(place(of: box.element, in: after))
        #expect(after.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element].element.text.rtfData
                == before.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element].element.text.rtfData)

        let saved = try Data(contentsOf: song.url)
        #expect { try song.setChordsAlone(slide: box.slide, element: box.element, line: line, names: ["Am", "F"]) } throws: { ($0 as? Refusal)?.reason == .notBuiltYet }
        #expect { try song.setChordsAlone(slide: box.slide, element: box.element, line: line, names: ["Am", "F", "C", "G"]) } throws: { ($0 as? Refusal)?.reason == .notBuiltYet }
        #expect { try song.setChordsAlone(slide: box.slide, element: box.element, line: 7, names: ["Am"]) } throws: { ($0 as? Refusal)?.reason == .lineGone }
        #expect(try Data(contentsOf: song.url) == saved)

        try song.undo()
        #expect(try Data(contentsOf: song.url) == original)
    }
}
